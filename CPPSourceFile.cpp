#include "Precomp.h"
#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"



std::atomic<int64_t> CPPSourceFile::nextKey(1);
std::atomic<int64_t> Error::nextKey(1);


CPPSourceFile::CPPSourceFile()
{
    IsDirty = true;
}

#if defined ( _WIN32 )
#include <sys/stat.h>
#endif


size_t CPPSourceFile::Hash() const
{
    return hash;
}

std::time_t GetFileWriteTime(const std::filesystem::path& filename)
{
#if defined ( _WIN32 )
    {
        struct _stat64 fileInfo;
        if (_wstati64(filename.wstring().c_str(), &fileInfo) != 0)
        {
            throw std::runtime_error("Failed to get last write time.");
        }
        return fileInfo.st_mtime;
    }
#else
    {
        auto fsTime = std::filesystem::last_write_time(filename);
        return decltype (fsTime)::clock::to_time_t(fsTime);
    }
#endif
}

CPPSourceFile::CPPSourceFile(std::string fullPath)
{
    FullPath = CPPSourceFile::FixPathSlashes(fullPath);
    std::filesystem::path p(FullPath);
    std::time_t cftime = GetFileWriteTime(p);
    Modified = cftime;
    Key = nextKey++;
    hash = std::hash<std::string>{}(FullPath);
    IsDirty = true;
}

std::string CPPSourceFile::Name() {
    return std::filesystem::path(FullPath).filename().string();
}

std::string CPPSourceFile::FormatPath(std::string filepath)
{
    if (filepath.empty())
        return std::string();

    std::transform(filepath.begin(), filepath.end(), filepath.begin(), [](unsigned char c) { return std::tolower(c); });
    return FixPathSlashes(filepath);
}

std::string CPPSourceFile::FixPathSlashes(std::string filepath)
{
    std::replace(filepath.begin(), filepath.end(), '/', '\\');
    size_t idx = filepath.find("\\\\");
    while (idx != std::string::npos)
    {
        filepath.replace(filepath.begin() + idx, filepath.begin() + idx + 2, "\\");
        idx = filepath.find("\\\\");
    }
    return filepath;
}

