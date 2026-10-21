using System;
using System.IO;
using System.Text.Json;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

namespace NotepadByFerryPot.Services
{
    public class AppSettings
    {
        public BaseTheme BaseTheme { get; set; } = BaseTheme.Inherit;
        public PrimaryColor PrimaryColor { get; set; } = PrimaryColor.Amber;
        public bool IsPureBlackDark { get; set; } = false;
        public string FontFamilyName { get; set; } = "Consolas";
        public double FontSize { get; set; } = 15.0;
        public string FontWeightName { get; set; } = "Normal";
        public bool WordWrap { get; set; } = true;
        public bool ShowLineNumbers { get; set; } = true;
        public bool HighlightActiveLine { get; set; } = true;
        public bool AutoIndent { get; set; } = true;
        public bool AutoCloseBrackets { get; set; } = true;
        public int TabSize { get; set; } = 4;
        public bool UseSpacesForTab { get; set; } = true;
        public bool ShowStatusBar { get; set; } = true;
        public bool FocusMode { get; set; } = false;
        public double ZoomLevel { get; set; } = 100.0;
    }

    public static class SettingsService
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NotepadByFerryPot"
        );
        private static readonly string SettingsFilePath = Path.Combine(SettingsDir, "settings.json");

        public static AppSettings Current { get; private set; } = new();

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                Current = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Ignore persistence errors
            }
        }
    }
}
