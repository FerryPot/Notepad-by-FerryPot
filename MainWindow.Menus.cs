using System;
using System.Windows;
using NotepadByFerryPot.Services;
using NotepadByFerryPot.Views;

namespace NotepadByFerryPot
{
    public partial class MainWindow : Window
    {
        #region Edit Commands

        private void Undo_Click(object sender, RoutedEventArgs e) => ActiveEditor.Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => ActiveEditor.Redo();
        private void Cut_Click(object sender, RoutedEventArgs e) => ActiveEditor.Cut();
        private void Copy_Click(object sender, RoutedEventArgs e) => ActiveEditor.Copy();
        private void Paste_Click(object sender, RoutedEventArgs e) => ActiveEditor.Paste();
        private void SelectAll_Click(object sender, RoutedEventArgs e) => ActiveEditor.SelectAll();

        private void InsertTimeDate_Click(object sender, RoutedEventArgs e)
        {
            string timeDate = DateTime.Now.ToString("h:mm tt M/d/yyyy");
            ActiveEditor.InsertText(timeDate);
        }

        #endregion

        #region Find & Replace

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            var findWindow = new FindReplaceWindow(ActiveEditor)
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

        #endregion
    }
}
