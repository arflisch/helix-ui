using System.Globalization;
using System.IO.Compression;
using System.Text;
using static Helix.Export.HelixXml;

namespace Helix.Export;

/// <summary>
/// A Word document (.docx) written block by block:
/// <code>
/// new HelixWord { PageNumbers = true }
///     .Title("Monthly report")
///     .Paragraph("Figures for November.")
///     .Details(("Site", "North"), ("Period", "November 2026"))
///     .Heading("Employees")
///     .Table(employees, c => c.Add("Name", e => e.Name).Add("Salary", e => e.Salary, "N0", HelixTotal.Sum))
///     .Save("report.docx");
/// </code>
/// </summary>
public sealed class HelixWord
{
    private readonly List<string> _blocks = [];
    private readonly List<bool> _lists = []; // one numbering instance per list: true = numbered

    /// <summary>Title and heading color, header fill of tables, as hex (Helix indigo by default).</summary>
    public string Accent { get; set; } = "#4F46E5";

    /// <summary>A4 landscape instead of portrait.</summary>
    public bool Landscape { get; set; }

    /// <summary>"1 / 3" in the footer.</summary>
    public bool PageNumbers { get; set; }

    /// <summary>A small text at the top of every page.</summary>
    public string? HeaderText { get; set; }

    /// <summary>Culture of the numbers and dates in tables (current culture).</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    private int TextWidth => (Landscape ? 16838 : 11906) - 2 * 1134;

    public HelixWord Title(string text) => Add($"<w:p><w:pPr><w:pStyle w:val=\"Title\"/></w:pPr>{Runs(text)}</w:p>");

    /// <summary>A heading, level 1 to 3.</summary>
    public HelixWord Heading(string text, int level = 1) =>
        Add($"<w:p><w:pPr><w:pStyle w:val=\"Heading{Math.Clamp(level, 1, 3)}\"/></w:pPr>{Runs(text)}</w:p>");

    /// <summary>A paragraph; line breaks in the text are kept.</summary>
    public HelixWord Paragraph(string text, bool bold = false, bool italic = false, bool muted = false) =>
        Add($"<w:p>{Runs(text, bold, italic, muted ? "6B7280" : null)}</w:p>");

    public HelixWord Bullets(IEnumerable<string> items) => List(items, numbered: false);

    public HelixWord Numbered(IEnumerable<string> items) => List(items, numbered: true);

    /// <summary>Label / value pairs, as a borderless two-column table.</summary>
    public HelixWord Details(params (string Label, object? Value)[] pairs)
    {
        var labelWidth = (int)(TextWidth * 0.3);
        var sb = new StringBuilder("<w:tbl><w:tblPr><w:tblW w:w=\"5000\" w:type=\"pct\"/><w:tblLayout w:type=\"fixed\"/>" +
            "<w:tblCellMar><w:left w:w=\"0\" w:type=\"dxa\"/><w:right w:w=\"120\" w:type=\"dxa\"/></w:tblCellMar></w:tblPr>");
        sb.Append($"<w:tblGrid><w:gridCol w:w=\"{labelWidth}\"/><w:gridCol w:w=\"{TextWidth - labelWidth}\"/></w:tblGrid>");
        foreach (var (label, value) in pairs)
        {
            sb.Append("<w:tr>");
            sb.Append($"<w:tc><w:tcPr><w:tcW w:w=\"{labelWidth}\" w:type=\"dxa\"/></w:tcPr><w:p><w:pPr><w:spacing w:before=\"20\" w:after=\"20\"/></w:pPr>{Runs(label, color: "6B7280", size: 20)}</w:p></w:tc>");
            sb.Append($"<w:tc><w:tcPr><w:tcW w:w=\"{TextWidth - labelWidth}\" w:type=\"dxa\"/></w:tcPr><w:p><w:pPr><w:spacing w:before=\"20\" w:after=\"20\"/></w:pPr>{Runs(HelixExportTable.Text(value, null, Culture))}</w:p></w:tc>");
            sb.Append("</w:tr>");
        }

        sb.Append("</w:tbl>");
        return Add(sb.ToString()).Spacer();
    }

    public HelixWord Table<T>(IEnumerable<T> rows, Action<HelixColumns<T>>? columns = null, string? title = null) =>
        Table(HelixExportTable.From(rows, columns, title));

    /// <summary>A table with a repeated header row, right-aligned numbers and an optional totals row.</summary>
    public HelixWord Table(HelixExportTable table)
    {
        if (table.Title is not null) Heading(table.Title, 2);
        var columns = table.Columns;
        if (columns.Count == 0) return this;
        var values = table.Read();
        var widths = Widths(table, values);
        var right = Enumerable.Range(0, columns.Count)
            .Select(c => HelixExportTable.IsRightAligned(columns[c], values.FirstOrDefault(r => r[c] is not null)?[c])).ToArray();
        var fill = HelixExcel.Tint(Hex(Accent), 0.9);

        var sb = new StringBuilder("<w:tbl><w:tblPr><w:tblW w:w=\"5000\" w:type=\"pct\"/><w:tblBorders>" +
            "<w:top w:val=\"single\" w:sz=\"4\" w:color=\"D1D5DB\"/><w:bottom w:val=\"single\" w:sz=\"4\" w:color=\"D1D5DB\"/>" +
            "<w:insideH w:val=\"single\" w:sz=\"4\" w:color=\"E5E7EB\"/></w:tblBorders><w:tblLayout w:type=\"fixed\"/>" +
            "<w:tblCellMar><w:left w:w=\"90\" w:type=\"dxa\"/><w:right w:w=\"90\" w:type=\"dxa\"/></w:tblCellMar></w:tblPr><w:tblGrid>");
        foreach (var w in widths) sb.Append($"<w:gridCol w:w=\"{w}\"/>");
        sb.Append("</w:tblGrid>");

        sb.Append("<w:tr><w:trPr><w:tblHeader/></w:trPr>");
        for (var c = 0; c < columns.Count; c++) TableCell(sb, widths[c], columns[c].Header, right[c], bold: true, fill: fill);
        sb.Append("</w:tr>");

        foreach (var row in values)
        {
            sb.Append("<w:tr><w:trPr><w:cantSplit/></w:trPr>");
            for (var c = 0; c < columns.Count; c++)
            {
                TableCell(sb, widths[c], HelixExportTable.Text(row[c], columns[c].Format, Culture), right[c],
                    center: row[c] is bool && columns[c].Align == HelixExportAlign.Auto || columns[c].Align == HelixExportAlign.Center);
            }

            sb.Append("</w:tr>");
        }

        if (table.HasTotals)
        {
            var labelColumn = Enumerable.Range(0, columns.Count).FirstOrDefault(c => columns[c].Total == HelixTotal.None, -1);
            sb.Append("<w:tr>");
            for (var c = 0; c < columns.Count; c++)
            {
                var total = columns[c].Total == HelixTotal.None ? null : HelixExportTable.TotalOf(columns[c].Total, values.Select(v => v[c]));
                var text = c == labelColumn ? table.TotalLabel
                    : total is null ? ""
                    : columns[c].Total == HelixTotal.Count ? HelixExportTable.Text(total, null, Culture)
                    : HelixExportTable.Text(total, columns[c].Format ?? "#,##0.##", Culture);
                TableCell(sb, widths[c], text, right[c], bold: true, topBorder: true);
            }

            sb.Append("</w:tr>");
        }

        sb.Append("</w:tbl>");
        return Add(sb.ToString()).Spacer();
    }

    public HelixWord PageBreak() => Add("<w:p><w:r><w:br w:type=\"page\"/></w:r></w:p>");

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
        var accent = Hex(Accent);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
        Write(zip, "[Content_Types].xml", Declaration +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>" +
            "<Override PartName=\"/word/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml\"/>" +
            "<Override PartName=\"/word/numbering.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml\"/>" +
            "<Override PartName=\"/word/footer1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml\"/>" +
            "<Override PartName=\"/word/header1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml\"/>" +
            "</Types>");
        Write(zip, "_rels/.rels", Declaration +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>" +
            "</Relationships>");
        Write(zip, "word/_rels/document.xml.rels", Declaration +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/numbering\" Target=\"numbering.xml\"/>" +
            "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer\" Target=\"footer1.xml\"/>" +
            "<Relationship Id=\"rId4\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/header\" Target=\"header1.xml\"/>" +
            "</Relationships>");

        var (w, h) = Landscape ? (16838, 11906) : (11906, 16838);
        var body = new StringBuilder(Declaration);
        body.Append($"<w:document {Namespaces}><w:body>");
        foreach (var block in _blocks) body.Append(block);
        body.Append("<w:sectPr>");
        if (HeaderText is not null) body.Append("<w:headerReference w:type=\"default\" r:id=\"rId4\"/>");
        if (PageNumbers) body.Append("<w:footerReference w:type=\"default\" r:id=\"rId3\"/>");
        body.Append($"<w:pgSz w:w=\"{w}\" w:h=\"{h}\"{(Landscape ? " w:orient=\"landscape\"" : "")}/>" +
            "<w:pgMar w:top=\"1134\" w:right=\"1134\" w:bottom=\"1134\" w:left=\"1134\" w:header=\"567\" w:footer=\"567\" w:gutter=\"0\"/></w:sectPr>");
        body.Append("</w:body></w:document>");
        Write(zip, "word/document.xml", body.ToString());
        Write(zip, "word/styles.xml", StylesXml(accent));
        Write(zip, "word/numbering.xml", NumberingXml());
        Write(zip, "word/footer1.xml", Declaration + $"<w:ftr {Namespaces}><w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr>" +
            "<w:fldSimple w:instr=\" PAGE \"><w:r><w:rPr><w:color w:val=\"6B7280\"/><w:sz w:val=\"18\"/></w:rPr><w:t>1</w:t></w:r></w:fldSimple>" +
            "<w:r><w:rPr><w:color w:val=\"6B7280\"/><w:sz w:val=\"18\"/></w:rPr><w:t xml:space=\"preserve\"> / </w:t></w:r>" +
            "<w:fldSimple w:instr=\" NUMPAGES \"><w:r><w:rPr><w:color w:val=\"6B7280\"/><w:sz w:val=\"18\"/></w:rPr><w:t>1</w:t></w:r></w:fldSimple></w:p></w:ftr>");
        Write(zip, "word/header1.xml", Declaration + $"<w:hdr {Namespaces}><w:p><w:pPr><w:jc w:val=\"right\"/></w:pPr>" +
            $"{Runs(HeaderText ?? "", color: "6B7280", size: 18)}</w:p></w:hdr>");
    }

    private const string Namespaces =
        "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"";

    private HelixWord Add(string xml)
    {
        _blocks.Add(xml);
        return this;
    }

    private HelixWord Spacer() => Add("<w:p><w:pPr><w:spacing w:after=\"0\"/></w:pPr></w:p>");

    private HelixWord List(IEnumerable<string> items, bool numbered)
    {
        _lists.Add(numbered);
        var id = _lists.Count;
        foreach (var item in items)
        {
            Add($"<w:p><w:pPr><w:pStyle w:val=\"ListParagraph\"/><w:numPr><w:ilvl w:val=\"0\"/><w:numId w:val=\"{id}\"/></w:numPr><w:spacing w:after=\"60\"/></w:pPr>{Runs(item)}</w:p>");
        }

        return this;
    }

    private static string Runs(string text, bool bold = false, bool italic = false, string? color = null, int? size = null)
    {
        var props = (bold ? "<w:b/>" : "") + (italic ? "<w:i/>" : "") + (color is null ? "" : $"<w:color w:val=\"{color}\"/>") + (size is null ? "" : $"<w:sz w:val=\"{size}\"/>");
        var rPr = props.Length == 0 ? "" : $"<w:rPr>{props}</w:rPr>";
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            sb.Append($"<w:r>{rPr}{(i > 0 ? "<w:br/>" : "")}<w:t xml:space=\"preserve\">{Esc(lines[i])}</w:t></w:r>");
        }

        return sb.ToString();
    }

    private static void TableCell(StringBuilder sb, int width, string text, bool right, bool bold = false, string? fill = null, bool center = false, bool topBorder = false)
    {
        sb.Append($"<w:tc><w:tcPr><w:tcW w:w=\"{width}\" w:type=\"dxa\"/>");
        if (topBorder) sb.Append("<w:tcBorders><w:top w:val=\"single\" w:sz=\"8\" w:color=\"9CA3AF\"/></w:tcBorders>");
        if (fill is not null) sb.Append($"<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"{fill}\"/>");
        sb.Append("<w:vAlign w:val=\"center\"/></w:tcPr><w:p><w:pPr><w:spacing w:before=\"50\" w:after=\"50\" w:line=\"240\" w:lineRule=\"auto\"/>");
        if (center || right) sb.Append($"<w:jc w:val=\"{(center ? "center" : "right")}\"/>");
        sb.Append($"</w:pPr>{Runs(text, bold, size: 19)}</w:p></w:tc>");
    }

    /// <summary>Column widths in twips, from the length of the content, within the text width.</summary>
    private int[] Widths(HelixExportTable table, List<object?[]> values)
    {
        var columns = table.Columns;
        var weights = new double[columns.Count];
        for (var c = 0; c < columns.Count; c++)
        {
            var longest = values.Take(200).Select(r => HelixExportTable.Text(r[c], columns[c].Format, Culture).Length).DefaultIfEmpty(0).Max();
            weights[c] = columns[c].Width ?? Math.Clamp(Math.Max(longest, columns[c].Header.Length * 0.9), 4, 40);
        }

        var sum = weights.Sum();
        return weights.Select(w => (int)(TextWidth * w / sum)).ToArray();
    }

    private static string StylesXml(string accent) => Declaration +
        "<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" +
        "<w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii=\"Calibri\" w:hAnsi=\"Calibri\" w:eastAsia=\"Calibri\" w:cs=\"Calibri\"/>" +
        "<w:sz w:val=\"22\"/><w:szCs w:val=\"22\"/><w:color w:val=\"1F2937\"/><w:lang w:val=\"en-GB\"/></w:rPr></w:rPrDefault>" +
        "<w:pPrDefault><w:pPr><w:spacing w:after=\"140\" w:line=\"264\" w:lineRule=\"auto\"/></w:pPr></w:pPrDefault></w:docDefaults>" +
        "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/><w:qFormat/></w:style>" +
        $"<w:style w:type=\"paragraph\" w:styleId=\"Title\"><w:name w:val=\"Title\"/><w:basedOn w:val=\"Normal\"/><w:next w:val=\"Normal\"/><w:qFormat/>" +
        $"<w:pPr><w:spacing w:after=\"240\"/></w:pPr><w:rPr><w:b/><w:color w:val=\"{accent}\"/><w:sz w:val=\"44\"/><w:szCs w:val=\"44\"/></w:rPr></w:style>" +
        Heading(1, 30, accent, 360) + Heading(2, 26, "111827", 280) + Heading(3, 23, "374151", 220) +
        "<w:style w:type=\"paragraph\" w:styleId=\"ListParagraph\"><w:name w:val=\"List Paragraph\"/><w:basedOn w:val=\"Normal\"/><w:pPr><w:ind w:left=\"720\"/></w:pPr></w:style>" +
        "</w:styles>";

    private static string Heading(int level, int size, string color, int before) =>
        $"<w:style w:type=\"paragraph\" w:styleId=\"Heading{level}\"><w:name w:val=\"heading {level}\"/><w:basedOn w:val=\"Normal\"/><w:next w:val=\"Normal\"/><w:qFormat/>" +
        $"<w:pPr><w:keepNext/><w:spacing w:before=\"{before}\" w:after=\"120\"/><w:outlineLvl w:val=\"{level - 1}\"/></w:pPr>" +
        $"<w:rPr><w:b/><w:color w:val=\"{color}\"/><w:sz w:val=\"{size}\"/><w:szCs w:val=\"{size}\"/></w:rPr></w:style>";

    private string NumberingXml()
    {
        var sb = new StringBuilder(Declaration);
        sb.Append("<w:numbering xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">");
        sb.Append("<w:abstractNum w:abstractNumId=\"0\"><w:multiLevelType w:val=\"singleLevel\"/><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\"/><w:numFmt w:val=\"bullet\"/>" +
            "<w:lvlText w:val=\"•\"/><w:lvlJc w:val=\"left\"/><w:pPr><w:ind w:left=\"720\" w:hanging=\"360\"/></w:pPr></w:lvl></w:abstractNum>");
        sb.Append("<w:abstractNum w:abstractNumId=\"1\"><w:multiLevelType w:val=\"singleLevel\"/><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\"/><w:numFmt w:val=\"decimal\"/>" +
            "<w:lvlText w:val=\"%1.\"/><w:lvlJc w:val=\"left\"/><w:pPr><w:ind w:left=\"720\" w:hanging=\"360\"/></w:pPr></w:lvl></w:abstractNum>");
        for (var i = 0; i < _lists.Count; i++)
        {
            sb.Append($"<w:num w:numId=\"{i + 1}\"><w:abstractNumId w:val=\"{(_lists[i] ? 1 : 0)}\"/>" +
                (_lists[i] ? "<w:lvlOverride w:ilvl=\"0\"><w:startOverride w:val=\"1\"/></w:lvlOverride>" : "") + "</w:num>");
        }

        sb.Append("</w:numbering>");
        return sb.ToString();
    }
}
