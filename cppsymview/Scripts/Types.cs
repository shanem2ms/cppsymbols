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
			Void
		};
		
		
		CppType mainType;
		Category category;
		
		public string Name => GetName();
		
		public EType(CppType t)
		{
			mainType = t;
			category = GetCategory();
		}
		
		public bool IsSupported => category != Category.Unsupported;
		
		
		string GetName()
		{
			if (category == Category.String)
			{
				return "const char *";
			}
			else
				return mainType.Token.Text;
		}
		
		public string GetCSNativeType()
		{
			CppType baseType = GetBaseType(mainType);
			if (category == Category.String ||
				IsPointerRefArray(mainType))
				return "IntPtr";
			else if (nativeTypes.ContainsKey(baseType.Kind))
			{
				return nativeTypes[baseType.Kind];
			}
			else
			{
				Api.WriteLine(mainType.Token.Text);
				return "unk";
			}
		}
		
		static bool IsPointerRefArray(CppType t)
		{
			if (t.Kind == CXTypeKind.Pointer || 
				t.Kind == CXTypeKind.LValueReference || 
				t.Kind == CXTypeKind.IncompleteArray ||
				t.Kind == CXTypeKind.VariableArray)
				return true;
			if (t.Next != null)
        		return IsPointerRefArray(t.Next);
        	return false;
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
			if (b.Kind == CXTypeKind.Unexposed && b.Token.Text.Contains('<'))
				return ParseTemplateType(b.Token.Text);
			return Category.Unsupported;
		}
		
		static Category ParseTemplateType(string templateType)
		{	
			int curIdx = -1;
			string tmp = "";
			int curStack = 0;
			
			int startParams = templateType.IndexOf('<');
			int endParams = templateType.LastIndexOf('>');
		
			string template = templateType.Substring(0, startParams);
			//Api.WriteLine(template);
			//Api.WriteLine(templateType.Substring(startParams + 1, endParams - startParams - 1));
			//Api.WriteLine("");
			if (template == "basic_string")
				return Category.String;
			else
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

		static int tmpIdx = 0;
        public string DeclarationStr(ref string param)
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
	    			return mainType.Token.Text + " " + param;
			}
			
			return "";
        }	
        
        public List<string> GetReturnString(string callstring)
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
			else
			{
        		outlines.Add($"    return {callstring};");
			}			
			return outlines;
        }
				
    }
}
