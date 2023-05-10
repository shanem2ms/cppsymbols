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

namespace cppsymview
{
    public class Node : IComparable<Node>, INotifyPropertyChanged
    {
        public Node parent;
        public OSYFile.DbNode dbNode;
        public bool enabled = false;
        public IEnumerable<Node> Children => allChildren.Where(c => c.enabled);
        public Token? Token { get; set; }
        public Token? TypeToken { get; set; }
        public CXCursorKind Kind { get; set; }
        public CXTypeKind TypeKind { get; set; }
        public uint Line { get; set; }
        public uint Column { get; set; }
        public uint StartOffset { get; set; }
        public uint EndOffset { get; set; }
        public long SourceFile { get; set; }

        public bool IsNodeExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;

        public Brush CursorBrush => new SolidColorBrush(ClangTypes.CursorColor[Kind]);
        public string CursorAbbrev => ClangTypes.CursorAbbrev[Kind];
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public List<Node> allChildren = new List<Node>();

        public int CompareTo(Node? other)
        {
            if (other == null) return -1;
            if (SourceFile != other.SourceFile)
                return SourceFile.CompareTo(other.SourceFile);
            if (StartOffset != other.StartOffset)
                return StartOffset.CompareTo(other.StartOffset);
            return 0;
        }

        public void Expand()
        {
            if (parent != null) { parent.Expand(); }
            IsNodeExpanded = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNodeExpanded)));
        }

        public void Select()
        {
            IsSelected = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }

        public void SetEnabled(bool _enabled, bool recursive)
        {
            this.enabled = _enabled;
            if (this.parent != null && this.enabled == true & !this.parent.enabled)
                this.parent.SetEnabled(true, false);
            if (recursive)
            {
                foreach (var child in allChildren)
                {
                    child.SetEnabled(_enabled, recursive);
                }
            }
        }
    }

    public class CPPEngineFile : INotifyPropertyChanged
    {
        string srcDir = string.Empty;
        string osyDir = string.Empty;

        OSYFile curFile;

        public Node[] nodesArray;
        public List<Node> topNodes = new List<Node>();
        public IEnumerable<Node> TopNodes => topNodes.Where(n => n.enabled);
        public event EventHandler<Node> SelectedNodeChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        List<Node> queryNodes = new List<Node>();
        public List<Node> QueryNodes => queryNodes;

        Dictionary<ClangTypes.CXCursorKind, int> cursorKindCounts;
        Dictionary<ClangTypes.CXTypeKind, int> typeKindCounts;

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
            foreach (Node n in nodesArray)
            {
                n.SetEnabled(false, false);
            }
            int srcFile = GetSourceFile(file);
            foreach (Node n in  nodesArray) 
            { 
                if (n.SourceFile == srcFile)
                {
                    n.SetEnabled(true, false);
                }
            }
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
            Node srchNode = new Node() { SourceFile = filenameKey, StartOffset = offset };
            int nodeIdx = Array.BinarySearch(this.nodesArray, srchNode);
            if (nodeIdx < 0) nodeIdx = (~nodeIdx) - 1;
            if (nodeIdx < 0 || nodeIdx >= nodesArray.Length)
                return null;
            return nodesArray[nodeIdx];
        }
    }
}
