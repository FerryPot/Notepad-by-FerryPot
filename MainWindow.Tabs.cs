using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using NotepadByFerryPot.Models;
using NotepadByFerryPot.Views;

namespace NotepadByFerryPot
{
    public partial class MainWindow : Window
    {
        #region Tab Management

        private NotepadDocument CreateNewTab(string? title = null, string? content = "", string? filePath = null)
        {
            var doc = new NotepadDocument
            {
                Title = title ?? $"Untitled {_untitledCounter++}",
                Content = content ?? string.Empty,
                SavedContent = content ?? string.Empty,
                FilePath = filePath,
                IsModified = false,
                IsFormattedView = Services.SettingsService.Current.IsDefaultViewFormatted
            };

            doc.PropertyChanged += Document_PropertyChanged;
            Documents.Add(doc);

            SetActiveDocument(doc);
            RebuildTabsUI();
            return doc;
        }

        private void SetActiveDocument(NotepadDocument doc)
        {
            if (_activeDocument != null && ActiveEditor != null)
            {
                ActiveEditor.SyncToDocument();
            }
            _activeDocument = doc;

            bool isFormatted = doc.IsFormattedView ?? Services.SettingsService.Current.IsDefaultViewFormatted;
            doc.IsFormattedView = isFormatted;

            if (ActiveEditor != null)
            {
                ActiveEditor.SetViewMode(isFormatted);
                ActiveEditor.Document = doc;
            }

            UpdateViewModeUI();
            UpdateTitle();
            UpdateStatusBar();
            RebuildTabsUI();
            UpdateFormattingToolbarState(ActiveEditor?.GetCurrentFormattingState());
        }

        private void RebuildTabsUI()
        {
            TabsStackPanel.Children.Clear();

            foreach (var doc in Documents)
            {
                bool isActive = doc == _activeDocument;

                var tabBorder = new Border
                {
                    Height = 30,
                    CornerRadius = new CornerRadius(15),
                    Background = isActive 
                        ? (Brush)FindResource("MaterialDesign.Brush.Primary") 
                        : (Brush)FindResource("MaterialDesignCardBackground"),
                    BorderBrush = (Brush)FindResource("MaterialDesignDivider"),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 4, 0),
                    Padding = new Thickness(10, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center,
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

                var sp = new StackPanel 
                { 
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center 
                };

                var brandDarkBrush = new SolidColorBrush(Color.FromRgb(0x1C, 0x1B, 0x1F));

                // Tab Title Text
                var titleText = new TextBlock
                {
                    Text = doc.DisplayTitle,
                    FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = isActive 
                        ? brandDarkBrush 
                        : (Brush)FindResource("MaterialDesignBody"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0),
                    FontSize = 12
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
                    VerticalAlignment = VerticalAlignment.Center,
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
                Width = 28,
                Height = 28,
                Margin = new Thickness(2, 0, 0, 0),
                ToolTip = "New Document (Ctrl+N)",
                Style = (Style)FindResource("MaterialDesignIconButton"),
                VerticalAlignment = VerticalAlignment.Center
            };
            var addIcon = new PackIcon 
            { 
                Kind = PackIconKind.Plus, 
                Width = 16, 
                Height = 16,
                Foreground = (Brush)FindResource("MaterialDesignBody")
            };
            addBtn.Content = addIcon;
            addBtn.Click += (s, e) => CreateNewTab();

            TabsStackPanel.Children.Add(addBtn);
        }

        private static bool DocumentNeedsSavePrompt(NotepadDocument doc)
        {
            // If the document has no file path on disk (untitled document):
            if (string.IsNullOrEmpty(doc.FilePath))
            {
                // If content is empty or contains only whitespace/newlines, do NOT warn
                if (string.IsNullOrWhiteSpace(doc.Content))
                {
                    return false;
                }

                // If it has real content, warn if it has unsaved changes
                return doc.IsModified || doc.Content != doc.SavedContent || !string.IsNullOrEmpty(doc.Content);
            }

            // If the document is linked to a file on disk:
            // Warn if it has unsaved changes (even if user deleted all text)
            return doc.IsModified || doc.Content != doc.SavedContent;
        }

        private bool CloseTab(NotepadDocument doc)
        {
            // Sync editor text first if this is the active document
            if (doc == _activeDocument && ActiveEditor != null)
            {
                ActiveEditor.SyncToDocument();
            }

            if (DocumentNeedsSavePrompt(doc))
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

                        // Ensure final path stays in the exact same folder
                        string fullDir = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string? newDir = Path.GetDirectoryName(Path.GetFullPath(newPath))?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (!string.Equals(fullDir, newDir, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show("The renamed file must remain in the same folder.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

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
    }
}
