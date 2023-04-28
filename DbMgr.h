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
    std::string m_dbfile;
    std::vector<DbNode> m_dbNodes;
    std::vector<DbToken> m_dbTokens;
    std::vector<DbError> m_dbErrors;
    std::vector<DbCPPSourcefile> m_dbSourceFiles;
public:
    DbFile(const std::string& dbfile);
    void UpdateRow(CPPSourceFilePtr node);
    void AddRowsPtr(std::vector<ErrorPtr>& range);
    int64_t AddRows(std::vector<Token>& range);
    CPPSourceFilePtr GetOrInsertFile(const std::string& commitName, const std::string& fileName);
    void AddNodes(std::vector<Node>& range);
    void Save();
    void Load();
};
