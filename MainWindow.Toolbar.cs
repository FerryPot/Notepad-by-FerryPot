using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using NotepadByFerryPot.Controls;
using NotepadByFerryPot.Services;

namespace NotepadByFerryPot
{
    public partial class MainWindow : Window
    {
        #region Formatting Toolbar Handlers

        private void HeadingMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void Heading1_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ApplyHeading("# "); }
        private void Heading2_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ApplyHeading("## "); }
        private void Heading3_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ApplyHeading("### "); }
        private void Heading4_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ApplyHeading("#### "); }
        private void Heading5_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ApplyHeading("##### "); }
        private void Heading6_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ApplyHeading("###### "); }
        private void Paragraph_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ApplyHeading(""); }

        private void ListMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void BulletList_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ApplyList("bullet"); }
        private void NumberedList_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ApplyList("number"); }
        private void Checklist_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ApplyList("check"); }

        private void Bold_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ToggleWrap("**", "**"); }
        private void Italic_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ToggleWrap("*", "*"); }
        private void Strikethrough_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ToggleWrap("~~", "~~"); }
        private void Underline_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ToggleWrap("<u>", "</u>"); }

        private void TextToolsMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void CaseLower_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ChangeCase(CaseTransformType.Lower); }
        private void CaseUpper_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ChangeCase(CaseTransformType.Upper); }
        private void CaseTitle_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ChangeCase(CaseTransformType.Title); }
        private void CaseSentence_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ChangeCase(CaseTransformType.Sentence); }
        private void CaseToggle_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ChangeCase(CaseTransformType.Toggle); }

        private void FontSizeIncrease_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ChangeSelectionFontSize(2.0); }
        private void FontSizeDecrease_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ChangeSelectionFontSize(-2.0); }

        private void Superscript_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ToggleSuperscript(); }
        private void Subscript_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ToggleSubscript(); }

        private void Link_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.InsertLink(); }

        private void TableMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void Table2x2_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.InsertTable(2, 2); }
        private void Table3x3_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.InsertTable(3, 3); }
        private void Table4x4_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.InsertTable(4, 4); }

        private void ClearFormatting_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.ClearFormatting(); }

        private void OverflowToolbar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateToolbarResponsiveLayout();
        }

        private void UpdateToolbarResponsiveLayout()
        {
            if (FullToolbarPanel == null || OverflowToolbarButton == null) return;

            // When window width is narrow (< 820px), collapse full toolbar and show "..." overflow button
            bool isNarrow = ActualWidth < 820;
            FullToolbarPanel.Visibility = isNarrow ? Visibility.Collapsed : Visibility.Visible;
            OverflowToolbarButton.Visibility = isNarrow ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateFormattingToolbarState(EditorFormattingState? state)
        {
            if (BoldToolbarButton == null || ItalicToolbarButton == null || StrikethroughToolbarButton == null ||
                HeadingDropdownButton == null || ListDropdownButton == null)
            {
                return;
            }

            state ??= new EditorFormattingState();

            bool isReadOnly = _activeDocument?.IsReadOnly ?? false;
            if (FullToolbarPanel != null)
            {
                FullToolbarPanel.IsEnabled = !isReadOnly;
            }
            if (OverflowToolbarButton != null)
            {
                OverflowToolbarButton.IsEnabled = !isReadOnly;
            }

            // Accent brush (Amber)
            Brush primaryBrush;
            try
            {
                primaryBrush = (Brush)FindResource("MaterialDesign.Brush.Primary");
            }
            catch
            {
                primaryBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            }

            // Normal text color (body)
            Brush bodyBrush;
            try
            {
                bodyBrush = (Brush)FindResource("MaterialDesignBody");
            }
            catch
            {
                bodyBrush = Brushes.Gray;
            }

            // Filled rounded background using the theme accent color (amber) at low opacity
            bool isDark = SettingsService.Current.BaseTheme == BaseTheme.Dark;
            Brush highlightBackground;
            Brush activeIconBrush;

            if (isDark)
            {
                // Dark and AMOLED themes: ~22% amber background, vibrant amber icon
                highlightBackground = new SolidColorBrush(Color.FromArgb(56, 255, 193, 7));
                activeIconBrush = primaryBrush;
            }
            else
            {
                // Light theme: ~20% amber background, high-contrast rich amber icon for crisp readability
                highlightBackground = new SolidColorBrush(Color.FromArgb(48, 255, 193, 7));
                activeIconBrush = new SolidColorBrush(Color.FromRgb(196, 125, 0));
            }
            highlightBackground.Freeze();
            if (activeIconBrush.CanFreeze) activeIconBrush.Freeze();

            // Bold
            BoldToolbarButton.Background = state.IsBold ? highlightBackground : Brushes.Transparent;
            BoldToolbarText.Foreground = state.IsBold ? activeIconBrush : bodyBrush;

            // Italic
            ItalicToolbarButton.Background = state.IsItalic ? highlightBackground : Brushes.Transparent;
            ItalicToolbarText.Foreground = state.IsItalic ? activeIconBrush : bodyBrush;

            // Strikethrough
            StrikethroughToolbarButton.Background = state.IsStrikethrough ? highlightBackground : Brushes.Transparent;
            StrikethroughToolbarText.Foreground = state.IsStrikethrough ? activeIconBrush : bodyBrush;

            // Underline
            if (UnderlineToolbarButton != null && UnderlineToolbarText != null)
            {
                UnderlineToolbarButton.Background = state.IsUnderline ? highlightBackground : Brushes.Transparent;
                UnderlineToolbarText.Foreground = state.IsUnderline ? activeIconBrush : bodyBrush;
            }

            // Heading Dropdown
            HeadingDropdownText.Text = state.Heading;
            bool isHeading = state.Heading != "Normal";
            HeadingDropdownButton.Background = isHeading ? highlightBackground : Brushes.Transparent;
            HeadingDropdownText.Foreground = isHeading ? activeIconBrush : bodyBrush;
            HeadingDropdownChevron.Foreground = isHeading ? activeIconBrush : bodyBrush;

            // List Dropdown
            ListDropdownButton.Background = state.IsList ? highlightBackground : Brushes.Transparent;
            ListDropdownIcon.Foreground = state.IsList ? activeIconBrush : bodyBrush;
            ListDropdownChevron.Foreground = state.IsList ? activeIconBrush : bodyBrush;
        }

        #endregion
    }
}
