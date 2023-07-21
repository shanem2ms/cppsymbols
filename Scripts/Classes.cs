using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using symlib;

namespace symlib.script
{    
	static class Classes
    {
        static CWriter cppwriter = new CWriter(@"c:\flash\src\exports.cpp");
        static CSNativeWriter csnativewriter = new CSNativeWriter(@"C:\flash\cslib\flashnet\NativeLib.cs");
        static CSApiWriter csapiwriter = new CSApiWriter(@"C:\flash\cslib\flashnet\Api.cs");

        static NS classTree = new NS();

        static List<WrappedObject> allClasses = new List<WrappedObject>();
        static Dictionary<string, WrappedObject>? classMap = null;
        static public Dictionary<string, WrappedObject> ClassMap {
    		get { if (classMap != null) return classMap;
				classMap = allClasses.GroupBy(t => t.name).ToDictionary(t => t.Key, t => t.First());
				return classMap;  }
			}

        static public void CollectWrappedTypes(Node pn, string ns)
        {
			foreach (Node n in pn.allChildren)
        	{
        		if ((n.Kind == CXCursorKind.ClassDecl ||
        			n.Kind == CXCursorKind.StructDecl) &&
        			n.CppType.Kind == CXTypeKind.Record)
        		{
        			string nm = ns + n.Token.Text;
        			allClasses.Add(new WrappedObject() { name = nm, enumobj = null });
        			CollectWrappedTypes(n, nm + "::");
        		}
        		if (n.Kind == CXCursorKind.EnumDecl &&
        			n.CppType.Kind == CXTypeKind.Enum)
        		{
        			Enum e = GetEnum(n);
        			string nm = ns + n.Token.Text;
        			allClasses.Add(new WrappedObject() { name = nm, enumobj = e });
        		}
        		if (n.Kind == CXCursorKind.Namespace)
        		{
        			bool donamespace = n.Token.Text.Length > 0;
        			string nm = ns + n.Token.Text;
        			CollectWrappedTypes(n, nm + "::");
        		}
        	}        
		}    	
		
		static Enum GetEnum(Node enumNode)
		{
			if (enumNode.Token.Text[0] == '(')
				return null;

			Enum e = new Enum() { name = enumNode.Token.Text };
			List<Node> enumValues = enumNode.FindChildren((n) => 
	        	{
	        		return n.Kind == CXCursorKind.EnumConstantDecl ?
	        			Node.FindChildResult.eTrue : Node.FindChildResult.eFalseNoTraverse;
	        	});
	        	
	        int value = 0;
			foreach (Node n in enumValues)
			{
				e.values.Add(new Tuple<string, int>(n.Token.Text, value++));
			}
			return e;							        	
		}

        public static void Wrap(Node pn, string ns)
        {
			csapiwriter.PushStaticClass(ns);
			ns = "";
			ProcessFunctions(pn);        
			AddClasses(pn, ns);
			csapiwriter.PopClass();
		}

        static void AddClasses(Node pn, string ns)
        {
        	foreach (Node n in pn.allChildren)
        	{
        		if ((n.Kind == CXCursorKind.ClassDecl ||
        			n.Kind == CXCursorKind.StructDecl) &&
        			n.CppType.Kind == CXTypeKind.Record)
        		{
        			string nm = ns + n.Token.Text;
        			if (ProcessClass(n))
        			{
	        			AddClasses(n, nm + ".");
	        			csapiwriter.PopClass();
        			}
        		}
        		else if (n.Kind == CXCursorKind.EnumDecl &&
        			n.CppType.Kind == CXTypeKind.Enum)
        		{
        			string nm = ns + n.Token.Text;
        			ProcessEnum(n);
        		}        		
        		else if (n.Kind == CXCursorKind.Namespace)
        		{
        			bool donamespace = n.Token.Text.Length > 0;
        			if (donamespace)
        				csapiwriter.PushNamespace(n.Token.Text);
        			string nm = ns + n.Token.Text;
        			AddClasses(n, nm + ".");
        			if (donamespace)
	        			csapiwriter.PopNamespace();
        		}
        	}
        }

		static void ProcessEnum(Node classNode)
        {        	
        	Enum e = GetEnum(classNode);
        	if (e == null)
        		return;
            csapiwriter.AddEnum(e);
        }
       
        static bool ProcessClass(Node classNode)
        {                	
       		if (classNode.Access == CXXAccessSpecifier.Private ||
       			classNode.Access == CXXAccessSpecifier.Protected)
       			return false;
       		if (classNode.Children.Count() == 0)
       			return false;
        	string filename = Api.Engine.SourceFiles[classNode.SourceFile - 1];     
        	if (filename.EndsWith(".cpp")) 
       			return false;      			
			List<NS> nodes = new List<NS>();
			NS.GetCanonicalNodes(classNode, nodes);
			NS nsclass = classTree.AddClass(nodes);
			
       		csapiwriter.PushClass(classNode);
       		
       		bool found = false; 
       		if (!classNode.IsAbstract)
       			found |= ProcessConstructors(classNode);
        	found |= ProcessMembers(classNode);        	

			return true;
		}

        static bool ProcessConstructors(Node classNode)
		{
        	long clsSrcFileIdx = classNode.SourceFile;
			List<Node> funcs = classNode.FindChildren((n) => 
	        	{
	        		return ((
	        				n.Kind == CXCursorKind.Constructor) &&
	        				n.Access == CXXAccessSpecifier.Public &&
	        				!n.IsDeleted &&
	        				n.SourceFile == clsSrcFileIdx) ?
	        			Node.FindChildResult.eTrue : Node.FindChildResult.eFalseNoTraverse;
	        	});
	        	
			string classname = Utils.GetCanonicalName(classNode);
			if (funcs.Count() == 0)
				return false;
        	
        	string filename = Api.Engine.SourceFiles[classNode.SourceFile - 1];
        	cppwriter.IncludeFiles.Add(filename);

			int ctorIdx = 0;
			foreach (Node func in funcs)
			{
	        	List<Node> pars = func.FindChildren((n) => 
	        	{
        		return (n.Kind == CXCursorKind.ParmDecl) ?
        			Node.FindChildResult.eTrue : Node.FindChildResult.eFalseTraverse;
	        	});
			
			
			    bool supported = true;
				
				List<Parameter> parms = pars.Select(p => new Parameter(p)).ToList();
				foreach (Parameter p in parms)
				{
					supported &= p.type.IsSupported;
				}
				
				if (supported)
				{
					Function f = new Function();
					f.classname = classname;
					f.parameters.AddRange(parms);
					f.returnType = null;
					f.idx = ctorIdx++;
					cppwriter.AddFunction(f);
					csnativewriter.AddFunction(f);
					csapiwriter.AddFunction(f);
				}
			}	        	
			
			return true;
		}

        static bool ProcessMembers(Node classNode)
		{
        	long clsSrcFileIdx = classNode.SourceFile;
			List<Node> funcs = classNode.FindChildren((n) => 
	        	{
	        		return ((n.Access == CXXAccessSpecifier.Public &&
	        				n.Kind == CXCursorKind.CXXMethod) &&
	        				n.SourceFile == clsSrcFileIdx) ?
	        			Node.FindChildResult.eTrue : Node.FindChildResult.eFalseNoTraverse;
	        	});
	        	
			string classname = Utils.GetCanonicalName(classNode);
			if (funcs.Count() == 0)
				return false;
        	
        	string filename = Api.Engine.SourceFiles[classNode.SourceFile - 1];
        	cppwriter.IncludeFiles.Add(filename);

			foreach (Node func in funcs)
			{
				func.SetEnabled(true, true);
								
				
	        	List<Node> pars = func.FindChildren((n) => 
	        	{
        		return (n.Kind == CXCursorKind.ParmDecl) ?
        			Node.FindChildResult.eTrue : Node.FindChildResult.eFalseTraverse;
	        	});
			
			
			    bool supported = true;

				Parameter returnparm = new Parameter(func);
				supported &= returnparm.type.IsSupported;

                List<Parameter> parms = pars.Select(p => new Parameter(p)).ToList();
                foreach (Parameter p in parms)
				{
					supported &= p.type.IsSupported;
				}
				
				if (supported)
				{
					Function f = new Function();
					f.classname = classname;
					f.funcname = func.Token.Text;
					f.cexportname = Utils.GetCFuncName(classname, func.Token.Text);
					f.isStatic = func.StorageClass == CX_StorageClass.Static;
                    f.parameters.AddRange(parms);
					f.returnType = returnparm.type;
					f.idx = -1;
					cppwriter.AddFunction(f);
					csnativewriter.AddFunction(f);
					csapiwriter.AddFunction(f);					
				}
				else
				{
					string unsupportedtypes = "";
                    foreach (Parameter p in parms)
                    {
                        if (!p.type.IsSupported)
							unsupportedtypes += " " + p.type.ToString();
					}
				
					//Api.WriteLine($"not supported: {classname}::{func.Token.Text}" + unsupportedtypes);
				}
			}	        	
			
			return true;
		}


        static bool ProcessFunctions(Node namespaceNode)
		{
			List<Node> funcs = namespaceNode.FindChildren((n) => 
	        	{
	        		return (n.Kind == CXCursorKind.FunctionDecl) ?
	        			Node.FindChildResult.eTrue : Node.FindChildResult.eFalseNoTraverse;
	        	});
	        	
			string nsname = Utils.GetCanonicalName(namespaceNode);
			List<Node> validFuncs = new List<Node>();
			foreach (Node func in funcs)
			{
				string filename = Api.Engine.SourceFiles[func.SourceFile - 1];
	        	if (filename.EndsWith(".cpp")) 
	       			continue;
	       		validFuncs.Add(func);
			}
			
			if (validFuncs.Count() == 0)
				return false;

			foreach (Node func in validFuncs)
			{
				string filename = Api.Engine.SourceFiles[func.SourceFile - 1];
	        	cppwriter.IncludeFiles.Add(filename);
				func.SetEnabled(true, true);
								
	        	List<Node> pars = func.FindChildren((n) => 
	        	{
        		return (n.Kind == CXCursorKind.ParmDecl) ?
        			Node.FindChildResult.eTrue : Node.FindChildResult.eFalseTraverse;
	        	});
			
			
			    bool supported = true;

				Parameter returnparm = new Parameter(func);
				supported &= returnparm.type.IsSupported;

                List<Parameter> parms = pars.Select(p => new Parameter(p)).ToList();
                foreach (Parameter p in parms)
                {
                    supported &= p.type.IsSupported;
                }

                if (supported)
				{
					Function f = new Function();
					f.classname = nsname;
					f.funcname = func.Token.Text;
					f.cexportname = Utils.GetCFuncName(nsname, func.Token.Text);
					f.isStatic = true;
                    f.parameters.AddRange(parms);
					f.returnType = returnparm.type;
					f.idx = -1;
					cppwriter.AddFunction(f);
					csnativewriter.AddFunction(f);
					csapiwriter.AddFunction(f);
				}
			}
			return true;
		}		
		
		public static void Write()
		{
            cppwriter.AddWrappedTypes(Parameter.wrappedTypes);
            cppwriter.Write();
			csnativewriter.Write();
			csapiwriter.Write();
		}

    }
    
    class NS : IEqualityComparer<NS>
    {
    	public string name;
    	public HashSet<NS> children = new HashSet<NS>();
    	public List<Function> functions = new List<Function>();
    	public Node node;
    	public bool IsClass;
    	public NS parent;

        public bool Equals(NS? x, NS? y)
        {
			return x?.name == y?.name && x?.IsClass == y?.IsClass;
        }

        public int GetHashCode([DisallowNull] NS obj)
        {
            return name.GetHashCode() + IsClass.GetHashCode();
        }
        
        public NS AddClass(List<NS> nslist)
        {
			if (nslist.Count() == 0)
				return this;

			NS curNS = nslist.First();

            NS childNode;
			if (!children.TryGetValue(nslist.First(), out childNode))
			{
				childNode = new NS()
				{ name = curNS.name, parent = this, IsClass = curNS.IsClass };
				children.Add(childNode);
			}
            nslist.RemoveAt(0);
            return childNode.AddClass(nslist);
        }

        public static void GetCanonicalNodes(Node n, List<NS> nodes)
		{
			if (n.Parent != null)
			{
				GetCanonicalNodes(n.Parent, nodes);
			}

			if (n.Kind == CXCursorKind.ClassDecl)
				nodes.Add(new NS() { name = n?.Token?.Text ?? "", IsClass = true });
			else if (n.Kind == CXCursorKind.Namespace)
                nodes.Add(new NS() { name = n?.Token?.Text ?? "", IsClass = false });
        }

    }


	class WrappedObject
	{
		public string name;
		public Enum enumobj;
	}
	
    class Function
    {
    	public List<Parameter> parameters = new List<Parameter>();
    	public EType returnType;
    	public string classname;
    	public string funcname;
    	public string cexportname;
    	public string cppname;
    	public int idx;
    	public bool isStatic;
    }

	class Enum
	{
		public string name;
		public List<Tuple<string, int>> values = new List<Tuple<string, int>>();
	}
}
