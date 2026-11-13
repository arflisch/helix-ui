namespace Helix.Export;

/// <summary>Lines between the cells of the table.</summary>
public enum HelixExcelBorders { None, Rows, All }

/// <summary>Colors of one cell, returned by <see cref="HelixExcelStyle.CellStyle"/>. Colors: "#RRGGBB" or success, warning, danger, info, primary.</summary>
public sealed record HelixExcelCellStyle(string? Color = null, string? Background = null, bool Bold = false, bool Italic = false);

/// <summary>A data cell being written: its column, its row object, its value and its index in the table (0-based).</summary>
public sealed record HelixExcelCell(HelixExportColumn Column, object Row, object? Value, int RowIndex);

/// <summary>
/// What the Excel file looks like. Start from a preset and change what you need:
/// <code>new HelixExcel { Style = HelixExcelStyle.Bold with { Accent = "#0F766E", NegativeColor = "danger" } }</code>
/// Colors are "#RRGGBB" or a name: primary (the accent), success, warning, danger, info, neutral, white, black.
/// </summary>
public sealed record HelixExcelStyle
{
    /// <summary>Main color: default header fill (light tint), header line and title.</summary>
    public string Accent { get; init; } = "#4F46E5";

    public string FontName { get; init; } = "Calibri";
    public double FontSize { get; init; } = 11;

    /// <summary>Header fill; null: a light tint of the accent; "none": no fill.</summary>
    public string? HeaderBackground { get; init; }

    public string HeaderColor { get; init; } = "#1F2937";
    public bool HeaderBold { get; init; } = true;

    /// <summary>Line under the header; null: the accent; "none": no line.</summary>
    public string? HeaderLine { get; init; }

    /// <summary>Title color (above the table); null: the accent.</summary>
    public string? TitleColor { get; init; }
    public double TitleSize { get; init; } = 15;

    /// <summary>Every other row filled with <see cref="StripeColor"/>.</summary>
    public bool Striped { get; init; }
    public string StripeColor { get; init; } = "#F3F4F6";

    public HelixExcelBorders Borders { get; init; } = HelixExcelBorders.None;
    public string BorderColor { get; init; } = "#E5E7EB";

    /// <summary>Fill of the totals row (none by default).</summary>
    public string? TotalsBackground { get; init; }

    /// <summary>Excel's gray gridlines around the cells (true).</summary>
    public bool ShowGridLines { get; init; } = true;

    /// <summary>Text color of negative numbers, e.g. "danger".</summary>
    public string? NegativeColor { get; init; }

    /// <summary>Height of every row in points (Excel's default when null, about 15).</summary>
    public double? RowHeight { get; init; }

    /// <summary>Colors per cell, e.g. <c>c => c.Column.Header == "Status" &amp;&amp; c.Value is "Late" ? new(Color: "danger", Bold: true) : null</c>.</summary>
    public Func<HelixExcelCell, HelixExcelCellStyle?>? CellStyle { get; init; }

    /// <summary>Light accent header with a line under it, no stripes (the default).</summary>
    public static HelixExcelStyle Default { get; } = new();

    /// <summary>Solid accent header with white text, striped rows.</summary>
    public static HelixExcelStyle Bold { get; } = new() { HeaderBackground = "primary", HeaderColor = "#FFFFFF", HeaderLine = "none", Striped = true, ShowGridLines = false };

    /// <summary>No fill, a dark line under the header, thin lines between rows.</summary>
    public static HelixExcelStyle Minimal { get; } = new()
    {
        HeaderBackground = "none", HeaderLine = "#111827", TitleColor = "#111827", Borders = HelixExcelBorders.Rows, ShowGridLines = false,
    };

    /// <summary>Every cell framed, gray header: a printable table.</summary>
    public static HelixExcelStyle Grid { get; } = new()
    {
        HeaderBackground = "#E5E7EB", HeaderLine = "none", Borders = HelixExcelBorders.All, BorderColor = "#9CA3AF", TitleColor = "#111827",
        TotalsBackground = "#F3F4F6",
    };

    /// <summary>"FFRRGGBB" for a color text, or null for none.</summary>
    internal string? Argb(string? color)
    {
        if (string.IsNullOrWhiteSpace(color) || color.Equals("none", StringComparison.OrdinalIgnoreCase) || color.Equals("transparent", StringComparison.OrdinalIgnoreCase)) return null;
        var hex = color.Trim().ToLowerInvariant() switch
        {
            "primary" or "accent" => HelixXml.Hex(Accent),
            "success" => "16A34A",
            "warning" => "D97706",
            "danger" => "DC2626",
            "info" => "0284C7",
            "neutral" => "6B7280",
            "white" => "FFFFFF",
            "black" => "000000",
            _ => HelixXml.Hex(color),
        };
        if (hex.Length == 3) hex = string.Concat(hex.Select(c => $"{c}{c}"));
        return hex.Length == 6 && hex.All(Uri.IsHexDigit) ? "FF" + hex : null;
    }
}
