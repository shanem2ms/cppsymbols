﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static symlib.ClangTypes;
using System.Runtime.InteropServices;

namespace symlib
{

    public class CppType
    {
        public CppType() { }
        public Token Token { get; set;}
        public CppType []Children { get; set; }

        public CppType Next => Children?.Length > 0 ? Children[0] : null;
        public CXTypeKind Kind { get; set; }
        public bool Const { get; set; }

        public override string ToString()
        {
            return Token.Text;
        }

        public IEnumerable<Node> RefNodes { get => CPPEngineFile.Inst.GetTypeReferences(this); }
    }
    public class Node : IComparable<Node>, INotifyPropertyChanged
    {
        CPPEngineFile engine;
        public Node parent;

        public Node Parent => parent;
        public OSYFile.DbNode dbNode;
        public bool enabled = false;
        public long Index { get; }
        public IEnumerable<Node> Children => allChildren.Where(c => c.enabled);
        public Token? Token { get; set; }
        public CppType CppType { get; set; }
        public CXCursorKind Kind { get; set; }
        public CXXAccessSpecifier Access { get; set; }
        public CX_StorageClass StorageClass { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsDeleted { get; set; }
        public uint Line { get; set; }
        public uint Column { get; set; }
        public uint StartOffset { get; set; }
        public uint EndOffset { get; set; }
        public long SourceFile { get; set; }

        public Node RefNode { get; set; }

        public bool IsNodeExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;

        public string CursorAbbrev => ClangTypes.CursorAbbrev[Kind];
        public event PropertyChangedEventHandler? PropertyChanged;

        public List<Node> allChildren = new List<Node>();

        public string FileName => this.SourceFile >= 0 ? engine.GetFileNameFromIdx(this.SourceFile) : "";

        public override string ToString()
        {
            return Token.Text;
        }


        public enum FindChildResult
        {
            eTrue,
            eFalseTraverse,
            eFalseNoTraverse,
            eAbort
        }
        public List<Node> FindChildren(Func<Node, FindChildResult> func)
        {
            List<Node> nodes = new List<Node>();
            FindChildrenRec(ref nodes, func);
            return nodes;
        }

        bool FindChildrenRec(ref List<Node> nodes, Func<Node, FindChildResult> func)
        {
            foreach (var node in this.allChildren)
            {
                var result = func(node);
                switch (result)
                {
                    case FindChildResult.eTrue:
                        nodes.Add(node);
                        break;
                    case FindChildResult.eFalseNoTraverse:
                        break;
                    case FindChildResult.eFalseTraverse:
                        if (!node.FindChildrenRec(ref nodes, func)) return false;
                        break;
                    case FindChildResult.eAbort:
                        return false;
                }
            }
            return true;
        }

        public Node(CPPEngineFile engine, long index)
        {
            this.engine = engine;
            this.Index = index;
        }
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
}
