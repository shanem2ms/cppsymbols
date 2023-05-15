using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using static cppsymview.ClangTypes;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Forms;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Automation;
using static cppsymview.OSYFile;
using System.Xml.Linq;
using System.Windows.Forms.Design;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace cppsymview
{
    public class CPPEngineFile : INotifyPropertyChanged
    {
        string srcDir = string.Empty;
        string osyFile = string.Empty;

        OSYFile curFile;

        public Node[] nodesArray;
        public List<Node> topNodes = new List<Node>();
        public IEnumerable<Node> TopNodes => topNodes.Where(n => n.enabled);
        public Node[] Nodes => nodesArray;
        public event EventHandler<Node> SelectedNodeChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        List<Node> queryNodes = new List<Node>();
        public List<Node> QueryNodes => queryNodes;

        Dictionary<CXCursorKind, int> cursorKindCounts;
        Dictionary<CXTypeKind, int> typeKindCounts;
        public Token []Tokens => curFile.Tokens;

        public CXCursorKind CursorFilter { get; set; } = CXCursorKind.None;
        public CXTypeKind TypeFilter { get; set; } = CXTypeKind.None;
        string curEditorFile;
        public string []SourceFiles => curFile.Filenames;

        bool currentFileOnly = true;
        public bool CurrentFileOnly { get => currentFileOnly; set { currentFileOnly = value; SetTopNodes(); } }
        public void Init(string sourcedir, string osyfile)
        {
            srcDir = sourcedir;
            osyFile = osyfile;
            LoadOSYFile(osyFile);
        }

        public string GetFileNameFromIdx(long idx)
        {
            return curFile.Filenames[idx-1];
        }

        void LoadOSYFile(string filename)
        {
            //string osypath = filename.Replace(srcDir, osyFile);
            //osypath = osypath + ".osy";
            string osypath = osyFile;            
            if (File.Exists(osypath))
            {
                curFile = new OSYFile(osypath);

                List<Node> nodes = new List<Node>();
                //nodesArray = new Node[curFile.Nodes.Length];
                foreach (OSYFile.DbNode dbnode in curFile.Nodes)
                {
                    Node node = new Node(this);
                    if (dbnode.parentNodeIdx >= 0)
                    {
                        node.parent = nodes[(int)dbnode.parentNodeIdx];
                        node.parent.allChildren.Add(node);

                    }
                    node.dbNode = dbnode;
                    if (dbnode.token >= 0)
                        node.Token = curFile.Tokens[(int)dbnode.token];
                    if (dbnode.typetoken >= 0)
                        node.TypeToken = curFile.Tokens[(int)dbnode.typetoken];


                    node.Kind = dbnode.kind;
                    node.TypeKind = dbnode.typeKind;
                    node.Line = dbnode.line;
                    node.Column = dbnode.column;
                    node.StartOffset = dbnode.startOffset;
                    node.EndOffset = dbnode.endOffset;
                    node.SourceFile = dbnode.sourceFile;
                    nodes.Add(node);
                }

                int idx = 0;
                foreach (OSYFile.DbNode dbnode in curFile.Nodes)
                {
                    if (dbnode.referencedIdx >= 0)
                    {
                        Node node = nodes[idx];
                        node.RefNode = nodes[(int)dbnode.referencedIdx];
                    }
                    idx++;
                }


                nodes.Sort();
                nodesArray = nodes.ToArray();
                topNodes = nodesArray.Where(n => n.parent == null).ToList();
            }

            cursorKindCounts = new Dictionary<CXCursorKind, int>();
            foreach (Node node in nodesArray)
            {
                if (!cursorKindCounts.ContainsKey(node.Kind))
                {
                    cursorKindCounts.Add(node.Kind, 1);
                }
                else
                    cursorKindCounts[node.Kind]++;
            }

            var sortedCursorKinds = cursorKindCounts.ToList();
            sortedCursorKinds.Sort((a, b) => { return b.Value.CompareTo(a.Value); });

            typeKindCounts = new Dictionary<CXTypeKind, int>();
            foreach (Node node in nodesArray)
            {
                if (!typeKindCounts.ContainsKey(node.TypeKind))
                {
                    typeKindCounts.Add(node.TypeKind, 1);
                }
                else
                    typeKindCounts[node.TypeKind]++;
            }
        }

        public void SetCurrentFile(string file)
        {
            this.curEditorFile = file;
            SetTopNodes();
        }

        public void RefreshNodeTree()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopNodes)));
        }

        void SetTopNodes()
        {
            if (nodesArray == null)
                return;
            if (CurrentFileOnly)
            {
                foreach (Node n in nodesArray)
                {
                    n.SetEnabled(false, false);
                }
                int srcFile = GetSourceFile(this.curEditorFile);
                foreach (Node n in nodesArray)
                {
                    if (n.SourceFile == srcFile)
                    {
                        n.SetEnabled(true, false);
                    }
                }
            }
            else
            {
                foreach (Node n in nodesArray)
                {
                    n.SetEnabled(true, false);
                }
            }

            int ct = TopNodes.Count();
            RefreshNodeTree();
        }

        public void Query(string querystr)
        {
            string tokenstr = querystr.Trim();
            
            HashSet<Token> tokens = curFile.Tokens.Where(t => t.Text.Contains(tokenstr)).ToHashSet();
            
            queryNodes = nodesArray.Where(n => (n.Token != null && tokens.Contains(n.Token)) || (n.TypeToken != null && tokens.Contains(n.TypeToken))).ToList();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QueryNodes)));
        }

        public void NotifySelectedNodeChanged(Node n)
        {
            SelectedNodeChanged?.Invoke(this, n);
        }
        public int GetSourceFile(string filename)
        {
            string f = filename.ToLower();
            int idx = Array.IndexOf(curFile.FilenamesLower, f);
            return idx + 1;
        }

        public Node GetNodeFor(int filenameKey, uint offset)
        {
            if (this.nodesArray == null)
                return null;
            Node srchNode = new Node(this) { SourceFile = filenameKey, StartOffset = offset };
            int nodeIdx = Array.BinarySearch(this.nodesArray, srchNode);
            if (nodeIdx < 0) nodeIdx = (~nodeIdx) - 1;
            if (nodeIdx < 0 || nodeIdx >= nodesArray.Length)
                return null;
            return nodesArray[nodeIdx];
        }
    }
}
