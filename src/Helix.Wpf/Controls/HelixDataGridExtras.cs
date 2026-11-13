using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Helix.Export;

namespace Helix.Wpf;

/// <summary>Groups, totals and CSV export of <see cref="HelixDataGrid"/>.</summary>
public partial class HelixDataGrid
{
    public static readonly DependencyProperty GroupByProperty = HelixProperty.Register<HelixDataGrid, string?>(nameof(GroupBy), null, (d, _) => ((HelixDataGrid)d).Reload());
    public static readonly DependencyProperty IsExportableProperty = HelixProperty.Register<HelixDataGrid, bool>(nameof(IsExportable), false);
    public static readonly DependencyProperty ExportFileNameProperty = HelixProperty.Register<HelixDataGrid, string>(nameof(ExportFileName), "export");
    public static readonly DependencyProperty ExportFormatsProperty = HelixProperty.Register<HelixDataGrid, HelixExportFormat>(nameof(ExportFormats), HelixExportFormat.Csv);
    public static readonly DependencyProperty ExportTitleProperty = HelixProperty.Register<HelixDataGrid, string?>(nameof(ExportTitle), null);
    public static readonly DependencyProperty ExcelStyleProperty = HelixProperty.Register<HelixDataGrid, HelixExcelStyle?>(nameof(ExcelStyle), null);

    private static readonly DependencyPropertyKey HasTotalsKey = DependencyProperty.RegisterReadOnly(nameof(HasTotals), typeof(bool), typeof(HelixDataGrid), new PropertyMetadata(false));
    public static readonly DependencyProperty HasTotalsProperty = HasTotalsKey.DependencyProperty;

    /// <summary>Total of a column, in the total row and in each group header: Sum, Average, Min, Max or Count.</summary>
    public static readonly DependencyProperty AggregateProperty = DependencyProperty.RegisterAttached("Aggregate", typeof(HelixAggregate), typeof(HelixDataGrid),
        new PropertyMetadata(HelixAggregate.None, OnColumnOptionChanged));

    /// <summary>Unit of a column, added after its totals and to the CSV header, e.g. "MW".</summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.RegisterAttached("Unit", typeof(string), typeof(HelixDataGrid),
        new PropertyMetadata(null, OnColumnOptionChanged));

    /// <summary>Format of a column's total (the binding's StringFormat by default).</summary>
    public static readonly DependencyProperty AggregateFormatProperty = DependencyProperty.RegisterAttached("AggregateFormat", typeof(string), typeof(HelixDataGrid),
        new PropertyMetadata(null, OnColumnOptionChanged));

    public static HelixAggregate GetAggregate(DependencyObject column) => (HelixAggregate)column.GetValue(AggregateProperty);
    public static void SetAggregate(DependencyObject column, HelixAggregate value) => column.SetValue(AggregateProperty, value);
    public static string? GetUnit(DependencyObject column) => (string?)column.GetValue(UnitProperty);
    public static void SetUnit(DependencyObject column, string? value) => column.SetValue(UnitProperty, value);
    public static string? GetAggregateFormat(DependencyObject column) => (string?)column.GetValue(AggregateFormatProperty);
    public static void SetAggregateFormat(DependencyObject column, string? value) => column.SetValue(AggregateFormatProperty, value);

    public static readonly DependencyProperty FilterProperty = HelixProperty.Register<HelixDataGrid, Predicate<object>?>(nameof(Filter), null, (d, _) => ((HelixDataGrid)d).OnFilterChanged());

    /// <summary>Keeps only the rows it accepts (client mode, before search and paging); set by a HelixFilterBar's Target.</summary>
    public Predicate<object>? Filter { get => (Predicate<object>?)GetValue(FilterProperty); set => SetValue(FilterProperty, value); }

    private void OnFilterChanged()
    {
        SetCurrentValue(PageProperty, 1);
        Reload();
    }

    /// <summary>Property path that groups the rows (e.g. "Region"): collapsible headers with their count and subtotals.</summary>
    public string? GroupBy { get => (string?)GetValue(GroupByProperty); set => SetValue(GroupByProperty, value); }

    /// <summary>An "Export" button in the toolbar: saves the filtered rows, in <see cref="ExportFormats"/>.</summary>
    public bool IsExportable { get => (bool)GetValue(IsExportableProperty); set => SetValue(IsExportableProperty, value); }

    /// <summary>File name suggested by the export, without extension.</summary>
    public string ExportFileName { get => (string)GetValue(ExportFileNameProperty); set => SetValue(ExportFileNameProperty, value); }

    /// <summary>Formats offered by the Export button: Csv (default), Excel, Word, or several (a menu), e.g. "Excel, Csv" or "All".</summary>
    public HelixExportFormat ExportFormats { get => (HelixExportFormat)GetValue(ExportFormatsProperty); set => SetValue(ExportFormatsProperty, value); }

    /// <summary>Title written above the table in Word exports (and the Excel sheet name).</summary>
    public string? ExportTitle { get => (string?)GetValue(ExportTitleProperty); set => SetValue(ExportTitleProperty, value); }

    /// <summary>The look of the Excel export: a preset (HelixExcelStyle.Bold, Minimal, Grid) or your own (set it in code or bind it).</summary>
    public HelixExcelStyle? ExcelStyle { get => (HelixExcelStyle?)GetValue(ExcelStyleProperty); set => SetValue(ExcelStyleProperty, value); }

    /// <summary>True when a column has an Aggregate: the total row is shown.</summary>
    public bool HasTotals => (bool)GetValue(HasTotalsProperty);

    public static string ExportText => HelixTexts.Current.Export;
    public static string TotalText => HelixTexts.Current.Total;

    /// <summary>Builds the subtotal text of a group header (MultiBinding: group items, grid).</summary>
    public static IMultiValueConverter GroupSummaryConverter { get; } = new GroupSummary();

    private IReadOnlyList<object> _allRows = [];
    private ButtonBase? _export;
    private Panel? _totals;
    private ScrollViewer? _scroller;

    private void InitExtras()
    {
        Columns.CollectionChanged += OnColumnsChanged;
        ColumnDisplayIndexChanged += (_, _) => UpdateTotals();
    }

    // Column options are read when the totals are rebuilt (new columns, new rows, reordering).
    private static void OnColumnOptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // A chevron column handles the row details itself: they no longer open on selection.
        if (Columns.OfType<HelixDetailsColumn>().Any()) SetCurrentValue(RowDetailsVisibilityModeProperty, DataGridRowDetailsVisibilityMode.Collapsed);
        UpdateTotals();
    }

    private void ApplyExtrasTemplate()
    {
        if (_export is not null) _export.Click -= OnExportClick;
        if (_scroller is not null) _scroller.ScrollChanged -= OnScrollChanged;
        _export = GetTemplateChild("PART_Export") as ButtonBase;
        _totals = GetTemplateChild("PART_Totals") as Panel;
        _scroller = GetTemplateChild("DG_ScrollViewer") as ScrollViewer;
        if (_export is not null) _export.Click += OnExportClick;
        if (_scroller is not null) _scroller.ScrollChanged += OnScrollChanged;
        UpdateTotals();
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_totals is not null && e.HorizontalChange != 0) _totals.RenderTransform = new TranslateTransform(-e.HorizontalOffset, 0);
    }

    private IEnumerable Grouped(IReadOnlyList<object> page)
    {
        if (GroupBy is not { Length: > 0 } groupBy) return page;
        var view = new ListCollectionView(page as IList ?? page.ToList());
        view.GroupDescriptions.Add(new PropertyGroupDescription(groupBy));
        if (GroupStyle.Count == 0 && TryFindResource("Helix.GridGroupItem") is Style container)
        {
            GroupStyle.Add(new GroupStyle { ContainerStyle = container });
        }

        return view;
    }

    // ----- Values and formats ------------------------------------------------------------------

    private static string? PathOf(DataGridColumn column) =>
        column is DataGridBoundColumn { Binding: Binding { Path.Path: { Length: > 0 } path } } ? path : column.SortMemberPath is { Length: > 0 } sort ? sort : null;

    private static string? FormatOf(DataGridColumn column) => column switch
    {
        HelixSignedColumn signed => signed.Format,
        DataGridBoundColumn { Binding: BindingBase { StringFormat: { Length: > 0 } format } } => format,
        _ => null,
    };

    private static string Format(object? value, string? format, CultureInfo culture)
    {
        if (value is null) return "";
        if (string.IsNullOrEmpty(format)) return Convert.ToString(value, culture) ?? "";
        try
        {
            return format.Contains("{0") ? string.Format(culture, format, value) : value is IFormattable f ? f.ToString(format, culture) : value.ToString() ?? "";
        }
        catch (FormatException)
        {
            return Convert.ToString(value, culture) ?? "";
        }
    }

    internal static double? Number(object? value) => value switch
    {
        null or bool or string or char or DateTime or DateTimeOffset or Enum => null,
        IConvertible convertible => convertible.ToDouble(CultureInfo.InvariantCulture),
        TimeSpan span => span.TotalMilliseconds,
        _ => null,
    };

    private static string Header(DataGridColumn column) => column.Header?.ToString() ?? "";

    private IEnumerable<DataGridColumn> VisibleInOrder => Columns.Where(c => c.Visibility == Visibility.Visible).OrderBy(c => c.DisplayIndex);

    private string? AggregateText(DataGridColumn column, IEnumerable<object> rows)
    {
        var aggregate = GetAggregate(column);
        if (aggregate == HelixAggregate.None || PathOf(column) is not { } path) return null;
        var culture = CultureInfo.CurrentCulture;
        if (aggregate == HelixAggregate.Count) return rows.Count(r => Value(r, path) is not null).ToString("N0", culture);

        var values = rows.Select(r => Number(Value(r, path))).OfType<double>().ToList();
        if (values.Count == 0) return null;
        double result = aggregate switch
        {
            HelixAggregate.Sum => values.Sum(),
            HelixAggregate.Average => values.Average(),
            HelixAggregate.Min => values.Min(),
            _ => values.Max(),
        };

        var text = Format(result, GetAggregateFormat(column) ?? FormatOf(column) ?? "#,0.##", culture);
        return GetUnit(column) is { Length: > 0 } unit && !text.EndsWith(unit, StringComparison.Ordinal) ? $"{text} {unit}" : text;
    }

    // ----- Total row ---------------------------------------------------------------------------

    private void UpdateTotals()
    {
        var has = Columns.Any(c => GetAggregate(c) != HelixAggregate.None);
        SetValue(HasTotalsKey, has);
        if (_totals is null) return;
        _totals.Children.Clear();
        if (!has) return;

        var first = true;
        foreach (var column in VisibleInOrder)
        {
            var text = AggregateText(column, _allRows);
            var cell = new DockPanel { LastChildFill = true, VerticalAlignment = VerticalAlignment.Center };
            if (first)
            {
                cell.Children.Add(new TextBlock
                {
                    Text = HelixTexts.Current.Total.ToUpper(CultureInfo.CurrentCulture),
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                });
                ((TextBlock)cell.Children[0]).SetResourceReference(TextBlock.ForegroundProperty, HelixKeys.TextMuted);
            }

            cell.Children.Add(new TextBlock
            {
                Text = text ?? "",
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextAlignment = text is not null && GetAggregate(column) != HelixAggregate.Count ? TextAlignment.Right : TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            });

            var border = new Border { Padding = new Thickness(12, 0, 12, 0), Child = cell };
            border.SetBinding(WidthProperty, new Binding(nameof(DataGridColumn.ActualWidth)) { Source = column });
            _totals.Children.Add(border);
            first = false;
        }
    }

    private sealed class GroupSummary : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not IEnumerable items || values[1] is not HelixDataGrid grid) return "";
            var rows = items.Cast<object>().ToList();
            var parts = grid.VisibleInOrder
                .Where(c => GetAggregate(c) is not (HelixAggregate.None or HelixAggregate.Count))
                .Select(c => grid.AggregateText(c, rows) is { } text ? $"{Header(c)} {text}" : null)
                .OfType<string>();
            return string.Join("  ·  ", parts);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    // ----- CSV export --------------------------------------------------------------------------

    /// <summary>The visible columns as CSV, values as displayed (every filtered row; the loaded page in server mode).</summary>
    public string ToCsv()
    {
        var culture = CultureInfo.CurrentCulture;
        var separator = culture.TextInfo.ListSeparator is { Length: > 0 } ls ? ls : ",";
        var columns = VisibleInOrder.Where(c => c is not HelixDetailsColumn && PathOf(c) is not null).ToList();
        string Escape(string value) =>
            value.Contains(separator) || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
                ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(separator, columns.Select(c => Escape(GetUnit(c) is { Length: > 0 } unit ? $"{Header(c)} ({unit})" : Header(c)))));
        foreach (var row in _allRows)
        {
            sb.AppendLine(string.Join(separator, columns.Select(c => Escape(Format(Value(row, PathOf(c)!), FormatOf(c), culture)))));
        }

        return sb.ToString();
    }

    /// <summary>Writes <see cref="ToCsv"/> to a file (UTF-8 with BOM, so that Excel reads accents).</summary>
    public void ExportCsv(string path) => File.WriteAllText(path, ToCsv(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

    /// <summary>
    /// The visible columns and the filtered rows, ready for Helix.Export: typed values (numbers and dates stay numbers and dates
    /// in Excel), binding formats and units, totals from Aggregate.
    /// </summary>
    public HelixExportTable ToExportTable()
    {
        var columns = VisibleInOrder.Where(c => c is not HelixDetailsColumn && PathOf(c) is not null).Select(c =>
        {
            var path = PathOf(c)!;
            var format = FormatOf(c);
            if (format is not null && format.StartsWith("{0:", StringComparison.Ordinal) && format.EndsWith('}') && format.IndexOf('}') == format.Length - 1)
            {
                format = format[3..^1];
            }
            else if (format is not null && format.Contains("{0"))
            {
                format = null; // "{0:N0} MW": keep the value, Excel shows it unformatted
            }

            return new HelixExportColumn
            {
                Header = GetUnit(c) is { Length: > 0 } unit ? $"{Header(c)} ({unit})" : Header(c),
                Value = row => Value(row, path),
                Format = format,
                Total = GetAggregate(c) switch
                {
                    HelixAggregate.Sum => HelixTotal.Sum,
                    HelixAggregate.Average => HelixTotal.Average,
                    HelixAggregate.Min => HelixTotal.Min,
                    HelixAggregate.Max => HelixTotal.Max,
                    HelixAggregate.Count => HelixTotal.Count,
                    _ => HelixTotal.None,
                },
            };
        }).ToList();
        return new HelixExportTable(columns, _allRows.Cast<object>().ToList()) { Title = ExportTitle, TotalLabel = HelixTexts.Current.Total };
    }

    /// <summary>The rows as an Excel, Word or CSV file (bytes), in one format.</summary>
    public byte[] ToFile(HelixExportFormat format) =>
        format == HelixExportFormat.Csv ? [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(ToCsv())] : HelixExport.Write(ToExportTable(), format, ExcelStyle);

    /// <summary>Asks where to save, then writes the rows; without a format, the first of <see cref="ExportFormats"/>.</summary>
    public void ExportFile(HelixExportFormat format = HelixExportFormat.None)
    {
        if (format == HelixExportFormat.None) format = HelixExport.Split(ExportFormats).FirstOrDefault(HelixExportFormat.Csv);
        var extension = HelixExport.Extension(format);
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = (string.IsNullOrWhiteSpace(ExportFileName) ? "export" : ExportFileName) + extension,
            DefaultExt = extension,
            Filter = $"{HelixExport.Label(format)}|*{extension}",
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try
        {
            File.WriteAllBytes(dialog.FileName, ToFile(format));
        }
        catch (IOException ex)
        {
            HelixToasts.Error(ex.Message, HelixTexts.Current.Export);
        }
        catch (UnauthorizedAccessException ex)
        {
            HelixToasts.Error(ex.Message, HelixTexts.Current.Export);
        }
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        var formats = HelixExport.Split(ExportFormats);
        if (formats.Count <= 1 || sender is not FrameworkElement button)
        {
            ExportFile();
            return;
        }

        var menu = new ContextMenu { PlacementTarget = button, Placement = PlacementMode.Bottom };
        foreach (var format in formats)
        {
            var item = new MenuItem { Header = HelixExport.Label(format) };
            item.Click += (_, _) => ExportFile(format);
            menu.Items.Add(item);
        }

        menu.IsOpen = true;
    }
}
