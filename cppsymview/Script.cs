using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Text;
using System.Linq;

namespace cppsymview.script
{
    public class Script
    {
		StringBuilder sb = new StringBuilder();    
        Dictionary<CXTypeKind, int> allParams = new Dictionary<CXTypeKind, int>();
		Dictionary<long,string> myFiles = new Dictionary<long, string>();
		HashSet<CXTypeKind> primitives = new HashSet<CXTypeKind>() {
			CXTypeKind.Float,
			CXTypeKind.Int,
			CXTypeKind.UInt,
			CXTypeKind.Bool,
			CXTypeKind.Char_S,
			CXTypeKind.Double,
			CXTypeKind.ULongLong,
			CXTypeKind.UChar,
			CXTypeKind.Enum,
			CXTypeKind.LongLong,
			CXTypeKind.Long,
			CXTypeKind.ULong,
			CXTypeKind.Record };
		
        public void Run()
        {                    	
    		var engine = Api.Engine;
    		
    		long idx = 0;
    		
    		foreach (var file in engine.SourceFiles)
    		{
    			string fp = Path.GetFullPath(file);
    			fp = fp.ToLower();
    			if (fp.StartsWith(@"c:\flash\src") &&
    				!fp.StartsWith(@"c:\flash\src\engine\thirdparty\imgui"))
    			{
	    			myFiles.Add(idx, file);
	    		}
	    		idx++;
    		}    		
    		foreach (var node in engine.Nodes)
    		{
    			node.enabled = false;
			}
    		foreach (var node in engine.Nodes)
    		{
    			if (!myFiles.ContainsKey(node.SourceFile - 1))
    				continue;
    			
    			if (node.Kind == CXCursorKind.ClassDecl &&
    				node.CppType?.Kind == CXTypeKind.Record)
    			{   
    				GetAllFunctions(node);
    			}
    		}
    		Api.WriteLine(sb.ToString());    		
    		Api.Engine.RefreshNodeTree();    		    	
    		//LogTypes();
        }
        
        void LogTypes()
        {
    		var plist = 
    			allParams.Select(kv => new Tuple<CXTypeKind, int>(kv.Key, kv.Value)).ToList();
    		plist.Sort((a, b) => b.Item2 - a.Item2);
    		sb.Clear();
    		foreach (var kv in plist)
    		{
    			sb.Append($"{kv.Item1}, {kv.Item2}\n");
    		}
    		Api.WriteLine(sb.ToString());
    	}
        
        void LogNodeLocation(Node n)
        {
        	string filename = Api.Engine.SourceFiles[n.SourceFile - 1];
			sb.Append($"[{n.Line}, {n.Column}]");	
        }
        
        CppType GetBaseType(CppType t)
        {
        	if (t.Next != null)
        		return GetBaseType(t.Next);
        	else
        		return t;
        }
        
        bool IsPrimitiveType(CppType t)
        {
        	return primitives.Contains(GetBaseType(t).Kind);
        }
        
        
        void GetAllFunctions(Node classNode)
        {        
        	long clsSrcFileIdx = classNode.SourceFile;
			List<Node> funcs = classNode.FindChildren((n) => 
        	{
        		return (n.Kind == CXCursorKind.CXXMethod &&
        				n.SourceFile == clsSrcFileIdx) ?
        			Node.FindChildResult.eTrue : Node.FindChildResult.eFalseTraverse;
        	});
			string classname = classNode.Token.Text; 
			if (funcs.Count() > 0)
			{
	        	string filename = Api.Engine.SourceFiles[classNode.SourceFile - 1];
				sb.Append($"{filename}\n");	
			}
			foreach (Node func in funcs)
			{
				func.SetEnabled(true, true);
				
				Node returnNode = func.FindChildren((n) => 
	        	{
        			return (n.Kind == CXCursorKind.TypeRef) ?
        				Node.FindChildResult.eTrue : Node.FindChildResult.eFalseTraverse;
	        	}).FirstOrDefault();
	        	

	        	List<Node> pars = func.FindChildren((n) => 
	        	{
        		return (n.Kind == CXCursorKind.ParmDecl) ?
        			Node.FindChildResult.eTrue : Node.FindChildResult.eFalseTraverse;
	        	});
			
			
			    bool supported = true;

				if (returnNode != null && returnNode.CppType != null)
				{
					supported &= IsPrimitiveType(returnNode.CppType);
				}

				foreach (Node p in pars)				
				{
					supported &= (p.CppType != null) && IsPrimitiveType(p.CppType);
				}
				
				if (supported)
				{
					string returntype = returnNode?.CppType?.Token.Text;
					if (returntype == null) returntype = "void";
					sb.Append($"   {returntype} {classname}::{func.Token.Text}(");
					foreach (Node p in pars)				
					{
						var paramName = p.CppType.Token.Text;
						sb.Append($"{paramName} {p.Token.Text}, ");
					}
					sb.Append(") ");
					LogNodeLocation(func);
					sb.Append("\n");
					
				}
			}
		}
    }
}