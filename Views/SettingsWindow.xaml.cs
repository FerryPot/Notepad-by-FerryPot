using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using NotepadByFerryPot.Services;

namespace NotepadByFerryPot.Views
{
    public partial class SettingsWindow : Window
    {
        private bool _isInitializing = true;
        public event EventHandler? SettingsApplied;

        public SettingsWindow()
        {
            InitializeComponent();
            PopulateControls();
            LoadCurrentSettings();
            _isInitializing = false;
            UpdatePreview();
        }

        private void PopulateControls()
        {
            // Populate Font Families
            var fonts = new[] { "Consolas", "Cascadia Code", "Segoe UI", "Roboto", "Courier New", "Lucida Console", "Arial", "Times New Roman" };
            foreach (var f in fonts)
            {
                FontFamilyComboBox.Items.Add(f);
            }

            // Populate Font Weights
            FontWeightComboBox.Items.Add("Normal");
            FontWeightComboBox.Items.Add("Medium");
            FontWeightComboBox.Items.Add("Bold");

            // Populate Tab Sizes
            TabSizeComboBox.Items.Add("2 spaces");
            TabSizeComboBox.Items.Add("4 spaces");
            TabSizeComboBox.Items.Add("8 spaces");
        }

        private void LoadCurrentSettings()
        {
            var s = SettingsService.Current;

            switch (s.BaseTheme)
            {
                case BaseTheme.Light: ThemeLightRadio.IsChecked = true; break;
                case BaseTheme.Dark: ThemeDarkRadio.IsChecked = true; break;
                default: ThemeSystemRadio.IsChecked = true; break;
            }

            AmoledCheckBox.IsChecked = s.IsPureBlackDark;
            StatusBarCheckBox.IsChecked = s.ShowStatusBar;

            FontFamilyComboBox.SelectedItem = s.FontFamilyName;
            if (FontFamilyComboBox.SelectedIndex == -1) FontFamilyComboBox.SelectedIndex = 0;

            FontSizeSlider.Value = s.FontSize;
            FontSizeValueText.Text = $"{s.FontSize} pt";

            FontWeightComboBox.SelectedItem = s.FontWeightName;
            if (FontWeightComboBox.SelectedIndex == -1) FontWeightComboBox.SelectedIndex = 0;

            WordWrapCheckBox.IsChecked = s.WordWrap;
            LineNumbersCheckBox.IsChecked = s.ShowLineNumbers;
            AutoIndentCheckBox.IsChecked = s.AutoIndent;
            AutoCloseBracketsCheckBox.IsChecked = s.AutoCloseBrackets;
            UseSpacesForTabCheckBox.IsChecked = s.UseSpacesForTab;

            TabSizeComboBox.SelectedIndex = s.TabSize switch
            {
                2 => 0,
                8 => 2,
                _ => 1
            };
        }

        private void NavigationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageAppearance == null || PageTypography == null || PageEditor == null || PageAbout == null)
                return;

            PageAppearance.Visibility = Visibility.Collapsed;
            PageTypography.Visibility = Visibility.Collapsed;
            PageEditor.Visibility = Visibility.Collapsed;
            PageAbout.Visibility = Visibility.Collapsed;

            switch (NavigationListBox.SelectedIndex)
            {
                case 0:
                    PageAppearance.Visibility = Visibility.Visible;
                    break;
                case 1:
                    PageTypography.Visibility = Visibility.Visible;
                    break;
                case 2:
                    PageEditor.Visibility = Visibility.Visible;
                    break;
                case 3:
                    PageAbout.Visibility = Visibility.Visible;
                    break;
                default:
                    PageAppearance.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            var s = SettingsService.Current;
            if (ThemeLightRadio.IsChecked == true) s.BaseTheme = BaseTheme.Light;
            else if (ThemeDarkRadio.IsChecked == true) s.BaseTheme = BaseTheme.Dark;
            else s.BaseTheme = BaseTheme.Inherit;

            ThemeService.ApplyTheme(s);
            SettingsService.Save();
            SettingsApplied?.Invoke(this, EventArgs.Empty);
        }

        private void SettingChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            SaveAndApply();
        }

        private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            SaveAndApply();
            UpdatePreview();
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            if (FontSizeValueText != null)
            {
                FontSizeValueText.Text = $"{(int)e.NewValue} pt";
            }
            SaveAndApply();
            UpdatePreview();
        }

        private void SaveAndApply()
        {
            var s = SettingsService.Current;

            s.IsPureBlackDark = AmoledCheckBox.IsChecked == true;
            s.ShowStatusBar = StatusBarCheckBox.IsChecked == true;

            if (FontFamilyComboBox.SelectedItem is string ff) s.FontFamilyName = ff;
            s.FontSize = FontSizeSlider.Value;
            if (FontWeightComboBox.SelectedItem is string fw) s.FontWeightName = fw;

            s.WordWrap = WordWrapCheckBox.IsChecked == true;
            s.ShowLineNumbers = LineNumbersCheckBox.IsChecked == true;
            s.AutoIndent = AutoIndentCheckBox.IsChecked == true;
            s.AutoCloseBrackets = AutoCloseBracketsCheckBox.IsChecked == true;
            s.UseSpacesForTab = UseSpacesForTabCheckBox.IsChecked == true;

            s.TabSize = TabSizeComboBox.SelectedIndex switch
            {
                0 => 2,
                2 => 8,
                _ => 4
            };

            ThemeService.ApplyTheme(s);
            SettingsService.Save();
            SettingsApplied?.Invoke(this, EventArgs.Empty);
        }

        private void UpdatePreview()
        {
            if (PreviewTextBlock == null) return;
            var s = SettingsService.Current;

            try
            {
                PreviewTextBlock.FontFamily = new FontFamily(s.FontFamilyName);
                PreviewTextBlock.FontSize = s.FontSize;
                PreviewTextBlock.FontWeight = s.FontWeightName switch
                {
                    "Bold" => FontWeights.Bold,
                    "Medium" => FontWeights.Medium,
                    _ => FontWeights.Normal
                };
            }
            catch
            {
                // Fallback
            }
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
