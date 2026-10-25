using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NotepadByFerryPot.Models;
using NotepadByFerryPot.Services;

namespace NotepadByFerryPot
{
    public partial class MainWindow : Window
    {
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
            string channelSuffix = BuildInfo.TitleSuffix;
            if (_activeDocument != null)
            {
                Title = $"{_activeDocument.DisplayTitle} - Notepad by FerryPot{channelSuffix}";
            }
            else
            {
                Title = $"Notepad by FerryPot{channelSuffix}";
            }
        }

        private void UpdateStatusBar()
        {
            if (_activeDocument == null) return;

            LineColStatusText.Text = $"Ln {_activeDocument.CurrentLine}, Col {_activeDocument.CurrentColumn}";
            WordCharStatusText.Text = $"{_activeDocument.CharacterCount:N0} characters";
            LineEndingStatusText.Text = _activeDocument.LineEnding;
            EncodingStatusText.Text = _activeDocument.EncodingName;
            ZoomStatusText.Text = $"{(int)Math.Round(SettingsService.Current.ZoomLevel)}%";

            if (ReadOnlyStatusText != null && ReadOnlySeparator != null)
            {
                var vis = _activeDocument.IsReadOnly ? Visibility.Visible : Visibility.Collapsed;
                ReadOnlyStatusText.Visibility = vis;
                ReadOnlySeparator.Visibility = vis;
            }
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
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"The file \"{filePath}\" does not exist or has been moved.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                long sizeBytes = fileInfo.Length;
                double sizeMB = sizeBytes / (1024.0 * 1024.0);

                // 1. File size check: warn if > 50 MB
                if (sizeMB > 50.0)
                {
                    var result = MessageBox.Show(
                        $"\"{fileInfo.Name}\" is {sizeMB:F1} MB.\n\nOpening very large files may take significant time or memory.\n\nDo you want to continue opening this file?",
                        "Large File Warning",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                // 2. Binary file detection: check first 8 KB for null characters
                int checkLength = (int)Math.Min(8192, sizeBytes);
                if (checkLength > 0)
                {
                    byte[] buffer = new byte[checkLength];
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        int read = fs.Read(buffer, 0, buffer.Length);
                        for (int i = 0; i < read; i++)
                        {
                            if (buffer[i] == 0)
                            {
                                var result = MessageBox.Show(
                                    $"\"{fileInfo.Name}\" appears to be a binary file (contains null characters).\n\nOpening binary files in a text editor may display garbled characters or cause slowdowns.\n\nDo you want to open it anyway?",
                                    "Binary File Detected",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning);

                                if (result != MessageBoxResult.Yes)
                                {
                                    return;
                                }
                                break;
                            }
                        }
                    }
                }

                // 3. Read file with BOM and encoding detection (UTF-8, UTF-16 LE/BE, ANSI fallback)
                var (content, encoding) = FileEncodingService.ReadFileWithEncoding(filePath);

                // 4. Detect Line Ending
                string lineEnding = content.Contains("\r\n") ? "Windows (CRLF)" : (content.Contains("\n") ? "Unix (LF)" : "Windows (CRLF)");

                // 5. Detect Read-Only status
                bool isReadOnly = (fileInfo.Attributes & FileAttributes.ReadOnly) != 0;

                // 6. Reuse current tab if empty & untitled, or create new tab
                NotepadDocument targetDoc;
                if (_activeDocument != null &&
                    _activeDocument.FilePath == null &&
                    !_activeDocument.IsModified &&
                    string.IsNullOrEmpty(_activeDocument.Content))
                {
                    targetDoc = _activeDocument;
                    targetDoc.Content = content;
                    targetDoc.Encoding = encoding;
                    targetDoc.LineEnding = lineEnding;
                    targetDoc.IsReadOnly = isReadOnly;
                    targetDoc.IsFormattedView = SettingsService.Current.IsDefaultViewFormatted;
                    targetDoc.MarkAsSaved(filePath);
                    SetActiveDocument(targetDoc);
                }
                else
                {
                    targetDoc = CreateNewTab(Path.GetFileName(filePath), content, filePath);
                    targetDoc.Encoding = encoding;
                    targetDoc.LineEnding = lineEnding;
                    targetDoc.IsReadOnly = isReadOnly;
                    targetDoc.IsFormattedView = SettingsService.Current.IsDefaultViewFormatted;
                    targetDoc.MarkAsSaved(filePath);
                    SetActiveDocument(targetDoc);
                }

                UpdateTitle();
                UpdateStatusBar();
                RebuildTabsUI();
            }
            catch (UnauthorizedAccessException uex)
            {
                MessageBox.Show(
                    $"Access denied to \"{Path.GetFileName(filePath)}\".\n\nYou do not have permission to open this file:\n{uex.Message}",
                    "Permission Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show(
                    $"Could not open \"{Path.GetFileName(filePath)}\".\n\nThe file is currently in use or locked by another process:\n{ioEx.Message}",
                    "File In Use",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not open file \"{Path.GetFileName(filePath)}\":\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool SaveDocument(NotepadDocument doc)
        {
            if (doc == _activeDocument && ActiveEditor != null)
            {
                ActiveEditor.SyncToDocument();
            }

            if (string.IsNullOrEmpty(doc.FilePath))
            {
                return SaveDocumentAs(doc);
            }

            // Check if file is read-only before attempting save
            if (File.Exists(doc.FilePath))
            {
                var attrs = File.GetAttributes(doc.FilePath);
                if ((attrs & FileAttributes.ReadOnly) != 0)
                {
                    var choice = MessageBox.Show(
                        $"\"{Path.GetFileName(doc.FilePath)}\" is marked as read-only.\n\nWould you like to save it with a different name using Save As?",
                        "Read-Only File",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (choice == MessageBoxResult.Yes)
                    {
                        return SaveDocumentAs(doc);
                    }
                    return false;
                }
            }

            if (SaveFileAtomically(doc.FilePath, doc.Content, doc.Encoding, out var error))
            {
                doc.MarkAsSaved(doc.FilePath);
                UpdateTitle();
                RebuildTabsUI();
                return true;
            }
            else
            {
                MessageBox.Show($"Could not save file:\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool SaveDocumentAs(NotepadDocument doc)
        {
            if (doc == _activeDocument && ActiveEditor != null)
            {
                ActiveEditor.SyncToDocument();
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Text Documents (*.txt)|*.txt|Markdown (*.md)|*.md|All Files (*.*)|*.*",
                FileName = doc.Title.StartsWith("Untitled") ? "Document.txt" : doc.Title
            };

            if (dlg.ShowDialog() == true)
            {
                if (File.Exists(dlg.FileName))
                {
                    var attrs = File.GetAttributes(dlg.FileName);
                    if ((attrs & FileAttributes.ReadOnly) != 0)
                    {
                        MessageBox.Show(
                            $"Cannot save to \"{Path.GetFileName(dlg.FileName)}\" because it is marked as read-only. Please choose another file or destination.",
                            "Read-Only File",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return false;
                    }
                }

                if (SaveFileAtomically(dlg.FileName, doc.Content, doc.Encoding, out var error))
                {
                    doc.MarkAsSaved(dlg.FileName);
                    UpdateTitle();
                    RebuildTabsUI();
                    return true;
                }
                else
                {
                    MessageBox.Show($"Could not save file:\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return false;
        }

        private static bool SaveFileAtomically(string targetPath, string content, Encoding encoding, out string? errorMessage)
        {
            errorMessage = null;
            string? dir = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(dir))
            {
                dir = Environment.CurrentDirectory;
            }

            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                errorMessage = $"Cannot access directory \"{dir}\":\n{ex.Message}";
                return false;
            }

            // Create temporary file in the same directory to allow atomic replace on the same filesystem volume
            string tempFile = Path.Combine(dir, $".tmp_{Guid.NewGuid():N}.tmp");

            try
            {
                // Write content with document encoding and original line endings to temp file
                File.WriteAllText(tempFile, content, encoding);

                if (File.Exists(targetPath))
                {
                    try
                    {
                        File.Replace(tempFile, targetPath, null);
                        return true;
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Move(tempFile, targetPath, overwrite: true);
                        return true;
                    }
                    catch (IOException)
                    {
                        try
                        {
                            File.Move(tempFile, targetPath, overwrite: true);
                            return true;
                        }
                        catch
                        {
                            // Fall back to direct write only if replace fails (e.g. network share)
                            File.WriteAllText(targetPath, content, encoding);
                            return true;
                        }
                    }
                }
                else
                {
                    File.Move(tempFile, targetPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
            finally
            {
                // Never leave a partial or temporary file behind
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch { }
                }
            }
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
    }
}
