using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace NotepadByFerryPot.Controls
{
    public partial class NotepadEditor
    {
        #region Formatting State Query and Events

        public event EventHandler<EditorFormattingState>? FormattingStateChanged;

        public EditorFormattingState GetCurrentFormattingState()
        {
            var state = new EditorFormattingState();
            if (!_isFormattedView || FormattedRichTextBox == null || FormattedRichTextBox.Document == null)
            {
                if (!_isFormattedView && MainTextBox != null && MainTextBox.Text.Length > 0)
                {
                    try
                    {
                        int caretIndex = MainTextBox.CaretIndex;
                        int lineIndex = MainTextBox.GetLineIndexFromCharacterIndex(caretIndex);
                        string lineText = MainTextBox.GetLineText(lineIndex).TrimStart();
                        int h = 0;
                        while (h < lineText.Length && lineText[h] == '#') h++;
                        if (h >= 1 && h <= 6 && h < lineText.Length && lineText[h] == ' ')
                        {
                            state.Heading = "H" + h;
                        }
                        else
                        {
                            state.Heading = "Normal";
                        }

                        string sel = MainTextBox.SelectedText;
                        if (!string.IsNullOrEmpty(sel))
                        {
                            state.IsBold = sel.StartsWith("**") && sel.EndsWith("**") && sel.Length >= 4;
                            state.IsItalic = (sel.StartsWith("*") && sel.EndsWith("*") && !sel.StartsWith("**") && sel.Length >= 2) ||
                                             (sel.StartsWith("_") && sel.EndsWith("_") && !sel.StartsWith("__") && sel.Length >= 2);
                            state.IsStrikethrough = sel.StartsWith("~~") && sel.EndsWith("~~") && sel.Length >= 4;
                            state.IsUnderline = sel.StartsWith("<u>", StringComparison.OrdinalIgnoreCase) && sel.EndsWith("</u>", StringComparison.OrdinalIgnoreCase) && sel.Length >= 7;
                            state.IsHighlight = (sel.StartsWith("<mark", StringComparison.OrdinalIgnoreCase) && sel.EndsWith("</mark>", StringComparison.OrdinalIgnoreCase) && sel.Length >= 13) ||
                                                (sel.StartsWith("==") && sel.EndsWith("==") && sel.Length >= 4);
                        }
                    }
                    catch { }
                }
                return state;
            }

            try
            {
                var sel = FormattedRichTextBox.Selection;
                if (sel != null)
                {
                    object weight = sel.GetPropertyValue(TextElement.FontWeightProperty);
                    state.IsBold = weight is FontWeight fw && (fw == FontWeights.Bold || fw == FontWeights.SemiBold || fw == FontWeights.ExtraBold || fw == FontWeights.Black);

                    object style = sel.GetPropertyValue(TextElement.FontStyleProperty);
                    state.IsItalic = style is FontStyle fs && (fs == FontStyles.Italic || fs == FontStyles.Oblique);

                    object decs = sel.GetPropertyValue(Inline.TextDecorationsProperty);
                    if (decs is TextDecorationCollection tdc && tdc.Count > 0)
                    {
                        foreach (var dec in tdc)
                        {
                            if (dec.Location == TextDecorationLocation.Strikethrough)
                            {
                                state.IsStrikethrough = true;
                            }
                            if (dec.Location == TextDecorationLocation.Underline)
                            {
                                state.IsUnderline = true;
                            }
                        }
                    }

                    object bg = sel.GetPropertyValue(TextElement.BackgroundProperty);
                    if (bg is SolidColorBrush scb && scb.Color.A > 0 && scb != System.Windows.Media.Brushes.Transparent)
                    {
                        state.IsHighlight = true;
                    }
                    else
                    {
                        var inline = (FormattedRichTextBox.CaretPosition ?? sel.Start)?.Parent as Inline;
                        while (inline != null)
                        {
                            if (inline.Background is SolidColorBrush iscb && iscb.Color.A > 0 && iscb != System.Windows.Media.Brushes.Transparent)
                            {
                                state.IsHighlight = true;
                                break;
                            }
                            if (inline.Tag is string itag && itag.StartsWith("mark", StringComparison.OrdinalIgnoreCase))
                            {
                                state.IsHighlight = true;
                                break;
                            }
                            inline = inline.Parent as Inline;
                        }
                    }

                    var caret = FormattedRichTextBox.CaretPosition ?? sel.Start;
                    if (caret != null)
                    {
                        var p = caret.Paragraph;
                        if (p != null && p.Tag is string tag)
                        {
                            bool isHeadingTag = false;
                            try
                            {
                                isHeadingTag = System.Text.RegularExpressions.Regex.IsMatch(tag, @"^h[1-6]$", System.Text.RegularExpressions.RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                            }
                            catch { }

                            state.Heading = isHeadingTag ? tag.ToUpper() : "Normal";
                        }
                        else
                        {
                            state.Heading = "Normal";
                        }

                        DependencyObject? current = p ?? (DependencyObject)caret.Parent;
                        if (p != null)
                        {
                            var tr = new TextRange(p.ContentStart, p.ContentStart.GetPositionAtOffset(2, LogicalDirection.Forward) ?? p.ContentEnd);
                            if (tr.Text.StartsWith("☐ ") || tr.Text.StartsWith("☑ "))
                            {
                                state.IsList = true;
                            }
                        }
                        while (current != null && !(current is FlowDocument))
                        {
                            if (current is ListItem || current is System.Windows.Documents.List)
                            {
                                state.IsList = true;
                                break;
                            }
                            if (current is FrameworkContentElement fce)
                                current = fce.Parent;
                            else if (current is FrameworkElement fe)
                                current = fe.Parent;
                            else
                                current = LogicalTreeHelper.GetParent(current);
                        }
                    }
                }
            }
            catch { }

            return state;
        }

        public void NotifyFormattingStateChanged()
        {
            var state = GetCurrentFormattingState();
            FormattingStateChanged?.Invoke(this, state);
        }

        #endregion
    }

    public class EditorFormattingState
    {
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsStrikethrough { get; set; }
        public bool IsUnderline { get; set; }
        public bool IsHighlight { get; set; }
        public string Heading { get; set; } = "Normal";
        public bool IsList { get; set; }
    }
}
