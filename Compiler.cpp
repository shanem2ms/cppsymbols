#include "Precomp.h"
#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"
#include "VCProject.h"
#include "Compiler.h"
#define FMT_HEADER_ONLY
#include "fmt/format.h"
#include <unordered_map>

Compiler *Compiler::m_sInstance = nullptr;

Compiler *Compiler::Inst()
{
    if (m_sInstance == nullptr)
        m_sInstance = new Compiler();
    return m_sInstance;
}

CXTranslationUnit Compiler::Compile(const std::string& fname, 
    const std::string &outpath, const std::vector<std::string>& includes,
    const std::vector<std::string>& defines, const std::vector<std::string> &miscArgs,
    bool buildPch, const std::string& pchfile, const std::string& rootdir, bool dolog)
{    
    ProjectCache projectCache;
    std::vector<std::string> clgargs =
        GenerateCompileArgs(fname, outpath, includes, defines,
            miscArgs,
            projectCache, buildPch, pchfile, rootdir, dolog);
  
    CXUnsavedFile unsavedFile;
    CXTranslationUnit translationUnit;
    CXIndex index = clang_createIndex(0, 0);
    const char** pargs = new const char* [clgargs.size()];
    for (size_t idx = 0; idx < clgargs.size(); ++idx)
    {
        pargs[idx] = clgargs[idx].c_str();
    }

    CXErrorCode errorCode =
        clang_parseTranslationUnit2(index, fname.c_str(), pargs, clgargs.size(), &unsavedFile, 0,
            (buildPch ? CXTranslationUnit_ForSerialization : 0),
            &translationUnit);
    clang_disposeIndex(index);

    if (errorCode != CXErrorCode::CXError_Success)
    {
        std::cout << "Error: " << fname << " " << errorCode << std::endl;
        return nullptr;
    }    

    unsigned int numDiagnostics = clang_getNumDiagnostics(translationUnit);
    std::vector<ErrorPtr> errors;
    unsigned int defaultDiag = clang_defaultDiagnosticDisplayOptions();
    for (unsigned int i = 0; i < numDiagnostics; ++i)
    {
        CXDiagnostic diagnostic = clang_getDiagnostic(translationUnit, i);
        ErrorPtr te = new Error();
        CXSourceLocation srcLoc = clang_getDiagnosticLocation(diagnostic);
        CXFile file;
        unsigned int line;
        unsigned int column;
        unsigned int offset;
        clang_getExpansionLocation(srcLoc, &file, &line, &column, &offset);
        te->Line = line;
        te->Column = column;
        CXString cxfilename = clang_getFileName(file);
        te->filePath = Str(cxfilename);
        te->compiledFilePath = fname;
        unsigned int category = clang_getDiagnosticCategory(diagnostic);
        te->Category = category;

        CXString cxspell = clang_getDiagnosticSpelling(diagnostic);
        te->Description = Str(cxspell);
        if (dolog)
        {
            std::cout << te->filePath << "[" << te->Line << "]: " << te->Description << std::endl;
        }
        errors.push_back(te);
        clang_disposeString(cxfilename);
        clang_disposeString(cxspell);
        clang_disposeDiagnostic(diagnostic);
    }
    std::filesystem::path destpath(outpath);
    CXCursor startCursor = clang_getTranslationUnitCursor(translationUnit);
    VisitContext* vc = new VisitContext();
    vc->pchFiles = std::set<std::string>();
    vc->dolog = dolog;
    vc->rootDir = rootdir;
    vc->compiledFileF = fname;
    vc->allocNodes.reserve(50000);
    vc->dbFile = new DbFile(destpath.string());
    vc->compilingFilePtr =
        vc->dbFile->GetOrInsertFile(CPPSourceFile::FormatPath(fname), vc->compiledFileF);

    {
        clang_visitChildren(startCursor, Node::ClangVisitor, vc);
    }

    vc->compilingFilePtr->CompiledTime = time(nullptr);

    try
    {
        vc->dbFile->UpdateRow(vc->compilingFilePtr);
    }
    catch (std::system_error& error)
    {        
        std::cout << error.what();
    }
    
    std::vector<Token> tokens;
    std::unordered_map<std::string, size_t> tokenMap;
    for (Node &node : vc->allocNodes)
    {
        {
            auto itToken = tokenMap.find(node.tmpTokenString);
            if (itToken == tokenMap.end())
            {
                itToken = tokenMap.insert(std::make_pair(node.tmpTokenString, tokens.size())).first;
                tokens.push_back(Token());
                tokens.back().Text = node.tmpTokenString;
            }
            node.token = itToken->second;
        }
        {
            auto itToken = tokenMap.find(node.tmpTypeTokenStr);
            if (itToken == tokenMap.end())
            {
                itToken = tokenMap.insert(std::make_pair(node.tmpTypeTokenStr, tokens.size())).first;
                tokens.push_back(Token());
                tokens.back().Text = node.tmpTypeTokenStr;
            }
            node.TypeToken = itToken->second;
        }
    }

    for (auto& e : errors)
    {
        e->CompiledFile = vc->compilingFilePtr;

        e->File =             
            vc->dbFile->GetOrInsertFile(CPPSourceFile::FormatPath(e->filePath), std::string());
    }
    int64_t tokenOffset = vc->dbFile->AddRows(tokens);
    for (Node& n : vc->allocNodes)
    {
        n.token = n.token == nulltoken ? 0 : n.token + tokenOffset;
        n.TypeToken = n.TypeToken == nulltoken ? 0 : n.TypeToken + tokenOffset;
    }
    vc->dbFile->AddRowsPtr(errors);
    for (auto& error : errors)
    {
        std::string erromsg = fmt::format("{}: [{}, {}] = {}", error->filePath, error->Line, error->Column, error->Description);
        std::cout << erromsg << std::endl;
    }
    if (errors.size() == 0)
    {
        vc->dbFile->AddNodes(vc->allocNodes);

        std::cout << "Nodes: " << vc->allocNodes.size() << std::endl;
        std::cout << "Tokens: " << tokens.size() << std::endl;
    }

    vc->dbFile->Save();
    delete vc;

    if (buildPch)
    {
        std::cout << "Saving " << pchfile << std::endl;
        CXSaveError saveError = (CXSaveError)clang_saveTranslationUnit(translationUnit, pchfile.c_str(), clang_defaultSaveOptions(translationUnit));
        if (saveError != CXSaveError::CXSaveError_None)
        {
            std::cout << "Save Error: " << saveError << std::endl;
        }
    }
    return translationUnit;
}

CXTranslationUnit Compiler::CompileInternal(const std::string& fname,
    const std::string& outpath, const std::vector<std::string>& includes,
    const std::vector<std::string>& defines, const std::vector<std::string>& miscArgs,
    ProjectCache& pc, bool buildPch, const std::string& pchfile, const std::string& rootdir, bool dolog)
{
    std::vector<std::string> clgargs =
        GenerateCompileArgs(fname, outpath, includes, defines, miscArgs,
            pc, buildPch, pchfile, rootdir, dolog);

    CXUnsavedFile unsavedFile;
    CXTranslationUnit translationUnit;
    CXIndex index = clang_createIndex(0, 0);
    const char** pargs = new const char* [clgargs.size()];
    for (size_t idx = 0; idx < clgargs.size(); ++idx)
    {
        pargs[idx] = clgargs[idx].c_str();
    }

    CXErrorCode errorCode =
        clang_parseTranslationUnit2(index, fname.c_str(), pargs, clgargs.size(), &unsavedFile, 0,
            (buildPch ? CXTranslationUnit_ForSerialization : 0),
            &translationUnit);
    clang_disposeIndex(index);

    if (errorCode != CXErrorCode::CXError_Success)
    {
        std::cout << "Error: " << fname << " " << errorCode << std::endl;
        return nullptr;
    }

    unsigned int numDiagnostics = clang_getNumDiagnostics(translationUnit);
    std::vector<ErrorPtr> errors;
    unsigned int defaultDiag = clang_defaultDiagnosticDisplayOptions();
    for (unsigned int i = 0; i < numDiagnostics; ++i)
    {
        CXDiagnostic diagnostic = clang_getDiagnostic(translationUnit, i);
        ErrorPtr te = new Error();
        CXSourceLocation srcLoc = clang_getDiagnosticLocation(diagnostic);
        CXFile file;
        unsigned int line;
        unsigned int column;
        unsigned int offset;
        clang_getExpansionLocation(srcLoc, &file, &line, &column, &offset);
        te->Line = line;
        te->Column = column;
        CXString cxfilename = clang_getFileName(file);
        te->filePath = Str(cxfilename);
        te->compiledFilePath = fname;
        unsigned int category = clang_getDiagnosticCategory(diagnostic);
        te->Category = category;

        CXString cxspell = clang_getDiagnosticSpelling(diagnostic);
        te->Description = Str(cxspell);
        if (dolog)
        {
            std::cout << te->filePath << "[" << te->Line << "]: " << te->Description << std::endl;
        }
        errors.push_back(te);
        clang_disposeString(cxfilename);
        clang_disposeString(cxspell);
        clang_disposeDiagnostic(diagnostic);
    }
    std::filesystem::path srcpath(fname);
    std::filesystem::path destpath(outpath);
    std::filesystem::path dbname = srcpath.filename().replace_extension("db");
    destpath = destpath / dbname;
    CXCursor startCursor = clang_getTranslationUnitCursor(translationUnit);
    VisitContext* vc = new VisitContext();
    vc->pchFiles = pc.pchFiles;
    vc->dolog = dolog;
    vc->rootDir = rootdir;
    vc->compiledFileF = fname;
    vc->allocNodes.reserve(50000);
    vc->dbFile = new DbFile(destpath.string());
    vc->compilingFilePtr =
        vc->dbFile->GetOrInsertFile(CPPSourceFile::FormatPath(fname), vc->compiledFileF);

    {
        clang_visitChildren(startCursor, Node::ClangVisitor, vc);
    }

    vc->compilingFilePtr->CompiledTime = time(nullptr);

    try
    {
        vc->dbFile->UpdateRow(vc->compilingFilePtr);
    }
    catch (std::system_error& error)
    {
        std::cout << error.what();
    }
    if (buildPch)
    {
        pc.pchFiles = vc->visitedFiles;
    }

    std::vector<Token> tokens;
    std::unordered_map<std::string, size_t> tokenMap;
    for (Node& node : vc->allocNodes)
    {
        {
            auto itToken = tokenMap.find(node.tmpTokenString);
            if (itToken == tokenMap.end())
            {
                itToken = tokenMap.insert(std::make_pair(node.tmpTokenString, tokens.size())).first;
                tokens.push_back(Token());
                tokens.back().Text = node.tmpTokenString;
            }
            node.token = itToken->second;
        }
        {
            auto itToken = tokenMap.find(node.tmpTypeTokenStr);
            if (itToken == tokenMap.end())
            {
                itToken = tokenMap.insert(std::make_pair(node.tmpTypeTokenStr, tokens.size())).first;
                tokens.push_back(Token());
                tokens.back().Text = node.tmpTypeTokenStr;
            }
            node.TypeToken = itToken->second;
        }
    }

    for (auto& e : errors)
    {
        e->CompiledFile = vc->compilingFilePtr;

        e->File =
            vc->dbFile->GetOrInsertFile(CPPSourceFile::FormatPath(e->filePath), std::string());
    }
    int64_t tokenOffset = vc->dbFile->AddRows(tokens);
    for (Node& n : vc->allocNodes)
    {
        n.token = n.token == nulltoken ? 0 : n.token + tokenOffset;
        n.TypeToken = n.TypeToken == nulltoken ? 0 : n.TypeToken + tokenOffset;
    }
    vc->dbFile->AddNodes(vc->allocNodes);
    vc->dbFile->AddRowsPtr(errors);

    std::cout << "Nodes: " << vc->allocNodes.size() << std::endl;
    std::cout << "Tokens: " << tokens.size() << std::endl;
    delete vc;
    return translationUnit;
}

bool Compiler::CompilePch(VCProjectPtr project, ProjectCache& pc)
{

    //std::cout << "Pch: " << project->PrecompSrc() << std::endl;
    CXTranslationUnit translationUnit = CompileInternal(project->PrecompSrc(), "", project->Includes(),
        project->AdditionalDefines(), std::vector<std::string>(), pc, true, "", "", false);
    if (translationUnit == nullptr)
        return false;

    std::cout << "Writing " << project->PrecompBin() << std::endl;
    CXSaveError saveError = (CXSaveError)clang_saveTranslationUnit(translationUnit, project->PrecompBin().c_str(), clang_defaultSaveOptions(translationUnit));
    if (saveError != CXSaveError::CXSaveError_None)
    {
        std::cout << "Save Error: " << saveError << std::endl;
    }
    clang_disposeTranslationUnit(translationUnit);
    return true;
}

bool Compiler::CompileSrc(VCProjectPtr project, const std::string &srcFile, 
    const std::string& outPath, const std::string& rootdir, ProjectCache& pc, bool doPrecomp, bool dolog) noexcept
{
    //std::cout << "  Src: " << srcFile << std::endl;    
    CXTranslationUnit translationUnit = CompileInternal(srcFile, outPath,
        project->Includes(), project->AdditionalDefines(), std::vector<std::string>(), pc, false, doPrecomp ? project->PrecompBin() : std::string(),
        rootdir, dolog);
    if (translationUnit == nullptr)
        return false;
    clang_disposeTranslationUnit(translationUnit);
    return true;
}

std::vector<std::string> Compiler::GenerateCompileArgs(const std::string& fname,
    const std::string& outpath, const std::vector<std::string>& includes,
    const std::vector<std::string>& defines, const std::vector<std::string>& miscArgs,
    ProjectCache& pc, bool buildPch, const std::string& pchfile, const std::string& rootdir, bool dolog)
{
    std::cout << "Compiling " << fname << std::endl;


    std::vector<std::string> vcincludes;

    vcincludes.insert(vcincludes.end(), includes.begin(), includes.end());

    std::vector<std::string> clgargsBk = {
                "-dI",
                "--no-warnings",
                "-g2",
                "-Wall",
                "-O0",
                "-fno-strict-aliasing",
                "-fno-omit-frame-pointer",
                "-fexceptions",
                "-fstack-protector",
                "-fno-short-enums",
                "-fms-compatibility",
                "-fms-extensions",
                "-fno-delayed-template-parsing",
                "-fsyntax-only",
                "-Wno-invalid-token-paste",
                "-Wno-c++11-narrowing" };

    std::vector<std::string> clgargs(miscArgs);
    clgargs.insert(clgargs.end(), clgargsBk.begin(), clgargsBk.end());
    for (auto define : defines)
    {
        clgargs.push_back(std::string("-D") + define);
    }
    for (auto incdir : vcincludes)
    {
        clgargs.push_back(std::string("-I") + incdir);
    }

    if (!pchfile.empty() && !buildPch)
    {
        clgargs.push_back("-include-pch");
        clgargs.push_back(pchfile);
    }

    for (const std::string& arg : clgargs)
    {
        std::cout << " " << arg;
    }
    std::cout << std::endl;

    return clgargs;
}


Compiler::Timer::Timer(const std::string& n)
{
    name = n;
    start = std::chrono::high_resolution_clock::now();
}

Compiler::Timer::~Timer()
{
    end = std::chrono::high_resolution_clock::now();
    auto visitTime = std::chrono::duration_cast<std::chrono::milliseconds>(end - start);
    float seconds = visitTime.count() / 1000.0f;
    //std::cout << name << ": " << seconds << "s" << std::endl;
}
