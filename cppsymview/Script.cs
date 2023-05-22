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
        HashSet<CXTypeKind> paramTypes = new HashSet<CXTypeKind>();
		Dictionary<long,string> myFiles = new Dictionary<long, string>();
		
        public void Run()
        {                    	
    		var engine = Api.Engine;
    		
    		long idx = 0;
    		
    		foreach (var file in engine.SourceFiles)
    		{
    			string fp = Path.GetFullPath(file);
    			fp = fp.ToLower();
    			if (fp.StartsWith(@"d:\vq\flash\src") &&
    				!fp.StartsWith(@"d:\vq\flash\src\engine\thirdparty\imgui"))
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
    				node.TypeKind == CXTypeKind.Record)
    			{   
    				GetAllFunctions(node);
    			}
    		}
    		foreach (var tk in paramTypes)
    		{
    			sb.Append($"{tk}\n");
    		}
    		Api.WriteLine(sb.ToString());
    		Api.Engine.RefreshNodeTree();
        }
        
        void LogNodeLocation(Node n)
        {
        	string filename = Api.Engine.SourceFiles[n.SourceFile - 1];
			sb.Append($"[{n.Line}, {n.Column}]");	
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

	        	List<Node> pars = func.FindChildren((n) => 
	        	{
        		return (n.Kind == CXCursorKind.ParmDecl) ?
        			Node.FindChildResult.eTrue : Node.FindChildResult.eFalseTraverse;
	        	});
			
				sb.Append($"   {classname}::{func.Token.Text}(");
				foreach (Node p in pars)				
				{
					paramTypes.Add(p.TypeKind);
					if (p.TypeKind == CXTypeKind.LValueReference)
						sb.Append($"{p.TypeToken.Text} {p.Token.Text}, ");
				}
				sb.Append(") ");
				LogNodeLocation(func);
				sb.Append("\n");
			}
		}
    }
}