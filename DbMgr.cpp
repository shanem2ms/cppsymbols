#include "Precomp.h"
#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"
#include "sqlite3.h"
#include "sqlite_orm/sqlite_orm.h"

using namespace sqlite_orm;

namespace sqlite_orm {
    template<>
    struct statement_binder<CXCursorKind> {

        int bind(sqlite3_stmt* stmt, int index, const CXCursorKind& value) {
            return statement_binder<int>().bind(stmt, index, (int)value);
            //  or return sqlite3_bind_text(stmt, index++, GenderToString(value).c_str(), -1, SQLITE_TRANSIENT);
        }
    };

    template<>
    struct type_printer<CXCursorKind> : public text_printer {};


    template<>
    struct field_printer<CXCursorKind> {
        std::string operator()(const CXCursorKind& t) const {
            return std::to_string((int)t);
        }
    };

    template<>
    struct row_extractor<CXCursorKind> {
        CXCursorKind extract(const char* row_value) {
            return (CXCursorKind)std::stoi(row_value);
        }

        CXCursorKind extract(sqlite3_stmt* stmt, int columnIndex) {
            auto cStr = (const char*)sqlite3_column_text(stmt, columnIndex);
            return this->extract((const char*)cStr);
        }
    };


    template<>
    struct statement_binder<CXTypeKind> {

        int bind(sqlite3_stmt* stmt, int index, const CXTypeKind& value) {
            return statement_binder<int>().bind(stmt, index, (int)value);
            //  or return sqlite3_bind_text(stmt, index++, GenderToString(value).c_str(), -1, SQLITE_TRANSIENT);
        }
    };

    template<>
    struct type_printer<CXTypeKind> : public text_printer {};


    template<>
    struct field_printer<CXTypeKind> {
        std::string operator()(const CXTypeKind& t) const {
            return std::to_string((int)t);
        }
    };

    template<>
    struct row_extractor<CXTypeKind> {
        CXTypeKind extract(const char* row_value) {
            return (CXTypeKind)std::stoi(row_value);
        }

        CXTypeKind extract(sqlite3_stmt* stmt, int columnIndex) {
            auto cStr = (const char*)sqlite3_column_text(stmt, columnIndex);
            return this->extract((const char*)cStr);
        }
    };


    template<>
    struct statement_binder<BNodePtr> {

        int bind(sqlite3_stmt* stmt, int index, const BNodePtr& value) {
            return statement_binder<uint64_t>().bind(stmt, index,
                (uint64_t)(value != nullptr ? value->Key : 0));
            //  or return sqlite3_bind_text(stmt, index++, GenderToString(value).c_str(), -1, SQLITE_TRANSIENT);
        }
    };

    template<>
    struct type_printer<BNodePtr> : public text_printer {};


    template<>
    struct field_printer<BNodePtr> {
        std::string operator()(const BNodePtr& t) const {
            return std::to_string(t->Key);
        }
    };

    template<>
    struct row_extractor<BNodePtr> {
        BNodePtr extract(const char* row_value) {
            return (BNodePtr)std::stoull(row_value);
        }

        BNodePtr extract(sqlite3_stmt* stmt, int columnIndex) {
            auto cStr = (const char*)sqlite3_column_text(stmt, columnIndex);
            return this->extract((const char*)cStr);
        }
    };

    template<>
    struct statement_binder<CPPSourceFilePtr> {

        int bind(sqlite3_stmt* stmt, int index, const CPPSourceFilePtr& value) {
            return statement_binder<uint64_t>().bind(stmt, index,
                (uint64_t)(value != nullptr ? value->Key : 0));
            //  or return sqlite3_bind_text(stmt, index++, GenderToString(value).c_str(), -1, SQLITE_TRANSIENT);
        }
    };

    template<>
    struct type_printer<CPPSourceFilePtr> : public text_printer {};



    template<>
    struct field_printer<CPPSourceFilePtr> {
        std::string operator()(const CPPSourceFilePtr& t) const {
            return std::to_string(t->Key);
        }
    };

    template<>
    struct row_extractor<CPPSourceFilePtr> {
        CPPSourceFilePtr extract(const char* row_value) {
            return (CPPSourceFilePtr)std::stoull(row_value);
        }

        CPPSourceFilePtr extract(sqlite3_stmt* stmt, int columnIndex) {
            auto cStr = (const char*)sqlite3_column_text(stmt, columnIndex);
            return this->extract((const char*)cStr);
        }
    };

    template<>
    struct statement_binder<TokenPtr> {

        int bind(sqlite3_stmt* stmt, int index, const TokenPtr& value) {
            return statement_binder<uint64_t>().bind(stmt, index, (uint64_t)(value != nullptr ? value->Key : 0));
            //  or return sqlite3_bind_text(stmt, index++, GenderToString(value).c_str(), -1, SQLITE_TRANSIENT);
        }
    };

    template<>
    struct type_printer<TokenPtr> : public text_printer {};


    template<>
    struct field_printer<TokenPtr> {
        std::string operator()(const TokenPtr& t) const {
            return std::to_string(t->Key);
        }
    };

    template<>
    struct row_extractor<TokenPtr> {
        TokenPtr extract(const char* row_value) {
            return (TokenPtr)std::stoull(row_value);
        }

        TokenPtr extract(sqlite3_stmt* stmt, int columnIndex) {
            auto cStr = (const char*)sqlite3_column_text(stmt, columnIndex);
            return this->extract((const char*)cStr);
        }
    };
}

bool g_fullDbRebuild = false;
bool g_doOptimizeOnStart = false;

auto& DbMgr::GetStorage(const std::string &dbname)
{
    
    using namespace sqlite_orm;

    static bool dbExists = std::filesystem::exists(dbname);
    if (stgOnce && dbExists && g_fullDbRebuild)
    {
        std::filesystem::remove(dbname);
    }

    static auto storage = make_storage(dbname,
        make_table("CPPSourceFile",
            make_column("Key", &CPPSourceFile::Key, primary_key()),
            make_column("FullPath", &CPPSourceFile::FullPath),
            make_column("Modified", &CPPSourceFile::Modified),
            make_column("CompiledTime", &CPPSourceFile::CompiledTime)),
        make_table("Node",
            make_column("Key", &BaseNode::Key, primary_key()),
            make_column("CompilingFile", &BaseNode::CompilingFile),
            make_column("ParentNode", &BaseNode::ParentNodeIdx),
            make_column("Referenced", &BaseNode::ReferencedIdx),
            make_column("Kind", &BaseNode::Kind),
            make_column("TypeKind", &BaseNode::TypeKind),
            make_column("Token", &BaseNode::token),
            make_column("TypeToken", &BaseNode::TypeToken),
            make_column("Line", &BaseNode::Line),
            make_column("Column", &BaseNode::Column),
            make_column("StartOffset", &BaseNode::StartOffset),
            make_column("EndOffset", &BaseNode::EndOffset),
            make_column("SourceFile", &BaseNode::SourceFile)),
        make_table("Token",
            make_column("Key", &Token::Key, primary_key()),
            make_column("Text", &Token::Text)),
        make_table("Error",
            make_column("Key", &Error::Key, primary_key()),
            make_column("Line", &Error::Line),
            make_column("Column", &Error::Column),
            make_column("Category", &Error::Category),
            make_column("Description", &Error::Description),
            make_column("File", &Error::File),
            make_column("CompiledFile", &Error::CompiledFile))
    );

    if (stgOnce && (!dbExists || g_fullDbRebuild))
    {
        storage.sync_schema();
        g_fullDbRebuild = false;
    }

    stgOnce = false;
    return storage;
}

static DbMgr* sDbMgr = nullptr;
DbMgr* DbMgr::Instance()
{
    if (sDbMgr == nullptr)
        sDbMgr = new DbMgr();
    return sDbMgr;
}
std::string DbMgr::dbname("vqclg.sqlite");
DbMgr::DbMgr() :
    stgOnce(true)
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
    if (!g_fullDbRebuild && g_doOptimizeOnStart)
    {
        DbMgr::Instance()->Optimize();
    }

    auto storage = GetStorage(DbMgr::dbname);
    SetNextKey<Node>(storage);
    SetNextKey<Token>(storage);
    SetNextKey<CPPSourceFile>(storage);
    SetNextKey<Error>(storage);

    std::vector<CPPSourceFile> sourcefiles = storage.get_all<CPPSourceFile>();
    for (const CPPSourceFile& sf : sourcefiles)
    {
        CPPSourceFile* psf = new CPPSourceFile(sf);
        m_sourceFiles.insert(std::make_pair(
            CPPSourceFile::FormatPath(sf.FullPath), psf));
    }

    //Optimize();
}


void DbMgr::Optimize()
{
    auto storage = GetStorage(DbMgr::dbname);

    std::vector<Token> tokens = storage.get_all<Token>();
    std::map<std::string, std::vector<int64_t>> tkmap;

    for (Token& tk : tokens)
    {
        auto ittk = tkmap.find(tk.Text);
        if (ittk == tkmap.end())
            ittk = tkmap.insert(std::make_pair(tk.Text, std::vector<int64_t>())).first;
        ittk->second.push_back(tk.Key);
    }

    std::vector<Token> newTokens;
    for (auto& pair : tkmap)
    {
        std::string txt = tokens[pair.second[0] - 1].Text;
        for (size_t idx = 0; idx < pair.second.size(); ++idx)
        {
            tokens[pair.second[idx] - 1].Key = newTokens.size() + 1;
        }
        newTokens.push_back(tokens[pair.second[0] - 1]);
    }

    std::vector<CPPSourceFile> sourcefiles = storage.get_all<CPPSourceFile>();

    std::vector<BaseNode> nodes = storage.get_all<BaseNode>();
    for (BaseNode& n : nodes)
    {
        int64_t tkey = (int64_t)n.token;
        n.token = tkey > 0 ? tkey - 1 : nulltoken;
        int64_t ttkey = (int64_t)n.TypeToken;
        n.TypeToken = ttkey > 0 ? ttkey - 1 : nulltoken;
        int64_t sfkey = (int64_t)n.SourceFile;
        n.SourceFile = sfkey > 0 ? &sourcefiles[sfkey - 1] : nullptr;
        int64_t cfkey = (int64_t)n.CompilingFile;
        n.CompilingFile = cfkey > 0 ? &sourcefiles[cfkey - 1] : nullptr;
    }

    try
    {
        storage.remove_all<BaseNode>();
        storage.begin_transaction();
        for (BaseNode& n : nodes)
        {
            storage.insert(n);
        }
        storage.commit();

        storage.remove_all<Token>();
        storage.begin_transaction();
        for (Token& t : newTokens)
        {
            storage.insert(t);
        }
        storage.commit();
    }
    catch (std::system_error& error)
    {
        std::cout << error.code().message();
    }
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
    std::lock_guard l(m_dbMutex);
    for (T* t : range)
    {
        t->Key = T::nextKey++;
    }
    auto& storage = GetStorage(DbMgr::dbname);
    storage.begin_transaction();
    for (T* t : range)
    {
        storage.insert(*t);
    }
    storage.commit();
}

template <class T> int64_t DbMgr::AddRows(std::vector<T>& range)
{
    int64_t offset;
    std::lock_guard l(m_dbMutex);
    offset = T::nextKey;
    for (T& t : range)
    {
        t.Key = T::nextKey++;
    }
    auto& storage = GetStorage(DbMgr::dbname);
    storage.begin_transaction();
    for (size_t idx = 0; idx < range.size(); idx += 1000)
    {
        size_t endIdx = std::min(idx + 1000, range.size());       
        storage.insert_range(range.begin() + idx, range.begin() + endIdx);
    }
    storage.commit();

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
    auto& storage = GetStorage(DbMgr::dbname);
    storage.begin_transaction();
    for (size_t idx = 0; idx < nodes.size(); idx += 1000)
    {
        size_t endIdx = std::min(idx + 1000, nodes.size());
        std::vector<BaseNode> bn(nodes.begin() + idx, nodes.begin() + endIdx);
        storage.insert_range(bn.begin(), bn.end());
    }
    storage.commit();
}

template <class T> void DbMgr::AddRow(T node)
{
    std::lock_guard l(m_dbMutex);
    auto& storage = GetStorage(DbMgr::dbname);
    storage.insert(*node);
}

template <class T> void DbMgr::UpdateRow(T node)
{
    std::lock_guard l(m_dbMutex);
    auto& storage = GetStorage(DbMgr::dbname);
    storage.update(*node);
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
    m_dbfile(outdbfile),
    m_ofstream(outdbfile, std::ios::out | std::ios::binary)
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
    return 0;
}
