using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static cppsymview.ClangTypes;
using System.Windows.Media;

namespace cppsymview
{
    public class Node : IComparable<Node>, INotifyPropertyChanged
    {
        CPPEngineFile engine;
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

        public Node RefNode { get; set; }

        public bool IsNodeExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;

        public Brush CursorBrush => new SolidColorBrush(ClangTypes.CursorColor[Kind]);
        public string CursorAbbrev => ClangTypes.CursorAbbrev[Kind];
        public event PropertyChangedEventHandler? PropertyChanged;

        public List<Node> allChildren = new List<Node>();

        public string FileName => engine.GetFileNameFromIdx(this.SourceFile);

        public Node(CPPEngineFile engine)
        {
            this.engine = engine;
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
