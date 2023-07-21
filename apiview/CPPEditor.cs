using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using symlib;
using static ICSharpCode.AvalonEdit.Document.TextDocumentWeakEventManager;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace cppsymview
{
    public class CPPTextEditor : TextEditor, INotifyPropertyChanged
    {
        CompletionWindow completionWindow;

        public event PropertyChangedEventHandler PropertyChanged;

        public string CPPName { get; set; }

        public Brush TabBrush => Brushes.Aqua;
        public string FilePath { get; set; }
        public CPPEngineFile Engine { get; set; }
        int srcFileKey;

        public event EventHandler<Node> NodeChanged;
        FileSystemWatcher watcher;

        public CPPTextEditor(string path, CPPEngineFile engine)
        {
            FilePath = path;
            Engine = engine;
            this.srcFileKey = this.Engine.GetSourceFile(this.FilePath);
            Engine.SelectedNodeChanged += Engine_SelectedNodeChanged;
            this.FontFamily = new FontFamily("Consolas");
            this.FontSize = 14;
            this.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            this.ShowLineNumbers = true;
            this.Background = new SolidColorBrush(Color.FromArgb(255, 225, 225, 225));

            string ext = Path.GetExtension(path).ToLower();
            string hightlightFile = null;
            if (ext == ".cpp" || ext == ".h") hightlightFile = "cppsymview.CPP-Mode.xshd";
            else if (ext == ".glsl" || ext == ".gsl") hightlightFile = "partmake.glsl.xshd";
            if (hightlightFile != null)
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(hightlightFile))
                {
                    using (XmlTextReader reader = new XmlTextReader(s))
                    {
                        this.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }

            TextArea.Caret.PositionChanged += Caret_PositionChanged;
            TextArea.SelectionChanged += TextArea_SelectionChanged;
            //Reload();
            SearchPanel.Install(this);
            Reload();

            watcher = new FileSystemWatcher(Path.GetDirectoryName(path));

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Changed += Watcher_Changed;

            watcher.Filter = Path.GetFileName(path);
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Dispatcher.BeginInvoke(() => { Reload(); });                
        }

        OffsetColorizer curNodeColorizer = null;

        private void Engine_SelectedNodeChanged(object? sender, Node e)
        {
            if (curNodeColorizer != null)
                this.TextArea.TextView.LineTransformers.Remove(curNodeColorizer);
            curNodeColorizer = null;
            if (e == null || e.SourceFile != this.srcFileKey)
                return;
            curNodeColorizer = new OffsetColorizer();
            curNodeColorizer.StartOffset = (int)e.StartOffset;
            curNodeColorizer.EndOffset = (int)e.EndOffset;
            this.TextArea.TextView.LineTransformers.Add(curNodeColorizer);
        }

        private void TextArea_SelectionChanged(object? sender, EventArgs e)
        {
        }

        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            Node node = this.Engine.GetNodeFor(this.srcFileKey, (uint)this.CaretOffset);
            if (node != null)
            {
                node.Expand();
                node.Select();
            }
            NodeChanged?.Invoke(this, node);
        }

        private void ParentWnd_BeforeCPPRun(object sender, bool e)
        {
            this.Save();
        }

        public void MakeActive()
        {
            this.Engine.SetCurrentFile(this.FilePath);
        }
        void Reload()
        {
            CPPName = Path.GetFileName(FilePath);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CPPName"));
            Document.FileName = this.FilePath;
            Load(this.FilePath);
            //this.Engine.SendSourceCode(Document.Text);
        }
        public void SaveAs(string path)
        {
            this.FilePath = path;
            CPPName = Path.GetFileName(path);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CPPName"));
            Save(path);
        }
        public void Save()
        {
            Save(this.FilePath);
        }

        class ErrorColorizer : DocumentColorizingTransformer
        {
            public ErrorColorizer()
            {
            }


            protected override void ColorizeLine(ICSharpCode.AvalonEdit.Document.DocumentLine line)
            {
            }

            void ApplyChanges(VisualLineElement element)
            {

                // Create an underline text decoration. Default is underline.
                TextDecoration myUnderline = new TextDecoration();
                Brush wavyBrush = (Brush)System.Windows.Application.Current.Resources["WavyBrush"];

                // Create a linear gradient pen for the text decoration.
                Pen myPen = new Pen();
                myPen.Brush = wavyBrush;
                myPen.Thickness = 6;
                myUnderline.Pen = myPen;
                myUnderline.PenThicknessUnit = TextDecorationUnit.FontRecommended;

                // apply changes here
                element.TextRunProperties.SetTextDecorations(new TextDecorationCollection() { myUnderline });

            }
        }

        public class MyCompletionData : ICompletionData
        {
            int startReplaceIdx = 0;
            public MyCompletionData(string text, int _startReplaceIdx)
            {
                this.Text = text;
                this.startReplaceIdx = _startReplaceIdx;
            }

            public System.Windows.Media.ImageSource Image
            {
                get { return null; }
            }

            public string Text { get; private set; }

            // Use this property if you want to show a fancy UIElement in the list.
            public object Content
            {
                get { return this.Text; }
            }

            public object DeCPPion
            {
                get { return "DeCPPion for " + this.Text; }
            }

            public double Priority => 1.0;

            public object Description => throw new NotImplementedException();

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, this.Text.Substring(startReplaceIdx));
            }
        }

        public class OffsetColorizer : DocumentColorizingTransformer
        {
            public int StartOffset { get; set; }
            public int EndOffset { get; set; }

            protected override void ColorizeLine(DocumentLine line)
            {
                if (line.Length == 0)
                    return;
                                
                if (StartOffset >= line.EndOffset || EndOffset < line.Offset)
                    return;

                int start = line.Offset > StartOffset ? line.Offset : StartOffset;
                int end = EndOffset > line.EndOffset ? line.EndOffset : EndOffset;

                ChangeLinePart(start, end, element => element.TextRunProperties.SetBackgroundBrush(Brushes.AliceBlue));
            }
        }
    }
}