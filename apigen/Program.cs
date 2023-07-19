// See https://aka.ms/new-console-template for more information
using symlib;
using symlib.script;

string root = @"c:\flash";
string osyfile = Path.Combine(root, @"build\debugclg\flash.osy");
CPPEngineFile cppengine = new CPPEngineFile();
cppengine.Init(root, osyfile);

Api.Engine = cppengine;

Api.WriteLine = (string text) => 
{ 
    Console.WriteLine(text);
};

Script script = new Script();
script.Run();

namespace symlib.script
{
    public class Api
    {
        public delegate void WriteDel(string text);
        public static WriteDel WriteLine;
        public delegate void FlushDel();
        public static FlushDel Flush;

        public static string ScriptFolder = null;
        public static CPPEngineFile Engine = null;
        public static void Reset(CPPEngineFile engine)
        {
            script.Api.Engine = engine;
        }
    }
}