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
        private static readonly Regex HexColorRegex = new Regex(@"^#([0-9a-fA-F]{6})$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        private static readonly Regex TableSplitRegex = new Regex(@"(?<!\\)\|", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        private static readonly Regex TableSepCellRegex = new Regex(@"^\s*:?-+:?\s*$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        public static List<string> SplitTableRow(string line)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("|")) trimmed = trimmed.Substring(1);
            if (trimmed.EndsWith("|")) trimmed = trimmed.Substring(0, trimmed.Length - 1);

            string[] parts;
            try
            {
                parts = TableSplitRegex.Split(trimmed);
            }
            catch (RegexMatchTimeoutException)
            {
                parts = trimmed.Split('|');
            }

            var result = new List<string>(parts.Length);
            foreach (var part in parts)
            {
                string unescaped = part.Replace(@"\|", "|");
                result.Add(unescaped.Trim());
            }
            return result;
        }

        public static bool IsTableSeparator(string line, out List<TextAlignment> alignments)
        {
            alignments = new List<TextAlignment>();
            string trimmed = line.Trim();
            if (!trimmed.StartsWith("|") || !trimmed.EndsWith("|")) return false;

            var cells = SplitTableRow(trimmed);
            if (cells.Count == 0) return false;

            foreach (var cell in cells)
            {
                string c = cell.Trim();
                if (string.IsNullOrEmpty(c)) return false;

                bool match = false;
                try
                {
                    match = TableSepCellRegex.IsMatch(c);
                }
                catch (RegexMatchTimeoutException)
                {
                    match = c.Replace("-", "").Replace(":", "").Length == 0 && c.Contains("-");
                }

                if (!match) return false;

                bool startColon = c.StartsWith(":");
                bool endColon = c.EndsWith(":");
                if (startColon && endColon)
                {
                    alignments.Add(TextAlignment.Center);
                }
                else if (endColon)
                {
                    alignments.Add(TextAlignment.Right);
                }
                else
                {
                    alignments.Add(TextAlignment.Left);
                }
            }

            return true;
        }

        public static bool IsValidHexColor(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return false;
            try
            {
                return HexColorRegex.IsMatch(hex.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static SolidColorBrush CreateHighlightBrush(string hex)
        {
            if (!IsValidHexColor(hex))
            {
                hex = "#FFF176";
            }

            hex = hex.Trim();
            byte r = Convert.ToByte(hex.Substring(1, 2), 16);
            byte g = Convert.ToByte(hex.Substring(3, 2), 16);
            byte b = Convert.ToByte(hex.Substring(5, 2), 16);

            // Alpha 105 (~41%) ensures great readability in Light, Dark, and AMOLED themes
            var brush = new SolidColorBrush(Color.FromArgb(105, r, g, b));
            brush.Freeze();
            return brush;
        }

        public static string GetHexFromHighlight(Inline inline)
        {
            if (inline.Tag is string tag && tag.StartsWith("mark:#", StringComparison.OrdinalIgnoreCase))
            {
                string color = tag.Substring(5).Trim();
                if (IsValidHexColor(color))
                {
                    return color.ToUpperInvariant();
                }
            }

            if (inline.Background is SolidColorBrush scb && scb.Color.A > 0 && scb != Brushes.Transparent)
            {
                return $"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
            }

            return "#FFF176";
        }
        public static FlowDocument ParseToFlowDocument(string markdown, FontFamily fontFamily, double baseFontSize, Brush foreground)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(32, 24, 32, 24),
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
                    if (tableLines.Count >= 2 && IsTableSeparator(tableLines[1], out _))
                    {
                        try
                        {
                            doc.Blocks.Add(CreateTable(tableLines, baseFontSize, foreground));
                            continue;
                        }
                        catch
                        {
                            // Safe fallback to plain paragraphs if table creation fails
                        }
                    }

                    foreach (var tLine in tableLines)
                    {
                        doc.Blocks.Add(CreateParagraph(tLine, baseFontSize, foreground));
                    }
                    continue;
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

            if (doc.Blocks.FirstBlock != null)
            {
                var m = doc.Blocks.FirstBlock.Margin;
                if (m.Top > 0)
                {
                    doc.Blocks.FirstBlock.Margin = new Thickness(m.Left, 0, m.Right, m.Bottom);
                }
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

                    if (pText.StartsWith("☐ "))
                    {
                        sb.AppendLine("- [ ] " + pText.Substring(2));
                        sb.AppendLine();
                        continue;
                    }
                    if (pText.StartsWith("☑ "))
                    {
                        sb.AppendLine("- [x] " + pText.Substring(2));
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
                        if (itemText.StartsWith("☐ "))
                        {
                            sb.AppendLine("- [ ] " + itemText.Substring(2));
                        }
                        else if (itemText.StartsWith("☑ "))
                        {
                            sb.AppendLine("- [x] " + itemText.Substring(2));
                        }
                        else if (itemText.StartsWith("- [ ]") || itemText.StartsWith("- [x]"))
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
                    var colAlignments = new List<TextAlignment>();
                    for (int c = 0; c < table.Columns.Count; c++)
                    {
                        var col = table.Columns[c];
                        if (col.Tag is string aStr && Enum.TryParse<TextAlignment>(aStr, out var ta))
                        {
                            colAlignments.Add(ta);
                        }
                        else
                        {
                            TextAlignment fallbackAlign = TextAlignment.Left;
                            if (table.RowGroups.Count > 0 && table.RowGroups[0].Rows.Count > 0 && c < table.RowGroups[0].Rows[0].Cells.Count)
                            {
                                fallbackAlign = table.RowGroups[0].Rows[0].Cells[c].TextAlignment;
                            }
                            colAlignments.Add(fallbackAlign);
                        }
                    }

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
                                // Escape pipe character in markdown table content so structure is preserved
                                cellText = cellText.Replace(@"\|", "|").Replace("|", @"\|").Trim();
                                cellTexts.Add(cellText);
                            }

                            while (cellTexts.Count < colAlignments.Count)
                            {
                                cellTexts.Add("");
                            }

                            sb.AppendLine("| " + string.Join(" | ", cellTexts) + " |");

                            if (r == 0)
                            {
                                var seps = new List<string>();
                                int numCols = Math.Max(cellTexts.Count, colAlignments.Count);
                                for (int c = 0; c < numCols; c++)
                                {
                                    var align = c < colAlignments.Count ? colAlignments[c] : TextAlignment.Left;
                                    switch (align)
                                    {
                                        case TextAlignment.Center:
                                            seps.Add(":---:");
                                            break;
                                        case TextAlignment.Right:
                                            seps.Add("---:");
                                            break;
                                        case TextAlignment.Left:
                                        default:
                                            seps.Add(":---");
                                            break;
                                    }
                                }
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
                bool hasUnderline = sp.TextDecorations != null && sp.TextDecorations.Any(d => d.Location == TextDecorationLocation.Underline);
                bool isSup = sp.BaselineAlignment == BaselineAlignment.Superscript || (sp.Tag as string == "sup");
                bool isSub = sp.BaselineAlignment == BaselineAlignment.Subscript || (sp.Tag as string == "sub");
                bool isMark = (sp.Tag is string t && t.StartsWith("mark", StringComparison.OrdinalIgnoreCase)) || (sp.Background is SolidColorBrush mBrush && mBrush.Color.A > 0 && mBrush != Brushes.Transparent);

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

                if (hasUnderline)
                {
                    inner = $"<u>{inner}</u>";
                }

                if (isSup)
                {
                    inner = $"<sup>{inner}</sup>";
                }

                if (isSub)
                {
                    inner = $"<sub>{inner}</sub>";
                }

                if (isMark)
                {
                    string hex = GetHexFromHighlight(sp);
                    inner = $"<mark style=\"background:{hex}\">{inner}</mark>";
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
                bool hasUnderline = r.TextDecorations != null && r.TextDecorations.Any(d => d.Location == TextDecorationLocation.Underline);
                bool isSup = r.BaselineAlignment == BaselineAlignment.Superscript || (r.Tag as string == "sup");
                bool isSub = r.BaselineAlignment == BaselineAlignment.Subscript || (r.Tag as string == "sub");
                bool isMark = (r.Tag is string rt && rt.StartsWith("mark", StringComparison.OrdinalIgnoreCase)) || (r.Background is SolidColorBrush rBrush && rBrush.Color.A > 0 && rBrush != Brushes.Transparent);

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

                if (hasUnderline)
                {
                    result = $"<u>{result}</u>";
                }

                if (isSup)
                {
                    result = $"<sup>{result}</sup>";
                }

                if (isSub)
                {
                    result = $"<sub>{result}</sub>";
                }

                if (isMark)
                {
                    string hex = GetHexFromHighlight(r);
                    result = $"<mark style=\"background:{hex}\">{result}</mark>";
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
                             @"(?<italic>(?:\*(?<iContent>[^*\n]+?)\*|(?<!\w)_(?<iContent>[^_\n]+?)_(?!\w)))|" +
                             @"(?<u><\s*[uU]\s*>(?<uContent>.+?)<\s*/\s*[uU]\s*>)|" +
                             @"(?<sup><\s*(?i:sup)\s*>(?<supContent>.+?)<\s*/\s*(?i:sup)\s*>)|" +
                             @"(?<sub><\s*(?i:sub)\s*>(?<subContent>.+?)<\s*/\s*(?i:sub)\s*>)|" +
                             @"(?<mark><\s*(?i:mark)(?:\s+(?i:style)\s*=\s*[""']\s*background\s*:\s*(?<markColor>[^""';\s]+)\s*;?\s*[""'])?\s*>(?<markContent>.+?)<\s*/\s*(?i:mark)\s*>|==(?<markContent>.+?)==)";

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
                    else if (match.Groups["u"].Success)
                    {
                        string content = match.Groups["uContent"].Value;
                        var uSpan = new Span { TextDecorations = TextDecorations.Underline, Tag = "u" };
                        AddFormattedInlinesInternal(uSpan.Inlines, content, foreground);
                        inlines.Add(uSpan);
                    }
                    else if (match.Groups["sup"].Success)
                    {
                        string content = match.Groups["supContent"].Value;
                        var supSpan = new Span
                        {
                            BaselineAlignment = BaselineAlignment.Superscript,
                            FontSize = 11,
                            Tag = "sup"
                        };
                        AddFormattedInlinesInternal(supSpan.Inlines, content, foreground);
                        inlines.Add(supSpan);
                    }
                    else if (match.Groups["sub"].Success)
                    {
                        string content = match.Groups["subContent"].Value;
                        var subSpan = new Span
                        {
                            BaselineAlignment = BaselineAlignment.Subscript,
                            FontSize = 11,
                            Tag = "sub"
                        };
                        AddFormattedInlinesInternal(subSpan.Inlines, content, foreground);
                        inlines.Add(subSpan);
                    }
                    else if (match.Groups["mark"].Success)
                    {
                        string content = match.Groups["markContent"].Value;
                        bool hasColorAttr = match.Groups["markColor"].Success;
                        string colorHex;
                        bool isValid = true;

                        if (hasColorAttr)
                        {
                            string rawColor = match.Groups["markColor"].Value;
                            if (IsValidHexColor(rawColor))
                            {
                                colorHex = rawColor.Trim().ToUpperInvariant();
                            }
                            else
                            {
                                // Validate the colour with a hex regex; reject anything else.
                                isValid = false;
                                colorHex = "";
                            }
                        }
                        else
                        {
                            colorHex = "#FFF176";
                        }

                        if (isValid)
                        {
                            var markSpan = new Span
                            {
                                Background = CreateHighlightBrush(colorHex),
                                Tag = $"mark:{colorHex}"
                            };
                            AddFormattedInlinesInternal(markSpan.Inlines, content, foreground);
                            inlines.Add(markSpan);
                        }
                        else
                        {
                            // Rejected color: render content safely without highlight background
                            AddFormattedInlinesInternal(inlines, content, foreground);
                        }
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

        public static Brush GetTableBorderBrush()
        {
            try
            {
                if (Application.Current?.TryFindResource("MaterialDesignDivider") is Brush b)
                    return b;
            }
            catch { }
            return new SolidColorBrush(Color.FromArgb(50, 128, 128, 128));
        }

        public static Brush GetTableHeaderBackground()
        {
            var b = new SolidColorBrush(Color.FromArgb(24, 128, 128, 128));
            b.Freeze();
            return b;
        }

        public static Brush GetTableZebraBackground()
        {
            var b = new SolidColorBrush(Color.FromArgb(12, 128, 128, 128));
            b.Freeze();
            return b;
        }

        public static void ApplyTablePreset(Table table, string presetName, bool? hasHeader = null)
        {
            if (table == null || table.RowGroups.Count == 0) return;

            string currentPreset = "grid";
            bool currentHeader = true;

            if (table.Tag is string tagStr)
            {
                if (tagStr.Contains("preset:zebra")) currentPreset = "zebra";
                else if (tagStr.Contains("preset:minimal")) currentPreset = "minimal";
                else if (tagStr.Contains("preset:grid")) currentPreset = "grid";

                if (tagStr.Contains("header:false")) currentHeader = false;
                else if (tagStr.Contains("header:true")) currentHeader = true;
            }

            if (!string.IsNullOrEmpty(presetName))
            {
                currentPreset = presetName.ToLowerInvariant();
            }
            if (hasHeader.HasValue)
            {
                currentHeader = hasHeader.Value;
            }

            table.Tag = $"preset:{currentPreset};header:{currentHeader.ToString().ToLowerInvariant()}";

            var borderBrush = GetTableBorderBrush();
            var headerBg = GetTableHeaderBackground();
            var zebraBg = GetTableZebraBackground();
            var padding = new Thickness(14, 8, 14, 8);

            if (currentPreset == "minimal")
            {
                table.BorderBrush = borderBrush;
                table.BorderThickness = new Thickness(0, 1, 0, 1);
            }
            else
            {
                table.BorderBrush = borderBrush;
                table.BorderThickness = new Thickness(1, 1, 0, 0);
            }

            var rowGroup = table.RowGroups[0];
            int totalRows = rowGroup.Rows.Count;

            for (int r = 0; r < totalRows; r++)
            {
                var row = rowGroup.Rows[r];
                bool isHeader = (r == 0 && currentHeader);

                if (isHeader)
                {
                    row.Background = headerBg;
                }
                else if (currentPreset == "zebra" && (r % 2 == 1))
                {
                    row.Background = zebraBg;
                }
                else
                {
                    row.Background = Brushes.Transparent;
                }

                for (int c = 0; c < row.Cells.Count; c++)
                {
                    var cell = row.Cells[c];
                    cell.Padding = padding;
                    cell.BorderBrush = borderBrush;

                    if (isHeader)
                    {
                        foreach (var b in cell.Blocks)
                        {
                            if (b is Paragraph p) p.FontWeight = FontWeights.Bold;
                        }

                        if (currentPreset == "minimal")
                        {
                            cell.BorderThickness = new Thickness(0, 0, 0, 2);
                        }
                        else
                        {
                            cell.BorderThickness = new Thickness(0, 0, 1, 1);
                        }
                    }
                    else
                    {
                        if (r == 0)
                        {
                            foreach (var b in cell.Blocks)
                            {
                                if (b is Paragraph p) p.FontWeight = FontWeights.Normal;
                            }
                        }

                        if (currentPreset == "minimal")
                        {
                            cell.BorderThickness = (r == totalRows - 1)
                                ? new Thickness(0, 0, 0, 0)
                                : new Thickness(0, 0, 0, 1);
                        }
                        else
                        {
                            cell.BorderThickness = new Thickness(0, 0, 1, 1);
                        }
                    }
                }
            }
        }

        public static Block CreateTable(List<string> tableLines, double baseFontSize, Brush foreground)
        {
            var table = new Table
            {
                CellSpacing = 0,
                Margin = new Thickness(0, 10, 0, 14)
            };

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            var headerCells = SplitTableRow(tableLines[0]);
            List<TextAlignment> alignments = new List<TextAlignment>();
            if (tableLines.Count >= 2)
            {
                IsTableSeparator(tableLines[1], out alignments);
            }

            int colCount = Math.Max(headerCells.Count, alignments.Count);
            if (colCount == 0) colCount = 1;

            for (int c = 0; c < colCount; c++)
            {
                var col = new TableColumn();
                var align = c < alignments.Count ? alignments[c] : TextAlignment.Left;
                col.Tag = align.ToString();
                table.Columns.Add(col);
            }

            var headerRow = new TableRow();
            for (int c = 0; c < colCount; c++)
            {
                string content = c < headerCells.Count ? headerCells[c] : "";
                var align = c < alignments.Count ? alignments[c] : TextAlignment.Left;
                var p = new Paragraph { FontWeight = FontWeights.Bold, Margin = new Thickness(0), TextAlignment = align };
                AddFormattedInlines(p, content, foreground);
                var cell = new TableCell(p)
                {
                    Padding = new Thickness(14, 8, 14, 8),
                    TextAlignment = align
                };
                headerRow.Cells.Add(cell);
            }
            rowGroup.Rows.Add(headerRow);

            for (int r = 2; r < tableLines.Count; r++)
            {
                var rowCells = SplitTableRow(tableLines[r]);
                var row = new TableRow();
                for (int c = 0; c < colCount; c++)
                {
                    string content = c < rowCells.Count ? rowCells[c] : "";
                    var align = c < alignments.Count ? alignments[c] : TextAlignment.Left;
                    var p = new Paragraph { Margin = new Thickness(0), TextAlignment = align };
                    AddFormattedInlines(p, content, foreground);
                    var cell = new TableCell(p)
                    {
                        Padding = new Thickness(14, 8, 14, 8),
                        TextAlignment = align
                    };
                    row.Cells.Add(cell);
                }
                rowGroup.Rows.Add(row);
            }

            ApplyTablePreset(table, "grid", hasHeader: true);
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
            bool isChecklist = items.Any(it => it.marker == "- [ ]" || it.marker == "- [x]");
            var list = new List
            {
                MarkerStyle = isChecklist ? TextMarkerStyle.None : (isNumbered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc),
                Margin = new Thickness(0, 4, 0, 10),
                Padding = isChecklist ? new Thickness(8, 0, 0, 0) : new Thickness(24, 0, 0, 0)
            };

            foreach (var item in items)
            {
                if (item.marker == "- [ ]" || item.marker == "- [x]")
                {
                    bool isChecked = item.marker == "- [x]";
                    string glyph = isChecked ? "☑ " : "☐ ";
                    var p = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                    AddFormattedInlines(p, glyph + item.text, foreground);
                    list.ListItems.Add(new ListItem(p));
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
            if (line.StartsWith("- [ ] ") || line.StartsWith("- [x] ", StringComparison.OrdinalIgnoreCase)) return true;
            int dot = line.IndexOf(". ");
            if (dot > 0 && int.TryParse(line.Substring(0, dot), out _)) return true;
            return false;
        }

        private static (string marker, string text) ParseListLine(string line)
        {
            if (line.StartsWith("- [ ] ")) return ("- [ ]", line.Substring(6));
            if (line.StartsWith("- [x] ", StringComparison.OrdinalIgnoreCase)) return ("- [x]", line.Substring(6));
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
