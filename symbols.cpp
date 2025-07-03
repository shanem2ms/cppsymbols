#include "Precomp.h"
#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"
#include "Compiler.h"
#include "OsyToSqlite.h"

#ifdef WIN32
#define stat _stat
#endif


class VisitContext;
typedef VisitContext* VisitContextPtr;
void InitCx();

inline std::string noquotes(const char* arg)
{
    if (arg[0] == '\"')
    {
        size_t len = strlen(arg);
        return std::string(arg + 1, arg + len - 2);
    }
    else
        return std::string(arg);
}

void printUsage()
{
    std::cout << "C++ Symbols - A tool for parsing C++ source code and generating AST databases\n\n";
    std::cout << "USAGE:\n";
    std::cout << "  symbols [COMMAND] [OPTIONS]\n\n";
    std::cout << "COMMANDS:\n";
    std::cout << "  --compile <file>              Parse C++ source file and generate OSY database\n";
    std::cout << "  --dump <file.osy>             Display contents of OSY file for debugging\n";
    std::cout << "  --validate <file.osy>         Validate the structure of an OSY file\n";
    std::cout << "  --merge <files...>            Merge multiple OSY files into one\n";
    std::cout << "  --to-sqlite <in.osy> <out.sqlite>  Convert OSY file to SQLite database\n";
    std::cout << "  --help                        Show this help message\n\n";
    std::cout << "OPTIONS (for --compile):\n";
    std::cout << "  --output <file>               Specify output OSY file path\n";
    std::cout << "  --include-directory <path>    Add include directory (can be used multiple times)\n";
    std::cout << "  --define <macro[=value]>      Define preprocessor macro (can be used multiple times)\n\n";
    std::cout << "EXAMPLES:\n";
    std::cout << "  # Parse a C++ file and generate OSY database\n";
    std::cout << "  symbols --compile main.cpp --output main.osy --include-directory /usr/include --define DEBUG=1\n\n";
    std::cout << "  # Merge multiple OSY files\n";
    std::cout << "  symbols --merge file1.osy file2.osy --output merged.osy\n\n";
    std::cout << "  # Dump OSY file contents\n";
    std::cout << "  symbols --dump main.osy\n\n";
    std::cout << "  # Convert OSY to SQLite\n";
    std::cout << "  symbols --to-sqlite main.osy main.sqlite\n\n";
}

extern bool g_fullDbRebuild;
int main(int argc, char* argv[])
{
    if (argc < 2)
    {
        printUsage();
        return -1;
    }
    
    std::vector<std::string> includeFiles;
    std::vector<std::string> defines;
    std::set<std::string> misc_args;
    std::string srcFile;
    std::string outFile;
    std::string pchFile;
    bool dolog = false;
    uint32_t loggingFlags = 0;

    if (!strcmp(argv[1], "--help") || !strcmp(argv[1], "-h"))
    {
        printUsage();
        return 0;
    }
    else if (!strcmp(argv[1], "--dump"))
    {
        if (argc < 3)
        {
            std::cerr << "Error: --dump requires an OSY file argument\n";
            printUsage();
            return -1;
        }
        std::cout << "Reading " << argv[2] << std::endl;
        DbFile dbFile;
        dbFile.Load(argv[2]);
        dbFile.ConsoleDump();
    }
    else if (!strcmp(argv[1], "--validate"))
    {
        if (argc < 3)
        {
            std::cerr << "Error: --validate requires an OSY file argument\n";
            printUsage();
            return -1;
        }
        std::cout << "Reading " << argv[2] << std::endl;
        DbFile dbFile;
        dbFile.Load(argv[2]);
        dbFile.Validate();
    }
    else if (!strcmp(argv[1], "--merge"))
    {
        std::vector<std::string> mergeFiles;
        for (int i = 2; i < argc; ++i)  // Start from 2 to skip "--merge"
        {
            std::string str(argv[i]);
            if (str == "--output")
            {
                i++;
                if (i < argc)
                    outFile = noquotes(argv[i]);
                else
                {
                    std::cerr << "Error: --output requires a file argument\n";
                    return -1;
                }
            }
            else if (str[0] != '-')  // Not a flag, must be a file to merge
            {
                mergeFiles.push_back(noquotes(argv[i]));
            }
        }

        if (mergeFiles.empty())
        {
            std::cerr << "Error: --merge requires at least one OSY file to merge\n";
            printUsage();
            return -1;
        }

        if (outFile.empty())
        {
            std::cerr << "Error: --merge requires --output to specify the output file\n";
            printUsage();
            return -1;
        }

        using namespace std::chrono;
        milliseconds ms0 = duration_cast<milliseconds>(
            system_clock::now().time_since_epoch());
        std::cout << "Reading " << mergeFiles[0] << std::endl;
        DbFile dbFile;
        dbFile.Load(mergeFiles[0]);
        for (int idx = 1; idx < mergeFiles.size(); ++idx)
        {
            std::cout << "Merging " << mergeFiles[idx] << std::endl;
            DbFile dbMerge;
            dbMerge.Load(mergeFiles[idx]);
            dbFile.Merge(dbMerge);

//            milliseconds ms1 = duration_cast<milliseconds>(
//                system_clock::now().time_since_epoch());

//            float seconds = (ms1 - ms0).count() / 1000.0f;
//            std::cout << seconds << "seconds" << std::endl;
        }

        std::cout << "Writing " << outFile << std::endl;
        dbFile.Save(outFile);
        milliseconds ms1 = duration_cast<milliseconds>(
            system_clock::now().time_since_epoch());

        float seconds = (ms1 - ms0).count() / 1000.0f;
        std::cout << seconds << "seconds" << std::endl;
    }
    else if (!strcmp(argv[1], "--to-sqlite"))
    {
        if (argc < 4)
        {
            std::cerr << "Error: --to-sqlite requires input OSY file and output SQLite file arguments\n";
            printUsage();
            return -1;
        }
        
        std::string osyFile = noquotes(argv[2]);
        std::string sqliteFile = noquotes(argv[3]);
        
        OsyToSqlite converter;
        if (!converter.Convert(osyFile, sqliteFile))
        {
            std::cerr << "Failed to convert OSY to SQLite." << std::endl;
            return -1;
        }
    }
    else if (!strcmp(argv[1], "--compile"))
    {
        if (argc < 3)
        {
            std::cerr << "Error: --compile requires a C++ source file argument\n";
            printUsage();
            return -1;
        }
        
        srcFile = noquotes(argv[2]);
        
        // Parse remaining arguments
        for (int i = 3; i < argc; ++i)
        {
            std::string str(argv[i]);
            if (str == "--output")
            {
                i++;
                if (i < argc)
                    outFile = noquotes(argv[i]);
                else
                {
                    std::cerr << "Error: --output requires a file argument\n";
                    return -1;
                }
            }
            else if (str == "--include-directory")
            {
                i++;
                if (i < argc)
                    includeFiles.push_back(noquotes(argv[i]));
                else
                {
                    std::cerr << "Error: --include-directory requires a path argument\n";
                    return -1;
                }
            }
            else if (str == "--define")
            {
                i++;
                if (i < argc)
                    defines.push_back(noquotes(argv[i]));
                else
                {
                    std::cerr << "Error: --define requires a macro argument\n";
                    return -1;
                }
            }
            else if (str[0] == '-')
            {
                misc_args.insert(str);
            }
        }

        if (outFile.empty())
        {
            std::cerr << "Error: --compile requires --output to specify the output OSY file\n";
            printUsage();
            return -1;
        }

        std::filesystem::path p = std::filesystem::absolute(std::filesystem::path(srcFile));
        srcFile = p.string();
        bool doPch = misc_args.find("--emit-pch") != misc_args.end();
        std::vector<std::string> misc(misc_args.begin(), misc_args.end());
        Compiler::Inst()->Compile(srcFile, outFile, includeFiles, defines, misc, doPch, pchFile, "", loggingFlags);
    }
    else
    {
        std::cerr << "Error: Unknown command '" << argv[1] << "'\n";
        printUsage();
        return -1;
    }

    std::cout << "Complete." << std::endl;
}
