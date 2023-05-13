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
using System.Windows.Shapes;
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
        public IEnumerable<Node> TopNodes => engine.TopNodes;
        //string root = @"C:\flash";
        string root = @"D:\vq\flash";
        public ObservableCollection<TextEditor> Editors { get; } = new ObservableCollection<TextEditor>();
        Settings settings = Settings.Load();
        ScriptEngine scriptEngine = new ScriptEngine();
        CSEditor scriptTextEditor;

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();

            script.Api.WriteLine = WriteOutput;
            folderView.Root = root;
            folderView.OnFileSelected += FolderView_OnFileSelected;
            //ConnectTcp();
            engine.Init(root, root + @"\build\debugclg\flash.osy");
            this.nodesTreeView.SelectedItemChanged += NodesTreeView_SelectedItemChanged;
            this.nodesListView.SelectionChanged += NodesListView_SelectionChanged;
            this.EditorsCtrl.SelectionChanged += EditorsCtrl_SelectionChanged;

            foreach (string openfile in settings.Files)
            {
                CreateEditor(openfile);
            }

            scriptTextEditor = new CSEditor("Script.cs", engine, scriptEngine);
            Editors.Add(scriptTextEditor);
            EditorsCtrl.SelectedItem = scriptTextEditor;
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
            settings.Files.Add(e);
            settings.Save();
            CreateEditor(e);
        }

        void CreateEditor(string path)
        {
            CPPTextEditor cppTextEditor = new CPPTextEditor(path, engine);
            cppTextEditor.NodeChanged += CppTextEditor_NodeChanged;
            Editors.Add(cppTextEditor);
            EditorsCtrl.SelectedItem = cppTextEditor;
        }

        void WriteOutput(string text)
        {
            this.OutputConsole.Text += text;
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

        private void CurFileChk_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (sender as CheckBox);
            this.engine.CurrentFileOnly = cb.IsChecked??false;

        }

        private void CursorTypesCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void KindCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void RunScript_Click(object sender, RoutedEventArgs e)
        {
            this.scriptTextEditor.Save();
            Source src = new Source() { code = this.scriptTextEditor.Document.Text, filepath = "Script.cs" };
            this.scriptEngine.Run(new List<Source>() { src }, this.engine);
        }

        private void ClearOutput_Click(object sender, RoutedEventArgs e)
        {
            OutputConsole.Text = "";
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

}
