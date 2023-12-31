﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using symlib;

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

namespace cppsymview
{
    public class Source
    {
        public string filepath;
        public string code;
    }
    public class ScriptEngine
    {
        public event EventHandler<bool> OnCompileErrors;

        MetadataReference[] references;
        CSharpCompilation Compile(List<Source> sources)
        {
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            foreach (Source src in sources)
            {
                SyntaxTree tree = CSharpSyntaxTree.ParseText(src.code).
                    WithFilePath(src.filepath);
                syntaxTrees.Add(tree);
            }


            if (this.references == null)
            {
                HashSet<string> refPaths = new HashSet<string>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.IsDynamic)
                        continue;
                    string loc = assembly.Location.Trim();
                    if (loc.Length > 0)
                        refPaths.Add(loc);
                }

                references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();
            }
            string assemblyName = Path.GetRandomFileName();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: syntaxTrees,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            return compilation;
        }

        public void Run(List<Source> codeToCompile, CPPEngineFile scene)
        {
            using (var ms = new MemoryStream())
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var compilation = Compile(codeToCompile);
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    symlib.script.Api.WriteLine("Compilation failed!");
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        symlib.script.Api.WriteLine($"\t{diagnostic.Id}: [{diagnostic.Location.GetMappedLineSpan()}] {diagnostic.GetMessage()}");
                    }

                    OnCompileErrors?.Invoke(this, true);
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);

                    Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                    var type = assembly.GetType("symlib.script.Script");
                    var instance = assembly.CreateInstance("symlib.script.Script");
                    if (instance != null && type != null)
                    {
                        var meth = type.GetMember("Run").First() as MethodInfo;
                        if (meth != null)
                        {
                            symlib.script.Api.Reset(scene);
                            try
                            {
                                meth.Invoke(instance, new object[] { });
                            }
                            catch
                            (Exception ex)
                            {
                                symlib.script.Api.WriteLine(ex.ToString());
                            }
                        }
                        else
                        {
                            symlib.script.Api.WriteLine("Script.Run not found");
                            OnCompileErrors?.Invoke(this, true);
                        }
                    }
                    else
                    {
                        symlib.script.Api.WriteLine("symlib.script.Script not found");
                        OnCompileErrors?.Invoke(this, true);
                    }
                }
                sw.Stop();
                TimeSpan ts = sw.Elapsed;
                string elapsedTime = String.Format("{0:00}.{1:00}",
                    ts.TotalSeconds,
                    ts.Milliseconds / 10);

                symlib.script.Api.WriteLine("TotalTime " + elapsedTime);
                symlib.script.Api.Flush();
            }


        }

        public enum CodeCompleteType
        {
            Member,
            Function,
            New
        }

        public List<string> CodeComplete(string codeToCompile, int position, string variable, CodeCompleteType ccType)
        {
            //Write("Parsing the code into the SyntaxTree");
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeToCompile);
            if (this.references == null)
            {
                HashSet<string> refPaths = new HashSet<string>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.IsDynamic)
                        continue;
                    string loc = assembly.Location.Trim();
                    if (loc.Length > 0)
                        refPaths.Add(loc);
                }

                references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();
            }
            string assemblyName = Path.GetRandomFileName();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            if (ccType == CodeCompleteType.New)
            {
                var allSymbols = semanticModel.LookupSymbols(position);
                return allSymbols.Where(s => {
                    if (s.IsDefinition && s.Kind == SymbolKind.NamedType)
                    {
                        INamedTypeSymbol nts = s as INamedTypeSymbol;
                        if ((nts.TypeKind == TypeKind.Class || nts.TypeKind == TypeKind.Struct) &&
                            nts.Constructors.Length > 0)
                            return true;
                    }
                    return false;
                }).Select(s => s.Name).ToList();
            }
            else
            {
                var syntaxNode = syntaxTree.GetRoot().DescendantNodes(new TextSpan(position, variable.Length))
                    .Where(n => n is ExpressionSyntax &&
                    n.Span.Start == position && n.Span.End == (position + variable.Length)).FirstOrDefault();
                if (syntaxNode != null)
                {
                    if (ccType == CodeCompleteType.Member)
                    {
                        var info = semanticModel.GetTypeInfo(syntaxNode as ExpressionSyntax);
                        if (info.Type != null)
                        {
                            var hashset = info.Type.GetMembers().Select(m => m.Name).ToHashSet();
                            hashset.Remove(".ctor");
                            hashset.RemoveWhere((str) => { return str.StartsWith("get_") || str.StartsWith("set_"); });
                            return hashset.ToList();
                        }
                    }
                    else if (ccType == CodeCompleteType.Function)
                    {
                        var syminfo = semanticModel.GetSymbolInfo(syntaxNode);
                        List<string> symlist = new List<string>();
                        foreach (var sym in syminfo.CandidateSymbols)
                        {
                            string symstr = sym.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                            symlist.Add(symstr);
                        }
                        return symlist;
                    }
                }
            }
            return null;
        }
    }
}