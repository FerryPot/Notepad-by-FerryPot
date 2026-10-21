using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using NotepadByFerryPot.Controls;
using NotepadByFerryPot.Models;
using NotepadByFerryPot.Services;
using NotepadByFerryPot.Views;

namespace NotepadByFerryPot
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<NotepadDocument> Documents { get; } = new();
        private NotepadDocument? _activeDocument;
        private int _untitledCounter = 1;
        private bool _isFocusMode = false;

        public MainWindow()
        {
            InitializeComponent();
            SettingsService.Load();
            ThemeService.ApplyTheme(SettingsService.Current);

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Create initial document
            CreateNewTab();
            ApplySettingsToWindow();
            UpdateViewModeUI();
        }

        public void ApplySettingsToWindow()
        {
            var s = SettingsService.Current;
            WordWrapMenuItem.IsChecked = s.WordWrap;
            LineNumbersMenuItem.IsChecked = s.ShowLineNumbers;
            StatusBarMenuItem.IsChecked = s.ShowStatusBar;
            FormattingToolbarMenuItem.IsChecked = s.ShowFormattingToolbar;

            StatusBarRow.Height = s.ShowStatusBar ? GridLength.Auto : new GridLength(0);
            FormattingBarRow.Height = s.ShowFormattingToolbar ? GridLength.Auto : new GridLength(0);

            ActiveEditor.ApplySettings();
        }

        #region Tab Management

        private NotepadDocument CreateNewTab(string? title = null, string? content = "", string? filePath = null)
        {
            var doc = new NotepadDocument
            {
                Title = title ?? $"Untitled {_untitledCounter++}",
                Content = content ?? string.Empty,
                SavedContent = content ?? string.Empty,
                FilePath = filePath,
                IsModified = false
            };

            doc.PropertyChanged += Document_PropertyChanged;
            Documents.Add(doc);

            SetActiveDocument(doc);
            RebuildTabsUI();
            return doc;
        }

        private void SetActiveDocument(NotepadDocument doc)
        {
            _activeDocument = doc;
            ActiveEditor.Document = doc;

            UpdateTitle();
            UpdateStatusBar();
            RebuildTabsUI();
        }

        private void RebuildTabsUI()
        {
            TabsStackPanel.Children.Clear();

            foreach (var doc in Documents)
            {
                bool isActive = doc == _activeDocument;

                var tabBorder = new Border
                {
                    CornerRadius = new CornerRadius(16),
                    Background = isActive 
                        ? (Brush)FindResource("MaterialDesign.Brush.Primary") 
                        : (Brush)FindResource("MaterialDesignCardBackground"),
                    BorderBrush = (Brush)FindResource("MaterialDesignDivider"),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 6, 4),
                    Padding = new Thickness(12, 6, 8, 6),
                    Cursor = Cursors.Hand,
                    Tag = doc
                };

                tabBorder.MouseLeftButtonDown += (s, e) =>
                {
                    if (s is Border b && b.Tag is NotepadDocument d)
                    {
                        SetActiveDocument(d);
                    }
                };

                tabBorder.MouseRightButtonDown += (s, e) =>
                {
                    if (s is Border b && b.Tag is NotepadDocument d)
                    {
                        SetActiveDocument(d);
                    }
                };

                // Context Menu on Tab Right-Click (New tab, Rename, Close, Close others, Close to right)
                var contextMenu = new ContextMenu();

                var newTabItem = new MenuItem { Header = "New tab", InputGestureText = "Ctrl+N" };
                newTabItem.Click += (s, e) => CreateNewTab();
                contextMenu.Items.Add(newTabItem);

                var renameItem = new MenuItem { Header = "Rename..." };
                renameItem.Click += (s, e) => PromptRenameTab(doc);
                contextMenu.Items.Add(renameItem);

                contextMenu.Items.Add(new Separator());

                var closeTabItem = new MenuItem { Header = "Close tab", InputGestureText = "Ctrl+W" };
                closeTabItem.Click += (s, e) => CloseTab(doc);
                contextMenu.Items.Add(closeTabItem);

                var closeOthersItem = new MenuItem { Header = "Close other tabs" };
                closeOthersItem.IsEnabled = Documents.Count > 1;
                closeOthersItem.Click += (s, e) => CloseOtherTabs(doc);
                contextMenu.Items.Add(closeOthersItem);

                int docIndex = Documents.IndexOf(doc);
                var closeRightItem = new MenuItem { Header = "Close tabs to the right" };
                closeRightItem.IsEnabled = docIndex < Documents.Count - 1;
                closeRightItem.Click += (s, e) => CloseTabsToTheRight(doc);
                contextMenu.Items.Add(closeRightItem);

                tabBorder.ContextMenu = contextMenu;

                var sp = new StackPanel { Orientation = Orientation.Horizontal };

                var brandDarkBrush = new SolidColorBrush(Color.FromRgb(0x1C, 0x1B, 0x1F));

                // Modified dot / icon
                var titleText = new TextBlock
                {
                    Text = doc.DisplayTitle,
                    FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = isActive 
                        ? brandDarkBrush 
                        : (Brush)FindResource("MaterialDesignBody"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                sp.Children.Add(titleText);

                // Close Button
                var closeBtn = new Button
                {
                    Width = 18,
                    Height = 18,
                    Padding = new Thickness(0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Style = (Style)FindResource("MaterialDesignIconButton"),
                    Tag = doc
                };

                var closeIcon = new PackIcon
                {
                    Kind = PackIconKind.Close,
                    Width = 14,
                    Height = 14,
                    Foreground = isActive 
                        ? brandDarkBrush 
                        : (Brush)FindResource("MaterialDesignBodyLight")
                };
                closeBtn.Content = closeIcon;

                closeBtn.Click += (s, e) =>
                {
                    e.Handled = true;
                    if (s is Button btn && btn.Tag is NotepadDocument d)
                    {
                        CloseTab(d);
                    }
                };

                sp.Children.Add(closeBtn);
                tabBorder.Child = sp;
                TabsStackPanel.Children.Add(tabBorder);
            }

            // Plus Button
            var addBtn = new Button
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(4, 0, 0, 4),
                ToolTip = "New Document (Ctrl+N)",
                Style = (Style)FindResource("MaterialDesignIconButton")
            };
            var addIcon = new PackIcon { Kind = PackIconKind.Plus, Width = 18, Height = 18 };
            addBtn.Content = addIcon;
            addBtn.Click += (s, e) => CreateNewTab();

            TabsStackPanel.Children.Add(addBtn);
        }

        private bool CloseTab(NotepadDocument doc)
        {
            // Sync editor text first if this is the active document
            if (doc == _activeDocument && ActiveEditor != null)
            {
                doc.Content = ActiveEditor.EditorTextBox.Text;
            }

            bool hasUnsaved = doc.IsModified || 
                              doc.Content != doc.SavedContent || 
                              (!string.IsNullOrEmpty(doc.Content) && string.IsNullOrEmpty(doc.FilePath));

            if (hasUnsaved)
            {
                var result = MessageBox.Show(
                    $"Do you want to save changes to \"{doc.Title}\"?\n\nAny unsaved changes will be lost if you do not save.",
                    "Notepad by FerryPot",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                {
                    return false;
                }
                else if (result == MessageBoxResult.Yes)
                {
                    if (!SaveDocument(doc))
                    {
                        return false;
                    }
                }
            }

            doc.PropertyChanged -= Document_PropertyChanged;
            int index = Documents.IndexOf(doc);
            Documents.Remove(doc);

            if (Documents.Count == 0)
            {
                CreateNewTab();
            }
            else if (_activeDocument == doc)
            {
                int nextIndex = Math.Min(index, Documents.Count - 1);
                SetActiveDocument(Documents[nextIndex]);
            }
            else
            {
                RebuildTabsUI();
            }

            return true;
        }

        private void PromptRenameTab(NotepadDocument doc)
        {
            var dialog = new RenameDialog(doc.Title)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                string newName = dialog.NewName;
                if (!string.IsNullOrEmpty(doc.FilePath) && File.Exists(doc.FilePath))
                {
                    try
                    {
                        string dir = Path.GetDirectoryName(doc.FilePath)!;
                        string ext = Path.GetExtension(doc.FilePath);
                        if (!newName.EndsWith(ext, StringComparison.OrdinalIgnoreCase) && !newName.Contains('.'))
                        {
                            newName += ext;
                        }

                        string newPath = Path.Combine(dir, newName);
                        if (!string.Equals(doc.FilePath, newPath, StringComparison.OrdinalIgnoreCase))
                        {
                            if (File.Exists(newPath))
                            {
                                MessageBox.Show($"A file named \"{newName}\" already exists in this folder.", "Rename Tab", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            File.Move(doc.FilePath, newPath);
                            doc.FilePath = newPath;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not rename file on disk: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                doc.Title = newName;
                UpdateTitle();
                RebuildTabsUI();
            }
        }

        private void CloseOtherTabs(NotepadDocument keepDoc)
        {
            var docsToClose = Documents.Where(d => d != keepDoc).ToList();
            foreach (var d in docsToClose)
            {
                if (!CloseTab(d))
                {
                    break;
                }
            }
        }

        private void CloseTabsToTheRight(NotepadDocument doc)
        {
            int index = Documents.IndexOf(doc);
            if (index < 0) return;

            var docsToClose = Documents.Skip(index + 1).ToList();
            foreach (var d in docsToClose)
            {
                if (!CloseTab(d))
                {
                    break;
                }
            }
        }

        #endregion

        #region Document Events & Status

        private void Document_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == _activeDocument)
            {
                if (e.PropertyName == nameof(NotepadDocument.DisplayTitle))
                {
                    UpdateTitle();
                    RebuildTabsUI();
                }
                else if (e.PropertyName == nameof(NotepadDocument.CurrentLine) ||
                         e.PropertyName == nameof(NotepadDocument.CurrentColumn) ||
                         e.PropertyName == nameof(NotepadDocument.WordCount) ||
                         e.PropertyName == nameof(NotepadDocument.CharacterCount) ||
                         e.PropertyName == nameof(NotepadDocument.LineEnding) ||
                         e.PropertyName == nameof(NotepadDocument.EncodingName))
                {
                    UpdateStatusBar();
                }
            }
        }

        private void UpdateTitle()
        {
            if (_activeDocument != null)
            {
                Title = $"{_activeDocument.DisplayTitle} - Notepad by FerryPot";
            }
            else
            {
                Title = "Notepad by FerryPot";
            }
        }

        private void UpdateStatusBar()
        {
            if (_activeDocument == null) return;

            LineColStatusText.Text = $"Ln {_activeDocument.CurrentLine}, Col {_activeDocument.CurrentColumn}";
            WordCharStatusText.Text = $"{_activeDocument.WordCount:N0} words  |  {_activeDocument.CharacterCount:N0} chars";
            LineEndingStatusText.Text = _activeDocument.LineEnding;
            EncodingStatusText.Text = _activeDocument.EncodingName;
            ZoomStatusText.Text = $"{(int)SettingsService.Current.ZoomLevel}%";
        }

        #endregion

        #region File Operations

        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            CreateNewTab();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Text Documents (*.txt)|*.txt|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    OpenFile(file);
                }
            }
        }

        private void OpenFile(string filePath)
        {
            try
            {
                // Detect BOM / Encoding
                Encoding encoding = Encoding.UTF8;
                string content = File.ReadAllText(filePath, encoding);

                // If current tab is empty and untitled, reuse it
                if (_activeDocument != null &&
                    _activeDocument.FilePath == null &&
                    !_activeDocument.IsModified &&
                    string.IsNullOrEmpty(_activeDocument.Content))
                {
                    _activeDocument.Content = content;
                    _activeDocument.MarkAsSaved(filePath);
                    UpdateTitle();
                    RebuildTabsUI();
                }
                else
                {
                    var doc = CreateNewTab(Path.GetFileName(filePath), content, filePath);
                    doc.MarkAsSaved(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool SaveDocument(NotepadDocument doc)
        {
            if (string.IsNullOrEmpty(doc.FilePath))
            {
                return SaveDocumentAs(doc);
            }

            try
            {
                File.WriteAllText(doc.FilePath, doc.Content, doc.Encoding);
                doc.MarkAsSaved(doc.FilePath);
                UpdateTitle();
                RebuildTabsUI();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool SaveDocumentAs(NotepadDocument doc)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "Text Documents (*.txt)|*.txt|Markdown (*.md)|*.md|All Files (*.*)|*.*",
                FileName = doc.Title.StartsWith("Untitled") ? "Document.txt" : doc.Title
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(dlg.FileName, doc.Content, doc.Encoding);
                    doc.MarkAsSaved(dlg.FileName);
                    UpdateTitle();
                    RebuildTabsUI();
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not save file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return false;
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (_activeDocument != null) SaveDocument(_activeDocument);
        }

        private void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            if (_activeDocument != null) SaveDocumentAs(_activeDocument);
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (_activeDocument != null) CloseTab(_activeDocument);
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (_activeDocument == null) return;

            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                var flowDoc = new System.Windows.Documents.FlowDocument(
                    new System.Windows.Documents.Paragraph(
                        new System.Windows.Documents.Run(_activeDocument.Content)
                    )
                )
                {
                    FontFamily = ActiveEditor.EditorTextBox.FontFamily,
                    FontSize = ActiveEditor.EditorTextBox.FontSize
                };

                var paginator = ((System.Windows.Documents.IDocumentPaginatorSource)flowDoc).DocumentPaginator;
                printDlg.PrintDocument(paginator, _activeDocument.Title);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Edit Commands

        private void Undo_Click(object sender, RoutedEventArgs e) => ActiveEditor.EditorTextBox.Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => ActiveEditor.EditorTextBox.Redo();
        private void Cut_Click(object sender, RoutedEventArgs e) => ActiveEditor.EditorTextBox.Cut();
        private void Copy_Click(object sender, RoutedEventArgs e) => ActiveEditor.EditorTextBox.Copy();
        private void Paste_Click(object sender, RoutedEventArgs e) => ActiveEditor.EditorTextBox.Paste();
        private void SelectAll_Click(object sender, RoutedEventArgs e) => ActiveEditor.EditorTextBox.SelectAll();

        private void InsertTimeDate_Click(object sender, RoutedEventArgs e)
        {
            string timeDate = DateTime.Now.ToString("h:mm tt M/d/yyyy");
            ActiveEditor.EditorTextBox.SelectedText = timeDate;
        }

        #endregion

        #region Find & Replace

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            var findWindow = new FindReplaceWindow(ActiveEditor.EditorTextBox)
            {
                Owner = this
            };
            findWindow.Show();
        }

        #endregion

        #region View & Tools

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsService.Current;
            if (s.FontSize < 40)
            {
                s.FontSize += 2;
                s.ZoomLevel = (s.FontSize / 15.0) * 100.0;
                ActiveEditor.ApplySettings();
                UpdateStatusBar();
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsService.Current;
            if (s.FontSize > 10)
            {
                s.FontSize -= 2;
                s.ZoomLevel = (s.FontSize / 15.0) * 100.0;
                ActiveEditor.ApplySettings();
                UpdateStatusBar();
            }
        }

        private void ResetZoom_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsService.Current;
            s.FontSize = 15;
            s.ZoomLevel = 100.0;
            ActiveEditor.ApplySettings();
            UpdateStatusBar();
        }

        private void WordWrap_Toggle(object sender, RoutedEventArgs e)
        {
            var s = SettingsService.Current;
            s.WordWrap = !s.WordWrap;
            SettingsService.Save();
            ApplySettingsToWindow();
        }

        private void LineNumbers_Toggle(object sender, RoutedEventArgs e)
        {
            var s = SettingsService.Current;
            s.ShowLineNumbers = !s.ShowLineNumbers;
            SettingsService.Save();
            ApplySettingsToWindow();
        }

        private void StatusBar_Toggle(object sender, RoutedEventArgs e)
        {
            var s = SettingsService.Current;
            s.ShowStatusBar = !s.ShowStatusBar;
            SettingsService.Save();
            ApplySettingsToWindow();
        }

        private void FormattingToolbar_Toggle(object sender, RoutedEventArgs e)
        {
            var s = SettingsService.Current;
            s.ShowFormattingToolbar = !s.ShowFormattingToolbar;
            SettingsService.Save();
            ApplySettingsToWindow();
        }

        #region Formatting Toolbar Handlers

        private void HeadingMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void Heading1_Click(object sender, RoutedEventArgs e) => ActiveEditor.ApplyHeading("# ");
        private void Heading2_Click(object sender, RoutedEventArgs e) => ActiveEditor.ApplyHeading("## ");
        private void Heading3_Click(object sender, RoutedEventArgs e) => ActiveEditor.ApplyHeading("### ");
        private void Heading4_Click(object sender, RoutedEventArgs e) => ActiveEditor.ApplyHeading("#### ");
        private void Paragraph_Click(object sender, RoutedEventArgs e) => ActiveEditor.ApplyHeading("");

        private void ListMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void BulletList_Click(object sender, RoutedEventArgs e) => ActiveEditor.ApplyList("bullet");
        private void NumberedList_Click(object sender, RoutedEventArgs e) => ActiveEditor.ApplyList("number");
        private void Checklist_Click(object sender, RoutedEventArgs e) => ActiveEditor.ApplyList("check");

        private void Bold_Click(object sender, RoutedEventArgs e) => ActiveEditor.ToggleWrap("**", "**");
        private void Italic_Click(object sender, RoutedEventArgs e) => ActiveEditor.ToggleWrap("*", "*");
        private void Strikethrough_Click(object sender, RoutedEventArgs e) => ActiveEditor.ToggleWrap("~~", "~~");
        private void Link_Click(object sender, RoutedEventArgs e) => ActiveEditor.InsertLink();

        private void TableMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void Table2x2_Click(object sender, RoutedEventArgs e) => ActiveEditor.InsertTable(2, 2);
        private void Table3x3_Click(object sender, RoutedEventArgs e) => ActiveEditor.InsertTable(3, 3);
        private void Table4x4_Click(object sender, RoutedEventArgs e) => ActiveEditor.InsertTable(4, 4);

        private void ClearFormatting_Click(object sender, RoutedEventArgs e) => ActiveEditor.ClearFormatting();

        #endregion

        #region View Mode Handlers (Formatted vs Syntax View)

        private void ToggleViewMode_Click(object sender, RoutedEventArgs e)
        {
            SetViewMode(!ActiveEditor.IsFormattedView);
        }

        private void SyntaxView_Click(object sender, RoutedEventArgs e)
        {
            SetViewMode(false);
        }

        private void FormattedView_Click(object sender, RoutedEventArgs e)
        {
            SetViewMode(true);
        }

        private void SetViewMode(bool formatted)
        {
            ActiveEditor.SetViewMode(formatted);
            UpdateViewModeUI();
        }

        private void UpdateViewModeUI()
        {
            bool isFormatted = ActiveEditor.IsFormattedView;

            SyntaxViewMenuItem.IsChecked = !isFormatted;
            FormattedViewMenuItem.IsChecked = isFormatted;

            if (isFormatted)
            {
                ToolbarViewModeIcon.Kind = PackIconKind.CodeTags;
                ToolbarViewModeText.Text = "Syntax View";

                StatusBarViewModeIcon.Kind = PackIconKind.EyeOutline;
                StatusBarViewModeText.Text = "Formatted View";
            }
            else
            {
                ToolbarViewModeIcon.Kind = PackIconKind.EyeOutline;
                ToolbarViewModeText.Text = "Formatted View";

                StatusBarViewModeIcon.Kind = PackIconKind.CodeTags;
                StatusBarViewModeText.Text = "Syntax View";
            }
        }

        #endregion

        private void FocusMode_Click(object sender, RoutedEventArgs e)
        {
            _isFocusMode = !_isFocusMode;

            TopAppBarRow.Height = _isFocusMode ? new GridLength(0) : GridLength.Auto;
            TabsBarRow.Height = _isFocusMode ? new GridLength(0) : GridLength.Auto;
            FormattingBarRow.Height = _isFocusMode ? new GridLength(0) : (SettingsService.Current.ShowFormattingToolbar ? GridLength.Auto : new GridLength(0));
            StatusBarRow.Height = _isFocusMode ? new GridLength(0) : (SettingsService.Current.ShowStatusBar ? GridLength.Auto : new GridLength(0));

            WindowState = _isFocusMode ? WindowState.Maximized : WindowState.Normal;
            WindowStyle = _isFocusMode ? WindowStyle.None : WindowStyle.SingleBorderWindow;

            FullscreenControlBar.Visibility = _isFocusMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Stats_Click(object sender, RoutedEventArgs e)
        {
            if (_activeDocument != null)
            {
                var statsWindow = new StatsWindow(_activeDocument)
                {
                    Owner = this
                };
                statsWindow.ShowDialog();
            }
        }

        private void SetCRLF_Click(object sender, RoutedEventArgs e)
        {
            if (_activeDocument != null)
            {
                _activeDocument.LineEnding = "Windows (CRLF)";
                _activeDocument.Content = _activeDocument.Content.Replace("\r\n", "\n").Replace("\n", "\r\n");
            }
        }

        private void SetLF_Click(object sender, RoutedEventArgs e)
        {
            if (_activeDocument != null)
            {
                _activeDocument.LineEnding = "Unix (LF)";
                _activeDocument.Content = _activeDocument.Content.Replace("\r\n", "\n");
            }
        }

        private void SetEncodingUTF8_Click(object sender, RoutedEventArgs e)
        {
            if (_activeDocument != null) _activeDocument.Encoding = Encoding.UTF8;
        }

        private void SetEncodingUTF16_Click(object sender, RoutedEventArgs e)
        {
            if (_activeDocument != null) _activeDocument.Encoding = Encoding.Unicode;
        }

        private void SetEncodingASCII_Click(object sender, RoutedEventArgs e)
        {
            if (_activeDocument != null) _activeDocument.Encoding = Encoding.ASCII;
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this
            };
            settingsWindow.SettingsApplied += (s, args) =>
            {
                ApplySettingsToWindow();
                UpdateStatusBar();
            };
            settingsWindow.ShowDialog();
        }

        #endregion

        #region Drag and Drop & Shortcuts

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        OpenFile(file);
                    }
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _isFocusMode)
            {
                e.Handled = true;
                FocusMode_Click(sender, e);
                return;
            }

            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

            if (ctrl && e.Key == Key.N)
            {
                e.Handled = true;
                CreateNewTab();
            }
            else if (ctrl && e.Key == Key.O)
            {
                e.Handled = true;
                OpenFile_Click(sender, e);
            }
            else if (ctrl && shift && e.Key == Key.S)
            {
                e.Handled = true;
                SaveAsFile_Click(sender, e);
            }
            else if (ctrl && e.Key == Key.S)
            {
                e.Handled = true;
                SaveFile_Click(sender, e);
            }
            else if (ctrl && e.Key == Key.W)
            {
                e.Handled = true;
                CloseTab_Click(sender, e);
            }
            else if (ctrl && e.Key == Key.F)
            {
                e.Handled = true;
                Find_Click(sender, e);
            }
            else if (ctrl && e.Key == Key.H)
            {
                e.Handled = true;
                Find_Click(sender, e);
            }
            else if (ctrl && shift && (e.Key == Key.M || e.Key == Key.P))
            {
                e.Handled = true;
                ToggleViewMode_Click(sender, e);
            }
            else if (ctrl && !shift && e.Key == Key.P)
            {
                e.Handled = true;
                Print_Click(sender, e);
            }
            else if (ctrl && e.Key == Key.B)
            {
                e.Handled = true;
                Bold_Click(sender, e);
            }
            else if (ctrl && e.Key == Key.I)
            {
                e.Handled = true;
                Italic_Click(sender, e);
            }
            else if (ctrl && e.Key == Key.K)
            {
                e.Handled = true;
                Link_Click(sender, e);
            }
            else if (ctrl && shift && (e.Key == Key.S || e.Key == Key.X))
            {
                e.Handled = true;
                Strikethrough_Click(sender, e);
            }
            else if (ctrl && e.Key == Key.OemComma)
            {
                e.Handled = true;
                Settings_Click(sender, e);
            }
            else if (e.Key == Key.F11)
            {
                e.Handled = true;
                FocusMode_Click(sender, e);
            }
            else if (e.Key == Key.F5)
            {
                e.Handled = true;
                InsertTimeDate_Click(sender, e);
            }
            else if (ctrl && (e.Key == Key.OemPlus || e.Key == Key.Add))
            {
                e.Handled = true;
                ZoomIn_Click(sender, e);
            }
            else if (ctrl && (e.Key == Key.OemMinus || e.Key == Key.Subtract))
            {
                e.Handled = true;
                ZoomOut_Click(sender, e);
            }
            else if (ctrl && (e.Key == Key.D0 || e.Key == Key.NumPad0))
            {
                e.Handled = true;
                ResetZoom_Click(sender, e);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Sync active editor content first
            if (_activeDocument != null && ActiveEditor != null)
            {
                _activeDocument.Content = ActiveEditor.EditorTextBox.Text;
            }

            // Check all documents for unsaved content
            foreach (var doc in Documents)
            {
                bool hasUnsaved = doc.IsModified || 
                                  doc.Content != doc.SavedContent || 
                                  (!string.IsNullOrEmpty(doc.Content) && string.IsNullOrEmpty(doc.FilePath));

                if (hasUnsaved)
                {
                    SetActiveDocument(doc);
                    var result = MessageBox.Show(
                        $"Do you want to save changes to \"{doc.Title}\"?\n\nAny unsaved changes will be lost if you do not save.",
                        "Notepad by FerryPot",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                    else if (result == MessageBoxResult.Yes)
                    {
                        if (!SaveDocument(doc))
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }

            SettingsService.Save();
        }

        #endregion
    }
}
