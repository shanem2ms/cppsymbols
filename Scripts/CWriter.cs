using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using System.Text;
using System.Linq;
using static symlib.script.EType;
using System.Security.AccessControl;
using System.Diagnostics;

namespace symlib.script
{
	class CWriter
	{		
		string outfile;
		List<string> ctorLines = new List<string>();
        List<string> wrappObjLines = new List<string>();
        public HashSet<string> IncludeFiles = new HashSet<string>();
		public CWriter(string _outfile)
		{
			outfile = _outfile;
			IncludeFiles.Add("StdIncludes.h");
        }

        public void AddWrappedTypes(Dictionary<string, EType> types)
        {
            wrappObjLines.Add(@"template <typename T> class converter
{};
template <> class converter<std::string>
{public:    typedef char* OutType; };
");
            foreach (var kv in types)
            {
                wrappObjLines.Add($"template <> class converter<{kv.Value.basetype}>{{public: typedef CPtr<{kv.Value.basetype}> *OutType; }};");
            }
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
            lines.Add("");
            lines.Add(cveccls);
            lines.Add("");
            lines.AddRange(wrappObjLines);
            lines.Add(retcnvstr);
            lines.Add("");
            lines.Add(cfuncheaders);
			lines.AddRange(ctorLines);
			File.WriteAllLines(outfile, lines);
		}

        static int tmpIdx = 0;

        public void AddFunction(Function f)
 		{
			string cclass = f.classname.Replace("::", "_");
			string ctornum = f.idx >= 0 ? f.idx.ToString() : "";
			bool isConstructor = f.funcname == null;
			f.cppname = isConstructor ? $"{cclass}_Ctor{ctornum}" : $"{cclass}{f.cexportname}";
			string retarg = isConstructor ? $"CPtr<{f.classname}> *" : f.returnType.CPtrType;
			bool hasThisArg = !isConstructor && !f.isStatic;
			string thisarg = hasThisArg ? $"CPtr<{f.classname}> *pthis" : "";
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
                if (parmname.Length == 0)
                    parmname = $"tmp{tmpIdx++}";

                string suffix;
                line += $"{GetCppDeclarationStr(tp.type, out suffix)} {parmname}{suffix}";
				callline += GetCppCall(tp.type, parmname);
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
				ctorLines.AddRange(GetCppReturnString(f.returnType,
                    f.classname + "::" + f.funcname + "(" + callline + ")"));
				ctorLines.Add(" }");
			}
			else
			{
				ctorLines.Add(line + ") {");
				ctorLines.AddRange(GetCppReturnString(f.returnType,
                    "(*pthis)->" + f.funcname + "(" + callline + ")"));
				ctorLines.Add(" }");
			}
 		}

        public string GetCppDeclarationStr(EType t, out string suffix)
        {
            suffix = "";
            if (t.category == Category.String)
            {
                return "const char *";
            }
            else if (t.category == Category.Vector && t.subtype != null)
            {
                string s;
                return t.CPtrType;
            }
            else if (t.category == Category.Primitive)
            {
                if (t.mainType.Kind == CXTypeKind.ConstantArray ||
                    t.mainType.Kind == CXTypeKind.IncompleteArray ||
                    t.mainType.Kind == CXTypeKind.VariableArray)
                {
                    suffix = "[]";
                    return t.mainType.Next.Token.Text;;
                }
                else
                    return t.ctype;
            }
            else if (t.category == Category.WrappedObject ||
                t.category == Category.WrappedEnum)
            {

                return t.CPtrType;
            }
            return "";
        }
        public string GetCppCall(EType t, string parmname)
        {
            if (t.category == Category.String && t.stringType == StringType.StdString)
                return $"std::string({parmname})";
            else if (t.category == Category.WrappedObject)
                return "*" + parmname;
            else if (t.shared_ptr)
                return $"std::shared_ptr<{t.basetype}>({parmname})";
            else
                return (t.cderef > 0 ? "*" : "") + parmname;
        }
        public List<string> GetCppReturnString(EType t, string callstring)
        {
            List<string> outlines = new List<string>();
            if (t.category == Category.Void)
            {
                outlines.Add($"    {callstring};");
            }
            else if (t.category == Category.String && t.stringType == EType.StringType.StdString)
            {
                outlines.Add($"    return retcnv({callstring});");
            }
            else if (t.category == Category.WrappedObject)
            {
                outlines.Add($"    return retcnv({callstring});");
            }
            else if (t.category == Category.Vector)
            {
                outlines.Add($"    {t.CPtrType} cptrret = {t.ctype}::Make({callstring});");
                outlines.Add($"    return cptrret;");
            }
            else
            {
                string refstr = (t.mainType.Kind == CXTypeKind.LValueReference) ? "&" : "";
                outlines.Add($"    return {refstr}{callstring};");
            }
            return outlines;
        }

        string LogNodeLocation(Node n)
        {
			return $"[{n.Line}, {n.Column}]";	
        
        }

        const string cptrbase = @"
template <typename T, typename _ = void> class cptr {
public:
};

template <typename T, std::enable_if_t<!std::is_abstract_v<T>>> class cptr {
    T val;
public:
    cptr(const T& _val) : val(_val) {}
    operator const T& () const { return val; }
};";
        const string cptrcls = @"


class ICPtr {
public:
virtual ~ICPtr() = 0 {} };
template<typename T> class CPtr : public ICPtr
{ public:
    static CPtr<T>* Make(T* _ptr) { return new CPtr(_ptr); }
    static CPtr<T>* Make(const std::shared_ptr<T>& _ssptr) { return new CPtr(_ssptr); }
    static CPtr<T>* Make(const T* _ptr) { return new CPtr(const_cast<T*>(_ptr)); }
    static CPtr<T>* Make(const T& _ptr) { return new CPtr(_ptr); }
    operator T* () { return ptr != nullptr ? ptr : sptr.get(); }
    operator T& () { return ptr != nullptr ? *ptr : *sptr.get(); }
    operator const std::shared_ptr<T>& () const { if (sptr != nullptr) return sptr; else throw; }
    operator std::shared_ptr<T>() { if (sptr != nullptr) return sptr; else throw; }
    operator const T* () const { return ptr != nullptr ? ptr : sptr.get(); }
    operator T& () const { return ptr != nullptr ? *ptr : *sptr.get(); }
    T *operator -> () { return ptr != nullptr ? ptr : sptr.get(); }
    ~CPtr() override { sptr = nullptr; }
private:
    CPtr(const T& _ptr) : sptr(nullptr) { ptr = const_cast<T*>(&_ptr); }
    CPtr(T* _ptr) : ptr(_ptr), sptr(nullptr) {}
    CPtr(const std::shared_ptr<T>& _ssptr) : ptr(nullptr), sptr(_ssptr) {}  T* ptr; std::shared_ptr<T> sptr; };
";
        string retcnvstr= @"
//template <typename T> converter<T>::OutType retcnv(T t)
//{ return t; }
template <typename T> converter<T>::OutType retcnv(const T *t)
{ return CPtr<T>::Make(t); }
template <typename T> converter<T>::OutType retcnv(T* t)
{ return CPtr<T>::Make(t); }
template <typename T> converter<T>::OutType retcnv(const T& t)
{ return CPtr<T>::Make(t); }
template <typename T> converter<T>::OutType retcnv(std::shared_ptr<typename T> t)
{ return CPtr<T>::Make(t); }

template<> char* retcnv<std::string>(const std::string &strtmp)
{
    char* retstr = new char[strtmp.size() + 1];
    memcpy(retstr, strtmp.c_str(), strtmp.size());
    retstr[strtmp.size()] = 0;
    return retstr;
}";

    const string cveccls = @"
class IEnumerator
{ public:
    virtual void* Current() = 0;
    virtual bool MoveNext() = 0;
    virtual void Reset() = 0; };
class IVec
{ public:
    virtual size_t Size() = 0;
    virtual void* GetItem(size_t idx) = 0;
    virtual IEnumerator* GetEnumerator() = 0; };
template<typename T> class CVec : public IVec
{     std::vector<T>* pvec;
public:
    operator std::vector<T>* () { return pvec; }
    operator std::vector<T>& () { return *pvec; }
    operator const std::vector<T>* () const { return pvec; }
    operator const std::vector<T>& () const { return *pvec; }
    static CVec<T>* Make(const std::vector<T>& _pvec) { return new CVec(_pvec); }
    static CVec<T>* Make(const std::vector<T>* _pvec) { return new CVec(_pvec); }
    // IEnumerator overload
    class Enumerator : public IEnumerator
    {
        std::vector<T>* pvec;
        std::vector<T>::iterator it;
    public:
        Enumerator(std::vector<T>* _pvec) {
            pvec = _pvec;
            Reset(); }
        void* Current() override { T* ptr = &(*it); return ptr; }
        bool MoveNext() override { if (it == pvec->end()) return false; it++; return true; }
        void Reset() override { it = pvec->begin(); }
    };
    // IVec overloads
    size_t Size() override { return pvec->size(); }
    void* GetItem(size_t idx) override { return &pvec->at(idx); }
    IEnumerator* GetEnumerator() override
    { return new Enumerator(pvec); }
private:
    CVec(const std::vector<T>& _pvec) { pvec = const_cast<std::vector<T> *>(&_pvec); }
    CVec(const std::vector<T>* _pvec) { pvec = const_cast<std::vector<T> *>(_pvec); } };
template<typename J> static CVec<J>* CVecMake(const std::vector<J>& _pvec) { return new CVec(_pvec); }
template<typename J> static CVec<J>* CVecMake(const std::vector<J>* _pvec) { return new CVec(_pvec); }
";
    const string cfuncheaders = @"#define CAPI extern ""C"" __declspec(dllexport)
CAPI void* IEnumerator_Current(IEnumerator* _ptr) { return _ptr->Current(); }
CAPI bool IEnumerator_MoveNext(IEnumerator* _ptr) { return _ptr->MoveNext(); }
CAPI void IEnumerator_Reset(IEnumerator* _ptr) { _ptr->Reset(); }
CAPI size_t IVec_Size(IVec* _ptr) { return _ptr->Size(); }
CAPI void* IVec_GetItem(IVec* _ptr, size_t idx) { return _ptr->GetItem(idx); }
CAPI IEnumerator* IVec_GetEnumerator(IVec* _ptr) { return _ptr->GetEnumerator(); }
CAPI void ICPtrFree(ICPtr* _ptr) { delete _ptr; }
";
    }
}