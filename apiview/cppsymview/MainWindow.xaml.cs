using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Linq;
using ICSharpCode.AvalonEdit;
using symlib;

namespace cppsymview
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        CPPEngineFile engine = new CPPEngineFile();

        public event PropertyChangedEventHandler? PropertyChanged;

        public CPPEngineFile Engine => engine;
        public ObservableCollection<TextEditor> Editors { get; } = new ObservableCollection<TextEditor>();
        Settings settings = Settings.Load();
        ScriptEngine scriptEngine = new ScriptEngine();
        
        public bool ClearOutput { get; set; } = true;
        string scriptDir;

        class Script
        {
            public CSEditor editor;
            public string filename;
        }

        List<Script> curScriptFiles = new List<Script>();

        public Node CurrentNode { get; set; } = null;

        public CppType CurrentType { get; set; } = null;

        public List<Node> NodeBkStack { get; } = new List<Node>();
        public bool NodeBkEnabled => NodeBkStack.Count() > 0;
        public List<Node> NodeFwdStack { get; } = new List<Node>();
        public bool NodeFwdEnabled => NodeFwdStack.Count() > 0;

        public bool LiveSwitching { get; set; } = true;

        StringBuilder scriptWriteBuffer = new StringBuilder();

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();

            symlib.script.Api.WriteLine = WriteOutput;
            symlib.script.Api.Flush = Flush;
            folderView.OnFileSelected += FolderView_OnFileSelected;


            DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (di.Name.ToLower() != "apiview")
                di = di.Parent;
            
            //ConnectTcp();
            //engine.Init(root, root + @"\build\debugclg\clouds\Particle.cpp.osy");        
            engine.Init("", @"D:\vq\CMake\Azure\CMakeVQMaster\build\x64-release-clg\Max.osy");
            folderView.BuildSourceFileTree(engine);
            this.nodesTreeView.SelectedItemChanged += NodesTreeView_SelectedItemChanged;
            this.nodesListView.SelectionChanged += NodesListView_SelectionChanged;

            this.scriptDir = Path.Combine(di.FullName, "Scripts");
            scriptsView.Root = this.scriptDir;
            scriptsView.OnFileSelected += FolderView_OnFileSelected;

            DirectoryInfo diScripts = new DirectoryInfo(this.scriptDir);
            foreach (FileInfo fi in diScripts.GetFiles())
            {
                Script script = new Script();
                script.filename = fi.FullName;
                curScriptFiles.Add(script);
            }

            foreach (string openfile in settings.Files)
            {
                CreateEditor(openfile);
            }
            this.EditorsCtrl.SelectionChanged += EditorsCtrl_SelectionChanged;


            Editors.CollectionChanged += Editors_CollectionChanged;
            EditorsCtrl.SelectedItem = curScriptFiles[0].editor;

        }

        private void Editors_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settings.Files.Clear();
            foreach (TextEditor editor in Editors)
            {
                settings.Files.Add(editor.Document.FileName);
            }
            settings.Save();
        }

        private void EditorsCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is CPPTextEditor)
            {
                CPPTextEditor editor = (CPPTextEditor)e.AddedItems[0];
                editor?.MakeActive();
            }
        }

        private void FolderView_OnFileSelected(object? sender, string e)
        {
            TextEditor te = GetOrMakeTextEditor(e);
            EditorsCtrl.SelectedItem = te;
        }


        TextEditor CreateEditor(string path)
        {
            string ext = Path.GetExtension(path);
            TextEditor te;
            if (ext == ".cs")
            {
                CSEditor ce = new CSEditor(path, this.scriptEngine);
                Script script = this.curScriptFiles.First((s) => s.filename == path);
                script.editor = ce;
                te = ce;
            }
            else
            {
                CPPTextEditor cppTextEditor = new CPPTextEditor(path, engine);
                cppTextEditor.NodeChanged += CppTextEditor_NodeChanged;
                te = cppTextEditor;
            }
            Editors.Add(te);
            return te;
        }

        void WriteOutput(string text)
        {
            scriptWriteBuffer.AppendLine(text);
        }

        void Flush()
        {
            this.OutputConsole.Text += scriptWriteBuffer.ToString();
            scriptWriteBuffer.Clear();
        }
        private void NodesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                Node node = (Node)e.AddedItems[0];
                node.Expand();
                node.Select();
            }
        }

        private void NodesTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SetCurrentNode((Node)(e.NewValue));
            this.engine.NotifySelectedNodeChanged((Node)e.NewValue);
        }

        void SetCurrentNode(Node node)
        {
            if (CurrentNode != null)
            {
                NodeBkStack.Add(CurrentNode);
            }
            NodeFwdStack.Clear();
            CurrentNode = node;            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentNode)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NodeBkEnabled)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NodeFwdEnabled)));
        }

        private void ParentBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)(sender);
            if (btn.Content == null)
                return;
            long parentNodeIndex = (long)btn.Content;
            Node node = engine.Nodes[parentNodeIndex];
            SetCurrentNode(node);
        }
        private void RefNodeBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)(sender);
            if (btn.Content == null)
                return;
            long refNodeIndex = (long)btn.Content;
            Node node = engine.Nodes[refNodeIndex];
            node.SetEnabled(true, true);
            this.engine.RefreshNodeTree();
            node.Select();
        }

        private void NodeBkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNode != null)
            {
                NodeFwdStack.Add(CurrentNode);
            }
            CurrentNode = NodeBkStack.LastOrDefault();
            NodeBkStack.Remove(CurrentNode);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentNode)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NodeBkEnabled)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NodeFwdEnabled)));
        }

        private void NodeFwdBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentNode != null)
            {
                NodeBkStack.Add(CurrentNode);
            }
            CurrentNode = NodeFwdStack.LastOrDefault();
            NodeFwdStack.Remove(CurrentNode);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentNode)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NodeBkEnabled)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NodeFwdEnabled)));
        }

        private void GoNodeBtn_Click(object sender, RoutedEventArgs e)
        {
            uint nodeId = 0;
            if (uint.TryParse(NodeIdTb.Text, out nodeId) && nodeId < Engine.Nodes.Length)
            {
                SetCurrentNode(Engine.Nodes[nodeId]);
            }
        }
        private void CppTextEditor_NodeChanged(object? sender, Node e)
        {
        }

        private void queryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (queryBox.Text.Trim().Length == 0)
                return;
            engine.Query(queryBox.Text);
            TreeOrListBtn.IsChecked = true;
        }

        private void nodesTreeView_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = e.OriginalSource as TreeViewItem;
            tvi.BringIntoView();
        }

        private void CursorTypesCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void KindCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void RunScript_Click(object sender, RoutedEventArgs e)
        {
            if (this.ClearOutput) { OutputConsole.Text = ""; }

            List<Source> sources = new List<Source>();
            foreach (var script in this.curScriptFiles)
            {
                if (script.editor != null)
                {
                    script.editor.Save();
                    sources.Add(new Source() { code = script.editor.Document.Text, filepath = script.filename });
                }
                else
                {
                    string text = File.ReadAllText(script.filename);
                    sources.Add(new Source() { code = text, filepath = script.filename });
                }
            }
            this.scriptEngine.Run(sources, this.engine);
        }

        TextEditor GetOrMakeTextEditor(string filename)
        {
            string srchfile = filename.ToLower();
            foreach (var editor in Editors)
            {
                string editorFile = editor.Document.FileName?.ToLower();
                if (srchfile == editorFile)
                    return editor;
            }
            return CreateEditor(filename);
        }
        private void GotoSrcBtn_Click(object sender, RoutedEventArgs e)
        {
            Node n = nodesTreeView.SelectedItem as Node;
            if (n != null)
            {
                string filename = engine.GetFileNameFromIdx(n.SourceFile);
                TextEditor te = GetOrMakeTextEditor(filename);
                EditorsCtrl.SelectedItem = te;
                te.ScrollTo((int)n.Line, (int)n.Column);
            }
        }

        private void GotoTreeBtn_Click(object sender, RoutedEventArgs e)
        {
            Node n = nodesListView.SelectedItem as Node;
            if (n != null)
            {
                string filename = engine.GetFileNameFromIdx(n.SourceFile);
                TextEditor te = GetOrMakeTextEditor(filename);
                EditorsCtrl.SelectedItem = te;
                te.ScrollTo((int)n.Line, (int)n.Column);
            }
        }


        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            TabItem ti = Util.FindParent<TabItem>(sender as DependencyObject);
            TextEditor te = ti.Content as TextEditor;
            Editors.Remove(te);
        }

        private void GoTypeBtn_Click(object sender, RoutedEventArgs e)
        {
            CppType cppType = engine.cppTypesArray.FirstOrDefault(c => TypeNameTb.Text == c.Token.Text);
            if (cppType != null)
            {
                CurrentType = cppType;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentType)));
            }
        }

        private void TypeTknBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)(sender);
            if (btn.Content == null)
                return;
            CppType type = (CppType)btn.Content;
            CurrentType = type;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentType)));
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)(sender);
            if (btn.Content == null)
                return;
            CppType type = (CppType)btn.Content;
            CurrentType = type;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentType)));
        }

        private void NodeRef_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)(sender);
            if (btn.Content == null)
                return;
            Node n = (Node)btn.Content;
            if (n != null)
            {
                n.SetEnabled(true, true);
                n.Expand();
                n.Select();
                string filename = engine.GetFileNameFromIdx(n.SourceFile);
                TextEditor te = GetOrMakeTextEditor(filename);
                EditorsCtrl.SelectedItem = te;
                te.ScrollTo((int)n.Line, (int)n.Column);
            }

        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Visibility))
            {
                var visible = System.Convert.ToBoolean(value, culture);
                if (InvertVisibility)
                    visible = !visible;
                return visible ? Visibility.Visible : Visibility.Collapsed;
            }
            throw new InvalidOperationException("Converter can only convert to value of type Visibility.");
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("Converter cannot convert back.");
        }

        public Boolean InvertVisibility { get; set; }
    }

    public class Util
    {
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }


    }
}
