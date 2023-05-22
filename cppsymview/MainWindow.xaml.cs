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
using ICSharpCode.AvalonEdit;

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
        string root = @"C:\flash";
        //string root = @"D:\vq\flash";
        public ObservableCollection<TextEditor> Editors { get; } = new ObservableCollection<TextEditor>();
        Settings settings = Settings.Load();
        ScriptEngine scriptEngine = new ScriptEngine();
        CSEditor scriptTextEditor;
        public bool ClearOutput { get; set; } = true;
        string projectDir;
        string curScriptFile;

        public bool LiveSwitching { get; set; } = true;

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();

            script.Api.WriteLine = WriteOutput;
            folderView.Root = root;
            folderView.OnFileSelected += FolderView_OnFileSelected;
            
            DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (di.Name.ToLower() != "cppsymview")
                di = di.Parent;

            this.projectDir = di.FullName;
            this.curScriptFile = Path.Combine(projectDir, "Script.cs");

            //ConnectTcp();
            //engine.Init(root, root + @"\build\debugclg\clouds\Particle.cpp.osy");        
            engine.Init(root, root + @"\build\debugclg\flash.osy");
            this.nodesTreeView.SelectedItemChanged += NodesTreeView_SelectedItemChanged;
            this.nodesListView.SelectionChanged += NodesListView_SelectionChanged;

            foreach (string openfile in settings.Files)
            {
                CreateEditor(openfile);
            }
            this.EditorsCtrl.SelectionChanged += EditorsCtrl_SelectionChanged;

            scriptTextEditor = new CSEditor(curScriptFile, scriptEngine);
            Editors.Add(scriptTextEditor);
            EditorsCtrl.SelectedItem = scriptTextEditor;
            Editors.CollectionChanged += Editors_CollectionChanged;
        }

        private void Editors_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            settings.Files.Clear();
            foreach (TextEditor editor in Editors)
            {
                if (editor is CPPTextEditor)
                {
                    CPPTextEditor cppeditor = (CPPTextEditor)editor;
                    settings.Files.Add(editor.Document.FileName);
                }
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
            CreateEditor(e);
        }

        CPPTextEditor CreateEditor(string path)
        {
            CPPTextEditor cppTextEditor = new CPPTextEditor(path, engine);
            cppTextEditor.NodeChanged += CppTextEditor_NodeChanged;
            Editors.Add(cppTextEditor);
            return cppTextEditor;
        }

        void WriteOutput(string text)
        {
            this.OutputConsole.Text += text + "\n";
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
            this.engine.NotifySelectedNodeChanged((Node)e.NewValue);
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
            this.scriptTextEditor.Save();
            Source src = new Source() { code = this.scriptTextEditor.Document.Text, filepath = curScriptFile };
            this.scriptEngine.Run(new List<Source>() { src }, this.engine);
        }

        //void GotoFileAndLocation()

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
        private void GotoNodeBtn_Click(object sender, RoutedEventArgs e)
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

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            TabItem ti = Util.FindParent<TabItem>(sender as DependencyObject);
            TextEditor te = ti.Content as TextEditor;
            Editors.Remove(te);
        }

        private void RefNodeBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)(sender);
            long refNodeIndex = (long)btn.Content;
            Node node = engine.Nodes[refNodeIndex];
            node.SetEnabled(true, true);
            this.engine.RefreshNodeTree();
            node.Select();
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
