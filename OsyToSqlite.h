#pragma once

#include <string>
#include <unordered_map>
#include <sqlite3.h>
#include "DbMgr.h"

class OsyToSqlite
{
private:
    sqlite3* m_db;
    DbFile m_dbFile;
    std::unordered_map<CXCursorKind, int64_t> m_kindToIdMap;

    bool OpenDatabase(const std::string& dbPath);
    void CloseDatabase();
    bool CreateTables();
    bool InsertSourceFiles();
    bool InsertTokens();
    bool InsertTypes();
    bool InsertKinds();
    bool InsertNodes();
    std::string GetCursorKindName(CXCursorKind kind);
    std::string GetTypeKindName(CXTypeKind kind);
    void BuildKindMapping();

public:
    OsyToSqlite();
    ~OsyToSqlite();
    
    bool Convert(const std::string& osyFilePath, const std::string& sqliteFilePath);
};
