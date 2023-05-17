#pragma once

#include <string>
#include <filesystem>
#include <mutex> 

class VSProject;
typedef VSProject* VSProjectPtr;
class CPPSourceFile;
typedef CPPSourceFile* CPPSourceFilePtr;
class IncludeNode;
typedef IncludeNode IncludeNodePtr;

class CPPSourceFile
{
public:
    std::string Name();
    static std::atomic<int64_t> nextKey;
    int64_t Key;
    std::string FullPath;
    int64_t nodeKey;
    VSProjectPtr Project;
    long long Modified;
    long long CompiledTime;
    bool IsDirty;
    size_t hash;

    CPPSourceFile();
    CPPSourceFile(std::string fullPath);
    size_t Hash() const;

    static std::string FormatPath(const std::string& filepath);
    static std::string FixPathSlashes(const std::string &filepath);
};


class IncludeNode
{
public:
    IncludeNode(std::string _f)
    {
        file = _f;
    }

    std::vector<IncludeNodePtr> parents;
    std::vector<IncludeNodePtr> children;
    std::string file;
};

inline bool operator== (const CPPSourceFile& lhs, const CPPSourceFile& rhs)
{
    return lhs.FullPath == rhs.FullPath;
}


class Error : public std::enable_shared_from_this<Error>
{
public:
    static std::atomic<int64_t> nextKey;
    int64_t Key;
    unsigned int Line;
    unsigned int Column;
    unsigned int Category;
    std::string Description;
    CPPSourceFilePtr File;
    CPPSourceFilePtr CompiledFile;

    std::string filePath;
    std::string compiledFilePath;
};