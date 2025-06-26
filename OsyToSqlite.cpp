#include "Precomp.h"
#include "OsyToSqlite.h"
#include <iostream>
#include <unordered_map>

extern std::unordered_map<CXCursorKind, std::string> sCursorKindMap;
extern std::unordered_map<CXTypeKind, std::string> sTypeKindMap;

OsyToSqlite::OsyToSqlite() : m_db(nullptr) {}

OsyToSqlite::~OsyToSqlite()
{
    CloseDatabase();
}

bool OsyToSqlite::OpenDatabase(const std::string& dbPath)
{
    if (sqlite3_open(dbPath.c_str(), &m_db) != SQLITE_OK)
    {
        std::cerr << "Can't open database: " << sqlite3_errmsg(m_db) << std::endl;
        return false;
    }
    return true;
}

void OsyToSqlite::CloseDatabase()
{
    if (m_db)
    {
        sqlite3_close(m_db);
        m_db = nullptr;
    }
}

bool OsyToSqlite::CreateTables()
{
    const char* createSourceFilesTable =
        "CREATE TABLE IF NOT EXISTS SourceFiles ("
        "Id INTEGER PRIMARY KEY,"
        "Path TEXT NOT NULL);";

    const char* createTokensTable =
        "CREATE TABLE IF NOT EXISTS Tokens ("
        "Id INTEGER PRIMARY KEY,"
        "Text TEXT NOT NULL);";

    const char* createKindsTable =
        "CREATE TABLE IF NOT EXISTS Kinds ("
        "Id INTEGER PRIMARY KEY,"
        "Name TEXT NOT NULL UNIQUE);";

    const char* createTypesTable =
        "CREATE TABLE IF NOT EXISTS Types ("
        "Id INTEGER PRIMARY KEY,"
        "Hash INTEGER NOT NULL,"
        "TokenId INTEGER,"
        "Kind TEXT,"
        "IsConst INTEGER);";

    const char* createTypeChildrenTable =
        "CREATE TABLE IF NOT EXISTS TypeChildren ("
        "TypeId INTEGER,"
        "ChildId INTEGER,"
        "FOREIGN KEY(TypeId) REFERENCES Types(Id),"
        "FOREIGN KEY(ChildId) REFERENCES Types(Id));";

    const char* createNodesTable =
        "CREATE TABLE IF NOT EXISTS Nodes ("
        "Id INTEGER PRIMARY KEY,"
        "CompilingFileId INTEGER,"
        "ParentId INTEGER,"
        "ReferencedId INTEGER,"
        "KindId INTEGER,"
        "Flags INTEGER,"
        "TypeId INTEGER,"
        "TokenId INTEGER,"
        "Line INTEGER,"
        "Column INTEGER,"
        "StartOffset INTEGER,"
        "EndOffset INTEGER,"
        "SourceFileId INTEGER,"
        "FOREIGN KEY(CompilingFileId) REFERENCES SourceFiles(Id),"
        "FOREIGN KEY(ParentId) REFERENCES Nodes(Id),"
        "FOREIGN KEY(ReferencedId) REFERENCES Nodes(Id),"
        "FOREIGN KEY(KindId) REFERENCES Kinds(Id),"
        "FOREIGN KEY(TypeId) REFERENCES Types(Id),"
        "FOREIGN KEY(TokenId) REFERENCES Tokens(Id),"
        "FOREIGN KEY(SourceFileId) REFERENCES SourceFiles(Id));";

    char* errMsg = nullptr;
    if (sqlite3_exec(m_db, createSourceFilesTable, 0, 0, &errMsg) != SQLITE_OK ||
        sqlite3_exec(m_db, createTokensTable, 0, 0, &errMsg) != SQLITE_OK ||
        sqlite3_exec(m_db, createKindsTable, 0, 0, &errMsg) != SQLITE_OK ||
        sqlite3_exec(m_db, createTypesTable, 0, 0, &errMsg) != SQLITE_OK ||
        sqlite3_exec(m_db, createTypeChildrenTable, 0, 0, &errMsg) != SQLITE_OK ||
        sqlite3_exec(m_db, createNodesTable, 0, 0, &errMsg) != SQLITE_OK)
    {
        std::cerr << "SQL error: " << errMsg << std::endl;
        sqlite3_free(errMsg);
        return false;
    }
    return true;
}

bool OsyToSqlite::InsertSourceFiles()
{
    const char* insertSql = "INSERT INTO SourceFiles (Id, Path) VALUES (?, ?);";
    sqlite3_stmt* stmt;
    if (sqlite3_prepare_v2(m_db, insertSql, -1, &stmt, nullptr) != SQLITE_OK) return false;

    const auto& sourceFiles = m_dbFile.GetSourceFiles();
    for (size_t i = 0; i < sourceFiles.size(); ++i)
    {
        sqlite3_bind_int64(stmt, 1, i + 1);
        sqlite3_bind_text(stmt, 2, sourceFiles[i].c_str(), -1, SQLITE_STATIC);
        if (sqlite3_step(stmt) != SQLITE_DONE)
        {
            std::cerr << "Failed to insert source file." << std::endl;
            sqlite3_finalize(stmt);
            return false;
        }
        sqlite3_reset(stmt);
    }
    sqlite3_finalize(stmt);
    return true;
}

bool OsyToSqlite::InsertTokens()
{
    const char* insertSql = "INSERT INTO Tokens (Id, Text) VALUES (?, ?);";
    sqlite3_stmt* stmt;
    if (sqlite3_prepare_v2(m_db, insertSql, -1, &stmt, nullptr) != SQLITE_OK) return false;

    for (const auto& token : m_dbFile.GetTokens())
    {
        sqlite3_bind_int64(stmt, 1, token.key);
        sqlite3_bind_text(stmt, 2, token.text.c_str(), -1, SQLITE_STATIC);
        if (sqlite3_step(stmt) != SQLITE_DONE)
        {
            std::cerr << "Failed to insert token." << std::endl;
            sqlite3_finalize(stmt);
            return false;
        }
        sqlite3_reset(stmt);
    }
    sqlite3_finalize(stmt);
    return true;
}

void OsyToSqlite::BuildKindMapping()
{
    m_kindToIdMap.clear();
    int64_t id = 1;
    for (const auto& node : m_dbFile.GetNodes())
    {
        if (m_kindToIdMap.find(node.kind) == m_kindToIdMap.end())
        {
            m_kindToIdMap[node.kind] = id++;
        }
    }
}

bool OsyToSqlite::InsertKinds()
{
    const char* insertSql = "INSERT INTO Kinds (Id, Name) VALUES (?, ?);";
    sqlite3_stmt* stmt;
    if (sqlite3_prepare_v2(m_db, insertSql, -1, &stmt, nullptr) != SQLITE_OK) return false;

    for (const auto& pair : sCursorKindMap)
    {
        sqlite3_bind_int64(stmt, 1, pair.first);
        sqlite3_bind_text(stmt, 2, pair.second.c_str(), -1, SQLITE_STATIC);
        int stepResult = sqlite3_step(stmt);
        if (stepResult != SQLITE_DONE)
        {
            const char* error_message = sqlite3_errmsg(m_db);
            int extended_code = sqlite3_extended_errcode(m_db);
            std::cerr << "Failed to insert kind." << std::endl;
            std::cout << "Constraint violation: " << error_message << ", " << extended_code << std::endl;
            sqlite3_finalize(stmt);
            return false;
        }
        sqlite3_reset(stmt);
    }
    sqlite3_finalize(stmt);
    return true;
}

bool OsyToSqlite::InsertTypes()
{
    const char* insertTypeSql = "INSERT INTO Types (Id, Hash, TokenId, Kind, IsConst) VALUES (?, ?, ?, ?, ?);";
    const char* insertChildSql = "INSERT INTO TypeChildren (TypeId, ChildId) VALUES (?, ?);";
    sqlite3_stmt* typeStmt;
    sqlite3_stmt* childStmt;

    if (sqlite3_prepare_v2(m_db, insertTypeSql, -1, &typeStmt, nullptr) != SQLITE_OK ||
        sqlite3_prepare_v2(m_db, insertChildSql, -1, &childStmt, nullptr) != SQLITE_OK)
    {
        return false;
    }

    for (const auto& type : m_dbFile.GetTypes())
    {
        sqlite3_bind_int64(typeStmt, 1, type.key);
        sqlite3_bind_int64(typeStmt, 2, type.hash);
        if (type.token != -1) sqlite3_bind_int64(typeStmt, 3, type.token); else sqlite3_bind_null(typeStmt, 3);
        sqlite3_bind_text(typeStmt, 4, GetTypeKindName(type.kind).c_str(), -1, SQLITE_STATIC);
        sqlite3_bind_int(typeStmt, 5, type.isconst);

        if (sqlite3_step(typeStmt) != SQLITE_DONE)
        {
            std::cerr << "Failed to insert type." << std::endl;
            sqlite3_finalize(typeStmt);
            sqlite3_finalize(childStmt);
            return false;
        }
        sqlite3_reset(typeStmt);

        for (const auto& childKey : type.children)
        {
            sqlite3_bind_int64(childStmt, 1, type.key);
            sqlite3_bind_int64(childStmt, 2, childKey);
            if (sqlite3_step(childStmt) != SQLITE_DONE)
            {
                std::cerr << "Failed to insert type child." << std::endl;
                sqlite3_finalize(typeStmt);
                sqlite3_finalize(childStmt);
                return false;
            }
            sqlite3_reset(childStmt);
        }
    }

    sqlite3_finalize(typeStmt);
    sqlite3_finalize(childStmt);
    return true;
}

bool OsyToSqlite::InsertNodes()
{
    const char* insertSql = "INSERT INTO Nodes (Id, CompilingFileId, ParentId, ReferencedId, KindId, Flags, TypeId, TokenId, Line, Column, StartOffset, EndOffset, SourceFileId) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);";
    sqlite3_stmt* stmt;
    if (sqlite3_prepare_v2(m_db, insertSql, -1, &stmt, nullptr) != SQLITE_OK) return false;

    for (const auto& node : m_dbFile.GetNodes())
    {
        sqlite3_bind_int64(stmt, 1, node.key);
        if (node.compilingFile != -1) sqlite3_bind_int64(stmt, 2, node.compilingFile); else sqlite3_bind_null(stmt, 2);
        if (node.parentNodeIdx != -1) sqlite3_bind_int64(stmt, 3, node.parentNodeIdx); else sqlite3_bind_null(stmt, 3);
        if (node.referencedIdx != -1) sqlite3_bind_int64(stmt, 4, node.referencedIdx); else sqlite3_bind_null(stmt, 4);
        
        sqlite3_bind_int64(stmt, 5, node.kind);

        sqlite3_bind_int(stmt, 6, node.flags);
        if (node.typeIdx != -1) sqlite3_bind_int64(stmt, 7, node.typeIdx); else sqlite3_bind_null(stmt, 7);
        if (node.token != -1) sqlite3_bind_int64(stmt, 8, node.token); else sqlite3_bind_null(stmt, 8);
        sqlite3_bind_int(stmt, 9, node.line);
        sqlite3_bind_int(stmt, 10, node.column);
        sqlite3_bind_int(stmt, 11, node.startOffset);
        sqlite3_bind_int(stmt, 12, node.endOffset);
        if (node.sourceFile != -1) sqlite3_bind_int64(stmt, 13, node.sourceFile); else sqlite3_bind_null(stmt, 13);

        int stepResult = sqlite3_step(stmt);
        if (stepResult != SQLITE_DONE)
        {
            const char* error_message = sqlite3_errmsg(m_db);
            int extended_code = sqlite3_extended_errcode(m_db);
            std::cout << "Constraint violation: " << error_message << ", " << extended_code << std::endl;
            sqlite3_finalize(stmt);
            return false;
        }
        sqlite3_reset(stmt);
    }
    sqlite3_finalize(stmt);
    return true;
}

std::string OsyToSqlite::GetCursorKindName(CXCursorKind kind)
{
    auto it = sCursorKindMap.find(kind);
    if (it != sCursorKindMap.end())
    {
        return it->second;
    }
    return "Unknown";
}

std::string OsyToSqlite::GetTypeKindName(CXTypeKind kind)
{
    auto it = sTypeKindMap.find(kind);
    if (it != sTypeKindMap.end())
    {
        return it->second;
    }
    return "Unknown";
}

bool OsyToSqlite::Convert(const std::string& osyFilePath, const std::string& sqliteFilePath)
{
    std::cout << "Loading OSY file: " << osyFilePath << std::endl;
    m_dbFile.Load(osyFilePath);

    BuildKindMapping();

    if (!OpenDatabase(sqliteFilePath))
    {
        return false;
    }

    if (sqlite3_exec(m_db, "BEGIN TRANSACTION;", 0, 0, 0) != SQLITE_OK) return false;

    if (!CreateTables() || !InsertSourceFiles() || !InsertTokens() || !InsertTypes() || !InsertKinds() || !InsertNodes())
    {
        sqlite3_exec(m_db, "ROLLBACK;", 0, 0, 0);
        CloseDatabase();
        return false;
    }

    if (sqlite3_exec(m_db, "COMMIT;", 0, 0, 0) != SQLITE_OK) return false;

    CloseDatabase();
    std::cout << "Successfully converted to SQLite: " << sqliteFilePath << std::endl;
    return true;
}
