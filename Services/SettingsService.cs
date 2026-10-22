using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

namespace NotepadByFerryPot.Services
{
    public class SafeBaseThemeConverter : JsonConverter<BaseTheme>
    {
        public override BaseTheme Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? str = reader.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    if (str.Equals("System", StringComparison.OrdinalIgnoreCase))
                        return BaseTheme.Inherit;
                    if (Enum.TryParse<BaseTheme>(str, true, out var parsed) && Enum.IsDefined(typeof(BaseTheme), parsed))
                        return parsed;
                }
                return BaseTheme.Inherit;
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int intVal))
            {
                if (Enum.IsDefined(typeof(BaseTheme), (BaseTheme)intVal))
                {
                    return (BaseTheme)intVal;
                }
                return BaseTheme.Inherit;
            }

            return BaseTheme.Inherit;
        }

        public override void Write(Utf8JsonWriter writer, BaseTheme value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }

    public class SafePrimaryColorConverter : JsonConverter<PrimaryColor>
    {
        public override PrimaryColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? str = reader.GetString();
                if (!string.IsNullOrWhiteSpace(str) &&
                    Enum.TryParse<PrimaryColor>(str, true, out var parsed) &&
                    Enum.IsDefined(typeof(PrimaryColor), parsed))
                {
                    return parsed;
                }
                return PrimaryColor.Amber;
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int intVal))
            {
                if (Enum.IsDefined(typeof(PrimaryColor), (PrimaryColor)intVal))
                {
                    return (PrimaryColor)intVal;
                }
                return PrimaryColor.Amber;
            }

            return PrimaryColor.Amber;
        }

        public override void Write(Utf8JsonWriter writer, PrimaryColor value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }

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
        public bool ShowFormattingToolbar { get; set; } = true;

        public void ValidateAndClamp()
        {
            if (!Enum.IsDefined(typeof(BaseTheme), BaseTheme))
            {
                BaseTheme = BaseTheme.Inherit;
            }

            if (!Enum.IsDefined(typeof(PrimaryColor), PrimaryColor))
            {
                PrimaryColor = PrimaryColor.Amber;
            }

            if (string.IsNullOrWhiteSpace(FontFamilyName))
            {
                FontFamilyName = "Consolas";
            }

            if (string.IsNullOrWhiteSpace(FontWeightName))
            {
                FontWeightName = "Normal";
            }

            if (double.IsNaN(FontSize) || double.IsInfinity(FontSize))
            {
                FontSize = 15.0;
            }
            else
            {
                FontSize = Math.Clamp(FontSize, 8.0, 72.0);
            }

            TabSize = Math.Clamp(TabSize, 1, 16);

            if (double.IsNaN(ZoomLevel) || double.IsInfinity(ZoomLevel))
            {
                ZoomLevel = 100.0;
            }
            else
            {
                ZoomLevel = Math.Clamp(ZoomLevel, 10.0, 500.0);
            }
        }
    }

    public static class SettingsService
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NotepadByFerryPot"
        );
        private static readonly string SettingsFilePath = Path.Combine(SettingsDir, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new SafeBaseThemeConverter(),
                new SafePrimaryColorConverter()
            }
        };

        public static AppSettings Current { get; private set; } = new();

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                }
                else
                {
                    Current = new AppSettings();
                }
            }
            catch (Exception ex)
            {
                CrashLogService.LogError("SettingsService.Load", ex);
                Current = new AppSettings();
            }
            finally
            {
                Current.ValidateAndClamp();
            }
        }

        public static bool Save()
        {
            try
            {
                Current.ValidateAndClamp();
                Directory.CreateDirectory(SettingsDir);
                var json = JsonSerializer.Serialize(Current, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                string logPath = CrashLogService.LogError("SettingsService.Save", ex);
                MessageBox.Show(
                    $"Unable to save application settings: {ex.Message}\n\nDetails logged to:\n{logPath}",
                    "Settings Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }
        }
    }
}
