#include "Precomp.h"
#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"
#include "Compiler.h"

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
extern bool g_fullDbRebuild;
int main(int argc, char* argv[])
{
    if (argc < 2)
        return -1;
    
    std::vector<std::string> includeFiles;
    std::vector<std::string> defines;
    std::set<std::string> misc_args;
    std::string srcFile;
    std::string outFile;
    std::string pchFile;
    bool dolog = false;
    uint32_t loggingFlags = 0;

    if (!strcmp(argv[1], "-dump"))
    {
        std::cout << "Reading " << argv[2] << std::endl;
        DbFile dbFile;
        dbFile.Load(argv[2]);
        dbFile.ConsoleDump();
    }
    if (!strcmp(argv[1], "-merge"))
    {
        std::vector<std::string> mergeFiles;
        for (int i = 1; i < argc; ++i)
        {
            std::string str(argv[i]);
            if (str.length() < 2)
                continue;
            if (str[0] == '-')
            {
                char cmd = str[1];// std::toupper(str[1]);
                switch (cmd)
                {
                case 'o':
                    i++;
                    outFile = noquotes(argv[i]);
                    break;
                }
            }
            else
            {
                mergeFiles.push_back(noquotes(argv[i]));
            }
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
    else
    {
        for (int i = 1; i < argc; ++i)
        {
            std::string str(argv[i]);
            if (str.length() < 2)
                continue;
            if (str[0] == '-')
            {
                char cmd = str[1];// std::toupper(str[1]);
                switch (cmd)
                {
                case 'I':
                    includeFiles.push_back(str.substr(2));
                    break;
                case 'D':
                    defines.push_back(str.substr(2));
                    break;
                case 'P':
                    i++;
                    pchFile = argv[i];
                    break;
                case 'c':
                    i++;
                    srcFile = argv[i];
                    break;
                case 'o':
                    i++;
                    outFile = argv[i];
                    break;
                case 'L':
                    i++;
                    loggingFlags = atoi(argv[i]);
                    i++;
                    break;
                default:
                    misc_args.insert(str);
                    break;
                }
            }
            else
                misc_args.insert(str);
        }

        if (loggingFlags != 0)
            InitCx();

        std::filesystem::path p = std::filesystem::absolute(std::filesystem::path(srcFile));
        srcFile = p.string();
        bool doPch = misc_args.find("-emit-pch") != misc_args.end();
        std::vector<std::string> misc(misc_args.begin(), misc_args.end());
        Compiler::Inst()->Compile(srcFile, outFile, includeFiles, defines, misc, doPch, pchFile, "", loggingFlags);
    }

    std::cout << "Complete." << std::endl;
}
