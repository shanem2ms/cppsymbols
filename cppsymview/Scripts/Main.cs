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
        Dictionary<CXTypeKind, int> allParams = new Dictionary<CXTypeKind, int>();
		Dictionary<long,string> myFiles = new Dictionary<long, string>();
				
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
    		Node samnode = engine.TopNodes.FirstOrDefault(tn => 
    			tn.Kind == CXCursorKind.Namespace && tn.Token?.Text == "sam");

    		Node bgfxnode = engine.TopNodes.FirstOrDefault(tn => 
    			tn.Kind == CXCursorKind.Namespace && tn.Token?.Text == "bgfx");
    			
			foreach (var node in engine.Nodes)
    		{
    			node.enabled = true;
			}
			

            Classes.CollectWrappedTypes(bgfxnode, "bgfx::");
            Classes.CollectWrappedTypes(samnode, "sam::");

            Classes.Wrap(bgfxnode, "bgfx");
            Classes.Wrap(samnode, "sam");


            //Api.Engine.RefreshNodeTree();    		    	
            //LogTypes();

            Classes.Write();
            foreach(var kv in Classes.ClassMap)
            {
            	//Api.WriteLine(kv.Key);
            }
            
            Api.WriteLine("Can't wrap the following:");
    		foreach (string utype in EType.utypes)
    		{
    			Api.WriteLine("    " + utype);
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