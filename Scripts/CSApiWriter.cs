using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using static symlib.script.EType;
using System.Runtime.Intrinsics.X86;

namespace symlib.script
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
				funcline += GetCSApiType(tp.type) + " " + varname;
				callline += GetCSApiCall(tp.type, varname, out _, out _);				
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
				funcline = $"public {staticstr} {GetCSApiType(f.returnType)} this[";
			else
	        	funcline = $"public {staticstr} {GetCSApiType(f.returnType)} {f.funcname}(";

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
				
				if (varname == "" || varname == "params")
					varname = $"tmp{tmpvaridx++}";
				funcline += GetCSApiType(tp.type) + " " + varname;
				callline += GetCSApiCall(tp.type, varname, out _, out _);
			}
			if (issquarebracket)
				funcline += "] { get {";
			else
	        	funcline += ") {";	        
	        fileLines.Add(funcline);
	        
	        fileLines.AddRange(GetCsReturnString(f.returnType, $"NativeLib.{f.cppname}({callline})"));
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

        public string GetCSApiType(EType t)
        {
            CppType baseType = GetBaseType(t.mainType);
            if (t.category == Category.String)
                return "string";
            else if (t.category == Category.WrappedObject)
            {
                return t.cstype;
            }
            else if (t.IsPtr)
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

        public List<string> GetCsReturnString(EType t, string callstring)
        {
            List<string> outlines = new List<string>();
            if (t.category == Category.Void)
            {
                outlines.Add($"    {callstring};");
            }
            else if (t.category == Category.String)
            {
                outlines.Add($"    IntPtr strtmp = {callstring};");
                outlines.Add($"    return Marshal.PtrToStringAnsi(strtmp);");
            }
            else if (t.category == Category.WrappedObject)
            {
                outlines.Add($"    return new {t.cstype}({callstring});");
            }
            else
            {
                outlines.Add($"    return {callstring};");
            }
            return outlines;
        }
        public string GetCSApiCall(EType t, string varname, out string header, out string footer)
        {
            if (t.category == Category.String)
            {
                header = String.Empty;
                footer = String.Empty;
                return $"Marshal.StringToHGlobalAnsi({varname})";
            }
            else
            {
                header = String.Empty;
                footer = String.Empty;
                return varname + (t.IsWrappedObject ? ".pthis" : "");
            }
        }


        public void Write()
		{
			fileLines.Add(footer);
			File.WriteAllLines(outfile, fileLines);
		}		
	}
}