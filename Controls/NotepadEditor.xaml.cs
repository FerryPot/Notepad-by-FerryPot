using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
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

        public static readonly Thickness EditorPadding = new Thickness(32, 24, 32, 24);

        public void ApplyDocumentPadding(FlowDocument? doc)
        {
            if (doc == null) return;
            doc.PagePadding = EditorPadding;
            if (doc.Blocks.FirstBlock != null)
            {
                var m = doc.Blocks.FirstBlock.Margin;
                if (m.Top > 0)
                {
                    doc.Blocks.FirstBlock.Margin = new Thickness(m.Left, 0, m.Right, m.Bottom);
                }
            }
        }

        private bool _isFormattedView = true;
        private bool _hasInitializedViewMode = false;
        public bool IsFormattedView
        {
            get => _isFormattedView;
            set => SetViewMode(value);
        }

        public static FontFamily GetFormattedFontFamily(string? settingsFontFamily)
        {
            if (string.IsNullOrWhiteSpace(settingsFontFamily))
                return new FontFamily("Segoe UI");

            var monospaceFonts = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Consolas", "Cascadia Code", "Cascadia Mono", "Courier New", 
                "Courier", "Lucida Console", "Lucida Sans Typewriter", 
                "Fira Code", "JetBrains Mono", "Inconsolata", "Source Code Pro", "Monospace"
            };

            if (monospaceFonts.Contains(settingsFontFamily.Trim()))
            {
                return new FontFamily("Segoe UI");
            }

            return new FontFamily(settingsFontFamily);
        }

        public void SetViewMode(bool formatted)
        {
            if (_hasInitializedViewMode && _isFormattedView == formatted) return;

            _hasInitializedViewMode = true;
            _isFormattedView = formatted;
            if (_isFormattedView)
            {
                // Switching from Syntax to Formatted
                if (FormattedRichTextBox.Document == null || MainTextBox.Text != _lastSyncedSyntaxText)
                {
                    try
                    {
                        var brush = (Brush)Application.Current.FindResource("MaterialDesignBody");
                        var ff = GetFormattedFontFamily(SettingsService.Current.FontFamilyName);
                        var doc = MarkdownParserService.ParseToFlowDocument(MainTextBox.Text, ff, SettingsService.Current.FontSize, brush);
                        FormattedRichTextBox.Document = doc;
                        ApplyDocumentPadding(FormattedRichTextBox.Document);
                        _lastSyncedSyntaxText = MainTextBox.Text;
                    }
                    catch
                    {
                        var ff = GetFormattedFontFamily(SettingsService.Current.FontFamilyName);
                        var doc = new FlowDocument(new Paragraph(new Run(MainTextBox.Text)) { Margin = new Thickness(0) })
                        {
                            PagePadding = EditorPadding,
                            FontFamily = ff,
                            FontSize = SettingsService.Current.FontSize,
                            TextAlignment = TextAlignment.Left
                        };
                        doc.SetResourceReference(FlowDocument.ForegroundProperty, "MaterialDesignBody");
                        FormattedRichTextBox.Document = doc;
                        ApplyDocumentPadding(FormattedRichTextBox.Document);
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
            NotifyFormattingStateChanged();
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
                    var ff = GetFormattedFontFamily(SettingsService.Current.FontFamilyName);
                    var doc = MarkdownParserService.ParseToFlowDocument(MainTextBox.Text, ff, SettingsService.Current.FontSize, brush);
                    FormattedRichTextBox.Document = doc;
                    ApplyDocumentPadding(FormattedRichTextBox.Document);
                    _lastSyncedSyntaxText = MainTextBox.Text;
                }
                catch
                {
                    var ff = GetFormattedFontFamily(SettingsService.Current.FontFamilyName);
                    var doc = new FlowDocument(new Paragraph(new Run(MainTextBox.Text)) { Margin = new Thickness(0) })
                    {
                        PagePadding = EditorPadding,
                        FontFamily = ff,
                        FontSize = SettingsService.Current.FontSize,
                        TextAlignment = TextAlignment.Left
                    };
                    doc.SetResourceReference(FlowDocument.ForegroundProperty, "MaterialDesignBody");
                    FormattedRichTextBox.Document = doc;
                    ApplyDocumentPadding(FormattedRichTextBox.Document);
                    _lastSyncedSyntaxText = MainTextBox.Text;
                }
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
