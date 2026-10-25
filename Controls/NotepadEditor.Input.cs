using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using NotepadByFerryPot.Models;
using NotepadByFerryPot.Services;

namespace NotepadByFerryPot.Controls
{
    public partial class NotepadEditor
    {
        public NotepadEditor()
        {
            InitializeComponent();
            Loaded += NotepadEditor_Loaded;

            AddHandler(UIElement.ManipulationStartingEvent, new EventHandler<ManipulationStartingEventArgs>(OnManipulationStarting), handledEventsToo: true);
            AddHandler(UIElement.ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(OnManipulationDelta), handledEventsToo: true);
        }

        private void OnManipulationStarting(object? sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = this;
            e.Mode = ManipulationModes.Scale | ManipulationModes.Translate;
        }

        private void OnManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
        {
            // Two-finger pinch zoom (touchscreen)
            double scale = (e.DeltaManipulation.Scale.X + e.DeltaManipulation.Scale.Y) / 2.0;
            if (scale > 0 && Math.Abs(scale - 1.0) > 0.0005)
            {
                double currentZoom = SettingsService.Current.ZoomLevel;
                double newZoom = Math.Clamp(currentZoom * scale, 10.0, 500.0);

                if (Math.Abs(newZoom - currentZoom) > 0.01)
                {
                    if (Window.GetWindow(this) is MainWindow mw)
                    {
                        mw.SetZoomLevel(newZoom, immediateSave: false);
                    }
                    else
                    {
                        SettingsService.Current.ZoomLevel = newZoom;
                        ApplySettings();
                    }
                }
                e.Handled = true;
            }
        }

        private void NotepadEditor_Loaded(object sender, RoutedEventArgs e)
        {
            // Find ScrollViewer inside TextBox template
            _mainScrollViewer = FindVisualChild<ScrollViewer>(MainTextBox);
            if (_mainScrollViewer != null)
            {
                _mainScrollViewer.ScrollChanged += MainScrollViewer_ScrollChanged;
            }
            FormattedRichTextBox.Padding = EditorPadding;
            MainTextBox.Padding = EditorPadding;
            LineNumbersBorder.Padding = new Thickness(8, EditorPadding.Top, 8, EditorPadding.Bottom);
            ApplyDocumentPadding(FormattedRichTextBox.Document);
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
                    editor.MainTextBox.IsReadOnly = newDoc.IsReadOnly;
                    editor.FormattedRichTextBox.IsReadOnly = newDoc.IsReadOnly;
                    editor.UpdateLineNumbers();
                    if (editor._isFormattedView)
                    {
                        editor.UpdateFormattedView();
                    }
                }
                else
                {
                    editor.MainTextBox.Text = string.Empty;
                    editor.MainTextBox.IsReadOnly = false;
                    editor.FormattedRichTextBox.IsReadOnly = false;
                }
                editor.NotifyFormattingStateChanged();
            }
        }

        private void Doc_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isSyncing) return;
            if (e.PropertyName == nameof(NotepadDocument.IsReadOnly) && Document != null)
            {
                MainTextBox.IsReadOnly = Document.IsReadOnly;
                FormattedRichTextBox.IsReadOnly = Document.IsReadOnly;
            }
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
            double effectiveFontSize = EffectiveFontSize;

            // Syntax View font (stays monospace unless user chose something else)
            MainTextBox.FontFamily = new FontFamily(settings.FontFamilyName);
            MainTextBox.FontSize = effectiveFontSize;
            MainTextBox.FontWeight = settings.FontWeightName switch
            {
                "Bold" => FontWeights.Bold,
                "Medium" => FontWeights.Medium,
                _ => FontWeights.Normal
            };
            MainTextBox.TextWrapping = settings.WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
            LineNumbersTextBlock.LineHeight = effectiveFontSize * 1.35;

            LineNumbersColumn.Width = settings.ShowLineNumbers ? GridLength.Auto : new GridLength(0);
            LineNumbersBorder.Visibility = settings.ShowLineNumbers ? Visibility.Visible : Visibility.Collapsed;

            // Formatted View font (proportional font: Segoe UI or chosen proportional font)
            var formattedFont = GetFormattedFontFamily(settings.FontFamilyName);
            FormattedRichTextBox.FontFamily = formattedFont;
            FormattedRichTextBox.FontSize = effectiveFontSize;
            FormattedRichTextBox.Padding = EditorPadding;
            MainTextBox.Padding = EditorPadding;
            LineNumbersBorder.Padding = new Thickness(8, EditorPadding.Top, 8, EditorPadding.Bottom);

            if (FormattedRichTextBox.Document != null)
            {
                FormattedRichTextBox.Document.FontFamily = formattedFont;
                FormattedRichTextBox.Document.FontSize = effectiveFontSize;
                FormattedRichTextBox.Document.LineHeight = effectiveFontSize * 1.5;
                FormattedRichTextBox.Document.SetResourceReference(FlowDocument.ForegroundProperty, "MaterialDesignBody");
                foreach (var block in FormattedRichTextBox.Document.Blocks)
                {
                    UpdateBlockFontSize(block, effectiveFontSize);
                }
                ApplyDocumentPadding(FormattedRichTextBox.Document);
            }

            UpdateLineNumbers();
            NotifyFormattingStateChanged();
        }

        private void UpdateBlockFontSize(Block block, double effectiveFontSize)
        {
            if (block is Paragraph p)
            {
                if (p.Tag is string tag && tag.Length == 2 && tag[0] == 'h' && int.TryParse(tag.Substring(1), out int level))
                {
                    double scale = level switch
                    {
                        1 => 1.85,
                        2 => 1.5,
                        3 => 1.25,
                        4 => 1.1,
                        5 => 1.05,
                        6 => 1.0,
                        _ => 1.0
                    };
                    p.FontSize = effectiveFontSize * scale;
                    p.Margin = new Thickness(0, effectiveFontSize * (level == 1 ? 0.6 : 0.4), 0, effectiveFontSize * 0.4);
                }
                else
                {
                    p.Margin = new Thickness(0, 0, 0, effectiveFontSize * 0.6);
                    p.LineHeight = effectiveFontSize * 1.45;
                }
            }
            else if (block is System.Windows.Documents.List list)
            {
                foreach (var item in list.ListItems)
                {
                    foreach (var b in item.Blocks)
                    {
                        UpdateBlockFontSize(b, effectiveFontSize);
                    }
                }
            }
            else if (block is Table table)
            {
                foreach (var rg in table.RowGroups)
                {
                    foreach (var row in rg.Rows)
                    {
                        foreach (var cell in row.Cells)
                        {
                            foreach (var b in cell.Blocks)
                            {
                                UpdateBlockFontSize(b, effectiveFontSize);
                            }
                        }
                    }
                }
            }
            else if (block is Section section)
            {
                foreach (var b in section.Blocks)
                {
                    UpdateBlockFontSize(b, effectiveFontSize);
                }
            }
            else if (block is BlockUIContainer buic && buic.Child is FrameworkElement fe)
            {
                if (fe is TextBlock tb)
                {
                    tb.FontSize = effectiveFontSize * 0.95;
                }
                else if (fe is Border border && border.Child is TextBlock btb)
                {
                    btb.FontSize = effectiveFontSize * 0.95;
                }
            }
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
            NotifyFormattingStateChanged();
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
            NotifyFormattingStateChanged();
        }

        #region Formatted View Event Handlers

        private void FormattedRichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateStatusFromFormatted();
            NotifyFormattingStateChanged();
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

        #region Empty Area Mouse Interaction

        private void SyntaxViewGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource != MainTextBox)
            {
                MainTextBox.Focus();
                var pt = e.GetPosition(MainTextBox);
                int charIndex = MainTextBox.GetCharacterIndexFromPoint(pt, true);
                if (charIndex >= 0)
                {
                    MainTextBox.CaretIndex = charIndex;
                }
                else
                {
                    MainTextBox.CaretIndex = MainTextBox.Text.Length;
                }
            }
        }

        private void FormattedRichTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pt = e.GetPosition(FormattedRichTextBox);
            var tp = FormattedRichTextBox.GetPositionFromPoint(pt, true);
            if (tp != null)
            {
                var p = tp.Paragraph;
                if (p != null)
                {
                    var start = p.ContentStart;
                    var endCheck = start.GetPositionAtOffset(2, LogicalDirection.Forward) ?? p.ContentEnd;
                    var range = new TextRange(start, endCheck);
                    string glyph = range.Text;
                    if (glyph.StartsWith("☐") || glyph.StartsWith("☑"))
                    {
                        var r1 = start.GetCharacterRect(LogicalDirection.Forward);
                        var r2 = endCheck.GetCharacterRect(LogicalDirection.Backward);
                        var bounds = new Rect(r1.TopLeft, r2.BottomRight);
                        bounds.Inflate(6, 6);
                        if (bounds.Contains(pt) || tp.CompareTo(endCheck) <= 0)
                        {
                            if (Document != null && Document.IsReadOnly)
                            {
                                e.Handled = true;
                                return;
                            }
                            bool isChecked = glyph.StartsWith("☑");
                            range.Text = isChecked ? "☐ " : "☑ ";
                            if (Document != null) Document.IsModified = true;
                            e.Handled = true;
                            FormattedRichTextBox.Focus();
                            return;
                        }
                    }
                }

                FormattedRichTextBox.Focus();
                FormattedRichTextBox.CaretPosition = tp;
            }
            else if (FormattedRichTextBox.Document != null)
            {
                FormattedRichTextBox.Focus();
                FormattedRichTextBox.CaretPosition = FormattedRichTextBox.Document.ContentEnd;
            }
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
    }
}
