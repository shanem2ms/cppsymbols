#pragma once

#include <iostream>
#include <string>
#include <vector>
#include <iostream>
#include <fstream>
#include <filesystem>
#include <atomic>
#include <map>
#include <set>
#include <sstream>
#include <mutex>
#include <ranges>
#include "cppstream.h"

class VSProject;
typedef VSProject* VSProjectPtr;
class CPPSourceFile;
typedef CPPSourceFile* CPPSourceFilePtr;
class IncludeNode;
typedef IncludeNode IncludeNodePtr;
struct Token;

class DbMgr
{
    std::mutex m_srcFileMutex;
    std::mutex m_dbMutex;
    std::map<std::string, CPPSourceFilePtr> m_sourceFiles;
    std::map<CPPSourceFilePtr, IncludeNode> cachedIncludeTree;
    std::map<std::string, TokenPtr> allTokenIds;
    std::mutex m_errorsMutex;
    std::vector<ErrorPtr> allErrors;
    std::vector<Node> m_nodes;
    std::string m_outfile;
    bool stgOnce;

public:
    CPPSourceFilePtr GetOrInsertFile(const std::string& commitName, const std::string& fileName);
    void AddPchFiles(const std::vector<std::string>& pchFiles);
    DbMgr(const std::string &outfile);
    template <class T> void AddRow(T node);
    template <class T> void AddRowsPtr(std::vector<T *>& range);
    template <class T> int64_t AddRows(std::vector<T>& range);
    void AddNodes(std::vector<Node>& range);
    template <class T> void UpdateRow(T node);
    static DbMgr* Instance();
    void AddErrors(const std::vector<ErrorPtr>& errors);
    void Initialize();
    void Optimize();
    bool NeedsCompile(const std::string& src) const;
};


struct DbCPPSourcefile
{
    int64_t key;
    std::string fullPath;
    long long modified;
    long long compiledTime;
};

struct DbNode
{
    int64_t key;
    int64_t compilingFile;
    int64_t parentNodeIdx;
    int64_t referencedIdx;
    CXCursorKind kind;
    CXTypeKind typeKind;
    int64_t token;
    int64_t typetoken;
    unsigned int line;
    unsigned int column;
    unsigned int startOffset;
    unsigned int endOffset;
    int64_t sourceFile;

    DbNode(const Node &);
};

struct DbToken : public CppStreamable
{
    int64_t key;
    std::string text;

    DbToken(int64_t _key, const std::string& _text) :
        key(_key),
        text(_text) {}

    void WriteBinaryData(ICppStreamWriter& data, void* pUserContext) const override
    {
        CppStream::Write(data, key);
        CppStream::Write(data, text);
    }

    size_t ReadBinaryData(const ICppStreamReader& data, size_t offset, void* pUserContext) override
    {
        offset = CppStream::Read(data, offset, key);
        offset = CppStream::Read(data, offset, text);
        return offset;
    }
};

struct DbError
{
    int64_t key;
    unsigned int line;
    unsigned int column;
    std::string description;
    int64_t file;
    int64_t compiledFile;
};

class DbHandle;
class DbFile
{
    std::map<std::string, CPPSourceFilePtr> m_sourceFiles;
    std::string m_dbfile;
    bool stgOnce;
    std::vector<DbNode> m_dbNodes;
    std::vector<DbToken> m_dbTokens;
    std::vector<DbError> m_dbErrors;
    std::vector<DbCPPSourcefile> m_dbSourceFiles;
public:
    DbFile(const std::string& outdbfile);
    void UpdateRow(CPPSourceFilePtr node);
    void AddRowsPtr(std::vector<ErrorPtr>& range);
    int64_t AddRows(std::vector<Token>& range);
    CPPSourceFilePtr GetOrInsertFile(const std::string& commitName, const std::string& fileName);
    void AddNodes(std::vector<Node>& range);
    void Save();
};