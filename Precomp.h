#pragma once

#define NOMINMAX
#include <iostream>
#include <string>
#include <vector>
#include <iostream>
#include <fstream>
#include <atomic>
#include <map>
#include <set>
#include <sstream>
#include <chrono>
#include <regex>
#include "Shlobj_core.h"
#include "clang-c/BuildSystem.h"
#include "clang-c/Index.h"

#include <cppcoro/sync_wait.hpp>
#include <cppcoro/task.hpp>
#include <cppcoro/static_thread_pool.hpp>
#include <cppcoro/when_all.hpp>

#include <sys/types.h>
#include <sys/stat.h>
#ifndef WIN32
#include <unistd.h>
#endif

#include <regex>


class Node;
class BaseNode;
typedef Node* NodePtr;
typedef BaseNode* BNodePtr;
struct Token;
typedef Token* TokenPtr; 
class CPPSourceFile;
typedef CPPSourceFile* CPPSourceFilePtr;
class VisitContext;
typedef VisitContext* VisitContextPtr;
class DbMgr;
typedef DbMgr* DbMgrPtr;
class Error;
typedef Error* ErrorPtr;
class DbFile;
typedef DbFile* DbFilePtr;

namespace co = cppcoro;

extern co::static_thread_pool g_threadPool;

namespace cppcoro
{
    template <typename Func>
    task<> dispatch(Func func) {
        co_await g_threadPool.schedule();
        co_await func();
    }
}