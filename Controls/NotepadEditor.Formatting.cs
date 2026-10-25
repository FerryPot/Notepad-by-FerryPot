using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using NotepadByFerryPot.Services;

namespace NotepadByFerryPot.Controls
{
    public partial class NotepadEditor
    {
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
            if (Document != null && Document.IsReadOnly) return;

            if (_isFormattedView)
            {
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
                    bool hasStrike = false;
                    if (decs != null && decs.Count > 0)
                    {
                        foreach (var dec in decs)
                        {
                            if (dec.Location == TextDecorationLocation.Strikethrough)
                            {
                                hasStrike = true;
                                break;
                            }
                        }
                    }
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
                NotifyFormattingStateChanged();
                FormattedRichTextBox.Focus();
                return;
            }

            int selStart = MainTextBox.SelectionStart;
            int selLength = MainTextBox.SelectionLength;
            string text = MainTextBox.Text;

            // Multi-line selection handling
            if (selLength > 0 && (MainTextBox.SelectedText.Contains('\n') || MainTextBox.SelectedText.Contains('\r')))
            {
                string selected = MainTextBox.SelectedText;
                var parts = System.Text.RegularExpressions.Regex.Split(selected, @"(\r\n|\n|\r)");
                int nonEmptyCount = 0;
                bool allWrapped = true;
                for (int i = 0; i < parts.Length; i += 2)
                {
                    if (string.IsNullOrWhiteSpace(parts[i])) continue;
                    nonEmptyCount++;
                    string trimmed = parts[i].Trim();
                    if (!(trimmed.StartsWith(prefix) && trimmed.EndsWith(suffix) && trimmed.Length >= prefix.Length + suffix.Length))
                    {
                        allWrapped = false;
                        break;
                    }
                }

                if (allWrapped && nonEmptyCount > 0)
                {
                    for (int i = 0; i < parts.Length; i += 2)
                    {
                        if (string.IsNullOrWhiteSpace(parts[i])) continue;
                        string raw = parts[i];
                        int l = 0; while (l < raw.Length && (raw[l] == ' ' || raw[l] == '\t')) l++;
                        string indent = raw.Substring(0, l);
                        int r = raw.Length; while (r > l && (raw[r - 1] == ' ' || raw[r - 1] == '\t')) r--;
                        string trailing = raw.Substring(r);
                        string core = raw.Substring(l, r - l);

                        if (core.StartsWith(prefix) && core.EndsWith(suffix) && core.Length >= prefix.Length + suffix.Length)
                        {
                            core = core.Substring(prefix.Length, core.Length - prefix.Length - suffix.Length);
                        }
                        parts[i] = indent + core + trailing;
                    }
                }
                else
                {
                    for (int i = 0; i < parts.Length; i += 2)
                    {
                        if (string.IsNullOrWhiteSpace(parts[i])) continue;
                        string raw = parts[i];
                        int l = 0; while (l < raw.Length && (raw[l] == ' ' || raw[l] == '\t')) l++;
                        string indent = raw.Substring(0, l);
                        int r = raw.Length; while (r > l && (raw[r - 1] == ' ' || raw[r - 1] == '\t')) r--;
                        string trailing = raw.Substring(r);
                        string core = raw.Substring(l, r - l);

                        core = StripWrap(core, prefix, suffix);
                        parts[i] = indent + prefix + core + suffix + trailing;
                    }
                }

                string replacement = string.Join("", parts);
                MainTextBox.SelectedText = replacement;
                MainTextBox.Select(selStart, replacement.Length);
                MainTextBox.Focus();
                return;
            }

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
                // Partially wrapped selection (e.g. "foo **bar**"): unwrap/strip existing markers
                else if (HasWrap(selected, prefix))
                {
                    string unwrapped = StripWrap(selected, prefix, suffix);
                    MainTextBox.SelectedText = unwrapped;
                    MainTextBox.Select(selStart, unwrapped.Length);
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

        private static string StripWrap(string text, string prefix, string suffix)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (prefix == "**") return text.Replace("**", "");
            if (prefix == "~~") return text.Replace("~~", "");
            if (prefix == "*") return System.Text.RegularExpressions.Regex.Replace(text, @"(?<!\*)\*(?!\*)", "");
            if (prefix == "`") return text.Replace("`", "");
            return text.Replace(prefix, "").Replace(suffix, "");
        }

        private static bool HasWrap(string text, string prefix)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (prefix == "**") return text.Contains("**");
            if (prefix == "~~") return text.Contains("~~");
            if (prefix == "*") return System.Text.RegularExpressions.Regex.IsMatch(text, @"(?<!\*)\*(?!\*)");
            if (prefix == "`") return text.Contains("`");
            return text.Contains(prefix);
        }

        public void ApplyHeading(string prefix)
        {
            if (Document != null && Document.IsReadOnly) return;

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
                NotifyFormattingStateChanged();
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
            if (Document != null && Document.IsReadOnly) return;

            if (_isFormattedView)
            {
                if (listType == "check")
                {
                    var sel = FormattedRichTextBox.Selection;
                    if (!sel.IsEmpty)
                    {
                        var paragraphs = new List<Paragraph>();
                        TextPointer cur = sel.Start;
                        while (cur != null && cur.CompareTo(sel.End) <= 0)
                            {
                            var p = cur.Paragraph;
                            if (p != null && !paragraphs.Contains(p))
                            {
                                paragraphs.Add(p);
                            }
                            cur = cur.GetNextContextPosition(LogicalDirection.Forward);
                        }

                        if (paragraphs.Count > 0)
                        {
                            bool allHaveCheck = paragraphs.All(p =>
                            {
                                var tr = new TextRange(p.ContentStart, p.ContentStart.GetPositionAtOffset(2, LogicalDirection.Forward) ?? p.ContentEnd);
                                return tr.Text.StartsWith("☐ ") || tr.Text.StartsWith("☑ ");
                            });

                            foreach (var p in paragraphs)
                            {
                                var tr = new TextRange(p.ContentStart, p.ContentStart.GetPositionAtOffset(2, LogicalDirection.Forward) ?? p.ContentEnd);
                                if (allHaveCheck)
                                {
                                    if (tr.Text.StartsWith("☐ ") || tr.Text.StartsWith("☑ "))
                                    {
                                        tr.Text = "";
                                    }
                                }
                                else
                                {
                                    if (!tr.Text.StartsWith("☐ ") && !tr.Text.StartsWith("☑ "))
                                    {
                                        p.ContentStart.InsertTextInRun("☐ ");
                                    }
                                }
                            }
                            if (Document != null) Document.IsModified = true;
                            NotifyFormattingStateChanged();
                            FormattedRichTextBox.Focus();
                            return;
                        }
                    }

                    var caret = FormattedRichTextBox.CaretPosition;
                    var targetP = caret?.Paragraph;
                    if (targetP != null)
                    {
                        var tr = new TextRange(targetP.ContentStart, targetP.ContentStart.GetPositionAtOffset(2, LogicalDirection.Forward) ?? targetP.ContentEnd);
                        if (tr.Text.StartsWith("☐ ") || tr.Text.StartsWith("☑ "))
                        {
                            tr.Text = "";
                        }
                        else
                        {
                            targetP.ContentStart.InsertTextInRun("☐ ");
                        }
                        if (Document != null) Document.IsModified = true;
                        NotifyFormattingStateChanged();
                        FormattedRichTextBox.Focus();
                        return;
                    }
                }
                else if (listType == "number")
                {
                    EditingCommands.ToggleNumbering.Execute(null, FormattedRichTextBox);
                }
                else
                {
                    EditingCommands.ToggleBullets.Execute(null, FormattedRichTextBox);
                }
                NotifyFormattingStateChanged();
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
            if (Document != null && Document.IsReadOnly) return;

            Window? owner = Window.GetWindow(this);

            if (_isFormattedView)
            {
                string text = FormattedRichTextBox.Selection.Text;
                bool hasSelection = !string.IsNullOrEmpty(text);

                var dialog = new Views.LinkDialog(text, hasSelection);
                if (owner != null) dialog.Owner = owner;

                if (dialog.ShowDialog() != true || dialog.ValidUri == null)
                {
                    return;
                }

                string linkText = string.IsNullOrEmpty(dialog.LinkText) ? dialog.ValidUri.OriginalString : dialog.LinkText;
                var targetUri = dialog.ValidUri;

                try
                {
                    if (!FormattedRichTextBox.Selection.IsEmpty)
                    {
                        FormattedRichTextBox.Selection.Text = "";
                    }

                    var caret = FormattedRichTextBox.CaretPosition;
                    var hl = new Hyperlink(new Run(linkText), caret)
                    {
                        NavigateUri = targetUri
                    };

                    SafeLinkService.ConfigureHyperlink(hl, targetUri.OriginalString);
                    try
                    {
                        hl.Foreground = (Brush)Application.Current.FindResource("MaterialDesign.Brush.Primary");
                    }
                    catch { }

                    FormattedRichTextBox.CaretPosition = hl.ElementEnd;
                }
                catch
                {
                    FormattedRichTextBox.Selection.Text = $"[{linkText}]({targetUri.OriginalString})";
                }

                FormattedRichTextBox.Focus();
                return;
            }

            int selStart = MainTextBox.SelectionStart;
            int selLen = MainTextBox.SelectionLength;
            string selText = selLen > 0 ? MainTextBox.SelectedText : "";
            bool hasSyntaxSelection = !string.IsNullOrEmpty(selText);

            var syntaxDialog = new Views.LinkDialog(selText, hasSyntaxSelection);
            if (owner != null) syntaxDialog.Owner = owner;

            if (syntaxDialog.ShowDialog() != true || syntaxDialog.ValidUri == null)
            {
                return;
            }

            string sLinkText = string.IsNullOrEmpty(syntaxDialog.LinkText) ? syntaxDialog.ValidUri.OriginalString : syntaxDialog.LinkText;
            string replacement = $"[{sLinkText}]({syntaxDialog.ValidUri.OriginalString})";

            MainTextBox.SelectedText = replacement;
            MainTextBox.Select(selStart + replacement.Length, 0);
            MainTextBox.Focus();
        }

        public void InsertTable(int rows = 3, int cols = 3)
        {
            if (Document != null && Document.IsReadOnly) return;

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

                Brush brush;
                try
                {
                    brush = (Application.Current?.TryFindResource("MaterialDesignBody") as Brush) ?? Brushes.Black;
                }
                catch
                {
                    brush = Brushes.Black;
                }

                var tableBlock = MarkdownParserService.CreateTable(tableLines, SettingsService.Current.FontSize, brush);

                var caret = FormattedRichTextBox.CaretPosition;
                Block? targetBlock = null;
                DependencyObject? cur = caret?.Paragraph ?? (DependencyObject?)caret?.Parent;
                while (cur != null)
                {
                    if (cur is Block b && FormattedRichTextBox.Document.Blocks.Contains(b))
                    {
                        targetBlock = b;
                        break;
                    }
                    if (cur is FrameworkContentElement fce)
                        cur = fce.Parent;
                    else if (cur is FrameworkElement fe)
                        cur = fe.Parent;
                    else
                        cur = LogicalTreeHelper.GetParent(cur);
                }

                var trailingParagraph = new Paragraph { Margin = new Thickness(0, 0, 0, SettingsService.Current.FontSize * 0.4) };

                if (targetBlock != null)
                {
                    FormattedRichTextBox.Document.Blocks.InsertAfter(targetBlock, tableBlock);
                    FormattedRichTextBox.Document.Blocks.InsertAfter(tableBlock, trailingParagraph);
                }
                else
                {
                    FormattedRichTextBox.Document.Blocks.Add(tableBlock);
                    FormattedRichTextBox.Document.Blocks.Add(trailingParagraph);
                }

                if (tableBlock is Table t && t.RowGroups.Count > 0 && t.RowGroups[0].Rows.Count > 0 && t.RowGroups[0].Rows[0].Cells.Count > 0)
                {
                    var firstCell = t.RowGroups[0].Rows[0].Cells[0];
                    if (firstCell.Blocks.FirstBlock != null)
                    {
                        FormattedRichTextBox.CaretPosition = firstCell.Blocks.FirstBlock.ContentStart;
                    }
                }

                if (Document != null) Document.IsModified = true;
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
            if (Document != null && Document.IsReadOnly) return;

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
                NotifyFormattingStateChanged();
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
            try
            {
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1));
            }
            catch (System.Text.RegularExpressions.RegexMatchTimeoutException) { }
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
    }
}
