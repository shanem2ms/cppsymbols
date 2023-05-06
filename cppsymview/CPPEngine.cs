using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents.DocumentStructures;
using System.Threading;
using System.Text.Unicode;
using System.Windows.Media.Animation;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Runtime.InteropServices.Marshalling;
using static cppsymview.ClangTypes;

namespace cppsymview
{
    public class Node : IComparable<Node>
    {
        public Node parent;
        public OSYFile.DbNode dbNode;
        public List<Node> Children { get; set;} =  new List<Node>();
        public Token Token { get; set; }
        public Token TypeToken { get; set; }
        public CXCursorKind Kind { get; set; }
        public CXTypeKind TypeKind { get; set; }
        public uint Line { get; set; }
        public uint Column { get; set; }
        public uint StartOffset { get; set; }
        public uint EndOffset { get; set; }
        public long SourceFile { get; set; }

        public int CompareTo(Node? other)
        {
            if (other == null) return -1;
            if (SourceFile != other.SourceFile)
                return SourceFile.CompareTo(other.SourceFile);
            if (StartOffset != other.StartOffset)
                return StartOffset.CompareTo(other.StartOffset);
            return 0;
        }
    }

    public class CPPEngineFile
    {
        string srcDir = string.Empty;
        string osyDir = string.Empty;

        OSYFile curFile;


        public Node[] nodesArray;
        public List<Node> topNodes = new List<Node>();
        public List<Node> TopNodes => topNodes;

        public void Init(string sourcedir, string osydir)
        {
            srcDir = sourcedir;
            osyDir = osydir;
        }

        public void CompileFile(string filename)
        {
            string osypath = filename.Replace(srcDir, osyDir);
            osypath = osypath + ".osy";
            if (File.Exists(osypath))
            {
                curFile = new OSYFile(osypath);

                List<Node> nodes = new List<Node>();
                //nodesArray = new Node[curFile.Nodes.Length];
                foreach (OSYFile.DbNode dbnode in curFile.Nodes)
                {
                    Node node = new Node();
                    if (dbnode.parentNodeIdx >= 0)
                    {
                        node.parent = nodes[(int)dbnode.parentNodeIdx];
                        node.parent.Children.Add(node);

                    }
                    node.dbNode = dbnode;
                    if (dbnode.token >= 0)
                        node.Token = curFile.Tokens[(int)dbnode.token];
                    if (dbnode.typetoken >= 0)
                        node.Token = curFile.Tokens[(int)dbnode.typetoken];


                    node.Kind = dbnode.kind;
                    node.TypeKind = dbnode.typeKind;
                    node.Line = dbnode.line;
                    node.Column = dbnode.column;
                    node.StartOffset = dbnode.startOffset;
                    node.EndOffset = dbnode.endOffset;
                    node.SourceFile = dbnode.sourceFile;
                    nodes.Add(node);
                }

                nodes.Sort();
                nodesArray = nodes.ToArray();
                topNodes = nodesArray.Where(n => n.parent == null).ToList();
            }
        }
    }
}
