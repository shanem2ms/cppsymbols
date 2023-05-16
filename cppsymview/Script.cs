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
        public void Run()
        {                    	
    		var engine = Api.Engine;
    		
    		long idx = 0;
    		Dictionary<long,string> myFiles = new Dictionary<long, string>();
    		foreach (var file in engine.SourceFiles)
    		{
    			string fp = Path.GetFullPath(file);
    			fp = fp.ToLower();
    			if (fp.StartsWith(@"d:\vq\flash\src") &&
    				fp.Contains("application") &&    			
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
    		StringBuilder sb = new StringBuilder();
    		foreach (var node in engine.Nodes)
    		{
    			if (!myFiles.ContainsKey(node.SourceFile - 1))
    				continue;
    			if (node.Kind == CXCursorKind.CXXMethod &&
    				node.TypeKind == CXTypeKind.FunctionProto)
    			{   
    				node.SetEnabled(true, true);
    				sb.Append($"t: {node.Token.Text} {engine.SourceFiles[node.SourceFile-1]}\n");
    				//Api.WriteLine();
    			}
    		}
    		Api.WriteLine(sb.ToString());
    		Api.Engine.RefreshNodeTree();
        }
    }
}