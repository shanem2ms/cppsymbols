#pragma once

#include <iostream>
#include <string>
#include <vector>
#include <fstream>
#include <atomic>
#include <map>
#include <set>
#include <sstream>
#include "clang-c/BuildSystem.h"
#include "clang-c/Index.h"



class CPPSourceFile;
typedef CPPSourceFile *CPPSourceFilePtr;
class VisitContext;
typedef VisitContext *VisitContextPtr;

#define nulltoken (-1)
struct Token
{
    int64_t Key;
    std::string Text;

    Token(int64_t key) : Key(key) { }
    ~Token() {  }
};
#define nullnode (-1)

class TypeNode
{
public:
    int64_t Key;
    CXTypeKind TypeKind;
    std::string tokenStr;
    int64_t tokenIdx;
    bool isConst;
    TypeNode* pNext;
    int64_t nextIdx;
    TypeNode() :
        TypeKind(CXType_Invalid),
        Key(nullnode),
        pNext(nullptr),
        isConst(false),
        nextIdx(nullnode)
    {}
};

std::ostream& operator<<(std::ostream& os, TypeNode const& m);

class BaseNode
{
public:
    int64_t Key;
    int64_t ParentNodeIdx;
    int64_t ReferencedIdx;
    int64_t token;
    int64_t TypeToken;
    CPPSourceFilePtr SourceFile;
    CPPSourceFilePtr CompilingFile;
    
    unsigned int Line;
    unsigned int Column;
    unsigned int StartOffset;
    unsigned int EndOffset;

    CXCursorKind Kind;
    int64_t TypeIdx;
    CXLinkageKind Linkage;
    CX_CXXAccessSpecifier AcessSpecifier;
    CX_StorageClass StorageClass;
    bool isAbstract;
public:
    BaseNode(int64_t key);
    ~BaseNode();

    uint64_t RefHash();
    void RemoveDuplicateNodes();

    /*
    void PrintTree(int level, StringBuilder sb)
    {
        sb.Append(new String(' ', level * 2) + ToString() + "\r\n");
        foreach(Node childnodes in Nodes)
        {
            childnodes.PrintTree(level + 1, sb);
        }
    }*/

    bool Equals(NodePtr node);
    std::string ToString();
    int CompareTo(NodePtr n);
    /*
    int GetHashCode()
    {
        unsigned long hashCode = 1035752329;
        hashCode = hashCode * -1521134295 + Kind->GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<Token>.Default.GetHashCode(Token);
        hashCode = hashCode * -1521134295 + Line.GetHashCode();
        hashCode = hashCode * -1521134295 + Column.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<CPPSourceFilePtr>.Default.GetHashCode(SourceFile);
        return hashCode;
    }*/

    static int64_t NodeFromCursor(CXCursor cursor, int64_t parentIdx,
        VisitContextPtr vc);
    static int64_t NodeRefFromCursor(CXCursor cursor, int64_t parentIdx, VisitContextPtr vc);
    static void LogNodeInfo(VisitContextPtr vc, int64_t node, std::string tag);
    static void LogTypeInfo(VisitContextPtr vc, std::ostringstream& strm, TypeNode *ptype);
    static CXChildVisitResult ClangVisitor(CXCursor cursor, CXCursor parent, CXClientData client_data);
    static TypeNode* TypeFromCxType(CXType cxtype, VisitContextPtr vc);
    static TypeNode *TypeFromCursor(CXCursor cursor, VisitContextPtr vc);
};

class Node : public BaseNode
{
public:
    std::string tmpTokenString;
    int32_t clangHash;
    bool isref;
    bool alive;
    Node* pParentPtr;
    Node* pRefPtr;
    TypeNode* pTypePtr;

    Node(int64_t key) : 
        BaseNode(key), 
        clangHash(0), 
        isref(false), 
        alive(true),
        pParentPtr(nullptr), 
        pRefPtr(nullptr),
        pTypePtr(nullptr) {}
};

inline std::string Str(CXString str)
{
    const char* cstr = clang_getCString(str);
    if (cstr == nullptr)
    {
        clang_disposeString(str);
        return std::string();
    }
    std::string retstr(cstr);
    clang_disposeString(str);
    return retstr;
}

class PrevFile
{
public:
    PrevFile(std::string n, NodePtr o)
    {
        name = n;
        node = o;
    }
    std::string name;
    NodePtr node;

};

inline bool operator ==(const PrevFile& pv1, const PrevFile& pv2)
{
    return pv1.name == pv2.name;
}

typedef PrevFile* PrevFilePtr;

inline bool operator < (const CXCursor &lhs, const CXCursor &rhs)
{
    return memcmp(&lhs, &rhs, sizeof(CXCursor)) < 0;
}

class VisitContext
{
public:
    //ProjectSettings ps;
    NodePtr curNode = nullptr;
    CPPSourceFilePtr curSourceFile = nullptr;
    std::map<std::string, std::set<std::string>> includedFiles;
    std::set<std::string> visitedFiles;
    CXFile prevFile = nullptr;
    std::string prevFileCommitName;
    std::set<std::string> newFiles;
    std::set<std::string> pchFiles;
    bool dolog = false;
    bool logthisfile = false;
    bool skipthisfile = false;
    std::string logTree;
    int depth = 0;
    bool parseAllProjFiles = false;
    std::vector<PrevFilePtr> fileStack;
    DbFilePtr dbFile = nullptr;
    std::string rootDir;
    std::string compiledFileF;
    CPPSourceFilePtr compilingFilePtr;
    std::map<CXCursor, int64_t> nodesMap;
    std::vector<Node> allocNodes;
    std::string isolateFile;
    std::string logFilterFile;

    void LogTree(const std::string& log)
    {
        logTree.append(log);
        std::cout << log;
    }
};
