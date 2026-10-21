using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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

        public TextBox EditorTextBox => MainTextBox;

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
                }
                else
                {
                    editor.MainTextBox.Text = string.Empty;
                }
            }
        }

        private void Doc_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NotepadDocument.Content) && Document != null)
            {
                if (MainTextBox.Text != Document.Content)
                {
                    int caret = MainTextBox.CaretIndex;
                    MainTextBox.Text = Document.Content;
                    MainTextBox.CaretIndex = Math.Min(caret, MainTextBox.Text.Length);
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
        }

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
                _ => '\0'
            };

            if (closing != '\0')
            {
                int caret = MainTextBox.CaretIndex;
                if (MainTextBox.SelectionLength > 0)
                {
                    // Wrap selection in brackets/quotes
                    string sel = MainTextBox.SelectedText;
                    MainTextBox.SelectedText = $"{ch}{sel}{closing}";
                    MainTextBox.Select(caret + 1, sel.Length);
                    e.Handled = true;
                    return;
                }

                // Auto-close pair
                MainTextBox.SelectedText = $"{ch}{closing}";
                MainTextBox.CaretIndex = caret + 1;
                e.Handled = true;
            }
            else if ((ch == ')' || ch == ']' || ch == '}' || ch == '"' || ch == '\'') &&
                     MainTextBox.CaretIndex < MainTextBox.Text.Length &&
                     MainTextBox.Text[MainTextBox.CaretIndex] == ch)
            {
                // Skip over already closed character
                MainTextBox.CaretIndex++;
                e.Handled = true;
            }
        }

        #region Markdown Formatting Helpers

        public void ToggleWrap(string prefix, string suffix)
        {
            int selStart = MainTextBox.SelectionStart;
            int selLength = MainTextBox.SelectionLength;
            string text = MainTextBox.Text;

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
