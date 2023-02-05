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
#include "Shlobj_core.h"
#include "clang-c/BuildSystem.h"
#include "clang-c/Index.h"
#include <cppcoro/task.hpp>
namespace co = cppcoro;

class VCProject;
typedef VCProject* VCProjectPtr;

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
        std::mutex allNodesMtx;
    };

private:
    static Compiler* m_sInstance;

    CXTranslationUnit CompileInternal(const std::string& fname, 
        const std::string &outpath, const std::vector<std::string>& includes,
        const std::vector<std::string>& defines, ProjectCache &pc,
        bool buildPch, const std::string& usePch, const std::string& rootdir, bool dolog);

    std::vector<std::string> GenerateCompileArgs(const std::string& fname,
        const std::string& outpath, const std::vector<std::string>& includes,
        const std::vector<std::string>& defines, ProjectCache& pc,
        bool buildPch, const std::string& usePch, const std::string& rootdir, bool dolog);

public:
    static Compiler* Inst();

    co::task<bool> CompilePch(VCProjectPtr project, ProjectCache& pc);
    co::task<bool> CompileSrc(VCProjectPtr project, const std::string &srcFile, 
        const std::string &outpath, 
        const std::string &rootdir, ProjectCache& pc, bool doPrecomp,
        bool dolog) noexcept;
    CXTranslationUnit Compile(const std::string& fname,
        const std::string& outpath, const std::vector<std::string>& includes,
        const std::vector<std::string>& defines,
        bool buildPch, const std::string& usePch, const std::string& rootdir, bool dolog);
};
