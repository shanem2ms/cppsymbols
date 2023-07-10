using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cppsymview.script
{
    class EType
    {
		static HashSet<CXTypeKind> primitives = new HashSet<CXTypeKind>() {
				CXTypeKind.Float,
				CXTypeKind.Int,
				CXTypeKind.UInt,
				CXTypeKind.Bool,
				CXTypeKind.Char_S,
				CXTypeKind.Double,
				CXTypeKind.ULongLong,
				CXTypeKind.UChar,
				CXTypeKind.LongLong,
				CXTypeKind.Long,
				CXTypeKind.ULong,
				CXTypeKind.Void };    
				
		static Dictionary<CXTypeKind, string> nativeTypes = 
			new Dictionary<CXTypeKind, string>()
		{
			{ CXTypeKind.Float, "float" },
			{ CXTypeKind.Int, "int" },
			{ CXTypeKind.UInt, "uint" },
			{ CXTypeKind.Bool, "bool" },
			{ CXTypeKind.Char_S, "char" },
			{ CXTypeKind.Double, "double" },
			{ CXTypeKind.ULongLong, "ulong" },
			{ CXTypeKind.UChar, "byte" },
			{ CXTypeKind.LongLong, "long" },
			{ CXTypeKind.Long, "int" },
			{ CXTypeKind.ULong, "uint" }
		};
				
		public enum Category
		{
			Unsupported,
			Primitive,
			String,
			Template,
			WrappedObject,
			Void
		};
		
		
		CppType mainType;
		public Category category;
		
		public bool IsWrappedObject => category == Category.WrappedObject;
		
		public static HashSet<string> utypes = new HashSet<string>();
		bool isPtr = false;
		bool valueAsPtr = false;
		public bool cderef = false;
		public string ctype = "";
        public string cstype = "";
        public string native = "";
        string basetype = "";
				
		
		public string Name => GetName();
		
		public EType(CppType t)
		{
			mainType = t;
			category = GetCategory();
			if (category == Category.Unsupported)
				utypes.Add(GetBaseType(t).ToString());
			
			BuildType(t);	
			if (false)
			{
			Api.WriteLine($"{category} {t.Token.Text}");
			Api.WriteLine(ctype);
			Api.WriteLine("");
			}
		}
		
		public bool IsSupported => category != Category.Unsupported;
		
		
		string GetName()
		{
			if (category == Category.String)
			{
				return "const char *";
			}
			else if (category == Category.WrappedObject ||
				category == Category.Primitive)
				return ctype;
			else
				return mainType.Token.Text;
		}
		
		public string GetCSNativeType()
		{
			CppType baseType = GetBaseType(mainType);
			if (category == Category.String ||
				isPtr)
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
				//Api.WriteLine(mainType.Token.Text);
				return "unk";
			}
		}
		
		public string GetCSApiType()
		{
			CppType baseType = GetBaseType(mainType);
			if (category == Category.String)
				return "string";
			else if (category == Category.WrappedObject)
			{
				return cstype;
			}				
			else if (isPtr)
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
				//Api.WriteLine(mainType.Token.Text);
				return "unk";
			}
		}		
		
		Category GetCategory()
		{
			if (mainType == null)
				return Category.Unsupported;
			if (mainType.Kind == CXTypeKind.Void)
				return Category.Void;
			CppType b = GetBaseType(mainType);
			if (IsPrimitiveType(b))
				return Category.Primitive;
			if (b.Kind == CXTypeKind.TemplateName)
				return Category.Unsupported;
			if (Classes.ClassMap.ContainsKey(b.Token.Text))
				return Category.WrappedObject;

			return Category.Unsupported;
		}
					
		
		static bool IsPrimitiveType(CppType b)
        {
        	return primitives.Contains(b.Kind);
        }        
        
        static CppType GetBaseType(CppType t)
        {
        	if (t.Kind == CXTypeKind.FunctionProto)
        		return t;
        	else if (t.Next != null)
        		return GetBaseType(t.Next);
        	else
        		return t;
        }

        void BuildType(CppType t)
        {
        	BuildTypeRec(t, 0);
        	cstype = cstype.Replace("::", ".");
        }
        
        void BuildTypeRec(CppType t, int l)
        {
        	if (t.Kind == CXTypeKind.FunctionProto)
                ctype += t.Token.Text;
        	else if (t.Next != null)
        	{
        		if (t.Kind == CXTypeKind.Pointer ||
        			t.Kind == CXTypeKind.LValueReference ||
        			t.Kind == CXTypeKind.ConstantArray)
        			isPtr = true;
        			
	    		if (t.Const) ctype += "const ";
        		BuildTypeRec(t.Next, l + 1);
        		if (t.Kind == CXTypeKind.Pointer)
                    ctype += " *";
        		if (t.Kind == CXTypeKind.LValueReference)
        		{
        			cderef = true;
					ctype += " *";// : " &";
        		}
        		if (t.Kind == CXTypeKind.ConstantArray)
                    ctype += "[]";
        	}
        	else
        	{
	    		if (t.Const) ctype += "const ";
	    		ctype += t.Token.Text;
	    		if (!isPtr && t.Kind == CXTypeKind.Record)
	    		{
	    			ctype += " *";
	    			cderef = true;
	    			isPtr = true;
	    			valueAsPtr = true;
	    			basetype = t.Token.Text;
	    			cstype = basetype;
	    		}
        	}
        }             
        
		static int tmpIdx = 0;
        public string GetCppDeclarationStr(ref string param)
        {
        	if (param.Length == 0)
        		param = $"tmp{tmpIdx++}";
        	if (category == Category.String)
        	{
        		return "const char *" + param;
        	}
        	else if (category == Category.Primitive)
        	{
	        	if (mainType.Kind == CXTypeKind.ConstantArray ||
	        		mainType.Kind == CXTypeKind.IncompleteArray ||
	        		mainType.Kind == CXTypeKind.VariableArray)
	    		{
	    			return mainType.Next.Token.Text + " " + param + "[]";
	    		}
	    		else
	    			return ctype + " " + param;
			}			
			else if (category == Category.WrappedObject)
			{
	        	if (mainType.Kind == CXTypeKind.ConstantArray ||
	        		mainType.Kind == CXTypeKind.IncompleteArray ||
	        		mainType.Kind == CXTypeKind.VariableArray)
	    		{
	    			return mainType.Next.Token.Text + " " + param + "[]";
	    		}
	    		else
	    			return ctype + " " + param;
			}
			return "";
        }	
        
        public List<string> GetCppReturnString(string callstring)
        {
        	List<string> outlines = new List<string>();
        	if (category == Category.Void)
        	{
        		outlines.Add($"    {callstring};");
        	}
        	else if (category == Category.String)
        	{
        		outlines.Add($"    {mainType.Token.Text} strtmp = {callstring};");
        		outlines.Add($"    char *retstr = new char[strtmp.length()];");
        		outlines.Add($"    memcpy(retstr, strtmp.c_str(), strtmp.length());");
        		outlines.Add($"    return retstr;");
        	}
        	else if (valueAsPtr)
        	{
        		string assigntype = ctype;
        		if (assigntype.StartsWith("const "))
        			assigntype = assigntype.Substring("const ".Length);
        		outlines.Add($"    {assigntype} strtmp = new {basetype}();");
        		outlines.Add($"    *strtmp = {callstring};");
        		outlines.Add($"    return strtmp;");
        	}
			else
			{
				string refstr = (mainType.Kind == CXTypeKind.LValueReference) ? "&" : "";			
        		outlines.Add($"    return {refstr}{callstring};");
			}			
			return outlines;
        }
				
		public List<string> GetCsReturnString(string callstring)
        {
        	List<string> outlines = new List<string>();
        	if (category == Category.Void)
        	{
        		outlines.Add($"    {callstring};");
        	}
        	else if (category == Category.String)
        	{
        		outlines.Add($"    IntPtr strtmp = {callstring};");
        		outlines.Add($"    return Marshal.PtrToStringAnsi(strtmp);");
        	}
			else
			{
        		outlines.Add($"    return {callstring};");
			}			
			return outlines;
		}
    }
}
