﻿using System;
using System.Collections.Generic;
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
    public partial class MainWindow : Window
    {
        CPPEngineFile engine = new CPPEngineFile();
        CPPTextEditor cppTextEditor;
        public MainWindow()
        {
            InitializeComponent();

            //ConnectTcp();
            engine.Init(@"C:\flash\src\core\", @"C:\flash\build\debugclg\");
            cppTextEditor = new CPPTextEditor(@"C:\flash\src\core\geo\SphericalProjection.cpp", engine);
            Editors.Children.Add(cppTextEditor);
        }

        private void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            cppTextEditor.Reparse();
        }
    }
}
