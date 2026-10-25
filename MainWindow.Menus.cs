using System;
using System.Windows;
using System.Windows.Threading;
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

        private DispatcherTimer? _saveZoomDebounceTimer;

        public void SetZoomLevel(double newZoom, bool immediateSave = true)
        {
            newZoom = Math.Clamp(newZoom, 10.0, 500.0);
            if (Math.Abs(SettingsService.Current.ZoomLevel - newZoom) < 0.001) return;

            SettingsService.Current.ZoomLevel = newZoom;
            ActiveEditor.ApplySettings();
            UpdateStatusBar();

            if (immediateSave)
            {
                _saveZoomDebounceTimer?.Stop();
                SettingsService.Save();
            }
            else
            {
                ScheduleSaveZoom();
            }
        }

        public void FlushPendingZoomSave()
        {
            if (_saveZoomDebounceTimer?.IsEnabled == true)
            {
                _saveZoomDebounceTimer.Stop();
                SettingsService.Save();
            }
        }

        private void ScheduleSaveZoom()
        {
            if (_saveZoomDebounceTimer == null)
            {
                _saveZoomDebounceTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(400)
                };
                _saveZoomDebounceTimer.Tick += (s, e) =>
                {
                    _saveZoomDebounceTimer.Stop();
                    SettingsService.Save();
                };
            }
            _saveZoomDebounceTimer.Stop();
            _saveZoomDebounceTimer.Start();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            double current = SettingsService.Current.ZoomLevel;
            double next = Math.Floor((current + 10.0) / 10.0) * 10.0;
            SetZoomLevel(next, immediateSave: true);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            double current = SettingsService.Current.ZoomLevel;
            double next = Math.Ceiling((current - 10.0) / 10.0) * 10.0;
            SetZoomLevel(next, immediateSave: true);
        }

        private void ResetZoom_Click(object sender, RoutedEventArgs e)
        {
            SetZoomLevel(100.0, immediateSave: true);
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
