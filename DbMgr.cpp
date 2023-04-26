#include "Precomp.h"
#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"
#include "cppstream.h"
#include "zlib.h"

bool g_fullDbRebuild = false;
bool g_doOptimizeOnStart = false;

static DbMgr* sDbMgr = nullptr;
DbMgr* DbMgr::Instance()
{
    return sDbMgr;
}

DbMgr::DbMgr(const std::string& outfile) :
    stgOnce(true),
    m_outfile(outfile)
{


}

template <class T, class U> void SetNextKey(U& storage)
{
    auto lastKey = storage.max(&T::Key);
    if (lastKey != nullptr)
        T::nextKey = (*lastKey + 1);
}

void DbMgr::Initialize()
{
    Node::nextKey = 0;
    Token::nextKey = 0;
    Error::nextKey = 0;
    CPPSourceFile::nextKey = 0;
}

CPPSourceFilePtr DbMgr::GetOrInsertFile(const std::string& commitName, const std::string& fileName)
{
    CPPSourceFilePtr sf = nullptr;
    {
        std::lock_guard l(m_srcFileMutex);
        auto itSrcFile = m_sourceFiles.find(commitName);
        if (itSrcFile == m_sourceFiles.end())
        {
            if (fileName.empty())
                return nullptr;

            sf = new CPPSourceFile(fileName);
            AddRow(sf);
            m_sourceFiles[commitName] = sf;
        }
        else
            sf = itSrcFile->second;
    }
    return sf;
}

template <class T> void DbMgr::AddRowsPtr(std::vector<T*>& range)
{
    throw;
    std::lock_guard l(m_dbMutex);
    for (T* t : range)
    {
        t->Key = T::nextKey++;
    }
}

template <class T> int64_t DbMgr::AddRows(std::vector<T>& range)
{
    throw;
    int64_t offset;
    std::lock_guard l(m_dbMutex);
    offset = T::nextKey;
    for (T& t : range)
    {
        t.Key = T::nextKey++;
    }

    return offset;
}


void DbMgr::AddNodes(std::vector<Node>& nodes)
{
    std::lock_guard l(m_dbMutex);
    int64_t offset = Node::nextKey;
    for (Node& t : nodes)
    {
        t.Key += offset;
        t.ParentNodeIdx = (t.ParentNodeIdx != nullnode) ? t.ParentNodeIdx + offset : 0;
        t.ReferencedIdx  = (t.ReferencedIdx != nullnode) ? t.ReferencedIdx + offset : 0;
        Node::nextKey++;
    }

    m_nodes.insert(m_nodes.end(), nodes.begin(), nodes.end());
}

template <class T> void DbMgr::AddRow(T node)
{
    std::lock_guard l(m_dbMutex);
}

template <class T> void DbMgr::UpdateRow(T node)
{
    std::lock_guard l(m_dbMutex);
}

void DbMgr::AddErrors(const std::vector<ErrorPtr>& errors)
{
    m_errorsMutex.lock();
    allErrors.insert(allErrors.end(), errors.begin(), errors.end());
    m_errorsMutex.unlock();
}

bool DbMgr::NeedsCompile(const std::string& src) const
{
    auto itPrecomp = m_sourceFiles.find(CPPSourceFile::FormatPath(src));
    return (itPrecomp == m_sourceFiles.end() ||
        itPrecomp->second->CompiledTime <
        itPrecomp->second->Modified);
}

template void DbMgr::AddRow(CPPSourceFilePtr node);
template void DbMgr::UpdateRow(CPPSourceFilePtr node);
template void DbMgr::AddRowsPtr(std::vector<ErrorPtr>& range);
template int64_t DbMgr::AddRows(std::vector<Token>& range);

CPPSourceFilePtr DbFile::GetOrInsertFile(const std::string& commitName, const std::string& fileName)
{
    CPPSourceFilePtr sf = nullptr;
    {
        auto itSrcFile = m_sourceFiles.find(commitName);
        if (itSrcFile == m_sourceFiles.end())
        {
            if (fileName.empty())
                return nullptr;

            sf = new CPPSourceFile(fileName);
            m_sourceFiles[commitName] = sf;
        }
        else
            sf = itSrcFile->second;
    }
    return sf;
}

DbNode::DbNode(const Node &n) :
    key(n.Key),
    compilingFile(n.CompilingFile->Key),
    parentNodeIdx(n.ParentNodeIdx),
    referencedIdx(n.ReferencedIdx),
    kind(n.Kind),
    typeKind(n.TypeKind),
    token(n.token),
    typetoken(n.TypeToken),
    line(n.Line),
    column(n.Column),
    startOffset(n.StartOffset),
    endOffset(n.EndOffset),
    sourceFile(n.SourceFile->Key)
{

}
void DbFile::AddNodes(std::vector<Node>& nodes)
{
    size_t idx = 0;
    for (Node& t : nodes)
    {
        if (t.SourceFile == nullptr)
        {
            idx++;
            continue;
        }
        t.Key = m_dbNodes.size();
        t.ParentNodeIdx = (t.ParentNodeIdx != nullnode) ? t.ParentNodeIdx : 0;
        t.ReferencedIdx = (t.ReferencedIdx != nullnode) ? t.ReferencedIdx : 0;
        m_dbNodes.push_back(DbNode(t));
    }    
}


DbFile::DbFile(const std::string& outdbfile) :
    m_dbfile(outdbfile)
{
}


void DbFile::UpdateRow(CPPSourceFilePtr node)
{

}

void DbFile::AddRowsPtr(std::vector<ErrorPtr>& range)
{

}

int64_t DbFile::AddRows(std::vector<Token>& range)
{
    size_t keyIdx = 0;
    m_dbTokens.reserve(m_dbTokens.size() + range.size());
    for (Token& token : range)
    {        
        m_dbTokens.push_back(DbToken(keyIdx++, token.Text));
    }
    return 0;
}

void DbFile::Save()
{
    std::vector<uint8_t> data;
    int64_t maxFileKey = 0;
    for (const auto& kv : m_sourceFiles)
    {
        maxFileKey = std::max(kv.second->Key, maxFileKey);
    }
    std::vector<std::string> orderedSourceFiles(maxFileKey + 1);

    for (const auto& kv : m_sourceFiles)
    {
        orderedSourceFiles[kv.second->Key] = kv.second->FullPath;
    }
    CppVecStreamWriter vecWriter(data);
    CppStream::Write(vecWriter, orderedSourceFiles);
    CppStream::Write(vecWriter, m_dbTokens);
    CppStream::Write(vecWriter, m_dbNodes);


    // Decoded data size (in bytes).
    const uLongf decodedCnt = (uLongf)data.size();

    static_assert(sizeof(uLongf) == sizeof(uint32_t));
    std::vector<uint8_t> compressedData(data.size() + sizeof(uint32_t));
    memcpy(compressedData.data(), &decodedCnt, sizeof(uint32_t));
    // Encoded data size (in bytes).
    uLongf encodedCnt = (uLongf)compressedData.size();

    int compressStatus = compress2(
        reinterpret_cast<Bytef*>(compressedData.data()),
        &encodedCnt,
        reinterpret_cast<const Bytef*>(data.data() + sizeof(uint32_t)),
        decodedCnt,
        Z_DEFAULT_COMPRESSION);

    std::ofstream ofstream(m_dbfile, std::ios::out | std::ios::binary);
    ofstream.write((const char*)compressedData.data(), encodedCnt + sizeof(uint32_t));
    ofstream.close();
}