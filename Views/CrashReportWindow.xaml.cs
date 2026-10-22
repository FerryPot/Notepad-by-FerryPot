using System;
using System.IO;
using System.Windows;
using NotepadByFerryPot.Services;

namespace NotepadByFerryPot.Views
{
    public partial class CrashReportWindow : Window
    {
        private readonly string _details;

        public CrashReportWindow(string logFilePath, string details)
        {
            InitializeComponent();
            _details = details;

            LogFilePathBox.Text = string.IsNullOrEmpty(logFilePath)
                ? Path.Combine(CrashLogService.LogsDirectory, "(log file could not be created)")
                : logFilePath;
        }

        private void CopyDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_details);
                CopyStatusText.Text = "Crash details copied to clipboard!";
                CopyStatusText.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to copy details to clipboard: {ex.Message}", "Notepad by FerryPot", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void ShowCrash(string context, Exception ex, bool isFatal = false)
        {
            string logPath = CrashLogService.LogError(context, ex);
            string details = $"Error Context: {context}\nTimestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nOS: {Environment.OSVersion}\nRuntime: {Environment.Version}\nException: {ex.GetType().FullName}\nMessage: {ex.Message}\nLog File: {logPath}\n\nStack Trace:\n{ex.StackTrace}";

            Action showAction = () =>
            {
                try
                {
                    var window = new CrashReportWindow(logPath, details);
                    window.ShowDialog();
                }
                catch
                {
                    MessageBox.Show(
                        $"An unexpected error occurred:\n{ex.Message}\n\nDetails saved to:\n{logPath}",
                        "Notepad by FerryPot - Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            };

            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(showAction);
                }
                catch
                {
                    MessageBox.Show(
                        $"An unexpected error occurred:\n{ex.Message}\n\nDetails saved to:\n{logPath}",
                        "Notepad by FerryPot - Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            else
            {
                showAction();
            }
        }
    }
}
