using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cppsymview.script
{    
	class Classes
    {
    	CWriter cppwriter = new CWriter(@"c:\flash\src\exports.cpp");
    	CSNativeWriter csnativewriter = new CSNativeWriter(@"C:\flash\cslib\flashnet\NativeLib.cs");
    	CSApiWriter csapiwriter = new CSApiWriter(@"C:\flash\cslib\flashnet\Api.cs");
    	
    	NS classTree = new NS();

        public void AddAllClasses(Node pn, string ns)
        {
			ProcessFunctions(pn);        
			AddClasses(pn, ns);
		}
        
        void AddClasses(Node pn, string ns)
        {
        	foreach (Node n in pn.allChildren)
        	{
        		if (n.Kind == CXCursorKind.ClassDecl &&
        			n.CppType.Kind == CXTypeKind.Record)
        		{
        			string nm = ns + n.Token.Text;
        			if (ProcessClass(n))
        			{
	        			AddClasses(n, nm + ".");
	        			csapiwriter.PopClass();
        			}
        		}
        		if (n.Kind == CXCursorKind.Namespace)
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
        
		public bool ProcessClass(Node classNode)
        {                	
       		if (classNode.Access == CXXAccessSpecifier.Private ||
       			classNode.Access == CXXAccessSpecifier.Protected ||
       			classNode.IsAbstract)
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
       		bool found = ProcessConstructors(classNode);
        	found |= ProcessMembers(classNode);        	

			return true;
		}	
			
		bool ProcessConstructors(Node classNode)
		{
        	long clsSrcFileIdx = classNode.SourceFile;
			List<Node> funcs = classNode.FindChildren((n) => 
	        	{
	        		return ((
	        				n.Kind == CXCursorKind.Constructor) &&
	        				n.Access == CXXAccessSpecifier.Public &&
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
				
				List<EType> etypes = pars.Select(p => new EType(p.CppType)).ToList();
				foreach (EType t in etypes)				
				{
					supported &= t.IsSupported;
				}
				
				if (supported)
				{
					Function f = new Function();
					f.classname = classname;
					foreach (var tp in pars.Zip(etypes, Tuple.Create))				
					{
						f.parameters.Add(new Parameter() { param = tp.Item1, type = tp.Item2 });
					}				
					f.returnType = null;
					f.idx = ctorIdx++;
					cppwriter.AddFunction(f);
					csnativewriter.AddFunction(f);
					csapiwriter.AddFunction(f);
				}
			}	        	
			
			return true;
		}
 
 		bool ProcessMembers(Node classNode)
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

				EType returnType = new EType(func.CppType.Next);
				supported &= returnType.IsSupported;
				
				List<EType> etypes = pars.Select(p => new EType(p.CppType)).ToList();
				foreach (EType t in etypes)				
				{
					supported &= t.IsSupported;
				}
				
				if (supported)
				{
					Function f = new Function();
					f.classname = classname;
					f.funcname = func.Token.Text;
					f.cexportname = Utils.GetCFuncName(classname, func.Token.Text);
					f.isStatic = func.StorageClass == CX_StorageClass.Static;
					
					foreach (var tp in pars.Zip(etypes, Tuple.Create))				
					{
						f.parameters.Add(new Parameter() { param = tp.Item1, type = tp.Item2 });
					}				
					f.returnType = returnType;
					f.idx = -1;
					cppwriter.AddFunction(f);
					csnativewriter.AddFunction(f);
					csapiwriter.AddFunction(f);					
				}
			}	        	
			
			return true;
		}		
						
						
 		bool ProcessFunctions(Node namespaceNode)
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

			csapiwriter.PushStaticClass(nsname);
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

				EType returnType = new EType(func.CppType.Next);
				supported &= returnType.IsSupported;
				
				List<EType> etypes = pars.Select(p => new EType(p.CppType)).ToList();
				foreach (EType t in etypes)				
				{
					supported &= t.IsSupported;
				}
				
				if (supported)
				{
					Function f = new Function();
					f.classname = nsname;
					f.funcname = func.Token.Text;
					f.cexportname = Utils.GetCFuncName(nsname, func.Token.Text);
					f.isStatic = true;
					
					foreach (var tp in pars.Zip(etypes, Tuple.Create))				
					{
						f.parameters.Add(new Parameter() { param = tp.Item1, type = tp.Item2 });
					}				
					f.returnType = returnType;
					f.idx = -1;
					cppwriter.AddFunction(f);
					csnativewriter.AddFunction(f);
					csapiwriter.AddFunction(f);					
				}
			}
			csapiwriter.PopClass();
			return true;
		}		
		
		public void Write()
		{
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


    class Parameter
    {
    	public EType type;
    	public Node param;
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
}
