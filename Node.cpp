#include "Precomp.h"
#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"


BaseNode::BaseNode(int64_t key) :
    Linkage(CXLinkageKind::CXLinkage_Invalid),
    Key(key),
    ParentNodeIdx(nullnode),
    ReferencedIdx(nullnode),
    Kind(CXCursorKind::CXCursor_FirstInvalid),
    TypeIdx(nullnode),
    token(nulltoken),
    TypeToken(nulltoken),
    Line(0),
    Column(0),
    StartOffset(0),
    EndOffset(0),
    SourceFile(nullptr),
    AcessSpecifier(CX_CXXInvalidAccessSpecifier),
    isAbstract(false),
    isDeleted(false),
    nTemplateArgs(0),
    StorageClass(CX_SC_Invalid)
{
}


uint64_t BaseNode::RefHash()
{
    return (uint64_t)(SourceFile != nullptr ? (SourceFile->Hash() << 32) : 0) +
        (uint64_t)(this->StartOffset << 8) +
        (uint64_t)this->Kind;
}

inline int uintCompare(unsigned int a, unsigned int b)
{
    if (a < b) return -1;
    if (a > b) return 1;
    return 0;
}


bool BaseNode::Equals(NodePtr node)
{
    return node != nullptr &&
        Kind == node->Kind &&
        Line == node->Line &&
        Column == node->Column &&
        *SourceFile == *node->SourceFile;
}

std::string BaseNode::ToString()
{
    std::ostringstream strm;
    strm << "[" << Line << ", " << Column << "]: [" << Kind;
    return strm.str();
}

int BaseNode::CompareTo(NodePtr n)
{
    int c = uintCompare(this->Kind, n->Kind);
    if (c != 0)
        return c;
    c = uintCompare(this->Line, n->Line);
    if (c != 0)
        return c;
    c = uintCompare(this->Column, n->Column);
    if (c != 0)
        return c;
    c = this->SourceFile->FullPath.compare(
        n->SourceFile->FullPath);
    return c;
}


// trim from start (in place)
static inline void ltrim(std::string& s) {
    s.erase(s.begin(), std::find_if(s.begin(), s.end(), [](unsigned char ch) {
        return !std::isspace(ch);
        }));
}

// trim from end (in place)
static inline void rtrim(std::string& s) {
    s.erase(std::find_if(s.rbegin(), s.rend(), [](unsigned char ch) {
        return !std::isspace(ch);
        }).base(), s.end());
}

// trim from both ends (in place)
static inline void trim(std::string& s) {
    ltrim(s);
    rtrim(s);
}

int64_t BaseNode::NodeFromCursor(CXCursor cursor,
    int64_t parentNode, VisitContextPtr vc)
{
    int64_t nodeIdx = vc->allocNodes.size();
    vc->allocNodes.push_back(Node(nodeIdx));
    Node& node = vc->allocNodes.back();
    node.Key = nodeIdx;
    node.CompilingFile = vc->compilingFilePtr;
    node.Kind = clang_getCursorKind(cursor);
    if (node.Kind == CXCursorKind::CXCursor_FirstInvalid ||
        node.Kind == CXCursorKind::CXCursor_NoDeclFound)
        return nullnode;
    node.clangHash = clang_hashCursor(cursor);
    node.isref = false;

    CXSourceRange range = clang_getCursorExtent(cursor);
    CXSourceLocation srcLoc = clang_getCursorLocation(cursor);
    CXSourceLocation srcEndLoc = clang_getRangeEnd(range);
    unsigned int defline = 0;
    unsigned int defcolumn = 0;
    unsigned int defoffset = 0;
    CXFile ifile;

    if (node.Kind == CXCursorKind::CXCursor_CXXMethod &&
        clang_isCursorDefinition(cursor) == 0)
    {
        CXCursor defcursor = clang_getCursorDefinition(cursor);
        CXCursorKind defCursorKind = clang_getCursorKind(defcursor);
        if (defCursorKind != CXCursor_FirstInvalid)
        {
            int32_t defhash = clang_hashCursor(defcursor);
            vc->definitionHashes.insert(std::make_pair(node.clangHash, defhash));
        }
    }

    node.Linkage = clang_getCursorLinkage(cursor);
    node.AcessSpecifier = clang_getCXXAccessSpecifier(cursor);
    node.StorageClass = clang_Cursor_getStorageClass(cursor);
    node.nTemplateArgs = clang_Cursor_getNumTemplateArguments(cursor);

    CXFile file;
    unsigned int line;
    unsigned int column;
    unsigned int offset;

    clang_getExpansionLocation(srcLoc, &file, &line, &column, &offset);
    unsigned int endOffset;
    clang_getExpansionLocation(srcEndLoc, nullptr,
        nullptr, nullptr, &endOffset);

    std::string tokenStr = Str(clang_getCursorSpelling(cursor));
    trim(tokenStr);

    node.pTypePtr = TypeFromCursor(cursor, vc);
    //node.ty
    node.isAbstract = clang_CXXRecord_isAbstract(cursor) != 0;
    node.isDeleted = clang_CXXMethod_isDeleted(cursor) != 0;
    node.tmpTokenString = tokenStr;
    node.ParentNodeIdx = parentNode;
    node.Line = line;
    node.StartOffset = offset;
    std::string fileName = Str(clang_getFileName(file));
    std::string commitName = CPPSourceFile::FormatPath(fileName);
    node.SourceFile = commitName.empty() ?
        vc->curSourceFile :
        vc->dbFile->GetOrInsertFile(commitName, fileName);
    node.EndOffset = endOffset;
    return nodeIdx;
}

int64_t BaseNode::NodeRefFromCursor(CXCursor cursor, int64_t parentIdx, VisitContextPtr vc)
{
    CXCursorKind cursorKind = clang_getCursorKind(cursor);
    if (cursorKind == CXCursorKind::CXCursor_FirstInvalid)
        return nullnode;
    int64_t nodeIdx = vc->allocNodes.size();
    vc->allocNodes.push_back(Node(nodeIdx));
    Node& node = vc->allocNodes.back();
    node.Key = nodeIdx;
    node.Kind = cursorKind;
    node.CompilingFile = vc->compilingFilePtr;
    node.clangHash = clang_hashCursor(cursor);
    node.isref = true;
    CXSourceLocation loc = clang_getCursorLocation(cursor);
    CXFile outfile;
    unsigned int outline, outcol, outoffset;
    clang_getExpansionLocation(loc, &outfile, &outline, &outcol, &outoffset);
    std::string fileName = Str(clang_getFileName(outfile));
    std::string commitName = CPPSourceFile::FormatPath(fileName);
    node.SourceFile = commitName.empty() ?
        vc->curSourceFile :
        vc->dbFile->GetOrInsertFile(commitName, fileName);
    node.Line = outline;
    node.Column = outcol;
    node.ParentNodeIdx = parentIdx;
    node.StartOffset = outoffset;
    return nodeIdx;
}

TypeNode* BaseNode::TypeFromCursor(CXCursor cursor, VisitContextPtr vc)
{
    CXType cxtype = clang_getCursorType(cursor);

    TypeNode* tn = TypeFromCxType(cursor, cxtype, vc);
    tn->CalcHash();
    return tn;
}

TypeNode* BaseNode::TypeFromCxType(CXCursor cursor, CXType cxtype, VisitContextPtr vc)
{
    TypeNode* tn = new TypeNode();
    tn->TypeKind = cxtype.kind;
    tn->tokenStr = Str(clang_getTypeSpelling(cxtype));
    tn->isConst = clang_isConstQualifiedType(cxtype);
    trim(tn->tokenStr);

    if (cxtype.kind == CXType_Pointer)
    {
        cxtype = clang_getPointeeType(cxtype);
        tn->children.push_back(TypeNode::Child(TypeFromCxType(cursor, cxtype, vc)));
    }
    else if (cxtype.kind == CXType_LValueReference)
    {
        cxtype = clang_getNonReferenceType(cxtype);
        tn->children.push_back(TypeNode::Child(TypeFromCxType(cursor, cxtype, vc)));
    }
    else if (cxtype.kind == CXType_Elaborated)
    {
        cxtype = clang_Type_getNamedType(cxtype);
        tn->children.push_back(TypeNode::Child(TypeFromCxType(cursor, cxtype, vc)));
    }
    else if (cxtype.kind == CXType_Typedef)
    {
        CXCursor c = clang_getTypeDeclaration(cxtype);
        cxtype = clang_getTypedefDeclUnderlyingType(c);
        tn->children.push_back(TypeNode::Child(TypeFromCxType(cursor, cxtype, vc)));
    }
    else if (cxtype.kind == CXType_ConstantArray ||
        cxtype.kind == CXType_IncompleteArray ||
        cxtype.kind == CXType_VariableArray)
    {
        cxtype = clang_getElementType(cxtype);
        tn->children.push_back(TypeNode::Child(TypeFromCxType(cursor, cxtype, vc)));
    }
    else if (cxtype.kind == CXType_FunctionProto)
    {
        cxtype = clang_getResultType(cxtype);
        tn->children.push_back(TypeNode::Child(TypeFromCxType(cursor, cxtype, vc)));
    }
    else if (cxtype.kind == CXType_Unexposed)
    {
        TypeNode* tnret = ParseTemplateType(tn->tokenStr);
        if (tnret != nullptr)
            return tnret;
    }

    constexpr int csize = sizeof("const ") - 1;
    if (tn->isConst && tn->tokenStr.starts_with("const "))
    {
        tn->tokenStr = tn->tokenStr.substr(csize);
    }
    return tn;
}

TypeNode* BaseNode::ParseTemplateType(const std::string& templateType)
{
    size_t startPos = templateType.find('<');
    if (startPos == std::string::npos)
        return nullptr;
    TypeNode* tn = new TypeNode();
    tn->TypeKind = CXType_Unexposed;
    tn->tokenStr = templateType;

    {
        TypeNode* tntemplate = new TypeNode();
        tntemplate->TypeKind = CXType_TemplateType;
        tntemplate->tokenStr = templateType.substr(0, startPos);
        tn->children.push_back(tntemplate);
    }
    BaseNode::ParseTemplateParmsRec(templateType, startPos+1, tn->children);
    return tn;
}

size_t BaseNode::ParseTemplateParmsRec(const std::string& templateType, size_t startOffset, std::vector<TypeNode::Child>& outchildren)
{
    static char srchChars[] = { '<', '>', ',' };
    constexpr int nchars = sizeof(srchChars) / sizeof(srchChars[0]);
    int curPos = startOffset;

    TypeNode* tn = new TypeNode();
    tn->TypeKind = CXType_TemplateParam;

    std::vector<TypeNode*> typenodes;
    while (curPos < templateType.size())
    {
        curPos = templateType.find_first_of(srchChars, curPos, nchars);
        if (curPos < 0)
            break;
        else if (templateType[curPos] == '<')
        {
            std::vector<TypeNode::Child> children;
            curPos = ParseTemplateParmsRec(templateType, curPos + 1, children);
        }
        else if (templateType[curPos] == '>')
        {
            tn->tokenStr = templateType.substr(startOffset, curPos - startOffset);
            outchildren.push_back(tn);
            return curPos + 1;
        }
        else if (templateType[curPos] == ',')
        {
            tn->tokenStr = templateType.substr(startOffset, curPos - startOffset);
            outchildren.push_back(tn);
            curPos++;
            startOffset = curPos;
            tn = new TypeNode();
            tn->TypeKind = CXType_TemplateParam;
        }
        else
        {
            curPos++;
        }
    }
    return curPos;
}

extern std::string cxc[CXCursor_OverloadCandidate + 1];
extern std::string cxt[CXType_Atomic + 1];

void BaseNode::LogTypeInfo(VisitContextPtr vc, std::ostringstream & strm, TypeNode* ptype)
{
    size_t depth = vc->depth + 1;
    
    strm << std::endl << std::string(depth * 3, ' ') <<
        " " << cxt[ptype->TypeKind] << ": " << ptype->tokenStr << " C:" << ptype->isConst;
    for (auto &child : ptype->children)
    {
        vc->depth = depth;
        LogTypeInfo(vc, strm, child.ptr);
    }
    vc->depth = depth - 1;
}

const char *vals[4] =
{
    "",
    "Public",
    "Protected",
    "Private"
};

const char* stgvals[] =
{
  "",
  "None",
  "Extern",
  "Static",
  "PrivateExtern",
  "OpenCLWorkGroupLocal",
  "Auto",
  "Register"
};

void BaseNode::LogNodeInfo(VisitContextPtr vc, int64_t nodeIdx, std::string tag)
{
    Node &node = vc->allocNodes[nodeIdx];
    size_t depth = vc->depth;
    std::ostringstream strm;
    strm << std::endl << std::string(depth * 3, ' ') <<
        tag << " " << (node.SourceFile != nullptr ? node.SourceFile->Name() : "") << " [" <<
        node.Line << ", " << node.Column << "] " << cxc[node.Kind] << " " << node.TypeIdx <<
        " " << node.tmpTokenString << " " << " " << vals[node.AcessSpecifier] << " " << stgvals[node.StorageClass] <<
        (node.isDeleted ? " D1" : " D0");
    if (node.nTemplateArgs > -1)
        strm << " " << "TA=" << node.nTemplateArgs;
    if (node.pTypePtr != nullptr)
    {
        LogTypeInfo(vc, strm, node.pTypePtr);
    }
    vc->LogTree(strm.str());
}

BaseNode::~BaseNode()
{
}

CXChildVisitResult BaseNode::ClangVisitor(CXCursor cursor, CXCursor parent, CXClientData client_data)
{
    VisitContext* vc = (VisitContext*)client_data;
    CXSourceLocation loc = clang_getCursorLocation(cursor);
    CXFile file;
    clang_getExpansionLocation(loc, &file, nullptr, nullptr, nullptr);
    if (vc->prevFile != file)
    {
        std::string fileName = Str(clang_getFileName(file));
        std::string commitName = CPPSourceFile::FormatPath(fileName);

        //if (vc->dolog)
            //vc->LogTree("\nChange File: " + commitName);

        if (!commitName.empty())
        {
            vc->skipthisfile = (vc->pchFiles.find(commitName) != vc->pchFiles.end());
            if (!vc->isolateFile.empty() && vc->isolateFile != fileName)
            {
                vc->skipthisfile = true;
            }
            ;
            vc->logthisfile = vc->dolog && vc->logFilterFile == fileName;

            PrevFilePtr pv = new PrevFile(commitName, nullptr);
            auto itPv = std::find(vc->fileStack.rbegin(), vc->fileStack.rend(), pv);
            if (itPv != vc->fileStack.rend())
            {
                vc->curNode = (*itPv)->node;
                vc->fileStack.erase(std::next(itPv.base()), vc->fileStack.end());
            }
            else if (!vc->prevFileCommitName.empty())
            {
                vc->fileStack.push_back(new PrevFile(vc->prevFileCommitName, vc->curNode));
                vc->curNode = nullptr;
            }

            vc->visitedFiles.insert(commitName);

            CPPSourceFilePtr sf = vc->dbFile->GetOrInsertFile(commitName, fileName);
            vc->curSourceFile = sf;

            if (vc->dolog)
            {
                std::ostringstream strm;
                strm << commitName << ": " << vc->fileStack.size();
                //vc->LogTree(strm.str());
            }
        }
        else
        {
            vc->curNode = nullptr;
            vc->curSourceFile = nullptr;
        }
        vc->prevFile = file;
        vc->prevFileCommitName = commitName;
    }
    
    auto itParNode = vc->nodesMap.find(parent);
    int64_t parNodeIdx = itParNode != vc->nodesMap.end() ? itParNode->second : nullnode;
    int64_t nodeIdx = NodeFromCursor(cursor, parNodeIdx, vc);
    if (vc->logthisfile && !vc->skipthisfile)
        LogNodeInfo(vc, nodeIdx, "node");
    vc->allocNodes[nodeIdx].ReferencedIdx = BaseNode::NodeRefFromCursor(clang_getCursorReferenced(cursor), nodeIdx, vc);
    if (vc->logthisfile && !vc->skipthisfile && vc->allocNodes[nodeIdx].ReferencedIdx != nullnode)
        LogNodeInfo(vc, vc->allocNodes[nodeIdx].ReferencedIdx, "noderef");

    vc->nodesMap.insert(std::make_pair(cursor, nodeIdx));
   
    return vc->skipthisfile ? CXChildVisitResult::CXChildVisit_Continue : CXChildVisitResult::CXChildVisit_Recurse;

}

std::ostream& operator<<(std::ostream& os, TypeNode const& m) {
    return os << "T: " << m.TypeKind;
}


void TypeNode::CalcHash()
{
    size_t hashCode = 1035752329;
    hashCode = hashCode * -1521134295 + isConst;
    hashCode = hashCode * -1521134295 + TypeKind;
    hashCode = hashCode * -1521134295 + std::hash<std::string>{}(tokenStr);
    for (auto& c : children)
    {
        c.ptr->CalcHash();
        hashCode = hashCode * c.ptr->hash;
    }

    hash = hashCode;
}