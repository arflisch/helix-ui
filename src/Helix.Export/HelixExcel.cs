using System.Globalization;
using System.IO.Compression;
using System.Text;
using static Helix.Export.HelixXml;

namespace Helix.Export;

/// <summary>
/// An Excel workbook (.xlsx) built in a few lines; one sheet per table:
/// <code>
/// new HelixExcel()
///     .Sheet("Employees", employees, c => c.Add("Name", e => e.Name).Add("Salary", e => e.Salary, "N0", HelixTotal.Sum))
///     .Save("employees.xlsx");
/// </code>
/// Numbers, dates and booleans are real Excel values (sortable, summable); the header is frozen and filterable.
/// </summary>
public sealed class HelixExcel
{
    private readonly List<(string Name, HelixExportTable Table)> _sheets = [];

    /// <summary>What the file looks like: header, font, stripes, borders, colors (see <see cref="HelixExcelStyle"/> and its presets).</summary>
    public HelixExcelStyle Style { get; set; } = HelixExcelStyle.Default;

    /// <summary>Shortcut for <c>Style.Accent</c>: header tint, header line and title color.</summary>
    public string Accent { get => Style.Accent; set => Style = Style with { Accent = value }; }

    /// <summary>Freeze the header row (true).</summary>
    public bool FreezeHeader { get; set; } = true;

    /// <summary>Filter buttons on the header (true).</summary>
    public bool AutoFilter { get; set; } = true;

    /// <summary>Culture used to estimate the column widths (current culture).</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    public HelixExcel Sheet<T>(string name, IEnumerable<T> rows, Action<HelixColumns<T>>? columns = null, string? title = null) =>
        Sheet(name, HelixExportTable.From(rows, columns, title));

    public HelixExcel Sheet(string name, HelixExportTable table)
    {
        _sheets.Add((name, table));
        return this;
    }

    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        WriteTo(stream);
        return stream.ToArray();
    }

    public void Save(string path)
    {
        using var file = File.Create(path);
        WriteTo(file);
    }

    public void WriteTo(Stream stream)
    {
        if (_sheets.Count == 0) Sheet("Sheet1", new HelixExportTable([], []));
        var styles = new StyleSheet(Style);
        var names = UniqueNames(_sheets.Select(s => s.Name));
        var sheetXml = _sheets.Select(s => SheetXml(s.Table, styles)).ToList();

        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
        Write(zip, "[Content_Types].xml", Declaration +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
            string.Concat(sheetXml.Select((_, i) => $"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>")) +
            "</Types>");
        Write(zip, "_rels/.rels", Declaration +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>");
        Write(zip, "xl/workbook.xml", Declaration +
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<bookViews><workbookView/></bookViews><sheets>" +
            string.Concat(names.Select((n, i) => $"<sheet name=\"{Esc(n)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>")) +
            "</sheets><calcPr calcId=\"191029\" fullCalcOnLoad=\"1\"/></workbook>");
        Write(zip, "xl/_rels/workbook.xml.rels", Declaration +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            string.Concat(sheetXml.Select((_, i) => $"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>")) +
            $"<Relationship Id=\"rId{sheetXml.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
            "</Relationships>");
        for (var i = 0; i < sheetXml.Count; i++) Write(zip, $"xl/worksheets/sheet{i + 1}.xml", sheetXml[i]);
        Write(zip, "xl/styles.xml", styles.Xml());
    }

    private string SheetXml(HelixExportTable table, StyleSheet styles)
    {
        var style = Style;
        var columns = table.Columns;
        var values = table.Read();
        var rows = table.Rows as IList<object> ?? table.Rows.ToList();
        var headerRow = table.Title is null ? 1 : 3;
        var firstData = headerRow + 1;
        var lastData = headerRow + values.Count;
        var sb = new StringBuilder();
        sb.Append(Declaration).Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");

        var grid = style.ShowGridLines ? "" : " showGridLines=\"0\"";
        if (FreezeHeader && columns.Count > 0)
        {
            sb.Append($"<sheetViews><sheetView{grid} workbookViewId=\"0\"><pane ySplit=\"{headerRow}\" topLeftCell=\"A{firstData}\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
        }
        else if (grid.Length > 0)
        {
            sb.Append($"<sheetViews><sheetView{grid} workbookViewId=\"0\"/></sheetViews>");
        }

        var rowHeight = style.RowHeight is { } h ? h.ToString("0.##", CultureInfo.InvariantCulture) : null;
        sb.Append($"<sheetFormatPr defaultRowHeight=\"{rowHeight ?? (style.FontSize * 1.36).ToString("0.##", CultureInfo.InvariantCulture)}\"{(rowHeight is null ? "" : " customHeight=\"1\"")}/>");

        if (columns.Count > 0)
        {
            sb.Append("<cols>");
            for (var c = 0; c < columns.Count; c++)
            {
                var width = columns[c].Width ?? EstimateWidth(columns[c], values.Take(300).Select(r => r[c])) * Math.Max(1, style.FontSize / 11);
                sb.Append($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"{width.ToString("0.##", CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
            }

            sb.Append("</cols>");
        }

        string RowOpen(int r) => rowHeight is null ? $"<row r=\"{r}\">" : $"<row r=\"{r}\" ht=\"{rowHeight}\" customHeight=\"1\">";

        sb.Append("<sheetData>");
        if (table.Title is not null)
        {
            var titleStyle = styles.Get(new CellFormat { Bold = true, Size = style.TitleSize, Color = style.Argb(style.TitleColor ?? "primary") });
            sb.Append($"<row r=\"1\" ht=\"{(style.TitleSize * 1.5).ToString("0.##", CultureInfo.InvariantCulture)}\" customHeight=\"1\"><c r=\"A1\" t=\"inlineStr\" s=\"{titleStyle}\"><is><t xml:space=\"preserve\">{Esc(table.Title)}</t></is></c></row>");
        }

        var aligns = Enumerable.Range(0, columns.Count).Select(c => AlignOf(columns[c], values.FirstOrDefault(r => r[c] is not null)?[c])).ToArray();
        if (columns.Count > 0)
        {
            var headerLine = style.Argb(style.HeaderLine ?? "primary");
            var header = new CellFormat
            {
                Bold = style.HeaderBold,
                Color = style.Argb(style.HeaderColor),
                Fill = style.HeaderBackground is null ? "FF" + Tint(Hex(style.Accent), 0.88) : style.Argb(style.HeaderBackground),
                Bottom = headerLine is null ? BodyBorder(style, "bottom") : "medium|" + headerLine,
                Top = BodyBorder(style, "top"),
                Sides = BodyBorder(style, "side"),
                Wrap = true,
            };
            sb.Append(rowHeight is null ? $"<row r=\"{headerRow}\">" : RowOpen(headerRow));
            for (var c = 0; c < columns.Count; c++)
            {
                sb.Append($"<c r=\"{Ref(c, headerRow)}\" t=\"inlineStr\" s=\"{styles.Get(header with { Align = aligns[c] == "right" ? "right" : aligns[c] })}\"><is><t xml:space=\"preserve\">{Esc(columns[c].Header)}</t></is></c>");
            }

            sb.Append("</row>");
        }

        for (var r = 0; r < values.Count; r++)
        {
            var row = headerRow + 1 + r;
            sb.Append(RowOpen(row));
            for (var c = 0; c < columns.Count; c++)
            {
                var format = new CellFormat
                {
                    Fill = style.Striped && r % 2 == 1 ? style.Argb(style.StripeColor) : null,
                    Bottom = BodyBorder(style, "bottom"),
                    Top = BodyBorder(style, "top"),
                    Sides = BodyBorder(style, "side"),
                    Align = aligns[c] == "right" ? null : aligns[c],
                };
                var value = values[r][c];
                if (style.NegativeColor is not null && HelixExportTable.IsNumber(value) && Convert.ToDouble(value, CultureInfo.InvariantCulture) < 0)
                {
                    format = format with { Color = style.Argb(style.NegativeColor) };
                }

                if (style.CellStyle?.Invoke(new HelixExcelCell(columns[c], rows[r], value, r)) is { } custom)
                {
                    format = format with
                    {
                        Color = custom.Color is null ? format.Color : style.Argb(custom.Color),
                        Fill = custom.Background is null ? format.Fill : style.Argb(custom.Background),
                        Bold = custom.Bold, Italic = custom.Italic,
                    };
                }

                Cell(sb, Ref(c, row), value, columns[c], styles, format);
            }

            sb.Append("</row>");
        }

        if (table.HasTotals && columns.Count > 0)
        {
            var row = lastData + 1;
            var labelColumn = Enumerable.Range(0, columns.Count).FirstOrDefault(c => columns[c].Total == HelixTotal.None, -1);
            var totals = new CellFormat
            {
                Bold = true, Fill = style.Argb(style.TotalsBackground), Top = "thin|FF9CA3AF",
                Bottom = BodyBorder(style, "bottom"), Sides = BodyBorder(style, "side"),
            };
            sb.Append(RowOpen(row));
            for (var c = 0; c < columns.Count; c++)
            {
                var column = columns[c];
                if (column.Total != HelixTotal.None)
                {
                    var cached = HelixExportTable.TotalOf(column.Total, values.Select(v => v[c]));
                    var function = column.Total switch { HelixTotal.Sum => "SUM", HelixTotal.Average => "AVERAGE", HelixTotal.Count => "COUNTA", HelixTotal.Min => "MIN", _ => "MAX" };
                    var formula = values.Count == 0 ? "0" : $"{function}({Ref(c, firstData)}:{Ref(c, lastData)})";
                    var format = totals with { NumberFormat = column.Total == HelixTotal.Count ? "0" : NumberFormat(column, cached) };
                    if (style.NegativeColor is not null && cached is double d && d < 0) format = format with { Color = style.Argb(style.NegativeColor) };
                    sb.Append($"<c r=\"{Ref(c, row)}\" s=\"{styles.Get(format)}\"><f>{formula}</f>{(cached is null ? "" : $"<v>{Convert.ToDouble(cached, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture)}</v>")}</c>");
                }
                else if (c == labelColumn)
                {
                    sb.Append($"<c r=\"{Ref(c, row)}\" t=\"inlineStr\" s=\"{styles.Get(totals)}\"><is><t>{Esc(table.TotalLabel)}</t></is></c>");
                }
                else
                {
                    sb.Append($"<c r=\"{Ref(c, row)}\" s=\"{styles.Get(totals)}\"/>");
                }
            }

            sb.Append("</row>");
        }

        sb.Append("</sheetData>");
        if (AutoFilter && columns.Count > 0)
        {
            sb.Append($"<autoFilter ref=\"{Ref(0, headerRow)}:{Ref(columns.Count - 1, Math.Max(headerRow, lastData))}\"/>");
        }

        sb.Append("<pageMargins left=\"0.5\" right=\"0.5\" top=\"0.6\" bottom=\"0.6\" header=\"0.3\" footer=\"0.3\"/>");
        sb.Append("</worksheet>");
        return sb.ToString();
    }

    private static string? AlignOf(HelixExportColumn column, object? sample) => column.Align switch
    {
        HelixExportAlign.Left => "left",
        HelixExportAlign.Center => "center",
        HelixExportAlign.Right => "right",
        _ => sample is bool ? "center" : HelixExportTable.IsRightAligned(column, sample) ? "right" : null,
    };

    /// <summary>Thin border of a body cell for the chosen <see cref="HelixExcelBorders"/>, as "style|ARGB".</summary>
    private static string? BodyBorder(HelixExcelStyle style, string side) => style.Borders switch
    {
        HelixExcelBorders.All => "thin|" + style.Argb(style.BorderColor),
        HelixExcelBorders.Rows when side == "bottom" => "thin|" + style.Argb(style.BorderColor),
        _ => null,
    };

    private static void Cell(StringBuilder sb, string reference, object? value, HelixExportColumn column, StyleSheet styles, CellFormat format)
    {
        var empty = format.Fill is not null || format.Bottom is not null || format.Top is not null || format.Sides is not null;
        switch (value)
        {
            case null:
                if (empty) sb.Append($"<c r=\"{reference}\" s=\"{styles.Get(format)}\"/>");
                return;
            case bool b:
                sb.Append($"<c r=\"{reference}\" t=\"b\" s=\"{styles.Get(format with { Align = format.Align ?? "center" })}\"><v>{(b ? 1 : 0)}</v></c>");
                return;
            case DateTime d:
                var dateFormat = column.ExcelFormat ?? (column.Format is { } f ? DateFormat(f) : d.TimeOfDay == TimeSpan.Zero ? "yyyy-mm-dd" : "yyyy-mm-dd hh:mm");
                sb.Append($"<c r=\"{reference}\" s=\"{styles.Get(format with { NumberFormat = dateFormat })}\"><v>{d.ToOADate().ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                return;
            case TimeSpan t:
                sb.Append($"<c r=\"{reference}\" s=\"{styles.Get(format with { NumberFormat = column.ExcelFormat ?? "[h]:mm:ss" })}\"><v>{t.TotalDays.ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                return;
            case TimeOnly time:
                sb.Append($"<c r=\"{reference}\" s=\"{styles.Get(format with { NumberFormat = column.ExcelFormat ?? "hh:mm" })}\"><v>{time.ToTimeSpan().TotalDays.ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                return;
            case var n when HelixExportTable.IsNumber(n):
                var number = Convert.ToDouble(n, CultureInfo.InvariantCulture);
                if (double.IsNaN(number) || double.IsInfinity(number)) return;
                sb.Append($"<c r=\"{reference}\" s=\"{styles.Get(format with { NumberFormat = NumberFormat(column, n) })}\"><v>{number.ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                return;
            default:
                var text = HelixExportTable.Text(value, column.Format, CultureInfo.CurrentCulture);
                if (text.Length > 32767) text = text[..32767];
                sb.Append($"<c r=\"{reference}\" t=\"inlineStr\" s=\"{styles.Get(format with { Wrap = text.Contains('\n') })}\"><is><t xml:space=\"preserve\">{Esc(text)}</t></is></c>");
                return;
        }
    }

    private static string? NumberFormat(HelixExportColumn column, object? sample) =>
        column.ExcelFormat ?? (column.Format is { } f ? NumberFormat(f) : sample is float or double or decimal ? "#,##0.##########" : null);

    /// <summary>.NET numeric format → Excel number format: N2 → #,##0.00, F1 → 0.0, P0 → 0%, C2 → #,##0.00, D3 → 000.</summary>
    internal static string? NumberFormat(string format)
    {
        if (format.Length == 0) return null;
        var kind = char.ToUpperInvariant(format[0]);
        var digits = format.Length > 1 && int.TryParse(format[1..], out var d) ? d : (int?)null;
        string Decimals(int fallback) => (digits ?? fallback) > 0 ? "." + new string('0', digits ?? fallback) : "";
        return kind switch
        {
            'N' when format.Length <= 3 => "#,##0" + Decimals(2),
            'F' when format.Length <= 3 => "0" + Decimals(2),
            'P' when format.Length <= 3 => "0" + Decimals(2) + "%",
            'C' when format.Length <= 3 => "#,##0" + Decimals(2),
            'D' when format.Length <= 3 => digits is { } n ? new string('0', n) : "0",
            'E' when format.Length <= 3 => "0" + Decimals(6) + "E+00",
            _ => format.Length > 3 || !char.IsLetter(format[0]) ? Literals(format) : null,
        };
    }

    /// <summary>.NET custom formats quote literals with ' or "; Excel only with ".</summary>
    internal static string Literals(string format)
    {
        var sb = new StringBuilder(format.Length + 4);
        var quote = '\0';
        foreach (var c in format)
        {
            if (quote == '\0' && c is '\'' or '"') { quote = c; sb.Append('"'); }
            else if (c == quote) { quote = '\0'; sb.Append('"'); }
            else if (quote != '\0' && c == '"') sb.Append("\\\"");
            else sb.Append(c);
        }

        if (quote != '\0') sb.Append('"');
        return sb.ToString();
    }

    /// <summary>.NET date format → Excel: dd/MM/yyyy HH:mm → dd/mm/yyyy hh:mm (Excel reads mm after hh as minutes).</summary>
    internal static string DateFormat(string format) => format switch
    {
        "d" => "yyyy-mm-dd",
        "g" or "f" => "yyyy-mm-dd hh:mm",
        "G" or "F" or "s" => "yyyy-mm-dd hh:mm:ss",
        "t" => "hh:mm",
        "T" => "hh:mm:ss",
        _ => Literals(format.Replace("tt", "AM/PM").ToLowerInvariant().Replace("am/pm", "AM/PM").Replace("fff", "000")),
    };

    private double EstimateWidth(HelixExportColumn column, IEnumerable<object?> sample)
    {
        var longest = column.Header.Length + 2;
        foreach (var value in sample)
        {
            var text = value is DateTime d ? (d.TimeOfDay == TimeSpan.Zero ? "2026-01-01" : "2026-01-01 00:00") : HelixExportTable.Text(value, column.Format, Culture);
            var line = text.Split('\n').Max(l => l.Length);
            if (line > longest) longest = line;
        }

        return Math.Clamp(longest * 1.1 + 2, 8, 60);
    }

    internal static string Ref(int column, int row)
    {
        var name = "";
        for (var c = column + 1; c > 0; c = (c - 1) / 26) name = (char)('A' + (c - 1) % 26) + name;
        return name + row.ToString(CultureInfo.InvariantCulture);
    }

    private static List<string> UniqueNames(IEnumerable<string> names)
    {
        var result = new List<string>();
        foreach (var raw in names)
        {
            var clean = string.Join(' ', new string(raw.Select(c => "[]:*?/\\".IndexOf(c) < 0 ? c : ' ').ToArray())
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim('\'').Trim();
            if (clean.Length == 0) clean = "Sheet";
            if (clean.Length > 31) clean = clean[..31];
            var name = clean;
            for (var i = 2; result.Contains(name, StringComparer.OrdinalIgnoreCase); i++)
            {
                var suffix = $" ({i})";
                name = (clean.Length + suffix.Length > 31 ? clean[..(31 - suffix.Length)] : clean) + suffix;
            }

            result.Add(name);
        }

        return result;
    }

    /// <summary>The look of a cell; borders are "style|ARGB" (thin, medium).</summary>
    private sealed record CellFormat
    {
        public string? NumberFormat { get; init; }
        public bool Bold { get; init; }
        public bool Italic { get; init; }
        public double? Size { get; init; }
        public string? Color { get; init; }
        public string? Fill { get; init; }
        public string? Top { get; init; }
        public string? Bottom { get; init; }
        public string? Sides { get; init; }
        public string? Align { get; init; }
        public bool Wrap { get; init; }
    }

    /// <summary>Collects the fonts, fills, borders and formats used, and writes styles.xml.</summary>
    private sealed class StyleSheet
    {
        private readonly HelixExcelStyle _style;
        private readonly List<string> _formats = [];
        private readonly List<(bool Bold, bool Italic, double Size, string? Color)> _fonts;
        private readonly List<string?> _fills = [null, null]; // 0: none, 1: gray125 (both required by Excel)
        private readonly List<(string? Top, string? Bottom, string? Sides)> _borders = [(null, null, null)];
        private readonly List<(int NumFmt, int Font, int Fill, int Border, string? Align, bool Wrap)> _xfs = [(0, 0, 0, 0, null, false)];

        public StyleSheet(HelixExcelStyle style)
        {
            _style = style;
            _fonts = [(false, false, style.FontSize, null)];
        }

        public int Get(CellFormat f)
        {
            var numFmt = f.NumberFormat is null ? 0 : FormatId(f.NumberFormat);
            var font = IndexOf(_fonts, (f.Bold, f.Italic, f.Size ?? _style.FontSize, f.Color));
            var fill = f.Fill is null ? 0 : IndexOf(_fills, f.Fill, start: 2);
            var border = IndexOf(_borders, (f.Top, f.Bottom, f.Sides));
            return IndexOf(_xfs, (numFmt, font, fill, border, f.Align, f.Wrap));
        }

        private static int IndexOf<T>(List<T> list, T item, int start = 0)
        {
            var index = list.FindIndex(start, x => EqualityComparer<T>.Default.Equals(x, item));
            if (index >= 0) return index;
            list.Add(item);
            return list.Count - 1;
        }

        private int FormatId(string format)
        {
            var builtIn = format switch { "0" => 1, "0.00" => 2, "#,##0" => 3, "#,##0.00" => 4, "0%" => 9, "0.00%" => 10, _ => -1 };
            if (builtIn >= 0) return builtIn;
            var index = _formats.IndexOf(format);
            if (index < 0) { _formats.Add(format); index = _formats.Count - 1; }
            return 164 + index;
        }

        private static string Side(string name, string? spec)
        {
            if (spec is null) return $"<{name}/>";
            var parts = spec.Split('|');
            return $"<{name} style=\"{parts[0]}\"><color rgb=\"{parts[1]}\"/></{name}>";
        }

        public string Xml()
        {
            var sb = new StringBuilder(Declaration);
            sb.Append("<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            if (_formats.Count > 0)
            {
                sb.Append($"<numFmts count=\"{_formats.Count}\">");
                for (var i = 0; i < _formats.Count; i++) sb.Append($"<numFmt numFmtId=\"{164 + i}\" formatCode=\"{Esc(_formats[i])}\"/>");
                sb.Append("</numFmts>");
            }

            var name = Esc(_style.FontName);
            sb.Append($"<fonts count=\"{_fonts.Count}\">");
            foreach (var (bold, italic, size, color) in _fonts)
            {
                sb.Append("<font>").Append(bold ? "<b/>" : "").Append(italic ? "<i/>" : "")
                  .Append($"<sz val=\"{size.ToString("0.##", CultureInfo.InvariantCulture)}\"/>")
                  .Append(color is null ? "" : $"<color rgb=\"{color}\"/>")
                  .Append($"<name val=\"{name}\"/><family val=\"2\"/></font>");
            }

            sb.Append("</fonts>");
            sb.Append($"<fills count=\"{_fills.Count}\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>");
            foreach (var fill in _fills.Skip(2)) sb.Append($"<fill><patternFill patternType=\"solid\"><fgColor rgb=\"{fill}\"/><bgColor indexed=\"64\"/></patternFill></fill>");
            sb.Append("</fills>");
            sb.Append($"<borders count=\"{_borders.Count}\">");
            foreach (var (top, bottom, sides) in _borders)
            {
                sb.Append("<border>").Append(Side("left", sides)).Append(Side("right", sides)).Append(Side("top", top)).Append(Side("bottom", bottom)).Append("<diagonal/></border>");
            }

            sb.Append("</borders>");
            sb.Append("<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>");
            sb.Append($"<cellXfs count=\"{_xfs.Count}\">");
            foreach (var (numFmt, font, fill, border, align, wrap) in _xfs)
            {
                sb.Append($"<xf numFmtId=\"{numFmt}\" fontId=\"{font}\" fillId=\"{fill}\" borderId=\"{border}\" xfId=\"0\"");
                if (numFmt > 0) sb.Append(" applyNumberFormat=\"1\"");
                if (font > 0) sb.Append(" applyFont=\"1\"");
                if (fill > 0) sb.Append(" applyFill=\"1\"");
                if (border > 0) sb.Append(" applyBorder=\"1\"");
                if (align is null && !wrap) { sb.Append("/>"); continue; }
                sb.Append(" applyAlignment=\"1\"><alignment");
                if (align is not null) sb.Append($" horizontal=\"{align}\"");
                if (wrap) sb.Append(" vertical=\"center\" wrapText=\"1\"");
                sb.Append("/></xf>");
            }

            sb.Append("</cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");
            return sb.ToString();
        }
    }

    /// <summary>The color mixed with white: 0.88 → a light tint for the header fill.</summary>
    internal static string Tint(string hex, double amount)
    {
        var value = Convert.ToInt32(hex.Length >= 6 ? hex[..6] : "4F46E5", 16);
        int Mix(int channel) => (int)Math.Round(channel + (255 - channel) * amount);
        return $"{Mix(value >> 16 & 0xFF):X2}{Mix(value >> 8 & 0xFF):X2}{Mix(value & 0xFF):X2}";
    }
}
