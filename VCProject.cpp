// symbols.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include "Precomp.h"
#include "CPPSourceFile.h"
#include "Compiler.h"
#include "DbMgr.h"
#include "Node.h"
#include "VCProject.h"
#include "pugixml.hpp"

namespace std
{// for string delimiter
    vector<string> split(string s, string delimiter) {
        size_t pos_start = 0, pos_end, delim_len = delimiter.length();
        string token;
        vector<string> res;

        while ((pos_end = s.find(delimiter, pos_start)) != string::npos) {
            token = s.substr(pos_start, pos_end - pos_start);
            pos_start = pos_end + delim_len;
            res.push_back(token);
        }

        res.push_back(s.substr(pos_start));
        return res;
    }
}

std::string GetPath(const std::filesystem::path& basePath, const std::string& relativePath)
{
    std::filesystem::path relpath = basePath;
    relpath.append(relativePath);
    std::filesystem::path abspath = std::filesystem::absolute(relpath);
    return abspath.string();
}

void VCProject::Parse(const std::filesystem::path& solutionDir)
{
    std::filesystem::path projectPath = solutionDir;
    projectPath.append(m_relativePath);
    pugi::xml_document doc;
    pugi::xml_parse_result result = doc.load_file(projectPath.string().c_str());

    pugi::xpath_node_set clrSupport =
        doc.select_nodes("/Project/PropertyGroup[@Condition=\"'$(Configuration)|$(Platform)'=='Release|x64'\"]/CLRSupport/text()");

    if (clrSupport.size() > 0 &&
        !strncmp(clrSupport[0].node().value(), "true", 4))
    {
        std::cout << "Skipping CLR project " << m_relativePath;
        return;
    }

    pugi::xpath_node_set includePaths =
        doc.select_nodes("/Project/PropertyGroup/IncludePath[@Condition=\"'$(Configuration)|$(Platform)'=='Release|x64'\"]/text()");

    pugi::xpath_node_set additionalIncludes = 
        doc.select_nodes("/Project/ItemDefinitionGroup[@Condition=\"'$(Configuration)|$(Platform)'=='Release|x64'\"]/ClCompile/AdditionalIncludeDirectories/text()");
    
    pugi::xpath_node_set precompHeaderFile =
        doc.select_nodes("/Project/ItemDefinitionGroup[@Condition=\"'$(Configuration)|$(Platform)'=='Release|x64'\"]/ClCompile/PrecompiledHeaderFile/text()");
    if (precompHeaderFile.size() > 0)
        m_precompHdrInc = precompHeaderFile[0].node().value();

    pugi::xpath_node_set additionalDefines =
        doc.select_nodes("/Project/ItemDefinitionGroup[@Condition=\"'$(Configuration)|$(Platform)'=='Release|x64'\"]/ClCompile/PreprocessorDefinitions/text()");
    if (m_additionalDefines.size() > 0)
        m_additionalDefines = std::split(additionalDefines[0].node().value(), ";");
    for (auto it = m_additionalDefines.begin(); it != m_additionalDefines.end();)
    {
        if (it->length() == 0 || (*it)[0] == '%')
            it = m_additionalDefines.erase(it);
        else
            ++it;
    }

    std::filesystem::path buildFolder = solutionDir;
    buildFolder /= "sym";
    m_buildFolder = buildFolder.string();
    std::filesystem::create_directories(m_buildFolder);

    std::vector<std::string> includes;
    if (includePaths.size() > 0)
    {
        auto inc = includePaths[0].node();
        std::vector<std::string> ip = std::split(inc.value(), ";");
        includes.insert(includes.end(), ip.begin(), ip.end());
    }
    std::filesystem::path projectDir = projectPath.parent_path();
    if (additionalIncludes.size() > 0)
    {
        auto inc = additionalIncludes[0].node();
        std::vector<std::string> ip = std::split(inc.value(), ";");
        includes.insert(includes.end(), ip.begin(), ip.end());
    }

    const char sdir[] = "$(SolutionDir)";
    const std::string ipathvar = "$(IncludePath)";

    for (auto inc : includes)
    {
        if (inc[0] == '.')
        {
            m_includes.push_back(GetPath(projectDir, inc));
        }
        else if (inc[0] == '%')
        { }
        else if (inc == ipathvar)
        { }
        else
        {
            size_t itFind = inc.find(sdir);
            if (itFind != std::string::npos)
            {
                std::filesystem::path dir = solutionDir;
                auto itsubstr = inc.substr(itFind + sizeof(sdir) - 1);
                while (itsubstr.length() > 0 && itsubstr[0] == '\\' ||
                    itsubstr[0] == '/')
                    itsubstr = itsubstr.substr(1);
                dir.append(itsubstr);
                std::filesystem::path abspath = std::filesystem::absolute(dir);
                m_includes.push_back(abspath.string());
            }
            else
                m_includes.push_back(inc);
        }
    }

    std::sort(m_includes.begin(), m_includes.end());
    m_includes.erase(std::unique(m_includes.begin(), m_includes.end()), m_includes.end());

    if (!m_precompHdrInc.empty())
    {
        for (auto inc : m_includes)
        {
            std::filesystem::path p(inc);
            p /= m_precompHdrInc;
            if (std::filesystem::exists(p))
            {
                p.replace_extension(".h.pch");
                m_precompBin = p.string();
                break;
            }
        }
    }

    pugi::xpath_node_set compiledFiles =
        doc.select_nodes("/Project/ItemGroup/ClCompile/@Include");
    for (auto compiledfile : compiledFiles)
    {
        auto f = compiledfile.attribute();
        m_sources.push_back(GetPath(projectDir, f.value()));
    }

    pugi::xpath_node_set precompFiles =
        doc.select_nodes("/Project/ItemGroup/ClCompile/PrecompiledHeader[@Condition=\"'$(Configuration)|$(Platform)'=='Release|x64'\" and text() = 'Create']");
    if (precompFiles.size() > 0)
    {
        pugi::xml_node n = precompFiles[0].node();
        std::string relPath = n.parent().attribute("Include").value();
        m_precompHdrSrc = GetPath(projectDir, relPath);
    }

}


// Add project level pch files cache
co::task<> VCProject::GenerateCompileList(const std::string& filter, bool doPrecomp, const std::string& rootdir)
{
    Compiler::ProjectCache pc;

    std::filesystem::path p(rootdir);
    p = p / "parsed";
    std::filesystem::create_directory(p);

    if (doPrecomp && !PrecompBin().empty())
    {

        if (DbMgr::Instance()->NeedsCompile(m_precompHdrSrc))
        {
            bool succeeded = co_await Compiler::Inst()->CompilePch(this, pc);

            if (!succeeded)
                co_return;
        }
    }

    std::string outPath = p.string();
    std::regex r(filter);
    std::vector<co::task<>> tasks;
    for (auto src : m_sources)
    {
        if ((filter.length() == 0 ||
            std::regex_match(src, r)) &&
            DbMgr::Instance()->NeedsCompile(src))
        {
            tasks.push_back(
                co::dispatch([this, src, doPrecomp, rootdir, &pc, &outPath]()
                    { return Compiler::Inst()->CompileSrc(this, src, outPath, rootdir, pc, doPrecomp, false); }));
        }
    }


    co_await co::when_all(std::move(tasks));

    co_return;
}

co::task<> VCProject::CompileAll(const std::string& filter, bool doPrecomp, const std::string &rootdir)
{
    Compiler::ProjectCache pc;

    std::filesystem::path p(rootdir);
    p = p / "parsed";
    std::filesystem::create_directory(p);

    if (doPrecomp && !PrecompBin().empty())
    {

        if (DbMgr::Instance()->NeedsCompile(m_precompHdrSrc))
        {
            bool succeeded = co_await Compiler::Inst()->CompilePch(this, pc);

            if (!succeeded)
                co_return;
        }
    }

    std::string outPath = p.string();
    std::regex r(filter);
    std::vector<co::task<>> tasks;
    for (auto src : m_sources)
    {
        if ((filter.length() == 0 ||
            std::regex_match(src, r)) &&
            DbMgr::Instance()->NeedsCompile(src))
        {
            tasks.push_back(
                co::dispatch([this, src, doPrecomp, rootdir, &pc, &outPath]()
                    { return Compiler::Inst()->CompileSrc(this, src, outPath, rootdir, pc, doPrecomp, false); }));
        }
    }
    

    co_await co::when_all(std::move(tasks));

    co_return;
}
