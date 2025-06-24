#pragma once

#include <string>
#include <sqlite3.h>
#include "DbMgr.h"

class OsyToSqlite
{
private:
    sqlite3* m_db;
    DbFile m_dbFile;

    bool OpenDatabase(const std::string& dbPath);
    void CloseDatabase();
    bool CreateTables();
    bool InsertSourceFiles();
    bool InsertTokens();
    bool InsertTypes();
    bool InsertNodes();
    std::string GetCursorKindName(CXCursorKind kind);
    std::string GetTypeKindName(CXTypeKind kind);

public:
    OsyToSqlite();
    ~OsyToSqlite();
    
    bool Convert(const std::string& osyFilePath, const std::string& sqliteFilePath);
};
