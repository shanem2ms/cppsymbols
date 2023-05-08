using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
        CPPTextEditor cppTextEditor;

        public event PropertyChangedEventHandler? PropertyChanged;

        public CPPEngineFile Engine => engine;
        public List<Node> TopNodes => engine.TopNodes;
        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();

            string prefix = @"D:\vq\";
            //ConnectTcp();
            engine.Init(prefix + @"flash\src\core\", prefix + @"flash\build\debugclg\");
            cppTextEditor = new CPPTextEditor(prefix + @"flash\src\core\geo\SphericalProjection.cpp", engine);
            cppTextEditor.NodeChanged += CppTextEditor_NodeChanged;
            this.nodesTreeView.SelectedItemChanged += NodesTreeView_SelectedItemChanged;
            this.nodesListView.SelectionChanged += NodesListView_SelectionChanged;
            Editors.Children.Add(cppTextEditor);
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

        private void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            cppTextEditor.Reparse();
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
