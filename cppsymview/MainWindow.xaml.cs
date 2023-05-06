using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        public List<Node> TopNodes => engine.TopNodes;
        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();

            string prefix = @"D:\vq\";
            //ConnectTcp();
            engine.Init(prefix + @"flash\src\core\", prefix + @"flash\build\debugclg\");
            cppTextEditor = new CPPTextEditor(prefix + @"flash\src\core\geo\SphericalProjection.cpp", engine);
            Editors.Children.Add(cppTextEditor);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopNodes)));
        }

        private void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            cppTextEditor.Reparse();
        }
    }
}
