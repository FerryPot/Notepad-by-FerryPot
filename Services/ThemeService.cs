using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

namespace NotepadByFerryPot.Services
{
    public static class ThemeService
    {
        public static void ApplyTheme(AppSettings settings)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            // Apply base theme
            theme.SetBaseTheme(settings.BaseTheme);

            // Apply primary color
            var primaryColor = SwatchHelper.Lookup[(MaterialDesignColor)settings.PrimaryColor];
            theme.SetPrimaryColor(primaryColor);

            // AMOLED Pure Black Dark Mode override
            if (settings.BaseTheme == BaseTheme.Dark && settings.IsPureBlackDark)
            {
                theme.Background = Color.FromRgb(0, 0, 0);
            }

            paletteHelper.SetTheme(theme);
        }
    }
}
