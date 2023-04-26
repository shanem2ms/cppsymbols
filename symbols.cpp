#include "Precomp.h"

#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"
#include "VCProject.h"
#include "Compiler.h"

typedef DbMgr* DbMgrPtr;

#ifdef WIN32
#define stat _stat
#endif


class VSProject;
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
                pchFile = str.substr(2);
                break;
            case 'c':
                srcFile = str.substr(2);
                break;
            case 'o':
                outFile = str.substr(2);
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
    DbMgr::Instance()->Initialize();  
    Compiler::Inst()->Compile(srcFile, outFile, includeFiles, defines, misc, true, pchFile, "", false);

    std::cout << "Complete." << std::endl;
}

