namespace Helix.Export;

/// <summary>File formats of an export; combine them for a choice ("Excel | Word").</summary>
[Flags]
public enum HelixExportFormat { None = 0, Csv = 1, Excel = 2, Word = 4, All = Csv | Excel | Word }

/// <summary>
/// One-liners for the common case — a list to a file:
/// <code>
/// byte[] xlsx = HelixExport.ToExcel(employees);                          // every property, one sheet
/// byte[] docx = HelixExport.ToWord(employees, title: "Employees");
/// string csv  = HelixExport.ToCsv(employees, c => c.Add("Name", e => e.Name).Add("Salary", e => e.Salary, "N0"));
/// </code>
/// For several sheets or a full report, use <see cref="HelixExcel"/> and <see cref="HelixWord"/>.
/// </summary>
public static class HelixExport
{
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string WordContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public const string CsvContentType = "text/csv;charset=utf-8";

    public static byte[] ToExcel<T>(IEnumerable<T> rows, Action<HelixColumns<T>>? columns = null, string sheet = "Data", string? title = null) =>
        new HelixExcel().Sheet(sheet, rows, columns, title).ToBytes();

    public static byte[] ToWord<T>(IEnumerable<T> rows, Action<HelixColumns<T>>? columns = null, string? title = null, bool landscape = false)
    {
        var word = new HelixWord { Landscape = landscape, PageNumbers = true };
        if (title is not null) word.Title(title);
        return word.Table(rows, columns).ToBytes();
    }

    public static string ToCsv<T>(IEnumerable<T> rows, Action<HelixColumns<T>>? columns = null, string? separator = null) =>
        HelixCsv.Write(HelixExportTable.From(rows, columns), separator);

    /// <summary>
    /// A table to the bytes of a file in one format. The table title becomes the Excel sheet name and title row,
    /// or the Word document title.
    /// </summary>
    public static byte[] Write(HelixExportTable table, HelixExportFormat format) => format switch
    {
        HelixExportFormat.Excel => new HelixExcel().Sheet(table.Title ?? "Data", table).ToBytes(),
        HelixExportFormat.Word => WordOf(table).ToBytes(),
        HelixExportFormat.Csv => HelixCsv.ToBytes(table),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Pick one format: Csv, Excel or Word."),
    };

    private static HelixWord WordOf(HelixExportTable table)
    {
        var word = new HelixWord { PageNumbers = true, Landscape = table.Columns.Count > 6 };
        if (table.Title is null) return word.Table(table);
        return word.Title(table.Title).Table(new HelixExportTable(table.Columns, table.Rows) { TotalLabel = table.TotalLabel });
    }

    /// <summary>".xlsx", ".docx" or ".csv".</summary>
    public static string Extension(HelixExportFormat format) => format switch
    {
        HelixExportFormat.Excel => ".xlsx",
        HelixExportFormat.Word => ".docx",
        _ => ".csv",
    };

    public static string ContentType(HelixExportFormat format) => format switch
    {
        HelixExportFormat.Excel => ExcelContentType,
        HelixExportFormat.Word => WordContentType,
        _ => CsvContentType,
    };

    /// <summary>"Excel (.xlsx)", "Word (.docx)", "CSV (.csv)": the label of a menu entry.</summary>
    public static string Label(HelixExportFormat format) => format switch
    {
        HelixExportFormat.Excel => "Excel (.xlsx)",
        HelixExportFormat.Word => "Word (.docx)",
        _ => "CSV (.csv)",
    };

    /// <summary>The single formats contained in <paramref name="formats"/>, Excel first.</summary>
    public static IReadOnlyList<HelixExportFormat> Split(HelixExportFormat formats) =>
        new[] { HelixExportFormat.Excel, HelixExportFormat.Word, HelixExportFormat.Csv }.Where(f => formats.HasFlag(f)).ToArray();
}
