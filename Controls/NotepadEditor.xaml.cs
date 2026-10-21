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
