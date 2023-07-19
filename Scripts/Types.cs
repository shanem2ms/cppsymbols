using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace symlib.script
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

        public static Dictionary<CXTypeKind, string> nativeTypes =
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
            Vector,
            Void
        };


        public EType subtype = null;
        public CppType mainType;
        public Category category;

        public bool IsWrappedObject => category == Category.WrappedObject;

        public static HashSet<string> utypes = new HashSet<string>();
        int ptrcnt = 0;
        bool isconst = false;
        public int cderef = 0;
        bool valueAsPtr = false;
        public bool shared_ptr = false;
        public string ctype = "";
        public string cstype = "";
        public string native = "";
        public string basetype = "";
        List<Node> templateNodes;

        public bool IsPtr => ptrcnt > 0;
        public enum StringType
        {
            RawPtr,
            StdString
        }

        public string CPtrType => (category == Category.WrappedObject ||
            category == Category.Vector) ?
            ctype + "*" : ctype;

        public StringType stringType = StringType.RawPtr;

        public override string ToString()
        {
            return $"category={category} name={mainType.Token.Text} ctype={ctype} cstype={cstype}";
        }
        public string Name => GetName();

        public EType(CppType t, List<Node> _templateNodes)
        {
            mainType = t;
            templateNodes = _templateNodes;
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
            if (category == Category.Vector && !IsPtr)
                // no vectors by value
                category = Category.Unsupported;

            cstype = cstype.Replace("::", ".");
            if (overflowed)
                Api.WriteLine($"OVERFLOW {t.Token.Text}");
        }

        bool overflowed = false;
        void BuildTypeRec(CppType t, int l)
        {
            if (l > 20)
            {
                overflowed = true;
                return;
            }
            if (t.Kind == CXTypeKind.FunctionProto)
                ctype += t.Token.Text;
            else if (t.Kind == CXTypeKind.Typedef &&
                 t.Token.Text == "std::string")
            {
                category = Category.String;
                stringType = StringType.StdString;
            }
            else if (!shared_ptr && t.Kind == CXTypeKind.Unexposed)
            {
                if (templateNodes.Count() > 2 &&
                    templateNodes[1].Kind == CXCursorKind.TemplateRef &&
                    (templateNodes[1].Token.Text == "shared_ptr"))
                {
                    shared_ptr = true;
                    for (int idx = 2; idx < templateNodes.Count(); ++idx)
                    {
                        if (templateNodes[idx].Kind == CXCursorKind.TypeRef)
                        {
                            int tridx = idx;
                            for (int idx2 = idx; idx2 < templateNodes.Count(); ++idx2)
                            {
                                if (templateNodes[idx2].Kind == CXCursorKind.TypeRef)
                                    tridx = idx2;
                            }
                            BuildTypeRec(templateNodes[tridx].CppType, l + 1);
                            break;
                        }
                    }
                }
                else if (templateNodes.Count() > 2 &&
                    templateNodes[1].Kind == CXCursorKind.TemplateRef &&
                    (templateNodes[1].Token.Text == "vector"))
                {
                    var nextNodes = templateNodes.GetRange(2, templateNodes.Count - 2);
                    Node nextTypeNode = nextNodes.FirstOrDefault(n => n.Kind != CXCursorKind.NamespaceRef);
                    if (nextTypeNode?.CppType != null)
                    {
                        this.subtype = new EType(nextTypeNode?.CppType, nextNodes);
                        this.ctype = nextTypeNode?.CppType.Token.Text;
                        category = Category.Vector;
                    }
                }

            }
            else if (t.Next != null)
            {
                if (t.Kind == CXTypeKind.Pointer ||
                    t.Kind == CXTypeKind.LValueReference ||
                    t.Kind == CXTypeKind.ConstantArray)
                    ptrcnt++;

                if (t.Const)
                {
                    isconst = true;
                    ctype += "const ";
                }
                BuildTypeRec(t.Next, l + 1);
                if (category == Category.WrappedObject)
                {
                    if (t.Kind == CXTypeKind.Pointer)
                        ctype = $"CPtr<{RemoveConst(ctype)}>";
                    if (t.Kind == CXTypeKind.LValueReference)
                    {
                        cderef++;
                        ctype = $"CPtr<{RemoveConst(ctype)}>";
                    }
                    if (t.Kind == CXTypeKind.ConstantArray)
                        ctype += "[]";
                }
                else if (category == Category.Vector && subtype != null)
                {
                    if (t.Kind == CXTypeKind.Pointer)
                        ctype = $"CVec<{RemoveConst(ctype)}>";
                    else if (t.Kind == CXTypeKind.LValueReference)
                    {
                        cderef++;
                        ctype = $"CVec<{RemoveConst(ctype)}>";
                    }
                }
                else
                {
                    if (t.Kind == CXTypeKind.Pointer)
                        ctype += " *";
                    if (t.Kind == CXTypeKind.LValueReference)
                    {
                        cderef++;
                        ctype += " *";
                    }
                    if (t.Kind == CXTypeKind.ConstantArray)
                        ctype += "[]";
                }
            }
            else
            {
                if (t.Const)
                {
                    isconst = true;
                    ctype += "const ";
                }
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
                        ctype = $"CPtr<{RemoveConst(ctype)}>";
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
            !(ptrcnt > 0 && (category == Category.Primitive ||
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


        static bool IsPrimitiveType(CppType b)
        {
            return primitives.Contains(b.Kind);
        }

        public static CppType GetBaseType(CppType t)
        {
            if (t.Kind == CXTypeKind.FunctionProto)
                return t;
            else if (t.Next != null)
                return GetBaseType(t.Next);
            else
                return t;
        }

        static string RemoveConst(string t)
        {
            if (t.StartsWith("const "))
                return t.Substring("const ".Length);
            else return t;
        }
    }

    class Parameter
    {
        public Parameter(Node n)
        {
            param = n;
            if (n.Kind == CXCursorKind.CXXMethod ||
                n.Kind == CXCursorKind.FunctionDecl)
            {
                type = new EType(n.CppType.Next, n.allChildren);
            }
            else
                type = new EType(n.CppType, n.allChildren);
        }

        public EType type;
        public Node param;
    }
}
