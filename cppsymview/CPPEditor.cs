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
        public CPPEngine Engine { get; set; }

        public CPPTextEditor(string path, CPPEngine engine)
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

            // in the constructor:
            this.TextArea.TextEntering += TextArea_TextEntering;
            this.TextArea.TextEntered += TextArea_TextEntered;
            //Reload();
            SearchPanel.Install(this);
            Reload();
        }


        private void ParentWnd_BeforeCPPRun(object sender, bool e)
        {
            this.Save();
        }

        void Reload()
        {
            CPPName = Path.GetFileName(FilePath);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CPPName"));
            Load(this.FilePath);
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

        private void TextArea_TextEntered(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            char[] symbolTermChars = new char[] { ' ', '\t', '{', '(' };
            List<string> members = null;
            if (e.Text == ".")
            {
                int spaceOffset = this.Text.LastIndexOfAny(symbolTermChars, this.CaretOffset);
                int len = this.CaretOffset - spaceOffset - 2;
                if (len <= 0)
                    return;
                string word = this.Text.Substring(spaceOffset + 1, len);
                //members = Engine.CodeComplete(this.Text, spaceOffset + 1, word, CPPEngine.CodeCompleteType.Member);
            }
            else if (e.Text == "(")
            {
                int spaceOffset = this.Text.LastIndexOfAny(symbolTermChars, this.CaretOffset);
                int len = this.CaretOffset - spaceOffset - 2;
                if (len <= 0)
                    return;
                string word = this.Text.Substring(spaceOffset + 1, len);
                //members = Engine.CodeComplete(this.Text, spaceOffset + 1, word, CPPEngine.CodeCompleteType.Function);
            }
            else if (e.Text == " ")
            {
                int spaceOffset = this.Text.LastIndexOfAny(symbolTermChars, this.CaretOffset - 2);
                int len = this.CaretOffset - spaceOffset - 2;
                if (len != 3 || this.Text.Substring(spaceOffset + 1, 3) != "new")
                    return;
                //members = Engine.CodeComplete(this.Text, spaceOffset + 1, "new", CPPEngine.CodeCompleteType.New);

            }

            if (members != null)
            {
                completionWindow = new CompletionWindow(this.TextArea);
                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                foreach (var member in members)
                {
                    data.Add(new MyCompletionData(member, 0));
                }
                completionWindow.Show();
                completionWindow.Closed += delegate
                {
                    completionWindow = null;
                };
            }
        }

        private void TextArea_TextEntering(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
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