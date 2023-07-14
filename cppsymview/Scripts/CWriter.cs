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
			foreach (var ifile in IncludeFiles)
			{
				string lf = ifile.ToLower();
				if (lf.StartsWith(dirname))
					lf = ifile.Substring(dirname.Length + 1);
				else
					lf = ifile;
				lines.Add($"#include \"{lf}\"");
			}
			lines.Add(cptrcls);
			lines.Add("#define CAPI extern \"C\" __declspec(dllexport)");
			lines.AddRange(ctorLines);
			File.WriteAllLines(outfile, lines);
		}	
        		
				 	
 		public void AddFunction(Function f)
 		{
			string cclass = f.classname.Replace("::", "_");
			string ctornum = f.idx >= 0 ? f.idx.ToString() : "";
			bool isConstructor = f.funcname == null;
			f.cppname = isConstructor ? $"{cclass}_Ctor{ctornum}" : $"{cclass}{f.cexportname}";
			string retarg = isConstructor ? $"CPtr<{f.classname}> *" : f.returnType.CPtrType;
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
				line += tp.type.GetCppDeclarationStr(ref parmname);
				callline += tp.type.GetCppCall(parmname);
				first = false;
			}
			if (isConstructor) {
				line += ") {";				
				line += $"return CPtr<{f.classname}>::Make(new {f.classname}({callline}));";
				line += "}";
				ctorLines.Add(line); 		
			}
			else if (f.isStatic)
			{
				ctorLines.Add(line + ") {");
				ctorLines.AddRange(f.returnType.GetCppReturnString(
					f.classname + "::" + f.funcname + "(" + callline + ")"));
				ctorLines.Add(" }");
			}
			else
			{
				ctorLines.Add(line + ") {");
				ctorLines.AddRange(f.returnType.GetCppReturnString(
					"pthis->" + f.funcname + "(" + callline + ")"));
				ctorLines.Add(" }");
			}
 		}
 		
 		
 		string LogNodeLocation(Node n)
        {
			return $"[{n.Line}, {n.Column}]";	
        
        }		

		const string cptrcls = @"template<typename T> class CPtr { public: static CPtr<T>* Make(T* _ptr) { return new CPtr(_ptr); } static CPtr<T>* Make(const std::shared_ptr<T>& _ssptr) { return new CPtr(_ssptr); } static CPtr<T>* Make(const T* _ptr) { return new CPtr(const_cast<T *>(_ptr)); } static CPtr<T>* Make(const T &_ptr) { return new CPtr(_ptr); }  operator T* () { return ptr != nullptr ? ptr : sptr.get(); }  operator T& () { return ptr != nullptr ? *ptr : *sptr.get(); }  operator const std::shared_ptr<T> &() const { if (sptr != nullptr) return sptr; else throw; }  operator std::shared_ptr<T>() { if (sptr != nullptr) return sptr; else throw; } operator const T* () const { return ptr != nullptr ? ptr : sptr.get(); }  operator T& () const { return ptr != nullptr ? *ptr : *sptr.get(); } private:  CPtr(const T& _ptr) : sptr(nullptr) { ptr = const_cast<T *>(& _ptr); }  CPtr(T* _ptr) : ptr(_ptr), sptr(nullptr) {}  CPtr(const std::shared_ptr<T>& _ssptr) : ptr(nullptr), sptr(_ssptr) {}  T* ptr; std::shared_ptr<T> sptr; };";
	}
}