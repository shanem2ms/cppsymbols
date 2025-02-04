using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace symlib.script
{
    public class Script
    {
        Dictionary<CXTypeKind, int> allParams = new Dictionary<CXTypeKind, int>();
		Dictionary<long,string> myFiles = new Dictionary<long, string>();
        bool verbose = false;
        public void Run()
        {                    	
    		var engine = Api.Engine;
    		
    		long idx = 0;
    		
    		foreach (var file in engine.SourceFiles)
    		{
    			string fp = Path.GetFullPath(file);
    			fp = fp.ToLower();
    			//if (fp.StartsWith(@"c:\flash\src") &&
    			//	!fp.StartsWith(@"c:\flash\src\engine\thirdparty\imgui"))
    			{
	    			myFiles.Add(idx, file);
	    		}
	    		idx++;
    		}    		
    			
			foreach (var node in engine.Nodes)
    		{
    			if (node.Kind == CXCursorKind.CXXMethod)
    				Api.WriteLine(node.Token.Text);
    			node.enabled = true;
			}
			

        }
        
    
        void LogTypes()
        {
    		var plist = 
    			allParams.Select(kv => new Tuple<CXTypeKind, int>(kv.Key, kv.Value)).ToList();
    		plist.Sort((a, b) => b.Item2 - a.Item2);
    		foreach (var kv in plist)
    		{
    			Api.WriteLine($"{kv.Item1}, {kv.Item2}");
    		}
    	}
	}
}