using System.Globalization;
using System.Text;

namespace Helix.Export;

/// <summary>CSV from a table: values as displayed, quoted when needed, UTF-8 with BOM so that Excel reads accents.</summary>
public static class HelixCsv
{
    /// <summary>
    /// The table as CSV text. The separator defaults to the culture's list separator (";" in French, "," in English),
    /// which is what Excel expects when it opens the file.
    /// </summary>
    public static string Write(HelixExportTable table, string? separator = null, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        separator ??= culture.TextInfo.ListSeparator;
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(separator, table.Columns.Select(c => Escape(c.Header, separator))));
        var values = table.Read();
        foreach (var row in values)
        {
            sb.AppendLine(string.Join(separator, table.Columns.Select((c, i) => Escape(HelixExportTable.Text(row[i], c.Format, culture), separator))));
        }

        if (table.HasTotals)
        {
            var labelColumn = Enumerable.Range(0, table.Columns.Count).FirstOrDefault(c => table.Columns[c].Total == HelixTotal.None, -1);
            sb.AppendLine(string.Join(separator, table.Columns.Select((c, i) =>
                i == labelColumn ? Escape(table.TotalLabel, separator)
                : c.Total == HelixTotal.None ? ""
                : Escape(HelixExportTable.Text(HelixExportTable.TotalOf(c.Total, values.Select(v => v[i])), c.Total == HelixTotal.Count ? null : c.Format, culture), separator))));
        }

        return sb.ToString();
    }

    /// <summary>The CSV as UTF-8 bytes with a BOM.</summary>
    public static byte[] ToBytes(HelixExportTable table, string? separator = null, CultureInfo? culture = null) =>
        [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(Write(table, separator, culture))];

    public static void Save(HelixExportTable table, string path, string? separator = null, CultureInfo? culture = null) =>
        File.WriteAllBytes(path, ToBytes(table, separator, culture));

    private static string Escape(string value, string separator) =>
        value.Contains(separator, StringComparison.Ordinal) || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
