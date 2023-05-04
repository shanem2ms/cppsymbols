#include "Precomp.h"
#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"
#include "Compiler.h"
#include "TcpServer.h"

#ifdef WIN32
#define stat _stat
#endif


class VisitContext;
typedef VisitContext* VisitContextPtr;

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

    if (!strcmp(argv[1], "-dump"))
    {
        std::cout << "Reading " << argv[2] << std::endl;
        DbFile dbFile;
        dbFile.Load(argv[2]);
    }
    else if (!strcmp(argv[1], "-host"))
    {
        TcpServer svr;
        svr.Start();
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
                default:
                    misc_args.insert(str);
                    break;
                }
            }
            else
                misc_args.insert(str);
        }

        bool doPch = misc_args.find("-emit-pch") != misc_args.end();
        std::vector<std::string> misc(misc_args.begin(), misc_args.end());
        Compiler::Inst()->Compile(srcFile, outFile, includeFiles, defines, misc, doPch, pchFile, "", false);
    }

    std::cout << "Complete." << std::endl;
}

