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
			WrappedEnum,
			Void
		};
		
		
		CppType mainType;
		public Category category;
		
		public bool IsWrappedObject => category == Category.WrappedObject;
		
		public static HashSet<string> utypes = new HashSet<string>();
		int ptrcnt = 0;
		public int cderef = 0;
		bool valueAsPtr = false;
		public string ctype = "";
        public string cstype = "";
        public string native = "";
        string basetype = "";
        enum StringType
        {
        	RawPtr,
        	StdString
        }
				
		StringType stringType = StringType.RawPtr;
		
		public override string ToString()
		{
			return $"category={category} name={mainType.Token.Text} ctype={ctype} cstype={cstype}";
		}
		public string Name => GetName();
		
		public EType(CppType t)
		{
			mainType = t;			
			BuildType(t);	
			if (category == Category.Unsupported)
				utypes.Add(t.ToString());
		}
		
		void BuildType(CppType t)
        {
        	category = Category.Unsupported;
        	BuildTypeRec(t, 0);
        	if (category == Category.String &&
        		stringType == StringType.StdString)
        	{
        		ctype = "char *";
        	}
        	cstype = cstype.Replace("::", ".");
        }
        
        void BuildTypeRec(CppType t, int l)
        {
        	if (t.Kind == CXTypeKind.FunctionProto)
                ctype += t.Token.Text;
           	else if (t.Kind == CXTypeKind.Typedef && 
        			t.Token.Text == "std::string")
    		{
    			category = Category.String;
    			stringType = StringType.StdString;
    		}
        	else if (t.Next != null)
        	{
        		if (t.Kind == CXTypeKind.Pointer ||
        			t.Kind == CXTypeKind.LValueReference ||
        			t.Kind == CXTypeKind.ConstantArray)
        			ptrcnt++;
        		        			
	    		if (t.Const) ctype += "const ";
        		BuildTypeRec(t.Next, l + 1);
        		if (t.Kind == CXTypeKind.Pointer)
                    ctype += " *";
        		if (t.Kind == CXTypeKind.LValueReference)
        		{
        			cderef++;
					ctype += " *";// : " &";
        		}
        		if (t.Kind == CXTypeKind.ConstantArray)
                    ctype += "[]";
        	}
    	 	else
        	{
	    		if (t.Const) ctype += "const ";
	    		ctype += t.Token.Text;
	    		if (t.Kind == CXTypeKind.Void && ptrcnt == 0)
					category = Category.Void;
				else if (t.Kind == CXTypeKind.Char_S && ptrcnt == 1)
					category = Category.String;
				else if (IsPrimitiveType(t) && ptrcnt == 0)
					category = Category.Primitive;
	    		else if (t.Kind == CXTypeKind.Record)
	    		{
	    			basetype = t.Token.Text;
	    			cstype = basetype;
	    			if (ptrcnt == 0)
	    			{
		    			ctype += " *";
		    			cderef++;
		    			ptrcnt++;
		    			valueAsPtr = true;
	    			}
	    		}
				if (category == Category.Unsupported &&
					Classes.ClassMap.ContainsKey(t.Token.Text))
				{
					category = (t.Kind == CXTypeKind.Enum) ?
						Category.WrappedEnum : Category.WrappedObject;
				}	    				    		
        	}
        }             
        		
		public bool IsSupported => category != Category.Unsupported &&
			!(ptrcnt>0 && (category == Category.Primitive ||
				category == Category.WrappedEnum));
		
		
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
				ptrcnt>0)
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
		
		public string GetCSApiCall(string varname, out string header, out string footer)
		{
			if (category == Category.String)
			{
				header = String.Empty;
				footer = String.Empty;
				return $"Marshal.StringToHGlobalAnsi({varname})";	
			}
			else
			{	header = String.Empty;
				footer = String.Empty;
				return varname + (IsWrappedObject ? ".pthis" : "");
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
			else if (ptrcnt>0)
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
			else if (category == Category.WrappedObject ||
				category == Category.WrappedEnum)
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
        
        public string GetCppCall(string parmname)
        {
        	if (category == Category.String && stringType == StringType.StdString)
        		return $"std::string({parmname})";
        	else
 				return (cderef>0 ? "*" : "") + parmname;
 		}
 		
        public List<string> GetCppReturnString(string callstring)
        {
        	List<string> outlines = new List<string>();
        	if (category == Category.Void)
        	{
        		outlines.Add($"    {callstring};");
        	}
        	else if (category == Category.String && stringType == StringType.StdString)
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
        	else if (category == Category.WrappedObject)
        	{
        		outlines.Add($"    return new {cstype}({callstring});");
        	}
			else
			{
        		outlines.Add($"    return {callstring};");
			}			
			return outlines;
		}
    }
}
