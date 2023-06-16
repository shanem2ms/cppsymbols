using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Text;
using System.Linq;

namespace cppsymview.script
{
	class CWriter
	{		
		string outfile;
		List<string> ctorLines = new List<string>();
		public HashSet<string> IncludeFiles = new HashSet<string>();
		public CWriter(string _outfile)
		{
			outfile = _outfile;
			IncludeFiles.Add("StdIncludes.h");
		}
		
		public void Write()
		{
			List<string> lines = new List<string>();
			string dirname = Path.GetDirectoryName(outfile);
			dirname = dirname.ToLower();
			Api.WriteLine(dirname);
			foreach (var ifile in IncludeFiles)
			{
				string lf = ifile.ToLower();
				if (lf.StartsWith(dirname))
					lf = ifile.Substring(dirname.Length + 1);
				else
					lf = ifile;
				lines.Add($"#include \"{lf}\"");
			}
			
			lines.Add("#define CAPI extern \"C\" __declspec(dllexport)");
			lines.AddRange(ctorLines);
			File.WriteAllLines(outfile, lines);
		}	
        		
				 	
 		public void AddFunction(Function f)
 		{
			string cclass = f.classname.Replace("::", "_");
			string ctornum = f.idx >= 0 ? f.idx.ToString() : "";
			bool isConstructor = f.funcname == null;
			f.cppname = isConstructor ? $"{cclass}_Ctor{ctornum}" : $"{cclass}_{f.cexportname}";
			string retarg = isConstructor ? $"{f.classname} *" : f.returnType.Name;
			bool hasThisArg = !isConstructor && !f.isStatic;
			string thisarg = hasThisArg ? $"{f.classname} *pthis" : "";
			string line = $"CAPI {retarg} {f.cppname}({thisarg}";
			bool first = true;
			string callline = "";
			foreach (var tp in f.parameters)				
			{
				if (!first || hasThisArg)
					line += ", ";
				if (!first)
					callline += ", ";
				string parmname = tp.param.Token.Text;
				line += tp.type.DeclarationStr(ref parmname);
				callline += parmname;
				first = false;
			}
			if (isConstructor) {
				line += ") { return new ";
				line += f.classname + "(" + callline;
				line += "); }";
				ctorLines.Add(line); 		
			}
			else if (f.isStatic)
			{
				ctorLines.Add(line + ") {");
				ctorLines.AddRange(f.returnType.GetReturnString(
					f.classname + "::" + f.funcname + "(" + callline + ")"));
				ctorLines.Add(" }");
			}
			else
			{
				ctorLines.Add(line + ") {");
				ctorLines.AddRange(f.returnType.GetReturnString(
					"pthis->" + f.funcname + "(" + callline + ")"));
				ctorLines.Add(" }");
			}
 		}
 		
 		
 		string LogNodeLocation(Node n)
        {
			return $"[{n.Line}, {n.Column}]";	
        
        }		
	}
}