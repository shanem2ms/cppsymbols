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
        public ObservableCollection<CPPTextEditor> Editors { get; } = new ObservableCollection<CPPTextEditor>();
        Settings settings = Settings.Load();

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();

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
        }

        private void EditorsCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopNodes)));
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
