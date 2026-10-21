using System;
using System.Windows;

namespace NotepadByFerryPot
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                    "Notepad By FerryPot - Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    MessageBox.Show(
                        $"A fatal error occurred:\n\n{ex.Message}\n\n{ex.StackTrace}",
                        "Notepad By FerryPot - Fatal Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            };
        }
    }
}
