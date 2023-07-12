using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace cppsymview.script
{
	class CSNativeWriter
	{	
		string outfile;
		List<string> fileLines = new List<string>();

			string header = @"using System;
using System.Runtime.InteropServices;

namespace flashnet
{
    static class NativeLib
    { 
    const string flashlibstr = @""flashlib.dll"";";

			string footer = @"        
    }
}";
		
		public CSNativeWriter(string _outfile)
		{
			outfile = _outfile;
			fileLines.Add(header);
		}
		
		public void AddFunction(Function f)
		{
			bool isConstructor = f.funcname == null;
			bool hasThisArg = !isConstructor && !f.isStatic;

			fileLines.Add(@"[DllImport(flashlibstr)]");
			string retarg = isConstructor ? $"IntPtr" : f.returnType.GetCSNativeType();

	        string funcline = $"public static extern {retarg} {f.cppname}(";
	        funcline += hasThisArg ? "IntPtr pthis" : "";
			bool first = true;
			int tmpvaridx = 0;
			

	        foreach (var tp in f.parameters)
			{
				if (!first || hasThisArg)
					funcline += ", ";
				first = false;
				string varname = tp.param.Token.Text;
				if (varname == "")
					varname = $"tmp{tmpvaridx++}";
				funcline += tp.type.GetCSNativeType() + " " + varname;
			}
	        funcline += ");";
	        fileLines.Add(funcline);
		}
		
		public void Write()
		{
			fileLines.Add(footer);
			File.WriteAllLines(outfile, fileLines);
		}
	}
}