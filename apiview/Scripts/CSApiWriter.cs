﻿using System;
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
using System.Collections.Generic;

namespace flashnet
{
";

		string footer = @"}";
		public CSApiWriter(string _outfile)
		{
			outfile = _outfile;
			fileLines.Add(header);
			fileLines.Add(iliststr);
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
            List<string> headerLines = new List<string>();
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
				callline += GetCSApiCall(tp.type, varname, out header, out _);
                if (header != String.Empty)
                    headerLines.Add(header);
            }
            funcline += ") {";			
			fileLines.Add(funcline);
            fileLines.AddRange(headerLines);
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
			List<string> headerLines = new List<string>();
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
				callline += GetCSApiCall(tp.type, varname, out header, out _);
				if (header != String.Empty)
					headerLines.Add(header);
            }
			if (issquarebracket)
				funcline += "] { get {";
			else
	        	funcline += ") {";	        
	        fileLines.Add(funcline);
			fileLines.AddRange(headerLines);
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
			else if (t.category == Category.Vector)
			{
				string ret = $"IList<{GetCSApiType(t.subtype)}>";
				return ret;
			}
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

		// public IList<string> GetShaderNames() {
		// return new List<string>(NativeLib.sam_EngineGetShaderNames(pthis), (IntPtr ptr) =>
		// { return ""; });
		// }

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
			else if (t.category == Category.Vector)
			{
				string instType = $"List<{GetCSApiType(t.subtype)}>";
				string retstr;

                if (t.subtype.category == Category.Primitive)
				{
					retstr = $"({GetCSApiType(t.subtype)})ptr";
				}
				else
				{
                    List<string> retstrs;
                    retstrs = GetCsReturnString(t.subtype, "ptr");
                    retstr = "{" + string.Join(' ', retstrs) + "}";

            }
                outlines.Add($"    return new {instType}({callstring}, (IntPtr ptr) => {retstr});");
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
			else if (t.category == Category.Vector)
			{
				header = $"var l{varname} = {varname} as List<byte>; if (l{varname}== null) throw new Exception();";
                footer = String.Empty;
                return $"l{varname}.ptr";
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


        const string iliststr = @"    class List<T> : IList<T>, IDisposable
    {
        public delegate T CreateDelegate(IntPtr ptr);
        public IntPtr ptr;
        CreateDelegate createDel;
        public List(IntPtr _ptr, CreateDelegate del)
        { ptr = _ptr; createDel = del; }
        public T this[int index] { get => createDel(NativeLib.IVec_GetItem(ptr, (ulong)index)); set => throw new NotImplementedException(); }
        public int Count => (int)NativeLib.IVec_Size(ptr);
        public bool IsReadOnly => true;
        public void Add(T item)
        {
            throw new NotImplementedException();
        }
        public void Clear()
        {
            throw new NotImplementedException();
        }
        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(NativeLib.IVec_GetEnumerator(ptr), createDel);
        }
        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }
        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }
        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(NativeLib.IVec_GetEnumerator(ptr), createDel);
        }
        public void Dispose()
        {
            NativeLib.IVec_Free(ptr);
        }
        public class Enumerator : IEnumerator<T>
        {
            IntPtr ptr;
            CreateDelegate createDel;
            public Enumerator(IntPtr _ptr, CreateDelegate del)
            { ptr = _ptr;  createDel = del; }
            public T Current => createDel(NativeLib.IEnumerator_Current(ptr));
            object System.Collections.IEnumerator.Current => createDel(NativeLib.IEnumerator_Current(ptr));
            public void Dispose()
            {
				NativeLib.IEnumerator_Free(ptr);
            }
            public bool MoveNext()
            {
                return NativeLib.IEnumerator_MoveNext(ptr);
            }
            public void Reset()
            {
                NativeLib.IEnumerator_Reset(ptr);
            }
        }
    }";
    }
}