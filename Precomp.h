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
#include "clang-c/BuildSystem.h"
#include "clang-c/Index.h"

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
class Error;
typedef Error* ErrorPtr;
class DbFile;
typedef DbFile* DbFilePtr;

#define CPPEXPORT __declspec(dllexport)
