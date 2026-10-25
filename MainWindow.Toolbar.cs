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
        private string _lastHighlightColor = "#FFF176";

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

        private void Highlight_Click(object sender, RoutedEventArgs e)
        {
            if (_activeDocument?.IsReadOnly == true) return;
            ActiveEditor.ApplyHighlight(_lastHighlightColor);
        }

        private void HighlightMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = HighlightBorder ?? (UIElement)btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void SetHighlightColor(string? hex)
        {
            if (_activeDocument?.IsReadOnly == true) return;
            if (hex != null)
            {
                _lastHighlightColor = hex;
                try
                {
                    if (HighlightColorIndicator != null)
                    {
                        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                        brush.Freeze();
                        HighlightColorIndicator.Background = brush;
                    }
                }
                catch { }
            }
            ActiveEditor.ApplyHighlight(hex);
        }

        private void HighlightYellow_Click(object sender, RoutedEventArgs e) => SetHighlightColor("#FFF176");
        private void HighlightGreen_Click(object sender, RoutedEventArgs e) => SetHighlightColor("#81C784");
        private void HighlightBlue_Click(object sender, RoutedEventArgs e) => SetHighlightColor("#64B5F6");
        private void HighlightPink_Click(object sender, RoutedEventArgs e) => SetHighlightColor("#F06292");
        private void HighlightOrange_Click(object sender, RoutedEventArgs e) => SetHighlightColor("#FFB74D");
        private void HighlightPurple_Click(object sender, RoutedEventArgs e) => SetHighlightColor("#BA68C8");
        private void HighlightNone_Click(object sender, RoutedEventArgs e) => SetHighlightColor(null);

        private void Link_Click(object sender, RoutedEventArgs e) { if (_activeDocument?.IsReadOnly == true) return; ActiveEditor.InsertLink(); }

        private void TableMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                PopulateTableMenu(btn.ContextMenu);
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void PopulateTableMenu(ContextMenu menu)
        {
            menu.Items.Clear();
            var editor = ActiveEditor;
            if (editor == null) return;

            bool isReadOnly = _activeDocument?.IsReadOnly == true;
            bool insideTable = editor.IsInsideTable();

            if (insideTable)
            {
                var cell = editor.GetCurrentTableCell();
                var table = editor.GetCurrentTable();

                // Row Operations
                var rowMenu = new MenuItem { Header = "Row", IsEnabled = !isReadOnly };
                var insertRowAbove = new MenuItem { Header = "Insert Row Above" };
                insertRowAbove.Click += (s, e) => editor.InsertRowAbove(cell);
                var insertRowBelow = new MenuItem { Header = "Insert Row Below" };
                insertRowBelow.Click += (s, e) => editor.InsertRowBelow(cell);
                var deleteRow = new MenuItem { Header = "Delete Row" };
                deleteRow.Click += (s, e) => editor.DeleteRow(cell);
                rowMenu.Items.Add(insertRowAbove);
                rowMenu.Items.Add(insertRowBelow);
                rowMenu.Items.Add(deleteRow);
                menu.Items.Add(rowMenu);

                // Column Operations
                var colMenu = new MenuItem { Header = "Column", IsEnabled = !isReadOnly };
                var insertColLeft = new MenuItem { Header = "Insert Column Left" };
                insertColLeft.Click += (s, e) => editor.InsertColumnLeft(cell);
                var insertColRight = new MenuItem { Header = "Insert Column Right" };
                insertColRight.Click += (s, e) => editor.InsertColumnRight(cell);
                var deleteCol = new MenuItem { Header = "Delete Column" };
                deleteCol.Click += (s, e) => editor.DeleteColumn(cell);
                colMenu.Items.Add(insertColLeft);
                colMenu.Items.Add(insertColRight);
                colMenu.Items.Add(deleteCol);
                menu.Items.Add(colMenu);

                // Column Alignment
                var alignMenu = new MenuItem { Header = "Align Column", IsEnabled = !isReadOnly };
                var curAlign = editor.GetCurrentColumnAlignment();
                var alignLeft = new MenuItem { Header = "Left", IsChecked = curAlign == TextAlignment.Left };
                alignLeft.Click += (s, e) => editor.SetColumnAlignment(TextAlignment.Left, cell);
                var alignCenter = new MenuItem { Header = "Center", IsChecked = curAlign == TextAlignment.Center };
                alignCenter.Click += (s, e) => editor.SetColumnAlignment(TextAlignment.Center, cell);
                var alignRight = new MenuItem { Header = "Right", IsChecked = curAlign == TextAlignment.Right };
                alignRight.Click += (s, e) => editor.SetColumnAlignment(TextAlignment.Right, cell);
                alignMenu.Items.Add(alignLeft);
                alignMenu.Items.Add(alignCenter);
                alignMenu.Items.Add(alignRight);
                menu.Items.Add(alignMenu);

                // Table Style Presets
                var styleMenu = new MenuItem { Header = "Table Style", IsEnabled = !isReadOnly };
                var curPreset = editor.GetCurrentTablePreset();
                var gridStyle = new MenuItem { Header = "Light Grid (Default)", IsChecked = curPreset == "grid" };
                gridStyle.Click += (s, e) => editor.SetTablePreset("grid", table);
                var zebraStyle = new MenuItem { Header = "Zebra Rows", IsChecked = curPreset == "zebra" };
                zebraStyle.Click += (s, e) => editor.SetTablePreset("zebra", table);
                var minimalStyle = new MenuItem { Header = "Minimal", IsChecked = curPreset == "minimal" };
                minimalStyle.Click += (s, e) => editor.SetTablePreset("minimal", table);
                styleMenu.Items.Add(gridStyle);
                styleMenu.Items.Add(zebraStyle);
                styleMenu.Items.Add(minimalStyle);
                menu.Items.Add(styleMenu);

                // Header Row Toggle
                bool curHeader = editor.GetCurrentTableHeaderState();
                var headerItem = new MenuItem { Header = "Header Row", IsCheckable = true, IsChecked = curHeader, IsEnabled = !isReadOnly };
                headerItem.Click += (s, e) => editor.ToggleHeaderRow(table);
                menu.Items.Add(headerItem);

                menu.Items.Add(new Separator());

                // Delete Table
                var delTableItem = new MenuItem { Header = "Delete Table", IsEnabled = !isReadOnly };
                delTableItem.Click += (s, e) => editor.DeleteTable(table);
                menu.Items.Add(delTableItem);

                menu.Items.Add(new Separator());

                // Submenu to insert new table even if inside a table
                var insertSubMenu = new MenuItem { Header = "Insert New Table", IsEnabled = !isReadOnly };
                var sub2x2 = new MenuItem { Header = "2 × 2 Table" };
                sub2x2.Click += (s, e) => editor.InsertTable(2, 2);
                var sub3x3 = new MenuItem { Header = "3 × 3 Table" };
                sub3x3.Click += (s, e) => editor.InsertTable(3, 3);
                var sub4x4 = new MenuItem { Header = "4 × 4 Table" };
                sub4x4.Click += (s, e) => editor.InsertTable(4, 4);
                insertSubMenu.Items.Add(sub2x2);
                insertSubMenu.Items.Add(sub3x3);
                insertSubMenu.Items.Add(sub4x4);
                menu.Items.Add(insertSubMenu);
            }
            else
            {
                var t2x2 = new MenuItem { Header = "2 × 2 Table", IsEnabled = !isReadOnly };
                t2x2.Click += (s, e) => editor.InsertTable(2, 2);
                var t3x3 = new MenuItem { Header = "3 × 3 Table", IsEnabled = !isReadOnly };
                t3x3.Click += (s, e) => editor.InsertTable(3, 3);
                var t4x4 = new MenuItem { Header = "4 × 4 Table", IsEnabled = !isReadOnly };
                t4x4.Click += (s, e) => editor.InsertTable(4, 4);

                menu.Items.Add(t2x2);
                menu.Items.Add(t3x3);
                menu.Items.Add(t4x4);
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

            // When window width is narrow (< 870px), collapse full toolbar and show "..." overflow button
            bool isNarrow = ActualWidth < 870;
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

            // Highlight
            if (HighlightBorder != null && HighlightToolbarIcon != null && HighlightDropdownChevron != null)
            {
                HighlightBorder.Background = state.IsHighlight ? highlightBackground : Brushes.Transparent;
                HighlightToolbarIcon.Foreground = state.IsHighlight ? activeIconBrush : bodyBrush;
                HighlightDropdownChevron.Foreground = state.IsHighlight ? activeIconBrush : bodyBrush;
            }

            // Table
            if (TableToolbarButton != null && TableToolbarIcon != null && TableToolbarChevron != null)
            {
                TableToolbarButton.Background = state.IsTable ? highlightBackground : Brushes.Transparent;
                TableToolbarIcon.Foreground = state.IsTable ? activeIconBrush : bodyBrush;
                TableToolbarChevron.Foreground = state.IsTable ? activeIconBrush : bodyBrush;
            }
        }

        #endregion
    }
}
