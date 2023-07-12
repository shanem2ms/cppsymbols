using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace cppsymview.script
{
	class CSApiWriter
	{
		string outfile;
		List<string> fileLines = new List<string>();
					string header = @"using System;
using System.Runtime.InteropServices;

namespace flashnet
{
";

		string footer = @"}";
		public CSApiWriter(string _outfile)
		{
			outfile = _outfile;
			fileLines.Add(header);
		}

		public void PushNamespace(string ns)
		{
			fileLines.Add($"public static class {ns}"); 
			fileLines.Add("{");
		}
		
		public void PopNamespace()
		{
			fileLines.Add("}");
		}
		
		string curclass = "";
		public void PushClass(Node classNode)
		{	
			curclass = classNode.Token.Text;
			fileLines.Add($"public class {classNode.Token.Text}");
			fileLines.Add("{");
			fileLines.Add("public IntPtr pthis;");
			AddWrapperConstructor();
		}
		
		public void PushStaticClass(string classname)
		{
			fileLines.Add($"public static class {classname}");
			fileLines.Add("{");
		}
		
		public void PopClass()
		{
			fileLines.Add("}");
		}
		
		void AddWrapperConstructor()
		{
			fileLines.Add($"public {curclass}(IntPtr _pthis) {{");
			fileLines.Add("pthis = _pthis; }");
		}
		
		void AddConstructor(Function f)
		{
			string funcline = $"public {curclass}(";
			bool first = true;
			int tmpvaridx = 0;
			string callline ="";
	        foreach (var tp in f.parameters)
			{
				if (!first)
				{
					funcline += ", ";
					callline += ", ";
				}
				first = false;
				string varname = tp.param.Token.Text;
				
				if (varname == "")
					varname = $"tmp{tmpvaridx++}";
				funcline += tp.type.GetCSApiType() + " " + varname;
				callline += tp.type.GetCSApiCall(varname, out _, out _);				
			}			
			funcline += ") {";			
			fileLines.Add(funcline);
			fileLines.Add($"pthis = NativeLib.{f.cppname}({callline});");
			fileLines.Add("}");
		}
		
		public void AddFunction(Function f)
		{
			bool isConstructor = f.funcname == null;
			
			if (isConstructor)
			{
				AddConstructor(f);
				return;
			}
				
			bool hasThisArg = !isConstructor && !f.isStatic;
	
			bool issquarebracket = f.funcname == "operator[]";
			bool comparison = 
				f.funcname == "operator<" ||
				f.funcname == "operator>" ||
				f.funcname == "operator==" ||
				f.funcname == "operator!=" ||
				f.funcname == "operator>=" ||
				f.funcname == "operator<=";
				
			if (comparison)
				return;
			string staticstr = f.isStatic ? "static" : "";
					
			string funcline = "";
			if (issquarebracket)
				funcline = $"public {staticstr} {f.returnType.GetCSApiType()} this[";
			else
	        	funcline = $"public {staticstr} {f.returnType.GetCSApiType()} {f.funcname}(";

			bool first = true;
			int tmpvaridx = 0;
			string callline = hasThisArg ? "pthis" : "";

	        foreach (var tp in f.parameters)
			{
				if (!first)
					funcline += ", ";
				if (!first || hasThisArg)
					callline += ", ";
				first = false;
				string varname = tp.param.Token.Text;
				
				if (varname == "")
					varname = $"tmp{tmpvaridx++}";
				funcline += tp.type.GetCSApiType() + " " + varname;
				callline += tp.type.GetCSApiCall(varname, out _, out _);
			}
			if (issquarebracket)
				funcline += "] { get {";
			else
	        	funcline += ") {";	        
	        fileLines.Add(funcline);
	        
	        fileLines.AddRange(f.returnType.GetCsReturnString($"NativeLib.{f.cppname}({callline})"));
	        if (issquarebracket)
	        	fileLines.Add("}}");
	        else
	        	fileLines.Add("}");
		}
		
		public void AddEnum(Enum e)
		{
			fileLines.Add($"public enum {e.name} {{");
			bool first = true;
			foreach (var v in e.values)
			{
				fileLines.Add((first ? "" : ",") + $"{v.Item1} = {v.Item2}" );
				first = false;
			}
			fileLines.Add("}");
		}
		
		public void Write()
		{
			fileLines.Add(footer);
			File.WriteAllLines(outfile, fileLines);
		}		
	}
}