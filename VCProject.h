#pragma once


class VCProject
{
    std::string m_name;
    std::string m_relativePath;
    std::vector<std::string> m_includes;
    std::vector<std::string> m_sources;
    std::string m_precompHdrSrc;
    std::string m_precompHdrInc;
    std::vector<std::string> m_additionalDefines;
    std::string m_buildFolder;
    std::string m_precompBin;
    std::map<std::string, std::string> m_defines;

public:
    VCProject(const std::string& name, const std::string& path)
    {
        m_name = name;
        m_relativePath = path;
    }

    const std::string& Name() const { return m_name; }
    const std::vector<std::string>& Includes() { return m_includes; }
    const std::vector<std::string>& Sources() { return m_sources; }
    const std::string& PrecompSrc() { return m_precompHdrSrc; }
    const std::string& PrecompBin() { return m_precompBin; }
    const std::string& PrecompHdr() { return m_precompHdrInc; }
    const std::string& BuildFolder() { return m_buildFolder; }
    const std::vector<std::string> AdditionalDefines() { return m_additionalDefines; }
    void Parse(const std::filesystem::path&solutionDir);
    void CompileAll(const std::string &filter, bool doPrecomp, const std::string& rootdir);
    void GenerateCompileList(const std::string& filter, bool doPrecomp, const std::string& rootdir);
};