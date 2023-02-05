#pragma once

#include <iostream>
#include <string>
#include <vector>
#include <fstream>
#include <atomic>
#include <map>
#include <set>
#include <sstream>
#include "Shlobj_core.h"
#include "clang-c/BuildSystem.h"
#include "clang-c/Index.h"



class CPPSourceFile;
typedef CPPSourceFile *CPPSourceFilePtr;
class VisitContext;
typedef VisitContext *VisitContextPtr;
class DbMgr;
typedef DbMgr *DbMgrPtr;

#define nulltoken (-1)
struct Token
{
    static std::atomic<int64_t> nextKey;
    static std::atomic<int64_t> numAlloc;
    int64_t Key;
    std::string Text;

    Token() : Key(-1) { numAlloc++; }
    ~Token() { numAlloc--; }
};
#define nullnode (-1)

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
    CXTypeKind TypeKind;
    CXLinkageKind Linkage;
    static std::atomic<int64_t> nextKey;
    static std::atomic<int64_t> numAlloc;

public:
    BaseNode();
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
    static CXChildVisitResult ClangVisitor(CXCursor cursor, CXCursor parent, CXClientData client_data);
};

class Node : public BaseNode
{
public:
    std::string tmpTokenString;
    std::string tmpTypeTokenStr;
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


    void LogTree(const std::string& log)
    {
        logTree.append(log);
        std::cout << log;
    }
};
