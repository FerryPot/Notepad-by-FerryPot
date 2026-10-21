using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace NotepadByFerryPot.Services
{
    public static class MarkdownParserService
    {
        public static FlowDocument ParseToFlowDocument(string markdown, FontFamily fontFamily, double baseFontSize, Brush foreground)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(28, 20, 28, 28),
                FontFamily = fontFamily,
                FontSize = baseFontSize,
                Foreground = foreground,
                LineHeight = baseFontSize * 1.5,
                IsOptimalParagraphEnabled = true
            };

            if (string.IsNullOrEmpty(markdown))
            {
                doc.Blocks.Add(new Paragraph(new Run(string.Empty)));
                return doc;
            }

            var lines = markdown.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            int i = 0;

            while (i < lines.Length)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                // 1. Code Block (```)
                if (trimmed.StartsWith("```"))
                {
                    string lang = trimmed.Length > 3 ? trimmed.Substring(3).Trim() : "";
                    i++;
                    var codeLines = new List<string>();
                    while (i < lines.Length && !lines[i].Trim().StartsWith("```"))
                    {
                        codeLines.Add(lines[i]);
                        i++;
                    }
                    if (i < lines.Length) i++; // skip closing ```

                    doc.Blocks.Add(CreateCodeBlock(string.Join(Environment.NewLine, codeLines), lang, baseFontSize));
                    continue;
                }

                // 2. Horizontal Rule (---, ***, ___)
                if (Regex.IsMatch(trimmed, @"^(---|___|\*\*\*)\s*$"))
                {
                    doc.Blocks.Add(CreateHorizontalRule());
                    i++;
                    continue;
                }

                // 3. Headings (#, ##, ###, ####, #####, ######)
                if (trimmed.StartsWith("#"))
                {
                    int level = 0;
                    while (level < trimmed.Length && trimmed[level] == '#') level++;
                    if (level <= 6 && level < trimmed.Length && trimmed[level] == ' ')
                    {
                        string headerText = trimmed.Substring(level + 1).Trim();
                        doc.Blocks.Add(CreateHeader(headerText, level, baseFontSize, foreground));
                        i++;
                        continue;
                    }
                }

                // 4. Blockquotes (> ...)
                if (trimmed.StartsWith(">"))
                {
                    var quoteLines = new List<string>();
                    while (i < lines.Length && lines[i].TrimStart().StartsWith(">"))
                    {
                        string qLine = lines[i].TrimStart();
                        quoteLines.Add(qLine.Length > 1 && qLine[1] == ' ' ? qLine.Substring(2) : qLine.Substring(1));
                        i++;
                    }
                    doc.Blocks.Add(CreateBlockquote(string.Join(Environment.NewLine, quoteLines), baseFontSize, foreground));
                    continue;
                }

                // 5. Tables (| col | col |)
                if (trimmed.StartsWith("|") && trimmed.EndsWith("|"))
                {
                    var tableLines = new List<string>();
                    while (i < lines.Length && lines[i].Trim().StartsWith("|") && lines[i].Trim().EndsWith("|"))
                    {
                        tableLines.Add(lines[i].Trim());
                        i++;
                    }
                    if (tableLines.Count >= 2 && tableLines[1].Contains("-"))
                    {
                        doc.Blocks.Add(CreateTable(tableLines, baseFontSize, foreground));
                        continue;
                    }
                    else
                    {
                        foreach (var tLine in tableLines)
                        {
                            doc.Blocks.Add(CreateParagraph(tLine, baseFontSize, foreground));
                        }
                        continue;
                    }
                }

                // 6. Lists (- , * , + , 1. , - [ ] )
                if (IsListLine(trimmed))
                {
                    var listItems = new List<(string marker, string text)>();
                    bool isNumbered = false;

                    while (i < lines.Length && IsListLine(lines[i].Trim()))
                    {
                        var parsed = ParseListLine(lines[i].Trim());
                        if (parsed.marker == "1." || (parsed.marker.Length > 0 && char.IsDigit(parsed.marker[0]))) isNumbered = true;
                        listItems.Add(parsed);
                        i++;
                    }

                    doc.Blocks.Add(CreateList(listItems, isNumbered, baseFontSize, foreground));
                    continue;
                }

                // 7. Empty line
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    var emptyP = new Paragraph { Margin = new Thickness(0, 0, 0, baseFontSize * 0.4) };
                    doc.Blocks.Add(emptyP);
                    i++;
                    continue;
                }

                // 8. Normal Paragraph (accumulate multi-line paragraph)
                var pLines = new List<string> { line };
                i++;
                while (i < lines.Length && 
                       !string.IsNullOrWhiteSpace(lines[i].Trim()) && 
                       !lines[i].Trim().StartsWith("#") && 
                       !lines[i].Trim().StartsWith("```") && 
                       !lines[i].Trim().StartsWith(">") && 
                       !IsListLine(lines[i].Trim()) &&
                       !(lines[i].Trim().StartsWith("|") && lines[i].Trim().EndsWith("|")))
                {
                    pLines.Add(lines[i]);
                    i++;
                }

                doc.Blocks.Add(CreateParagraph(string.Join(" ", pLines), baseFontSize, foreground));
            }

            return doc;
        }

        public static void AddFormattedInlines(Paragraph paragraph, string text, Brush foreground)
        {
            if (string.IsNullOrEmpty(text)) return;

            string pattern = @"(?<bolditalic>\*\*\*(.+?)\*\*\*)|" +
                             @"(?<bold>\*\*(.+?)\*\*|__(.+?)__)|" +
                             @"(?<strike>~~(.+?)~~)|" +
                             @"(?<italic>\*(.+?)\*|_(.+?)_)|" +
                             @"(?<code>`([^`]+)`)|" +
                             @"(?<link>\[(?<text>[^\]]+)\]\((?<url>[^)]+)\))";

            int lastIndex = 0;
            var matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    paragraph.Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
                }

                if (match.Groups["bolditalic"].Success)
                {
                    string raw = match.Groups["bolditalic"].Value;
                    string content = raw.Substring(3, raw.Length - 6);
                    var span = new Span(new Run(content)) { FontWeight = FontWeights.Bold, FontStyle = FontStyles.Italic };
                    paragraph.Inlines.Add(span);
                }
                else if (match.Groups["bold"].Success)
                {
                    string raw = match.Groups["bold"].Value;
                    string content = raw.Substring(2, raw.Length - 4);
                    var bold = new Bold(new Run(content));
                    paragraph.Inlines.Add(bold);
                }
                else if (match.Groups["strike"].Success)
                {
                    string raw = match.Groups["strike"].Value;
                    string content = raw.Substring(2, raw.Length - 4);
                    var strike = new Span(new Run(content)) { TextDecorations = TextDecorations.Strikethrough };
                    paragraph.Inlines.Add(strike);
                }
                else if (match.Groups["italic"].Success)
                {
                    string raw = match.Groups["italic"].Value;
                    string content = raw.Substring(1, raw.Length - 2);
                    var italic = new Italic(new Run(content));
                    paragraph.Inlines.Add(italic);
                }
                else if (match.Groups["code"].Success)
                {
                    string raw = match.Groups["code"].Value;
                    string content = raw.Substring(1, raw.Length - 2);
                    var codeRun = new Run(content)
                    {
                        FontFamily = new FontFamily("Consolas"),
                        Background = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128))
                    };
                    paragraph.Inlines.Add(codeRun);
                }
                else if (match.Groups["link"].Success)
                {
                    string linkText = match.Groups["text"].Value;
                    string url = match.Groups["url"].Value;
                    try
                    {
                        var hyperlink = new Hyperlink(new Run(linkText))
                        {
                            NavigateUri = new Uri(url.StartsWith("http") ? url : "https://" + url),
                            ToolTip = url
                        };
                        try
                        {
                            hyperlink.Foreground = (Brush)Application.Current.FindResource("MaterialDesign.Brush.Primary");
                        }
                        catch { }

                        hyperlink.RequestNavigate += (s, e) =>
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = e.Uri.AbsoluteUri,
                                    UseShellExecute = true
                                });
                            }
                            catch { }
                        };
                        paragraph.Inlines.Add(hyperlink);
                    }
                    catch
                    {
                        paragraph.Inlines.Add(new Run(linkText));
                    }
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                paragraph.Inlines.Add(new Run(text.Substring(lastIndex)));
            }
        }

        private static Block CreateTable(List<string> tableLines, double baseFontSize, Brush foreground)
        {
            var table = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 10, 0, 14)
            };

            try
            {
                table.BorderBrush = (Brush)Application.Current.FindResource("MaterialDesignDivider");
                table.BorderThickness = new Thickness(1);
            }
            catch { }

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            var headerCells = tableLines[0].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            int colCount = headerCells.Length;

            for (int c = 0; c < colCount; c++)
            {
                table.Columns.Add(new TableColumn());
            }

            var headerRow = new TableRow();
            try
            {
                headerRow.Background = (Brush)Application.Current.FindResource("MaterialDesignCardBackground");
            }
            catch { }

            foreach (var h in headerCells)
            {
                var p = new Paragraph { FontWeight = FontWeights.Bold, Margin = new Thickness(0) };
                AddFormattedInlines(p, h.Trim(), foreground);
                var cell = new TableCell(p)
                {
                    Padding = new Thickness(12, 8, 12, 8)
                };
                try
                {
                    cell.BorderBrush = (Brush)Application.Current.FindResource("MaterialDesignDivider");
                    cell.BorderThickness = new Thickness(0, 0, 1, 1);
                }
                catch { }
                headerRow.Cells.Add(cell);
            }
            rowGroup.Rows.Add(headerRow);

            for (int r = 2; r < tableLines.Count; r++)
            {
                var rowCells = tableLines[r].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                var row = new TableRow();
                for (int c = 0; c < colCount; c++)
                {
                    string content = c < rowCells.Length ? rowCells[c].Trim() : "";
                    var p = new Paragraph { Margin = new Thickness(0) };
                    AddFormattedInlines(p, content, foreground);
                    var cell = new TableCell(p)
                    {
                        Padding = new Thickness(12, 6, 12, 6)
                    };
                    try
                    {
                        cell.BorderBrush = (Brush)Application.Current.FindResource("MaterialDesignDivider");
                        cell.BorderThickness = new Thickness(0, 0, 1, 1);
                    }
                    catch { }
                    row.Cells.Add(cell);
                }
                rowGroup.Rows.Add(row);
            }

            return table;
        }

        private static Block CreateCodeBlock(string code, string lang, double baseFontSize)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 10, 14, 10),
                Child = new TextBlock
                {
                    Text = code,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = baseFontSize * 0.95,
                    TextWrapping = TextWrapping.Wrap
                }
            };

            try
            {
                border.Background = (Brush)Application.Current.FindResource("MaterialDesignCardBackground");
                border.BorderBrush = (Brush)Application.Current.FindResource("MaterialDesignDivider");
                border.BorderThickness = new Thickness(1);
            }
            catch { }

            return new BlockUIContainer(border) { Margin = new Thickness(0, 8, 0, 12) };
        }

        private static Block CreateBlockquote(string text, double baseFontSize, Brush foreground)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(4, 0, 0, 0),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 8, 0, 12),
                CornerRadius = new CornerRadius(0, 4, 4, 0),
                Child = new TextBlock
                {
                    Text = text,
                    FontStyle = FontStyles.Italic,
                    Foreground = foreground,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = baseFontSize
                }
            };

            try
            {
                border.BorderBrush = (Brush)Application.Current.FindResource("MaterialDesign.Brush.Primary");
                border.Background = (Brush)Application.Current.FindResource("MaterialDesignCardBackground");
            }
            catch { }

            return new BlockUIContainer(border);
        }

        private static Block CreateList(List<(string marker, string text)> items, bool isNumbered, double baseFontSize, Brush foreground)
        {
            var list = new List
            {
                MarkerStyle = isNumbered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
                Margin = new Thickness(0, 4, 0, 10),
                Padding = new Thickness(24, 0, 0, 0)
            };

            foreach (var item in items)
            {
                if (item.marker == "- [ ]" || item.marker == "- [x]")
                {
                    bool isChecked = item.marker == "- [x]";
                    var sp = new StackPanel { Orientation = Orientation.Horizontal };
                    var cb = new CheckBox
                    {
                        IsChecked = isChecked,
                        IsEnabled = false,
                        Margin = new Thickness(0, 0, 8, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    var tb = new TextBlock
                    {
                        Text = item.text,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextDecorations = isChecked ? TextDecorations.Strikethrough : null
                    };
                    sp.Children.Add(cb);
                    sp.Children.Add(tb);
                    var li = new ListItem();
                    li.Blocks.Add(new BlockUIContainer(sp));
                    list.ListItems.Add(li);
                }
                else
                {
                    var p = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                    AddFormattedInlines(p, item.text, foreground);
                    list.ListItems.Add(new ListItem(p));
                }
            }

            return list;
        }

        private static Block CreateHeader(string text, int level, double baseFontSize, Brush foreground)
        {
            double size = level switch
            {
                1 => baseFontSize * 1.85,
                2 => baseFontSize * 1.5,
                3 => baseFontSize * 1.25,
                4 => baseFontSize * 1.1,
                _ => baseFontSize
            };

            var p = new Paragraph
            {
                FontSize = size,
                FontWeight = level <= 2 ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = foreground,
                Margin = new Thickness(0, baseFontSize * (level == 1 ? 1.2 : 0.8), 0, baseFontSize * 0.4)
            };
            AddFormattedInlines(p, text, foreground);

            if (level == 1)
            {
                try
                {
                    p.BorderBrush = (Brush)Application.Current.FindResource("MaterialDesignDivider");
                    p.BorderThickness = new Thickness(0, 0, 0, 1);
                    p.Padding = new Thickness(0, 0, 0, 6);
                }
                catch { }
            }

            return p;
        }

        private static Block CreateParagraph(string text, double baseFontSize, Brush foreground)
        {
            var p = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, baseFontSize * 0.6),
                LineHeight = baseFontSize * 1.45
            };
            AddFormattedInlines(p, text, foreground);
            return p;
        }

        private static Block CreateHorizontalRule()
        {
            var rect = new System.Windows.Shapes.Rectangle
            {
                Height = 1,
                Margin = new Thickness(0, 12, 0, 16)
            };

            try
            {
                rect.Fill = (Brush)Application.Current.FindResource("MaterialDesignDivider");
            }
            catch { }

            return new BlockUIContainer(rect);
        }

        private static bool IsListLine(string line)
        {
            if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("+ ")) return true;
            if (line.StartsWith("- [ ] ") || line.StartsWith("- [x] ")) return true;
            int dot = line.IndexOf(". ");
            if (dot > 0 && int.TryParse(line.Substring(0, dot), out _)) return true;
            return false;
        }

        private static (string marker, string text) ParseListLine(string line)
        {
            if (line.StartsWith("- [ ] ")) return ("- [ ]", line.Substring(6));
            if (line.StartsWith("- [x] ")) return ("- [x]", line.Substring(6));
            if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("+ ")) return ("-", line.Substring(2));
            int dot = line.IndexOf(". ");
            if (dot > 0 && int.TryParse(line.Substring(0, dot), out _))
            {
                return (line.Substring(0, dot + 1), line.Substring(dot + 2));
            }
            return ("", line);
        }
    }
}
