using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
using System.ComponentModel;
using System.Runtime;
using symlib;

namespace cppsymview
{
    /// <summary>
    /// Interaction logic for FolderView.xaml
    /// </summary>
    public partial class FolderView : UserControl, INotifyPropertyChanged
    {
        public FolderView()
        {
            this.DataContext = this;
            InitializeComponent();
            this.FoldersTV.SelectedItemChanged += FoldersTV_SelectedItemChanged;
        }

        private void FoldersTV_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FVItem fvItem &&
                fvItem.Type == FVItemType.File)
            {
                OnFileSelected?.Invoke(this, fvItem.FullPath);
            }
        }

        public event EventHandler<string> OnFileSelected;

        string root = string.Empty;

        FVItem? topFolder = null;
        public event PropertyChangedEventHandler? PropertyChanged;

        public IEnumerable<FVItem> TopItems => topFolder?.Items ?? new List<FVItem>();

        public string Root { get => root; set { root = value; OnRootSet(); } }

        void OnRootSet()
        {
            topFolder = new FVFolder(new DirectoryInfo(root));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopItems)));
        }

        private void SearchTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (tb.Text.Length > 3)
            {

            }
        }

        public void BuildSourceFileTree(CPPEngineFile engine)
        {
            FVTFolder ftopFolder = new FVTFolder("", null);
            topFolder = ftopFolder;
            foreach (var sf in engine.SourceFiles)
            {
                string []dirs = sf.Split("\\");
                ftopFolder.AddPath(dirs, 0);
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TopItems)));
        }
    }

    public enum FVItemType
    {
        Folder,
        File
    }
    public abstract class FVItem
    {
        public abstract FVItemType Type { get; }
        public abstract List<FVItem> Items { get; }

        public abstract string Name { get; }
        public abstract string FullPath { get; }
    }

    public class FVFolder : FVItem
    {
        DirectoryInfo dirInfo;
        public FVFolder(DirectoryInfo di) 
        {
            Items = new List<FVItem>();
            var dirs = di.GetDirectories();
            foreach (DirectoryInfo dir in dirs) 
            {
                Items.Add(new FVFolder(dir));
            }
            var files = di.GetFiles();
            foreach (FileInfo fi in files)
            {
                Items.Add(new FVFile(fi));
            }

            dirInfo = di;
        }

        public override List<FVItem> Items { get; }
        public override FVItemType Type => FVItemType.Folder;

        public override string Name => dirInfo.Name;

        public override string FullPath => dirInfo.FullName;

        public override string ToString() => dirInfo.Name;
    }

    public class FVFile : FVItem
    {
        FileInfo fileInfo;
        public FVFile(FileInfo fi) 
        {
            fileInfo = fi;
        }

        public override FVItemType Type => FVItemType.File;

        public override List<FVItem> Items => new List<FVItem>();

        public override string ToString() => fileInfo.Name;

        public override string Name => fileInfo.Name;

        public override string FullPath => fileInfo.FullName;

    }

    public class FVTFolder : FVItem
    {
        string name;
        FVTFolder parent;
        FVItemType itemType;
        public FVTFolder(string _name, FVTFolder _parent)
        {
            name = _name;
            parent = _parent;
            Items = new List<FVItem>();
            itemType = FVItemType.Folder;
        }
        public void AddPath(string[] paths, int idx)
        {
            if (paths.Length == idx)
            {
                itemType = FVItemType.File;
                return;
            }
            FVTFolder fvt;
            FVItem? item = Items.FirstOrDefault(i => i.Name == paths[idx]);
            if (item != null)
                fvt = (FVTFolder)item;
            else
            {
                fvt = new FVTFolder(paths[idx], this);
                Items.Add(fvt);
            }
            fvt.AddPath(paths, idx + 1);
        }


        public override List<FVItem> Items { get; }
        public override FVItemType Type => itemType;

        public override string Name => name;

        public override string FullPath => parent == null ? name : Path.Combine(parent.FullPath, name);

        public override string ToString() => name;
    }

}
