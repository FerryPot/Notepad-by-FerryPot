using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
                PagePadding = new Thickness(24, 20, 24, 20),
                FontFamily = fontFamily,
                FontSize = baseFontSize,
                LineHeight = baseFontSize * 1.5,
                IsOptimalParagraphEnabled = true,
                TextAlignment = TextAlignment.Left
            };
            doc.SetResourceReference(FlowDocument.ForegroundProperty, "MaterialDesignBody");

            if (string.IsNullOrEmpty(markdown))
            {
                doc.Blocks.Add(new Paragraph(new Run(string.Empty)) { Margin = new Thickness(0) });
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
                if (Regex.IsMatch(trimmed, @"^(---|___|\*\*\*)\s*$", RegexOptions.None, TimeSpan.FromSeconds(1)))
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
                        doc.Blocks.Add(CreateHeader(headerText, level, baseFontSize, foreground, isFirst: doc.Blocks.Count == 0));
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

        public static string FlowDocumentToMarkdown(FlowDocument doc)
        {
            var sb = new StringBuilder();

            foreach (var block in doc.Blocks)
            {
                if (block is Paragraph p)
                {
                    int headingLevel = 0;
                    if (p.Tag is string tag && tag.StartsWith("h") && int.TryParse(tag.Substring(1), out int lvl))
                    {
                        headingLevel = lvl;
                    }
                    else if (p.FontSize >= 24 && p.FontWeight == FontWeights.Bold) headingLevel = 1;
                    else if (p.FontSize >= 20 && p.FontWeight == FontWeights.Bold) headingLevel = 2;
                    else if (p.FontSize >= 17 && (p.FontWeight == FontWeights.Bold || p.FontWeight == FontWeights.SemiBold)) headingLevel = 3;
                    else if (p.FontSize >= 15.5 && (p.FontWeight == FontWeights.Bold || p.FontWeight == FontWeights.SemiBold)) headingLevel = 4;
                    else if (p.FontSize >= 14 && (p.FontWeight == FontWeights.Bold || p.FontWeight == FontWeights.SemiBold) && p.Tag != null) headingLevel = 5;

                    bool isHeading = headingLevel > 0;
                    string pText = InlinesToMarkdown(p.Inlines, isParentBold: isHeading, isParentItalic: false).Trim();

                    if (isHeading)
                    {
                        while (pText.StartsWith("#")) pText = pText.Substring(1).TrimStart();
                        sb.AppendLine(new string('#', headingLevel) + " " + pText);
                        sb.AppendLine();
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(pText))
                    {
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine(pText);
                        sb.AppendLine();
                    }
                }
                else if (block is List list)
                {
                    bool isNumbered = list.MarkerStyle == TextMarkerStyle.Decimal;
                    int idx = 1;
                    foreach (var item in list.ListItems)
                    {
                        string itemText = "";
                        foreach (var b in item.Blocks)
                        {
                            if (b is Paragraph lp)
                            {
                                itemText += InlinesToMarkdown(lp.Inlines);
                            }
                            else if (b is BlockUIContainer bui && bui.Child is StackPanel sp)
                            {
                                foreach (var child in sp.Children)
                                {
                                    if (child is CheckBox cb)
                                    {
                                        itemText += cb.IsChecked == true ? "- [x] " : "- [ ] ";
                                    }
                                    else if (child is TextBlock tb)
                                    {
                                        itemText += tb.Text;
                                    }
                                }
                            }
                        }

                        itemText = itemText.Trim();
                        if (itemText.StartsWith("- [ ]") || itemText.StartsWith("- [x]"))
                        {
                            sb.AppendLine(itemText);
                        }
                        else
                        {
                            if (itemText.StartsWith("- ") || itemText.StartsWith("* ") || itemText.StartsWith("+ "))
                            {
                                itemText = itemText.Substring(2).TrimStart();
                            }
                            else
                            {
                                int dotIdx = itemText.IndexOf(". ");
                                if (dotIdx > 0 && int.TryParse(itemText.Substring(0, dotIdx), out _))
                                {
                                    itemText = itemText.Substring(dotIdx + 2).TrimStart();
                                }
                            }

                            if (isNumbered)
                            {
                                sb.AppendLine($"{idx}. {itemText}");
                                idx++;
                            }
                            else
                            {
                                sb.AppendLine($"- {itemText}");
                            }
                        }
                    }
                    sb.AppendLine();
                }
                else if (block is Table table)
                {
                    foreach (var group in table.RowGroups)
                    {
                        for (int r = 0; r < group.Rows.Count; r++)
                        {
                            var row = group.Rows[r];
                            var cellTexts = new List<string>();
                            foreach (var cell in row.Cells)
                            {
                                string cellText = "";
                                foreach (var b in cell.Blocks)
                                {
                                    if (b is Paragraph cp) cellText += InlinesToMarkdown(cp.Inlines);
                                }
                                cellTexts.Add(cellText.Trim());
                            }
                            sb.AppendLine("| " + string.Join(" | ", cellTexts) + " |");

                            if (r == 0)
                            {
                                var seps = new List<string>();
                                for (int c = 0; c < row.Cells.Count; c++) seps.Add("---");
                                sb.AppendLine("| " + string.Join(" | ", seps) + " |");
                            }
                        }
                    }
                    sb.AppendLine();
                }
                else if (block is BlockUIContainer container)
                {
                    if (container.Tag as string == "hr" || (container.Child is System.Windows.Shapes.Rectangle))
                    {
                        sb.AppendLine("---");
                        sb.AppendLine();
                    }
                    else if (container.Child is Border border && border.Child is TextBlock tb)
                    {
                        if (container.Tag as string == "quote")
                        {
                            sb.AppendLine("> " + tb.Text);
                            sb.AppendLine();
                        }
                        else
                        {
                            sb.AppendLine("```");
                            sb.AppendLine(tb.Text);
                            sb.AppendLine("```");
                            sb.AppendLine();
                        }
                    }
                }
            }

            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        public static string InlinesToMarkdown(InlineCollection inlines, bool isParentBold = false, bool isParentItalic = false)
        {
            var sb = new StringBuilder();
            foreach (var inline in inlines)
            {
                sb.Append(InlineToMarkdown(inline, isParentBold, isParentItalic));
            }
            return sb.ToString();
        }

        private static string InlineToMarkdown(Inline inline, bool isParentBold, bool isParentItalic)
        {
            if (inline is Hyperlink hl)
            {
                string text = InlinesToMarkdown(hl.Inlines, isParentBold, isParentItalic);
                return $"[{text}]({hl.NavigateUri})";
            }

            if (inline is Bold b)
            {
                string inner = InlinesToMarkdown(b.Inlines, isParentBold: true, isParentItalic: isParentItalic);
                return isParentBold ? inner : $"**{inner}**";
            }

            if (inline is Italic it)
            {
                string inner = InlinesToMarkdown(it.Inlines, isParentBold: isParentBold, isParentItalic: true);
                return isParentItalic ? inner : $"*{inner}*";
            }

            if (inline is Span sp)
            {
                bool hasStrike = sp.TextDecorations != null && sp.TextDecorations.Any(d => d.Location == TextDecorationLocation.Strikethrough);
                bool isBold = (sp.FontWeight == FontWeights.Bold || sp.FontWeight == FontWeights.SemiBold || sp.FontWeight == FontWeights.ExtraBold) && !isParentBold;
                bool isItalic = (sp.FontStyle == FontStyles.Italic) && !isParentItalic;

                string inner = InlinesToMarkdown(sp.Inlines, isParentBold || isBold, isParentItalic || isItalic);

                if (isBold && isItalic)
                {
                    inner = $"***{inner}***";
                }
                else if (isBold)
                {
                    inner = $"**{inner}**";
                }
                else if (isItalic)
                {
                    inner = $"*{inner}*";
                }

                if (hasStrike)
                {
                    inner = $"~~{inner}~~";
                }

                return inner;
            }

            if (inline is Run r)
            {
                string text = r.Text;
                if (string.IsNullOrEmpty(text)) return "";

                if (r.FontFamily != null && r.FontFamily.Source == "Consolas")
                {
                    return $"`{text}`";
                }

                bool hasStrike = r.TextDecorations != null && r.TextDecorations.Any(d => d.Location == TextDecorationLocation.Strikethrough);
                bool isBold = (r.FontWeight == FontWeights.Bold || r.FontWeight == FontWeights.SemiBold || r.FontWeight == FontWeights.ExtraBold) && !isParentBold;
                bool isItalic = (r.FontStyle == FontStyles.Italic) && !isParentItalic;

                string result = text;
                if (isBold && isItalic)
                {
                    result = $"***{result}***";
                }
                else if (isBold)
                {
                    result = $"**{result}**";
                }
                else if (isItalic)
                {
                    result = $"*{result}*";
                }

                if (hasStrike)
                {
                    result = $"~~{result}~~";
                }

                return result;
            }

            if (inline is LineBreak)
            {
                return "\n";
            }

            return "";
        }

        public static void AddFormattedInlines(Paragraph paragraph, string text, Brush foreground)
        {
            if (string.IsNullOrEmpty(text)) return;
            AddFormattedInlinesInternal(paragraph.Inlines, text, foreground);
        }

        private static void AddFormattedInlinesInternal(InlineCollection inlines, string text, Brush foreground)
        {
            if (string.IsNullOrEmpty(text)) return;

            string pattern = @"(?<code>`(?<codeText>[^`]+)`)|" +
                             @"(?<link>\[(?<linkText>[^\]]+)\]\((?<url>[^)]+)\))|" +
                             @"(?<bolditalic>(?:\*\*\*|___|\*\*_|__\*)(?<biContent>.+?)(?:\*\*\*|___|_\*\*|\*__))|" +
                             @"(?<bold>(?:\*\*|__)(?<bContent>.+?)(?:\*\*|__))|" +
                             @"(?<strike>~~(?<sContent>.+?)~~)|" +
                             @"(?<italic>(?:\*(?<iContent>[^*\n]+?)\*|(?<!\w)_(?<iContent>[^_\n]+?)_(?!\w)))";

            int lastIndex = 0;
            try
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));

                foreach (Match match in matches)
                {
                    if (match.Index > lastIndex)
                    {
                        inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
                    }

                    if (match.Groups["code"].Success)
                    {
                        string codeContent = match.Groups["codeText"].Value;
                        var codeRun = new Run(codeContent)
                        {
                            FontFamily = new FontFamily("Consolas"),
                            Background = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128))
                        };
                        inlines.Add(codeRun);
                    }
                    else if (match.Groups["link"].Success)
                    {
                        string linkText = match.Groups["linkText"].Value;
                        string url = match.Groups["url"].Value;
                        if (SafeLinkService.TryNormalizeAndValidateUrl(url, out Uri? validUri))
                        {
                            try
                            {
                                var hyperlink = new Hyperlink
                                {
                                    NavigateUri = validUri
                                };

                                SafeLinkService.ConfigureHyperlink(hyperlink, validUri.OriginalString);

                                try
                                {
                                    hyperlink.Foreground = (Brush)Application.Current.FindResource("MaterialDesign.Brush.Primary");
                                }
                                catch { }

                                AddFormattedInlinesInternal(hyperlink.Inlines, linkText, foreground);
                                inlines.Add(hyperlink);
                            }
                            catch
                            {
                                AddFormattedInlinesInternal(inlines, linkText, foreground);
                            }
                        }
                        else
                        {
                            // Malformed, invalid, or blocked URL: render safely as plain text
                            AddFormattedInlinesInternal(inlines, linkText, foreground);
                        }
                    }
                    else if (match.Groups["bolditalic"].Success)
                    {
                        string content = match.Groups["biContent"].Value;
                        var span = new Span { FontWeight = FontWeights.Bold, FontStyle = FontStyles.Italic };
                        AddFormattedInlinesInternal(span.Inlines, content, foreground);
                        inlines.Add(span);
                    }
                    else if (match.Groups["bold"].Success)
                    {
                        string content = match.Groups["bContent"].Value;
                        var bold = new Bold();
                        AddFormattedInlinesInternal(bold.Inlines, content, foreground);
                        inlines.Add(bold);
                    }
                    else if (match.Groups["strike"].Success)
                    {
                        string content = match.Groups["sContent"].Value;
                        var strike = new Span { TextDecorations = TextDecorations.Strikethrough };
                        AddFormattedInlinesInternal(strike.Inlines, content, foreground);
                        inlines.Add(strike);
                    }
                    else if (match.Groups["italic"].Success)
                    {
                        string content = match.Groups["iContent"].Value;
                        var italic = new Italic();
                        AddFormattedInlinesInternal(italic.Inlines, content, foreground);
                        inlines.Add(italic);
                    }

                    lastIndex = match.Index + match.Length;
                }

                if (lastIndex < text.Length)
                {
                    inlines.Add(new Run(text.Substring(lastIndex)));
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // Fall back to plain text for that block on timeout, not crash
                inlines.Clear();
                inlines.Add(new Run(text));
            }
            catch
            {
                inlines.Clear();
                inlines.Add(new Run(text));
            }
        }

        public static Block CreateTable(List<string> tableLines, double baseFontSize, Brush foreground)
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

            var container = new BlockUIContainer(border) { Margin = new Thickness(0, 8, 0, 12), Tag = "code" };
            return container;
        }

        private static Block CreateBlockquote(string text, double baseFontSize, Brush foreground)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(4, 0, 0, 0),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 8, 0, 12),
                CornerRadius = new CornerRadius(0, 4, 4, 0)
            };

            var tb = new TextBlock
            {
                Text = text,
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap,
                FontSize = baseFontSize
            };
            tb.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBody");
            border.Child = tb;

            try
            {
                border.SetResourceReference(Border.BorderBrushProperty, "MaterialDesign.Brush.Primary");
                border.SetResourceReference(Border.BackgroundProperty, "MaterialDesignCardBackground");
            }
            catch { }

            var container = new BlockUIContainer(border) { Tag = "quote" };
            return container;
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

        private static Block CreateHeader(string text, int level, double baseFontSize, Brush foreground, bool isFirst = false)
        {
            double size = level switch
            {
                1 => baseFontSize * 1.85,
                2 => baseFontSize * 1.5,
                3 => baseFontSize * 1.25,
                4 => baseFontSize * 1.1,
                5 => baseFontSize * 1.05,
                6 => baseFontSize * 1.0,
                _ => baseFontSize
            };

            var p = new Paragraph
            {
                FontSize = size,
                FontWeight = level <= 2 ? FontWeights.Bold : FontWeights.SemiBold,
                Margin = new Thickness(0, isFirst ? 0 : baseFontSize * (level == 1 ? 0.6 : 0.4), 0, baseFontSize * 0.4),
                Tag = "h" + level
            };
            AddFormattedInlines(p, text, foreground);

            if (level == 1)
            {
                try
                {
                    p.SetResourceReference(Block.BorderBrushProperty, "MaterialDesignDivider");
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

            return new BlockUIContainer(rect) { Tag = "hr" };
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
