using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using NotepadByFerryPot.Controls;

namespace NotepadByFerryPot.Views
{
    public partial class FindReplaceWindow : Window
    {
        private readonly NotepadEditor? _editor;
        private readonly TextBox? _fallbackTextBox;

        public FindReplaceWindow(NotepadEditor editor)
        {
            InitializeComponent();
            _editor = editor;
            Loaded += (s, e) => FindTextBox.Focus();
        }

        public FindReplaceWindow(TextBox targetTextBox)
        {
            InitializeComponent();
            _fallbackTextBox = targetTextBox;
            Loaded += (s, e) => FindTextBox.Focus();
        }

        private NotepadEditor? GetActiveEditor()
        {
            if (Owner is MainWindow mainWin)
            {
                return mainWin.ActiveEditor;
            }
            return _editor;
        }

        private TextBox? GetSyntaxTextBox()
        {
            var editor = GetActiveEditor();
            if (editor != null) return editor.EditorTextBox;
            return _fallbackTextBox;
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindNext();
            }
        }

        private void FindNext_Click(object sender, RoutedEventArgs e)
        {
            FindNext();
        }

        private Regex? CreateRegex(string query, bool matchCase, bool wholeWord, bool useRegex, out string? errorMessage)
        {
            errorMessage = null;
            string pattern;
            if (useRegex)
            {
                pattern = wholeWord ? $@"\b({query})\b" : query;
            }
            else
            {
                pattern = Regex.Escape(query);
                if (wholeWord)
                {
                    pattern = $@"\b{pattern}\b";
                }
            }

            RegexOptions options = RegexOptions.Multiline;
            if (!matchCase) options |= RegexOptions.IgnoreCase;

            try
            {
                return new Regex(pattern, options, TimeSpan.FromSeconds(1));
            }
            catch (ArgumentException ex)
            {
                errorMessage = ex.Message;
                return null;
            }
        }

        private bool FindNext()
        {
            string query = FindTextBox.Text;
            if (string.IsNullOrEmpty(query)) return false;

            bool matchCase = MatchCaseCheckBox.IsChecked == true;
            bool wholeWord = MatchWholeWordCheckBox.IsChecked == true;
            bool searchUp = SearchUpCheckBox.IsChecked == true;
            bool useRegex = UseRegexCheckBox.IsChecked == true;

            var regex = CreateRegex(query, matchCase, wholeWord, useRegex, out string? error);
            if (regex == null)
            {
                StatusTextBlock.Text = $"Regex error: {error}";
                return false;
            }

            try
            {
                var editor = GetActiveEditor();
                if (editor != null && editor.IsFormattedView)
                {
                    return FindNextFormatted(editor, regex, searchUp, query);
                }
                else
                {
                    return FindNextSyntax(regex, searchUp, query);
                }
            }
            catch (RegexMatchTimeoutException)
            {
                StatusTextBlock.Text = "This pattern is too complex (search timed out).";
                return false;
            }
        }

        private bool FindNextSyntax(Regex regex, bool searchUp, string query)
        {
            var target = GetSyntaxTextBox();
            if (target == null) return false;

            string text = target.Text;
            int startIndex = target.SelectionStart;

            if (searchUp)
            {
                int searchEnd = startIndex;
                var matches = regex.Matches(text.Substring(0, Math.Max(0, searchEnd)));
                if (matches.Count > 0)
                {
                    var match = matches[^1];
                    target.Select(match.Index, match.Length);
                    try { target.ScrollToLine(target.GetLineIndexFromCharacterIndex(match.Index)); } catch { }
                    target.Focus();
                    StatusTextBlock.Text = $"Match found at position {match.Index}.";
                    return true;
                }
                else
                {
                    StatusTextBlock.Text = $"Cannot find \"{query}\" backwards.";
                    return false;
                }
            }
            else
            {
                int searchStart = startIndex + target.SelectionLength;
                var match = regex.Match(text, Math.Min(searchStart, text.Length));
                if (match.Success)
                {
                    target.Select(match.Index, match.Length);
                    try { target.ScrollToLine(target.GetLineIndexFromCharacterIndex(match.Index)); } catch { }
                    target.Focus();
                    StatusTextBlock.Text = $"Match found at position {match.Index}.";
                    return true;
                }
                else
                {
                    StatusTextBlock.Text = $"Cannot find \"{query}\".";
                    return false;
                }
            }
        }

        private bool FindNextFormatted(NotepadEditor editor, Regex regex, bool searchUp, string query)
        {
            var richBox = editor.RichEditor;
            var doc = richBox.Document;
            if (doc == null) return false;

            var map = new FlowDocumentMap(doc);
            int currentStart = map.GetCharIndex(richBox.Selection.Start);
            int currentLen = map.GetCharIndex(richBox.Selection.End) - currentStart;

            if (searchUp)
            {
                int searchEnd = currentStart;
                var matches = regex.Matches(map.Text.Substring(0, Math.Max(0, searchEnd)));
                if (matches.Count > 0)
                {
                    var match = matches[^1];
                    var startPtr = map.GetPointer(match.Index, doc);
                    var endPtr = map.GetPointer(match.Index + match.Length, doc);

                    richBox.Selection.Select(startPtr, endPtr);
                    startPtr.Paragraph?.BringIntoView();
                    richBox.Focus();
                    StatusTextBlock.Text = $"Match found at position {match.Index}.";
                    return true;
                }
                else
                {
                    StatusTextBlock.Text = $"Cannot find \"{query}\" backwards.";
                    return false;
                }
            }
            else
            {
                int searchStart = currentStart + currentLen;
                var match = regex.Match(map.Text, Math.Min(searchStart, map.Text.Length));
                if (match.Success)
                {
                    var startPtr = map.GetPointer(match.Index, doc);
                    var endPtr = map.GetPointer(match.Index + match.Length, doc);

                    richBox.Selection.Select(startPtr, endPtr);
                    startPtr.Paragraph?.BringIntoView();
                    richBox.Focus();
                    StatusTextBlock.Text = $"Match found at position {match.Index}.";
                    return true;
                }
                else
                {
                    StatusTextBlock.Text = $"Cannot find \"{query}\".";
                    return false;
                }
            }
        }

        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            string query = FindTextBox.Text;
            if (string.IsNullOrEmpty(query)) return;

            bool matchCase = MatchCaseCheckBox.IsChecked == true;
            bool wholeWord = MatchWholeWordCheckBox.IsChecked == true;
            bool useRegex = UseRegexCheckBox.IsChecked == true;

            var regex = CreateRegex(query, matchCase, wholeWord, useRegex, out string? error);
            if (regex == null)
            {
                StatusTextBlock.Text = $"Regex error: {error}";
                return;
            }

            string replacement = ReplaceTextBox.Text;
            var editor = GetActiveEditor();

            try
            {
                if (editor != null && editor.IsFormattedView)
                {
                    ReplaceFormatted(editor, regex, replacement);
                }
                else
                {
                    ReplaceSyntax(regex, replacement);
                }
            }
            catch (RegexMatchTimeoutException)
            {
                StatusTextBlock.Text = "This pattern is too complex (operation timed out).";
            }
        }

        private void ReplaceFormatted(NotepadEditor editor, Regex regex, string replacement)
        {
            var richBox = editor.RichEditor;
            string selText = richBox.Selection.Text.TrimEnd('\r', '\n');
            if (selText.Length > 0 && regex.IsMatch(selText))
            {
                richBox.Selection.Text = replacement;
                editor.SyncToDocument();
                if (editor.Document != null) editor.Document.IsModified = true;
                FindNext();
            }
            else
            {
                FindNext();
            }
        }

        private void ReplaceSyntax(Regex regex, string replacement)
        {
            var target = GetSyntaxTextBox();
            if (target == null) return;

            if (target.SelectionLength > 0 && regex.IsMatch(target.SelectedText))
            {
                target.SelectedText = replacement;
                FindNext();
            }
            else
            {
                FindNext();
            }
        }

        private void ReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            string query = FindTextBox.Text;
            if (string.IsNullOrEmpty(query)) return;

            bool matchCase = MatchCaseCheckBox.IsChecked == true;
            bool wholeWord = MatchWholeWordCheckBox.IsChecked == true;
            bool useRegex = UseRegexCheckBox.IsChecked == true;

            var regex = CreateRegex(query, matchCase, wholeWord, useRegex, out string? error);
            if (regex == null)
            {
                StatusTextBlock.Text = $"Regex error: {error}";
                return;
            }

            string replacement = ReplaceTextBox.Text;
            var editor = GetActiveEditor();

            try
            {
                if (editor != null && editor.IsFormattedView)
                {
                    ReplaceAllFormatted(editor, regex, replacement);
                }
                else
                {
                    ReplaceAllSyntax(regex, replacement);
                }
            }
            catch (RegexMatchTimeoutException)
            {
                StatusTextBlock.Text = "This pattern is too complex (operation timed out).";
            }
        }

        private void ReplaceAllFormatted(NotepadEditor editor, Regex regex, string replacement)
        {
            var richBox = editor.RichEditor;
            var doc = richBox.Document;
            if (doc == null) return;

            richBox.BeginChange();
            try
            {
                int count = 0;
                int searchOffset = 0;

                while (true)
                {
                    var map = new FlowDocumentMap(doc);
                    if (searchOffset > map.Text.Length) break;

                    Match m = regex.Match(map.Text, searchOffset);
                    if (!m.Success) break;

                    var startPtr = map.GetPointer(m.Index, doc);
                    var endPtr = map.GetPointer(m.Index + m.Length, doc);

                    var range = new TextRange(startPtr, endPtr);
                    range.Text = replacement;

                    count++;
                    int advance = Math.Max(1, (m.Length > 0 ? replacement.Length : 1));
                    searchOffset = m.Index + advance;
                }

                StatusTextBlock.Text = $"Replaced {count} occurrence(s).";
                editor.SyncToDocument();
                if (editor.Document != null) editor.Document.IsModified = true;
            }
            finally
            {
                richBox.EndChange();
            }
        }

        private void ReplaceAllSyntax(Regex regex, string replacement)
        {
            var target = GetSyntaxTextBox();
            if (target == null) return;

            string text = target.Text;
            int count = regex.Matches(text).Count;
            target.SelectAll();
            target.SelectedText = regex.Replace(text, replacement);
            StatusTextBlock.Text = $"Replaced {count} occurrence(s).";
        }

        private class FlowDocumentMap
        {
            public string Text { get; }
            public List<TextPointer> Pointers { get; }

            public FlowDocumentMap(FlowDocument doc)
            {
                var sb = new StringBuilder();
                var pointers = new List<TextPointer>();

                TextPointer? navigator = doc.ContentStart;
                while (navigator != null && navigator.CompareTo(doc.ContentEnd) < 0)
                {
                    TextPointerContext context = navigator.GetPointerContext(LogicalDirection.Forward);
                    if (context == TextPointerContext.Text)
                    {
                        string runText = navigator.GetTextInRun(LogicalDirection.Forward);
                        int runLength = navigator.GetTextRunLength(LogicalDirection.Forward);
                        for (int i = 0; i < runLength; i++)
                        {
                            var charPtr = navigator.GetPositionAtOffset(i, LogicalDirection.Forward);
                            if (charPtr != null)
                            {
                                pointers.Add(charPtr);
                                sb.Append(runText[i]);
                            }
                        }
                        var next = runLength > 0 ? navigator.GetPositionAtOffset(runLength, LogicalDirection.Forward) : null;
                        var nextNav = next ?? navigator.GetNextContextPosition(LogicalDirection.Forward);
                        if (nextNav == null || nextNav.CompareTo(navigator) <= 0)
                        {
                            navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
                        }
                        else
                        {
                            navigator = nextNav;
                        }
                    }
                    else if (context == TextPointerContext.ElementEnd)
                    {
                        if (navigator.Parent is Paragraph || navigator.Parent is Block)
                        {
                            pointers.Add(navigator);
                            sb.Append('\n');
                        }
                        navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
                    }
                    else
                    {
                        navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
                    }
                }

                Text = sb.ToString();
                Pointers = pointers;
            }

            public int GetCharIndex(TextPointer position)
            {
                if (Pointers.Count == 0) return 0;
                int low = 0;
                int high = Pointers.Count - 1;
                while (low <= high)
                {
                    int mid = low + (high - low) / 2;
                    int cmp = Pointers[mid].CompareTo(position);
                    if (cmp == 0) return mid;
                    if (cmp < 0) low = mid + 1;
                    else high = mid - 1;
                }
                return Math.Min(low, Pointers.Count);
            }

            public TextPointer GetPointer(int charIndex, FlowDocument doc)
            {
                if (charIndex <= 0 || Pointers.Count == 0) return doc.ContentStart;
                if (charIndex < Pointers.Count) return Pointers[charIndex];
                return Pointers[^1].GetPositionAtOffset(1, LogicalDirection.Forward) ?? doc.ContentEnd;
            }
        }
    }
}
