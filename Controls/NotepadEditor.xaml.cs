using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using NotepadByFerryPot.Models;
using NotepadByFerryPot.Services;

namespace NotepadByFerryPot.Controls
{
    public partial class NotepadEditor : UserControl
    {
        private ScrollViewer? _mainScrollViewer;
        private int _cachedLineCount = -1;

        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(
                nameof(Document),
                typeof(NotepadDocument),
                typeof(NotepadEditor),
                new PropertyMetadata(null, OnDocumentChanged));

        public NotepadDocument? Document
        {
            get => (NotepadDocument?)GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        private string? _lastSyncedSyntaxText;
        private bool _isSyncing = false;

        public TextBox EditorTextBox => MainTextBox;
        public RichTextBox RichEditor => FormattedRichTextBox;
        public TextBoxBase ActiveInputControl => _isFormattedView ? FormattedRichTextBox : MainTextBox;

        public void Undo()
        {
            if (_isFormattedView)
            {
                if (FormattedRichTextBox.CanUndo) FormattedRichTextBox.Undo();
            }
            else
            {
                if (MainTextBox.CanUndo) MainTextBox.Undo();
            }
        }

        public void Redo()
        {
            if (_isFormattedView)
            {
                if (FormattedRichTextBox.CanRedo) FormattedRichTextBox.Redo();
            }
            else
            {
                if (MainTextBox.CanRedo) MainTextBox.Redo();
            }
        }

        public void Cut()
        {
            if (_isFormattedView) FormattedRichTextBox.Cut();
            else MainTextBox.Cut();
        }

        public void Copy()
        {
            if (_isFormattedView) FormattedRichTextBox.Copy();
            else MainTextBox.Copy();
        }

        public void Paste()
        {
            if (_isFormattedView) FormattedRichTextBox.Paste();
            else MainTextBox.Paste();
        }

        public void SelectAll()
        {
            if (_isFormattedView) FormattedRichTextBox.SelectAll();
            else MainTextBox.SelectAll();
        }

        public void InsertText(string text)
        {
            if (_isFormattedView)
            {
                FormattedRichTextBox.Selection.Text = text;
                FormattedRichTextBox.CaretPosition = FormattedRichTextBox.Selection.End;
                FormattedRichTextBox.Focus();
                UpdateStatusFromFormatted();
            }
            else
            {
                MainTextBox.SelectedText = text;
                MainTextBox.CaretIndex += text.Length;
                MainTextBox.Focus();
            }
        }

        private bool _isFormattedView = false;
        public bool IsFormattedView
        {
            get => _isFormattedView;
            set => SetViewMode(value);
        }

        public void SetViewMode(bool formatted)
        {
            if (_isFormattedView == formatted) return;

            _isFormattedView = formatted;
            if (_isFormattedView)
            {
                // Switching from Syntax to Formatted
                if (FormattedRichTextBox.Document == null || MainTextBox.Text != _lastSyncedSyntaxText)
                {
                    try
                    {
                        var brush = (Brush)Application.Current.FindResource("MaterialDesignBody");
                        var ff = new FontFamily(SettingsService.Current.FontFamilyName);
                        var doc = MarkdownParserService.ParseToFlowDocument(MainTextBox.Text, ff, SettingsService.Current.FontSize, brush);
                        FormattedRichTextBox.Document = doc;
                        _lastSyncedSyntaxText = MainTextBox.Text;
                    }
                    catch
                    {
                        var doc = new FlowDocument(new Paragraph(new Run(MainTextBox.Text)));
                        FormattedRichTextBox.Document = doc;
                        _lastSyncedSyntaxText = MainTextBox.Text;
                    }
                }

                SyntaxViewGrid.Visibility = Visibility.Collapsed;
                FormattedRichTextBox.Visibility = Visibility.Visible;
                FormattedRichTextBox.Focus();
                UpdateStatusFromFormatted();
            }
            else
            {
                // Switching from Formatted to Syntax
                string markdown;
                try
                {
                    markdown = MarkdownParserService.FlowDocumentToMarkdown(FormattedRichTextBox.Document);
                }
                catch
                {
                    var range = new TextRange(FormattedRichTextBox.Document.ContentStart, FormattedRichTextBox.Document.ContentEnd);
                    markdown = range.Text;
                }

                // If markdown equals MainTextBox.Text, preserve MainTextBox undo stack!
                if (markdown != MainTextBox.Text)
                {
                    _isSyncing = true;
                    try
                    {
                        MainTextBox.SelectAll();
                        MainTextBox.SelectedText = markdown;
                        _lastSyncedSyntaxText = markdown;
                        if (Document != null)
                        {
                            Document.Content = markdown;
                            Document.IsModified = true;
                        }
                    }
                    finally
                    {
                        _isSyncing = false;
                    }
                }
                else
                {
                    _lastSyncedSyntaxText = MainTextBox.Text;
                }

                SyntaxViewGrid.Visibility = Visibility.Visible;
                FormattedRichTextBox.Visibility = Visibility.Collapsed;
                MainTextBox.Focus();
                MainTextBox_SelectionChanged(this, new RoutedEventArgs());
            }
        }

        public string GetCurrentContent()
        {
            if (_isFormattedView)
            {
                return MarkdownParserService.FlowDocumentToMarkdown(FormattedRichTextBox.Document);
            }
            return MainTextBox.Text;
        }

        public void SyncToDocument()
        {
            if (Document != null)
            {
                Document.Content = GetCurrentContent();
            }
        }

        public void UpdateFormattedView()
        {
            if (_isFormattedView)
            {
                try
                {
                    var brush = (Brush)Application.Current.FindResource("MaterialDesignBody");
                    var ff = new FontFamily(SettingsService.Current.FontFamilyName);
                    var doc = MarkdownParserService.ParseToFlowDocument(MainTextBox.Text, ff, SettingsService.Current.FontSize, brush);
                    FormattedRichTextBox.Document = doc;
                    _lastSyncedSyntaxText = MainTextBox.Text;
                }
                catch
                {
                    var doc = new FlowDocument(new Paragraph(new Run(MainTextBox.Text)));
                    FormattedRichTextBox.Document = doc;
                    _lastSyncedSyntaxText = MainTextBox.Text;
                }
            }
        }

        public NotepadEditor()
        {
            InitializeComponent();
            Loaded += NotepadEditor_Loaded;
        }

        private void NotepadEditor_Loaded(object sender, RoutedEventArgs e)
        {
            // Find ScrollViewer inside TextBox template
            _mainScrollViewer = FindVisualChild<ScrollViewer>(MainTextBox);
            if (_mainScrollViewer != null)
            {
                _mainScrollViewer.ScrollChanged += MainScrollViewer_ScrollChanged;
            }
            ApplySettings();
        }

        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NotepadEditor editor)
            {
                if (e.OldValue is NotepadDocument oldDoc)
                {
                    oldDoc.PropertyChanged -= editor.Doc_PropertyChanged;
                }

                if (e.NewValue is NotepadDocument newDoc)
                {
                    newDoc.PropertyChanged += editor.Doc_PropertyChanged;
                    editor.MainTextBox.Text = newDoc.Content;
                    editor.UpdateLineNumbers();
                    if (editor._isFormattedView)
                    {
                        editor.UpdateFormattedView();
                    }
                }
                else
                {
                    editor.MainTextBox.Text = string.Empty;
                }
            }
        }

        private void Doc_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isSyncing) return;
            if (e.PropertyName == nameof(NotepadDocument.Content) && Document != null)
            {
                if (MainTextBox.Text != Document.Content)
                {
                    int caret = MainTextBox.CaretIndex;
                    MainTextBox.Text = Document.Content;
                    MainTextBox.CaretIndex = Math.Min(caret, MainTextBox.Text.Length);
                }
                if (_isFormattedView)
                {
                    UpdateFormattedView();
                }
            }
        }

        public void ApplySettings()
        {
            var settings = SettingsService.Current;

            MainTextBox.FontFamily = new FontFamily(settings.FontFamilyName);
            MainTextBox.FontSize = settings.FontSize;
            MainTextBox.FontWeight = settings.FontWeightName switch
            {
                "Bold" => FontWeights.Bold,
                "Medium" => FontWeights.Medium,
                _ => FontWeights.Normal
            };
            MainTextBox.TextWrapping = settings.WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
            LineNumbersTextBlock.LineHeight = settings.FontSize * 1.35;

            LineNumbersColumn.Width = settings.ShowLineNumbers ? GridLength.Auto : new GridLength(0);
            LineNumbersBorder.Visibility = settings.ShowLineNumbers ? Visibility.Visible : Visibility.Collapsed;

            UpdateLineNumbers();
        }

        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_mainScrollViewer != null)
            {
                LineNumbersScrollViewer.ScrollToVerticalOffset(_mainScrollViewer.VerticalOffset);
            }
        }

        private void MainTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;
            if (Document != null && Document.Content != MainTextBox.Text)
            {
                Document.Content = MainTextBox.Text;
            }
            UpdateLineNumbers();
        }

        private void MainTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (Document == null) return;

            int caretIndex = MainTextBox.CaretIndex;
            int line = MainTextBox.GetLineIndexFromCharacterIndex(caretIndex);
            int lineStart = MainTextBox.GetCharacterIndexFromLineIndex(line);
            int col = caretIndex - lineStart;

            Document.CurrentLine = line + 1;
            Document.CurrentColumn = col + 1;
            Document.CharacterCount = MainTextBox.Text.Length;
        }

        #region Formatted View Event Handlers

        private void FormattedRichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateStatusFromFormatted();
        }

        private void FormattedRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Document != null)
            {
                Document.IsModified = true;
            }
            UpdateStatusFromFormatted();
        }

        private void FormattedRichTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Enter key and Tab key work natively in RichTextBox
        }

        private void UpdateStatusFromFormatted()
        {
            if (Document == null) return;
            try
            {
                var range = new TextRange(FormattedRichTextBox.Document.ContentStart, FormattedRichTextBox.Document.ContentEnd);
                string raw = range.Text.Replace("\r\n", "\n");
                Document.CharacterCount = Math.Max(0, raw.Length - 1);

                var words = raw.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                Document.WordCount = words.Length;

                var caret = FormattedRichTextBox.CaretPosition;
                var lineStart = caret.GetLineStartPosition(0);
                if (lineStart != null)
                {
                    int col = lineStart.GetOffsetToPosition(caret);
                    Document.CurrentColumn = Math.Max(1, col + 1);
                }
            }
            catch { }
        }

        #endregion

        private void UpdateLineNumbers()
        {
            if (!SettingsService.Current.ShowLineNumbers) return;

            int lineCount = MainTextBox.LineCount;
            if (lineCount <= 0) lineCount = 1;

            if (lineCount == _cachedLineCount) return;
            _cachedLineCount = lineCount;

            var sb = new StringBuilder(lineCount * 6);
            for (int i = 1; i <= lineCount; i++)
            {
                sb.AppendLine(i.ToString());
            }

            LineNumbersTextBlock.Text = sb.ToString().TrimEnd();
        }

        private void MainTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var settings = SettingsService.Current;

            // Handle Tab key
            if (e.Key == Key.Tab && (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift))
            {
                e.Handled = true;
                int caret = MainTextBox.CaretIndex;

                if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    string tabInsert = settings.UseSpacesForTab ? new string(' ', settings.TabSize) : "\t";
                    MainTextBox.SelectedText = tabInsert;
                    MainTextBox.CaretIndex = caret + tabInsert.Length;
                }
                return;
            }

            // Handle Auto-Indent on Enter
            if (e.Key == Key.Enter && settings.AutoIndent && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                int caret = MainTextBox.CaretIndex;
                int lineIndex = MainTextBox.GetLineIndexFromCharacterIndex(caret);
                string lineText = MainTextBox.GetLineText(lineIndex);

                // Find leading whitespace
                var leadingWhitespace = new StringBuilder();
                foreach (char ch in lineText)
                {
                    if (ch == ' ' || ch == '\t')
                        leadingWhitespace.Append(ch);
                    else
                        break;
                }

                string newlineIndent = Environment.NewLine + leadingWhitespace.ToString();
                MainTextBox.SelectedText = newlineIndent;
                MainTextBox.CaretIndex = caret + newlineIndent.Length;
                return;
            }

            // Handle Backspace deleting matching pair
            if (e.Key == Key.Back && settings.AutoCloseBrackets && Keyboard.Modifiers == ModifierKeys.None)
            {
                int caret = MainTextBox.CaretIndex;
                if (caret > 0 && caret < MainTextBox.Text.Length && MainTextBox.SelectionLength == 0)
                {
                    char prev = MainTextBox.Text[caret - 1];
                    char next = MainTextBox.Text[caret];

                    if ((prev == '(' && next == ')') ||
                        (prev == '[' && next == ']') ||
                        (prev == '{' && next == '}') ||
                        (prev == '"' && next == '"') ||
                        (prev == '\'' && next == '\''))
                    {
                        MainTextBox.Select(caret - 1, 2);
                        MainTextBox.SelectedText = string.Empty;
                        MainTextBox.CaretIndex = caret - 1;
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        private void MainTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var settings = SettingsService.Current;
            if (!settings.AutoCloseBrackets) return;

            string text = e.Text;
            if (string.IsNullOrEmpty(text)) return;

            char ch = text[0];
            char closing = ch switch
            {
                '(' => ')',
                '[' => ']',
                '{' => '}',
                '"' => '"',
                '\'' => '\'',
                '`' => '`',
                '*' => '*',
                '~' => '~',
                _ => '\0'
            };

            if (closing != '\0')
            {
                int caret = MainTextBox.CaretIndex;
                if (MainTextBox.SelectionLength > 0)
                {
                    // Wrap selection in brackets/quotes/markdown tokens
                    string sel = MainTextBox.SelectedText;
                    MainTextBox.SelectedText = $"{ch}{sel}{closing}";
                    MainTextBox.Select(caret + 1, sel.Length);
                    e.Handled = true;
                    return;
                }

                // If typing asterisk/backtick/tilde and next character is already that character, skip over it
                if ((ch == '*' || ch == '`' || ch == '~') &&
                    caret < MainTextBox.Text.Length &&
                    MainTextBox.Text[caret] == ch)
                {
                    MainTextBox.CaretIndex++;
                    e.Handled = true;
                    return;
                }

                // Auto-close pair
                MainTextBox.SelectedText = $"{ch}{closing}";
                MainTextBox.CaretIndex = caret + 1;
                e.Handled = true;
            }
            else if ((ch == ')' || ch == ']' || ch == '}' || ch == '"' || ch == '\'' || ch == '*' || ch == '`' || ch == '~') &&
                     MainTextBox.CaretIndex < MainTextBox.Text.Length &&
                     MainTextBox.Text[MainTextBox.CaretIndex] == ch)
            {
                // Skip over already closed character
                MainTextBox.CaretIndex++;
                e.Handled = true;
            }
        }

        #region Markdown Formatting Helpers

        private void EnsureWordSelectionIfEmpty()
        {
            if (FormattedRichTextBox.Selection.IsEmpty)
            {
                var caret = FormattedRichTextBox.CaretPosition;
                if (caret == null) return;

                TextPointer start = caret;
                while (start != null && start.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text)
                {
                    string prev = start.GetTextInRun(LogicalDirection.Backward);
                    if (string.IsNullOrEmpty(prev) || char.IsWhiteSpace(prev[^1])) break;
                    var prevPtr = start.GetPositionAtOffset(-1, LogicalDirection.Backward);
                    if (prevPtr == null) break;
                    start = prevPtr;
                }

                TextPointer end = caret;
                while (end != null && end.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string next = end.GetTextInRun(LogicalDirection.Forward);
                    if (string.IsNullOrEmpty(next) || char.IsWhiteSpace(next[0])) break;
                    var nextPtr = end.GetPositionAtOffset(1, LogicalDirection.Forward);
                    if (nextPtr == null) break;
                    end = nextPtr;
                }

                if (start != null && end != null && start.CompareTo(end) < 0)
                {
                    FormattedRichTextBox.Selection.Select(start, end);
                }
            }
        }

        public void ToggleWrap(string prefix, string suffix)
        {
            if (_isFormattedView)
            {
                EnsureWordSelectionIfEmpty();
                if (prefix == "**")
                {
                    EditingCommands.ToggleBold.Execute(null, FormattedRichTextBox);
                }
                else if (prefix == "*")
                {
                    EditingCommands.ToggleItalic.Execute(null, FormattedRichTextBox);
                }
                else if (prefix == "***")
                {
                    EditingCommands.ToggleBold.Execute(null, FormattedRichTextBox);
                    EditingCommands.ToggleItalic.Execute(null, FormattedRichTextBox);
                }
                else if (prefix == "~~")
                {
                    var range = FormattedRichTextBox.Selection;
                    var decs = range.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
                    bool hasStrike = decs != null && decs.Count > 0;
                    if (hasStrike)
                        range.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
                    else
                        range.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Strikethrough);
                }
                else if (prefix == "`")
                {
                    var range = FormattedRichTextBox.Selection;
                    var font = range.GetPropertyValue(TextElement.FontFamilyProperty) as FontFamily;
                    if (font != null && font.Source == "Consolas")
                    {
                        range.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily(SettingsService.Current.FontFamilyName));
                        range.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
                    }
                    else
                    {
                        range.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Consolas"));
                        range.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)));
                    }
                }
                FormattedRichTextBox.Focus();
                return;
            }

            int selStart = MainTextBox.SelectionStart;
            int selLength = MainTextBox.SelectionLength;
            string text = MainTextBox.Text;

            // If no text is selected, check if caret is inside a word
            if (selLength == 0 && text.Length > 0)
            {
                int wordStart = selStart;
                int wordEnd = selStart;

                while (wordStart > 0 && !char.IsWhiteSpace(text[wordStart - 1]) && text[wordStart - 1] != '*' && text[wordStart - 1] != '~' && text[wordStart - 1] != '`')
                    wordStart--;

                while (wordEnd < text.Length && !char.IsWhiteSpace(text[wordEnd]) && text[wordEnd] != '*' && text[wordEnd] != '~' && text[wordEnd] != '`')
                    wordEnd++;

                if (wordEnd > wordStart)
                {
                    // Check if word is already wrapped with prefix and suffix
                    if (wordStart >= prefix.Length && wordEnd + suffix.Length <= text.Length &&
                        text.Substring(wordStart - prefix.Length, prefix.Length) == prefix &&
                        text.Substring(wordEnd, suffix.Length) == suffix)
                    {
                        // Unwrap
                        string word = text.Substring(wordStart, wordEnd - wordStart);
                        MainTextBox.Select(wordStart - prefix.Length, word.Length + prefix.Length + suffix.Length);
                        MainTextBox.SelectedText = word;
                        MainTextBox.Select(wordStart - prefix.Length, word.Length);
                        MainTextBox.Focus();
                        return;
                    }
                    else
                    {
                        // Wrap the word
                        string word = text.Substring(wordStart, wordEnd - wordStart);
                        MainTextBox.Select(wordStart, word.Length);
                        MainTextBox.SelectedText = prefix + word + suffix;
                        MainTextBox.Select(wordStart + prefix.Length, word.Length);
                        MainTextBox.Focus();
                        return;
                    }
                }
            }

            if (selLength > 0)
            {
                string selected = MainTextBox.SelectedText;

                // Check if already wrapped inside selection
                if (selected.StartsWith(prefix) && selected.EndsWith(suffix) && selected.Length >= prefix.Length + suffix.Length)
                {
                    string unwrapped = selected.Substring(prefix.Length, selected.Length - prefix.Length - suffix.Length);
                    MainTextBox.SelectedText = unwrapped;
                    MainTextBox.Select(selStart, unwrapped.Length);
                }
                // Check if wrapped outside selection
                else if (selStart >= prefix.Length && selStart + selLength + suffix.Length <= text.Length &&
                         text.Substring(selStart - prefix.Length, prefix.Length) == prefix &&
                         text.Substring(selStart + selLength, suffix.Length) == suffix)
                {
                    MainTextBox.Select(selStart - prefix.Length, selLength + prefix.Length + suffix.Length);
                    MainTextBox.SelectedText = selected;
                    MainTextBox.Select(selStart - prefix.Length, selected.Length);
                }
                else
                {
                    string wrapped = prefix + selected + suffix;
                    MainTextBox.SelectedText = wrapped;
                    MainTextBox.Select(selStart + prefix.Length, selected.Length);
                }
            }
            else
            {
                // No selection: check surrounding
                if (selStart >= prefix.Length && selStart + suffix.Length <= text.Length &&
                    text.Substring(selStart - prefix.Length, prefix.Length) == prefix &&
                    text.Substring(selStart, suffix.Length) == suffix)
                {
                    MainTextBox.Select(selStart - prefix.Length, prefix.Length + suffix.Length);
                    MainTextBox.SelectedText = string.Empty;
                    MainTextBox.CaretIndex = selStart - prefix.Length;
                }
                else
                {
                    MainTextBox.SelectedText = prefix + suffix;
                    MainTextBox.CaretIndex = selStart + prefix.Length;
                }
            }
            MainTextBox.Focus();
        }

        public void ApplyHeading(string prefix)
        {
            if (_isFormattedView)
            {
                var caret = FormattedRichTextBox.CaretPosition;
                var p = caret.Paragraph;
                if (p != null)
                {
                    int level = 0;
                    while (level < prefix.Length && prefix[level] == '#') level++;
                    double baseFontSize = SettingsService.Current.FontSize;

                    // If paragraph already has this heading level, toggle back to normal paragraph
                    if (p.Tag is string currentTag && currentTag == "h" + level)
                    {
                        level = 0;
                    }

                    if (level == 0)
                    {
                        p.FontSize = baseFontSize;
                        p.FontWeight = FontWeights.Normal;
                        p.Tag = null;
                        p.Margin = new Thickness(0, 0, 0, baseFontSize * 0.6);
                        p.BorderThickness = new Thickness(0);
                    }
                    else
                    {
                        double size = level switch
                        {
                            1 => baseFontSize * 1.85,
                            2 => baseFontSize * 1.5,
                            3 => baseFontSize * 1.25,
                            4 => baseFontSize * 1.1,
                            5 => baseFontSize * 1.05,
                            6 => baseFontSize * 1.0,
                            _ => baseFontSize
                        };
                        p.FontSize = size;
                        p.FontWeight = level <= 2 ? FontWeights.Bold : FontWeights.SemiBold;
                        p.Tag = "h" + level;
                        p.Margin = new Thickness(0, baseFontSize * (level == 1 ? 1.2 : 0.8), 0, baseFontSize * 0.4);

                        if (level == 1)
                        {
                            try
                            {
                                p.BorderBrush = (Brush)Application.Current.FindResource("MaterialDesignDivider");
                                p.BorderThickness = new Thickness(0, 0, 0, 1);
                                p.Padding = new Thickness(0, 0, 0, 6);
                            }
                            catch { }
                        }
                        else
                        {
                            p.BorderThickness = new Thickness(0);
                        }
                    }
                }
                FormattedRichTextBox.Focus();
                return;
            }

            int caretIndex = MainTextBox.CaretIndex;
            int lineIndex = MainTextBox.GetLineIndexFromCharacterIndex(caretIndex);
            int lineStart = MainTextBox.GetCharacterIndexFromLineIndex(lineIndex);
            int lineLength = MainTextBox.GetLineLength(lineIndex);
            string lineText = MainTextBox.GetLineText(lineIndex);

            string ending = "";
            if (lineText.EndsWith("\r\n")) { ending = "\r\n"; lineText = lineText[..^2]; }
            else if (lineText.EndsWith("\n") || lineText.EndsWith("\r")) { ending = lineText[^1].ToString(); lineText = lineText[..^1]; }

            string trimmed = lineText.TrimStart();
            int leadingSpaces = lineText.Length - trimmed.Length;
            string indent = lineText.Substring(0, leadingSpaces);

            if (trimmed.StartsWith("#"))
            {
                int hashCount = 0;
                while (hashCount < trimmed.Length && trimmed[hashCount] == '#')
                    hashCount++;
                if (hashCount < trimmed.Length && trimmed[hashCount] == ' ')
                    hashCount++;
                trimmed = trimmed.Substring(hashCount);
            }

            string newLineText = indent + prefix + trimmed + ending;
            MainTextBox.Select(lineStart, lineLength);
            MainTextBox.SelectedText = newLineText;
            MainTextBox.CaretIndex = Math.Min(lineStart + newLineText.Length - ending.Length, MainTextBox.Text.Length);
            MainTextBox.Focus();
        }

        public void ApplyList(string listType)
        {
            if (_isFormattedView)
            {
                if (listType == "number")
                {
                    EditingCommands.ToggleNumbering.Execute(null, FormattedRichTextBox);
                }
                else
                {
                    EditingCommands.ToggleBullets.Execute(null, FormattedRichTextBox);
                }
                FormattedRichTextBox.Focus();
                return;
            }

            int selStart = MainTextBox.SelectionStart;
            int selLength = MainTextBox.SelectionLength;

            int startLine = MainTextBox.GetLineIndexFromCharacterIndex(selStart);
            int endLine = MainTextBox.GetLineIndexFromCharacterIndex(selStart + Math.Max(0, selLength - 1));

            int firstLineStart = MainTextBox.GetCharacterIndexFromLineIndex(startLine);
            int lastLineLength = MainTextBox.GetLineLength(endLine);
            int lastLineStart = MainTextBox.GetCharacterIndexFromLineIndex(endLine);
            int totalSpan = (lastLineStart + lastLineLength) - firstLineStart;

            var sb = new StringBuilder();
            for (int i = startLine; i <= endLine; i++)
            {
                string rawLine = MainTextBox.GetLineText(i);
                string ending = "";
                if (rawLine.EndsWith("\r\n")) { ending = "\r\n"; rawLine = rawLine[..^2]; }
                else if (rawLine.EndsWith("\n") || rawLine.EndsWith("\r")) { ending = rawLine[^1].ToString(); rawLine = rawLine[..^1]; }

                string trimmed = rawLine.TrimStart();
                string indent = rawLine.Substring(0, rawLine.Length - trimmed.Length);

                bool hadMatchingMarker = false;
                if (trimmed.StartsWith("- [ ] ") || trimmed.StartsWith("- [x] "))
                {
                    if (listType == "check") hadMatchingMarker = true;
                    trimmed = trimmed.Substring(6);
                }
                else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    if (listType == "bullet") hadMatchingMarker = true;
                    trimmed = trimmed.Substring(2);
                }
                else
                {
                    int dotIdx = trimmed.IndexOf(". ");
                    if (dotIdx > 0 && int.TryParse(trimmed.Substring(0, dotIdx), out _))
                    {
                        if (listType == "number") hadMatchingMarker = true;
                        trimmed = trimmed.Substring(dotIdx + 2);
                    }
                }

                if (!hadMatchingMarker)
                {
                    string marker = listType switch
                    {
                        "bullet" => "- ",
                        "number" => $"{i - startLine + 1}. ",
                        "check" => "- [ ] ",
                        _ => ""
                    };
                    trimmed = marker + trimmed;
                }

                sb.Append(indent).Append(trimmed).Append(ending);
            }

            MainTextBox.Select(firstLineStart, totalSpan);
            MainTextBox.SelectedText = sb.ToString();
            MainTextBox.Focus();
        }

        public void InsertLink()
        {
            if (_isFormattedView)
            {
                string text = FormattedRichTextBox.Selection.Text;
                if (string.IsNullOrEmpty(text)) text = "link";

                var hl = new Hyperlink(new Run(text))
                {
                    NavigateUri = new Uri("https://"),
                    ToolTip = "https://"
                };
                try { hl.Foreground = (Brush)Application.Current.FindResource("MaterialDesign.Brush.Primary"); } catch { }

                var p = FormattedRichTextBox.CaretPosition.Paragraph;
                if (p != null)
                {
                    FormattedRichTextBox.Selection.Text = "";
                    p.Inlines.Add(hl);
                }
                FormattedRichTextBox.Focus();
                return;
            }

            int selStart = MainTextBox.SelectionStart;
            int selLen = MainTextBox.SelectionLength;

            if (selLen > 0)
            {
                string selText = MainTextBox.SelectedText;
                string replacement = $"[{selText}](https://)";
                MainTextBox.SelectedText = replacement;
                MainTextBox.Select(selStart + selText.Length + 3, 8);
            }
            else
            {
                string template = "[link](https://)";
                MainTextBox.SelectedText = template;
                MainTextBox.Select(selStart + 1, 4);
            }
            MainTextBox.Focus();
        }

        public void InsertTable(int rows = 3, int cols = 3)
        {
            if (_isFormattedView)
            {
                var tableLines = new List<string>();
                var headerCells = new List<string>();
                for (int c = 1; c <= cols; c++) headerCells.Add($"Column {c}");
                tableLines.Add("| " + string.Join(" | ", headerCells) + " |");

                var sepCells = new List<string>();
                for (int c = 1; c <= cols; c++) sepCells.Add("---");
                tableLines.Add("| " + string.Join(" | ", sepCells) + " |");

                for (int r = 1; r <= rows; r++)
                {
                    var rowCells = new List<string>();
                    for (int c = 1; c <= cols; c++) rowCells.Add($"Item {r}.{c}");
                    tableLines.Add("| " + string.Join(" | ", rowCells) + " |");
                }

                var brush = (Brush)Application.Current.FindResource("MaterialDesignBody");
                var tableBlock = MarkdownParserService.CreateTable(tableLines, SettingsService.Current.FontSize, brush);
                FormattedRichTextBox.Document.Blocks.Add(tableBlock);
                FormattedRichTextBox.Focus();
                return;
            }

            int selStart = MainTextBox.SelectionStart;
            var sb = new StringBuilder();

            int line = MainTextBox.GetLineIndexFromCharacterIndex(selStart);
            int lineStart = MainTextBox.GetCharacterIndexFromLineIndex(line);
            if (selStart > lineStart)
            {
                sb.AppendLine();
            }

            // Header row
            sb.Append("|");
            for (int c = 1; c <= cols; c++)
            {
                sb.Append($" Column {c} |");
            }
            sb.AppendLine();

            // Separator row
            sb.Append("|");
            for (int c = 1; c <= cols; c++)
            {
                sb.Append(" ---------- |");
            }
            sb.AppendLine();

            // Data rows
            for (int r = 1; r <= rows; r++)
            {
                sb.Append("|");
                for (int c = 1; c <= cols; c++)
                {
                    sb.Append($" Item {r}.{c}   |");
                }
                sb.AppendLine();
            }

            string tableText = sb.ToString();
            MainTextBox.SelectedText = tableText;
            MainTextBox.CaretIndex = selStart + tableText.Length;
            MainTextBox.Focus();
        }

        public void ClearFormatting()
        {
            if (_isFormattedView)
            {
                var range = new TextRange(FormattedRichTextBox.Selection.Start, FormattedRichTextBox.Selection.End);
                range.ClearAllProperties();
                var p = FormattedRichTextBox.CaretPosition.Paragraph;
                if (p != null)
                {
                    p.Tag = null;
                    p.FontSize = SettingsService.Current.FontSize;
                    p.FontWeight = FontWeights.Normal;
                    p.BorderThickness = new Thickness(0);
                }
                FormattedRichTextBox.Focus();
                return;
            }

            int selStart = MainTextBox.SelectionStart;
            int selLen = MainTextBox.SelectionLength;

            if (selLen == 0)
            {
                int line = MainTextBox.GetLineIndexFromCharacterIndex(selStart);
                selStart = MainTextBox.GetCharacterIndexFromLineIndex(line);
                selLen = MainTextBox.GetLineLength(line);
            }

            string text = MainTextBox.Text.Substring(selStart, selLen);

            // Strip markdown formatting
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");
            text = text.Replace("**", "")
                       .Replace("__", "")
                       .Replace("~~", "")
                       .Replace("`", "")
                       .Replace("*", "");

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                string indent = lines[i].Substring(0, lines[i].Length - trimmed.Length);

                if (trimmed.StartsWith("#"))
                {
                    int h = 0;
                    while (h < trimmed.Length && trimmed[h] == '#') h++;
                    if (h < trimmed.Length && trimmed[h] == ' ') h++;
                    trimmed = trimmed.Substring(h);
                }
                else if (trimmed.StartsWith("- [ ] ") || trimmed.StartsWith("- [x] "))
                {
                    trimmed = trimmed.Substring(6);
                }
                else if (trimmed.StartsWith("- ") || trimmed.StartsWith("+ "))
                {
                    trimmed = trimmed.Substring(2);
                }
                else
                {
                    int dot = trimmed.IndexOf(". ");
                    if (dot > 0 && int.TryParse(trimmed.Substring(0, dot), out _))
                    {
                        trimmed = trimmed.Substring(dot + 2);
                    }
                }
                lines[i] = indent + trimmed;
            }

            string cleaned = string.Join(Environment.NewLine, lines);
            MainTextBox.Select(selStart, selLen);
            MainTextBox.SelectedText = cleaned;
            MainTextBox.Select(selStart, cleaned.Length);
            MainTextBox.Focus();
        }

        #endregion

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
            return null;
        }
    }
}
