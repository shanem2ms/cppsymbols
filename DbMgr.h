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
class TypeNode;

struct DbCPPSourcefile
{
    int64_t key;
    std::string fullPath;
};

struct DbNode
{
    int64_t key;
    int64_t compilingFile;
    int64_t parentNodeIdx;
    int64_t referencedIdx;
    CXCursorKind kind;
    int32_t flags;
    int64_t typeIdx;
    int64_t token;
    unsigned int line;
    unsigned int column;
    unsigned int startOffset;
    unsigned int endOffset;
    int64_t sourceFile;

    size_t GetHashVal(size_t parentHashVal = 2166136261U) const;

    bool operator == (const DbNode& other) const;
    DbNode() {}
    DbNode(const Node &);
};

struct DbToken : public CppStreamable
{
    int64_t key;
    std::string text;

    DbToken() : key(0) {}

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

struct DbType : public CppStreamable
{
    int64_t key;
    int64_t hash;
    std::vector<int64_t> children;
    int64_t token;
    CXTypeKind kind;
    uint8_t isconst;

    DbType() :
        key(-1),
        token(-1),
        kind(CXType_Invalid),
        isconst(0)
    {}

    DbType(int64_t _key,
    int64_t _hash,
    const std::vector<int64_t> &_children,
    int64_t _token,
    CXTypeKind _kind,
    uint8_t _isconst) :
        key(_key),
        hash(_hash),
        children(_children),
        token(_token),
        kind(_kind),
        isconst(_isconst)
    {

    }
    void WriteBinaryData(ICppStreamWriter& data, void* pUserContext) const override
    {
        CppStream::Write(data, key);
        CppStream::Write(data, hash);
        CppStream::Write(data, children);
        CppStream::Write(data, token);
        CppStream::Write(data, kind);
        CppStream::Write(data, isconst);
    }

    size_t ReadBinaryData(const ICppStreamReader& data, size_t offset, void* pUserContext) override
    {
        offset = CppStream::Read(data, offset, key);
        offset = CppStream::Read(data, offset, hash);
        offset = CppStream::Read(data, offset, children);
        offset = CppStream::Read(data, offset, token);
        offset = CppStream::Read(data, offset, kind);
        offset = CppStream::Read(data, offset, isconst);
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

class CPPEXPORT DbFile
{
    std::map<std::string, CPPSourceFilePtr> m_sourceFiles;
    std::vector<DbNode> m_dbNodes;
    std::vector<DbToken> m_dbTokens;
    std::vector<DbType> m_dbTypes;
    std::vector<DbError> m_dbErrors;
    std::vector<std::string> m_dbSourceFiles;
public:
    DbFile();
    void UpdateRow(CPPSourceFilePtr node);
    void AddRowsPtr(std::vector<ErrorPtr>& range);
    int64_t AddRows(std::vector<Token>& range);
    int64_t AddRows(std::vector<TypeNode>& types);
    CPPSourceFilePtr GetOrInsertFile(const std::string& commitName, const std::string& fileName);
    void AddNodes(std::vector<Node>& range);
    void WriteStream(std::vector<uint8_t>& data);
    void CommitSourceFiles();
    void Save(const std::string& dbfile);
    void Load(const std::string& dbfile);
    void RemoveDuplicates();
    void Merge(const DbFile& other);
    size_t QueryNodes(const std::string& filename);
    void ConsoleDump();
};
