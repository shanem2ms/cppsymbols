using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security;

namespace cppsymview
{
    public class CPPEngineFile : INotifyPropertyChanged
    {
        string srcDir = string.Empty;
        string osyFile = string.Empty;

        OSYFile curFile;

        public Node[] nodesArray;
        public CppType[] cppTypesArray; 
        public List<Node> topNodes = new List<Node>();
        public IEnumerable<Node> TopNodes => topNodes.Where(n => n.enabled);
        public Node[] Nodes => nodesArray;
        public event EventHandler<Node> SelectedNodeChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        List<Node> sortedNodes;

        List<Node> queryNodes = new List<Node>();
        public List<Node> QueryNodes => queryNodes;

        Dictionary<CXCursorKind, int> cursorKindCounts;
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
                List<CppType> cppTypes = new List<CppType>();
                List<Node> nodes = new List<Node>();

                foreach (OSYFile.DbType dbtype in curFile.DbTypes)
                {
                    CppType cppType = new CppType();
                    if (dbtype.Token >= 0)
                        cppType.Token = curFile.Tokens[(int)dbtype.Token];
                    cppType.Kind = dbtype.Kind;
                    if (dbtype.Next >= 0)
                        cppType.Next = cppTypes[(int)dbtype.Next];
                    cppType.Const = dbtype.IsConst != 0;
                    cppTypes.Add(cppType);
                }
                cppTypesArray = cppTypes.ToArray();

                long index = 0;
                foreach (OSYFile.DbNode dbnode in curFile.Nodes)
                {
                    Node node = new Node(this, index++);
                    if (dbnode.parentNodeIdx >= 0)
                    {
                        node.parent = nodes[(int)dbnode.parentNodeIdx];
                        node.parent.allChildren.Add(node);

                    }
                    node.dbNode = dbnode;
                    if (dbnode.token >= 0)
                        node.Token = curFile.Tokens[(int)dbnode.token];

                    if (dbnode.typeIdx >= 0)
                        node.CppType = cppTypesArray[(int)dbnode.typeIdx];
                    node.Kind = dbnode.kind;
                    node.Access = (CXXAccessSpecifier)(dbnode.flags & 0x3);
                    node.IsAbstract = (dbnode.flags & 0x4) != 0;
                    node.StorageClass = (CX_StorageClass)((dbnode.flags >> 3) & 0x7);
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

                sortedNodes = new List<Node>(nodes);
                sortedNodes.Sort();
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

            MergeNamespaces(topNodes);
        }

        void MergeNamespaces(List<Node> nodes)
        {
            Dictionary<string, List<Node>> namespaces =
                new Dictionary<string, List<Node>>();
            foreach(Node n in nodes)
            {
                if (n.Kind == CXCursorKind.Namespace)
                {
                    List<Node> nsNodes;
                    if (!namespaces.TryGetValue(n.Token.Text, out nsNodes))
                    {
                        nsNodes = new List<Node>();
                        namespaces.Add(n.Token.Text, nsNodes);
                    }
                    nsNodes.Add(n);
                }
            }

            foreach (var kv in namespaces)
            {
                if (kv.Value.Count <= 1)
                    continue;
                List<Node> lnodes = kv.Value;
                Node keepNode = lnodes.First();
                lnodes.RemoveAt(0);
                List<Node> children = keepNode.allChildren;
                foreach (var n in lnodes)
                {
                    children.AddRange(n.allChildren);
                    nodes.Remove(n);
                }

                foreach (Node n in children)
                {
                    n.parent = keepNode;
                }
            }

            foreach (Node cn in nodes)
            {
                MergeNamespaces(cn.allChildren);
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
            
            queryNodes = nodesArray.Where(n => (n.Token != null && tokens.Contains(n.Token))).ToList();
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
            if (this.sortedNodes == null)
                return null;
            Node srchNode = new Node(this, 0) { SourceFile = filenameKey, StartOffset = offset };
            int nodeIdx = this.sortedNodes.BinarySearch(srchNode);
            if (nodeIdx < 0) nodeIdx = (~nodeIdx) - 1;
            if (nodeIdx < 0 || nodeIdx >= sortedNodes.Count())
                return null;
            return sortedNodes[nodeIdx];
        }
    }
}
