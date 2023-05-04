using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using System.Reflection;
using System.Xml;
using System.IO.Packaging;
using System.ComponentModel;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit.Folding;

namespace cppsymview
{
    public class CPPTextEditor : TextEditor, INotifyPropertyChanged
    {
        CompletionWindow completionWindow;

        public event PropertyChangedEventHandler PropertyChanged;

        public string CPPName { get; set; }

        public string FilePath { get; set; }
        public CPPEngineFile Engine { get; set; }

        public CPPTextEditor(string path, CPPEngineFile engine)
        {
            FilePath = path;
            Engine = engine;
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
        }

        private void TextArea_SelectionChanged(object? sender, EventArgs e)
        {
        }

        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            
        }

        private void ParentWnd_BeforeCPPRun(object sender, bool e)
        {
            this.Save();
        }

        public void Reparse()
        {
            this.Engine.CompileFile(this.FilePath);
        }
        void Reload()
        {
            CPPName = Path.GetFileName(FilePath);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CPPName"));
            Load(this.FilePath);
            Reparse();
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
    }
}