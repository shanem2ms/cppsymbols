#pragma once

#include <iostream>
#include <string>
#include <vector>
#include <fstream>
#include <atomic>
#include <map>
#include <set>
#include <sstream>
#include <chrono>
#include <functional>
#include "clang-c/BuildSystem.h"
#include "clang-c/Index.h"

class TypeNode;
class Compiler
{
    class Timer
    {
        std::chrono::steady_clock::time_point start;
        std::chrono::steady_clock::time_point end;
        std::string name;
    public:
        Timer(const std::string& n);
        ~Timer();
    };

public:
    struct ProjectCache
    {
        std::set<std::string> pchFiles;
        //std::mutex allNodesMtx;
    };

private:
    static Compiler* m_sInstance;

    CXTranslationUnit CompileInternal(const std::string& fname, 
        const std::string &outpath, const std::vector<std::string>& includes,
        const std::vector<std::string>& defines, const std::vector<std::string>& miscArgs, 
        ProjectCache &pc, bool buildPch, const std::string& usePch, 
        const std::string& rootdir, int loggingFlags);

    std::vector<std::string> GenerateCompileArgs(const std::string& fname,
        const std::string& outpath, const std::vector<std::string>& includes,
        const std::vector<std::string>& defines, const std::vector<std::string>& miscArgs,
        ProjectCache& pc, bool buildPch, const std::string& usePch, 
        const std::string& rootdir, int loggingFlags);

    void AddTypeNodes(std::function<size_t (const std::string &tokenStr)> addToken,
        std::vector<Node>& newNodes0,
        std::vector<TypeNode> &typeNodes);
public:
    static Compiler* Inst();

    std::vector<uint8_t> CompileWithArgs(const std::string& fname,        
        const std::vector<std::string>& args, int loggingFlags);

    std::vector<uint8_t> Compile(const std::string& fname,
        const std::string& outpath, const std::vector<std::string>& includes,
        const std::vector<std::string>& defines,
        const std::vector<std::string>& miscArgs,
        bool buildPch, const std::string& usePch, const std::string& rootdir, int loggingFlags);
};
