#include "Precomp.h"
#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"

std::atomic<int64_t> BaseNode::nextKey(1);
std::atomic<int64_t> BaseNode::numAlloc(0); 
std::atomic<int64_t> Token::nextKey(1);
std::atomic<int64_t> Token::numAlloc(0);


BaseNode::BaseNode() :
    Linkage(CXLinkageKind::CXLinkage_Invalid),
    Key(-1),
    ParentNodeIdx(nullnode),
    ReferencedIdx(nullnode),
    Kind(CXCursorKind::CXCursor_FirstInvalid),
    TypeKind(CXTypeKind::CXType_Invalid),
    token(nulltoken),
    TypeToken(nulltoken),
    Line(0),
    Column(0),
    StartOffset(0),
    EndOffset(0),
    SourceFile(nullptr)
{
    numAlloc++;
}

#ifdef SQLITE
void CommitToDb(SqlLiteDb db)
{
    db.AddRow(this);
    foreach(Node childnode in Nodes)
    {
        if (childnode != nullptr)
            childnode.CommitToDb(db);
    }
}
#endif

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
    vc->allocNodes.push_back(Node());
    int64_t nodeIdx = vc->allocNodes.size();
    Node& node = vc->allocNodes.back();
    node.CompilingFile = vc->compilingFilePtr;
    node.Kind = clang_getCursorKind(cursor);
    if (node.Kind == CXCursorKind::CXCursor_FirstInvalid ||
        node.Kind == CXCursorKind::CXCursor_NoDeclFound)
        return nullnode;

    CXSourceRange range = clang_getCursorExtent(cursor);
    CXSourceLocation srcLoc = clang_getCursorLocation(cursor);
    CXSourceLocation srcEndLoc = clang_getRangeEnd(range);
    unsigned int defline = 0;
    unsigned int defcolumn = 0;
    unsigned int defoffset = 0;
    CXFile ifile;

    if (node.Kind == CXCursorKind::CXCursor_ClassDecl)
    {
        CXCursor defcursor = clang_getCursorDefinition(cursor);
        CXSourceLocation defLoc = clang_getCursorLocation(defcursor);
        clang_getExpansionLocation(defLoc, &ifile, &defline, &defcolumn, &defoffset);
    }

    node.Linkage = clang_getCursorLinkage(cursor);
    CXFile file;
    unsigned int line;
    unsigned int column;
    unsigned int offset;

    clang_getExpansionLocation(srcLoc, &file, &line, &column, &offset);
    unsigned int endOffset;
    clang_getExpansionLocation(srcEndLoc, nullptr,
        nullptr, nullptr, &endOffset);

    if (defcolumn > 0)
    {
        //System.Diagnostics.Debug.WriteLine("{0} {1} -> {2} {3}", line, column,
        //     defcolumn, defcolumn);
        line = defcolumn;
        column = defcolumn;
        offset = defoffset;
    }

    CXType cxtype = clang_getCursorType(cursor);
    node.TypeKind = cxtype.kind;

    std::string tokenStr = Str(clang_getCursorSpelling(cursor));
    trim(tokenStr);

    node.tmpTokenString = tokenStr;
    std::string typeToken = Str(clang_getTypeSpelling(cxtype));
    trim(typeToken);
    node.tmpTypeTokenStr = typeToken;
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
    vc->allocNodes.push_back(Node());
    int64_t nodeIdx = vc->allocNodes.size();
    Node& node = vc->allocNodes.back();
    node.Kind = clang_getCursorKind(cursor);
    node.CompilingFile = vc->compilingFilePtr;
    if (node.Kind == CXCursorKind::CXCursor_FirstInvalid)
        return nullnode;
    CXSourceLocation loc = clang_getCursorLocation(cursor);
    CXFile outfile;
    unsigned int outline, outcol, outoffset;
    clang_getExpansionLocation(loc, &outfile, &outline, &outcol, &outoffset);
    std::string fileName = Str(clang_getFileName(outfile));
    if (fileName.empty())
        return nullnode;
    node.Line = outline;
    node.Column = outcol;
    node.ParentNodeIdx = parentIdx;
    node.StartOffset = outoffset;
    return nodeIdx;
}

void BaseNode::LogNodeInfo(VisitContextPtr vc, int64_t nodeIdx, std::string tag)
{
    Node &node = vc->allocNodes[nodeIdx];
    size_t depth = vc->curNode != nullptr ? vc->depth : 0;
    std::ostringstream strm;
    strm << std::endl << std::string(depth * 3, ' ') <<
        tag << " " << node.SourceFile->Name() << " " <<
        node.Line << " " << node.Column << " " << node.StartOffset <<
        " " << node.EndOffset << " " << node.Kind << " " << node.TypeKind <<
        " " << node.tmpTokenString << " " << node.Linkage;

    vc->LogTree(strm.str());
}

BaseNode::~BaseNode()
{
    numAlloc--;
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

        //vc->dolog = commitName == vc->compiledFile;
        if (vc->dolog)
            vc->LogTree("\nChange File: " + commitName);

        if (!commitName.empty())
        {
            vc->skipthisfile = (vc->pchFiles.find(commitName) != vc->pchFiles.end());

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
                vc->LogTree(strm.str());
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
    vc->allocNodes[nodeIdx].ReferencedIdx = BaseNode::NodeRefFromCursor(clang_getCursorReferenced(cursor), nodeIdx, vc);

    vc->nodesMap.insert(std::make_pair(cursor, nodeIdx));
   
    return vc->skipthisfile ? CXChildVisitResult::CXChildVisit_Continue : CXChildVisitResult::CXChildVisit_Recurse;

}