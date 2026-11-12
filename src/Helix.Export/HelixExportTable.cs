using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Helix.Export;

/// <summary>What the totals row computes for a column.</summary>
public enum HelixTotal { None, Sum, Average, Count, Min, Max }

/// <summary>Horizontal alignment of a column; Auto puts numbers and dates on the right.</summary>
public enum HelixExportAlign { Auto, Left, Center, Right }

/// <summary>One column of an export: a header, how to read the value from a row, and how to show it.</summary>
public sealed class HelixExportColumn
{
    public required string Header { get; init; }

    /// <summary>Reads the cell value from a row (the row is the object you passed in).</summary>
    public required Func<object, object?> Value { get; init; }

    /// <summary>.NET format ("N2", "P0", "dd/MM/yyyy HH:mm"…), used for Word and CSV and translated for Excel.</summary>
    public string? Format { get; init; }

    /// <summary>Excel number format when the translation of <see cref="Format"/> is not what you want ("#,##0.00 \"MW\"").</summary>
    public string? ExcelFormat { get; init; }

    public HelixTotal Total { get; init; }

    /// <summary>Excel column width in characters; estimated from the content when null.</summary>
    public double? Width { get; init; }

    public HelixExportAlign Align { get; init; }
}

/// <summary>
/// Rows and columns ready to be written by <see cref="HelixExcel"/>, <see cref="HelixWord"/> or <see cref="HelixCsv"/>.
/// Build it from a list with <see cref="From{T}"/>, or from your own column definitions (a grid's visible columns).
/// </summary>
public sealed class HelixExportTable
{
    public HelixExportTable(IEnumerable<HelixExportColumn> columns, IEnumerable<object> rows)
    {
        Columns = columns.ToList();
        Rows = rows;
    }

    public IReadOnlyList<HelixExportColumn> Columns { get; }
    public IEnumerable<object> Rows { get; }

    /// <summary>A title written above the table.</summary>
    public string? Title { get; init; }

    /// <summary>Label of the totals row, in the first column without a total ("Total").</summary>
    public string TotalLabel { get; init; } = "Total";

    public bool HasTotals => Columns.Any(c => c.Total != HelixTotal.None);

    /// <summary>
    /// A table from a list. Without <paramref name="columns"/>, one column per public property
    /// ([Display(Name, Order)], [DisplayFormat], [Browsable(false)] and [Display(AutoGenerateField = false)] are honoured).
    /// </summary>
    public static HelixExportTable From<T>(IEnumerable<T> rows, Action<HelixColumns<T>>? columns = null, string? title = null)
    {
        var builder = new HelixColumns<T>();
        if (columns is null) builder.Auto();
        else columns(builder);
        return new HelixExportTable(builder.Build(), rows.Cast<object>()) { Title = title };
    }

    /// <summary>The cell values, read once.</summary>
    internal List<object?[]> Read() =>
        Rows.Select(row => Columns.Select(c => Normalize(c.Value(row))).ToArray()).ToList();

    private static object? Normalize(object? value) => value switch
    {
        DateTimeOffset dto => dto.LocalDateTime,
        DateOnly d => d.ToDateTime(TimeOnly.MinValue),
        _ => value,
    };

    /// <summary>The total of a column over the read values, or null.</summary>
    internal static object? TotalOf(HelixTotal total, IEnumerable<object?> values)
    {
        if (total == HelixTotal.Count) return values.Count(v => v is not null && v is not string { Length: 0 });
        var numbers = values.Where(IsNumber).Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)).ToList();
        if (numbers.Count == 0) return null;
        return total switch
        {
            HelixTotal.Sum => numbers.Sum(),
            HelixTotal.Average => numbers.Average(),
            HelixTotal.Min => numbers.Min(),
            HelixTotal.Max => numbers.Max(),
            _ => null,
        };
    }

    internal static bool IsNumber(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    internal static bool IsRightAligned(HelixExportColumn column, object? sample) =>
        column.Align == HelixExportAlign.Right || (column.Align == HelixExportAlign.Auto && (IsNumber(sample) || sample is DateTime or TimeSpan or TimeOnly));

    /// <summary>A value as text, with the column format and the culture.</summary>
    internal static string Text(object? value, string? format, CultureInfo culture) => value switch
    {
        null => "",
        string s => s,
        bool b => b ? "✓" : "",
        Enum e => EnumText(e),
        DateTime d when format is null => d.TimeOfDay == TimeSpan.Zero ? d.ToString("d", culture) : d.ToString("g", culture),
        IFormattable f => f.ToString(format, culture),
        _ => value.ToString() ?? "",
    };

    private static string EnumText(Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.GetName()
            ?? member?.GetCustomAttribute<DescriptionAttribute>()?.Description
            ?? value.ToString();
    }
}

/// <summary>Columns of an export, declared in a lambda: <c>c => c.Add("Name", e => e.Name).Add("Salary", e => e.Salary, "N0", HelixTotal.Sum)</c>.</summary>
public sealed class HelixColumns<T>
{
    private readonly List<HelixExportColumn> _columns = [];

    public HelixColumns<T> Add(string header, Func<T, object?> value, string? format = null, HelixTotal total = HelixTotal.None,
        double? width = null, HelixExportAlign align = HelixExportAlign.Auto, string? excelFormat = null)
    {
        _columns.Add(new HelixExportColumn
        {
            Header = header, Value = row => value((T)row), Format = format, Total = total, Width = width, Align = align, ExcelFormat = excelFormat,
        });
        return this;
    }

    /// <summary>One column per public property (see <see cref="HelixExportTable.From{T}"/>).</summary>
    public HelixColumns<T> Auto()
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && IsSimple(p.PropertyType))
            .Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false)
            .Select(p => (Property: p, Display: p.GetCustomAttribute<DisplayAttribute>()))
            .Where(x => x.Display?.GetAutoGenerateField() != false)
            .OrderBy(x => x.Display?.GetOrder() ?? 10_000)
            .ToList();

        foreach (var (property, display) in properties)
        {
            var format = property.GetCustomAttribute<DisplayFormatAttribute>()?.DataFormatString;
            if (format is not null && format.StartsWith("{0:", StringComparison.Ordinal) && format.EndsWith('}')) format = format[3..^1];
            _columns.Add(new HelixExportColumn
            {
                Header = display?.GetName() ?? Humanize(property.Name),
                Value = row => property.GetValue(row),
                Format = format,
            });
        }

        return this;
    }

    internal IReadOnlyList<HelixExportColumn> Build() => _columns;

    private static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime)
            || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan) || type == typeof(Guid);
    }

    /// <summary>"StartDate" → "Start date".</summary>
    internal static string Humanize(string name)
    {
        var chars = new System.Text.StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
            {
                chars.Append(' ').Append(char.ToLowerInvariant(c));
            }
            else
            {
                chars.Append(c);
            }
        }

        return chars.ToString();
    }
}
