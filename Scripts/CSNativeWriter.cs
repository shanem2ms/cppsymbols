using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using static symlib.script.EType;
using System.Runtime.Intrinsics.X86;

namespace symlib.script
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
			string retarg = isConstructor ? $"IntPtr" : GetCSNativeType(f.returnType);

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
				if (varname == "" || varname == "params")
					varname = $"tmp{tmpvaridx++}";
				funcline += GetCSNativeType(tp.type) + " " + varname;
			}
	        funcline += ");";
	        fileLines.Add(funcline);
		}
        public string GetCSNativeType(EType t)
        {
            CppType baseType = EType.GetBaseType(t.mainType);
            if (t.category == Category.String ||
                t.IsPtr)
                return "IntPtr";
            else if (nativeTypes.ContainsKey(baseType.Kind))
            {
                return nativeTypes[baseType.Kind];
            }
            else if (baseType.Kind == CXTypeKind.Void)
            {
                return "void";
            }
            else if (baseType.Kind == CXTypeKind.Enum)
                return "int";
            else
            {
                return "unk";
            }
        }
        public void Write()
		{
			fileLines.Add(footer);
			File.WriteAllLines(outfile, fileLines);
		}
	}
}