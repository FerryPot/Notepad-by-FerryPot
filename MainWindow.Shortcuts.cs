using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using NotepadByFerryPot.Services;
using NotepadByFerryPot.Views;

namespace NotepadByFerryPot
{
    public partial class MainWindow : Window
    {
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
            if (_activeDocument != null)
            {
                _activeDocument.IsFormattedView = formatted;
            }
            ActiveEditor.SetViewMode(formatted);
            UpdateViewModeUI();
            UpdateFormattingToolbarState(ActiveEditor?.GetCurrentFormattingState());
        }

        private void UpdateViewModeUI()
        {
            bool isFormatted = ActiveEditor.IsFormattedView;

            SyntaxViewMenuItem.IsChecked = !isFormatted;
            FormattedViewMenuItem.IsChecked = isFormatted;

            if (StatusBarViewModeButton != null && StatusBarViewModeIcon != null && StatusBarViewModeText != null)
            {
                if (isFormatted)
                {
                    StatusBarViewModeIcon.Kind = PackIconKind.FormatLetterCase;
                    StatusBarViewModeText.Text = "Formatted";
                    StatusBarViewModeButton.ToolTip = "Switch to Markdown syntax (Ctrl+Shift+M)";
                }
                else
                {
                    StatusBarViewModeIcon.Kind = PackIconKind.LanguageMarkdown;
                    StatusBarViewModeText.Text = "Markdown";
                    StatusBarViewModeButton.ToolTip = "Switch to Formatted (Ctrl+Shift+M)";
                }
            }

            UpdateStatusBar();
        }

        #endregion

        #region Focus, Minimize, Stats, Encodings, Settings

        private void FocusMode_Click(object sender, RoutedEventArgs e)
        {
            _isFocusMode = !_isFocusMode;

            TopAppBarRow.Height = _isFocusMode ? new GridLength(0) : GridLength.Auto;
            TabsBarRow.Height = _isFocusMode ? new GridLength(0) : GridLength.Auto;
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
            else if (ctrl && !shift && e.Key == Key.U)
            {
                e.Handled = true;
                Underline_Click(sender, e);
            }
            else if (ctrl && e.Key == Key.K)
            {
                e.Handled = true;
                Link_Click(sender, e);
            }
            else if (ctrl && shift && e.Key == Key.X)
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
                ActiveEditor.SyncToDocument();
            }

            // Check all documents for unsaved content
            foreach (var doc in Documents)
            {
                if (DocumentNeedsSavePrompt(doc))
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
