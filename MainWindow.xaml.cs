using System.Collections.ObjectModel;
using System.Windows;
using NotepadByFerryPot.Models;
using NotepadByFerryPot.Services;

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
            SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplySettingsToWindow();
            CreateNewTab();
            UpdateViewModeUI();
            UpdateToolbarResponsiveLayout();
            ActiveEditor.FormattingStateChanged += (s, state) => UpdateFormattingToolbarState(state);
            UpdateFormattingToolbarState(ActiveEditor?.GetCurrentFormattingState());
        }

        public void ApplySettingsToWindow()
        {
            var s = SettingsService.Current;
            WordWrapMenuItem.IsChecked = s.WordWrap;
            LineNumbersMenuItem.IsChecked = s.ShowLineNumbers;
            StatusBarMenuItem.IsChecked = s.ShowStatusBar;
            FormattingToolbarMenuItem.IsChecked = s.ShowFormattingToolbar;

            StatusBarRow.Height = s.ShowStatusBar ? GridLength.Auto : new GridLength(0);
            if (ToolbarVerticalDivider != null && FormattingToolbarPanel != null)
            {
                var vis = s.ShowFormattingToolbar ? Visibility.Visible : Visibility.Collapsed;
                ToolbarVerticalDivider.Visibility = vis;
                FormattingToolbarPanel.Visibility = vis;
            }

            ActiveEditor.ApplySettings();
            UpdateFormattingToolbarState(ActiveEditor?.GetCurrentFormattingState());
        }
    }
}
