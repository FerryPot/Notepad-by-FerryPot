using System;
using System.Windows;
using NotepadByFerryPot.Views;

namespace NotepadByFerryPot
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (s, args) =>
            {
                CrashReportWindow.ShowCrash("DispatcherUnhandledException", args.Exception, isFatal: false);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    CrashReportWindow.ShowCrash("AppDomain.UnhandledException", ex, isFatal: true);
                }
            };
        }
    }
}
