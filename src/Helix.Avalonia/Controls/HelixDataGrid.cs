using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Helix.Export;

namespace Helix.Avalonia;

/// <summary>What a server-side grid asks for: one page, already searched and sorted.</summary>
public sealed record HelixGridRequest(string? Search, string? SortBy, bool Descending, int Page, int PageSize);

/// <summary>One page of server data and the total number of matching rows.</summary>
public sealed record HelixGridPage(IReadOnlyList<object> Items, int TotalCount);

/// <summary>
/// A DataGrid with the Helix look, a search box, paging, a column chooser, density, groups, a totals row,
/// export (Excel, Word, CSV) and states (loading, empty, error).
/// Client mode: bind <see cref="Source"/>; search, sort and paging run in memory.
/// Server mode: set <see cref="ItemsProvider"/>; it receives each request (cancelled when a newer one starts).
/// Columns are ordinary DataGrid columns (DataGridTextColumn, DataGridTemplateColumn…) in <see cref="Columns"/>.
/// </summary>
[TemplatePart("PART_Grid", typeof(DataGrid))]
[TemplatePart("PART_Totals", typeof(Panel))]
[TemplatePart("PART_Export", typeof(Button))]
[TemplatePart("PART_Columns", typeof(Button))]
[TemplatePart("PART_Retry", typeof(Button))]
[TemplatePart("PART_Previous", typeof(Button))]
[TemplatePart("PART_Next", typeof(Button))]
public class HelixDataGrid : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> SourceProperty = AvaloniaProperty.Register<HelixDataGrid, IEnumerable?>(nameof(Source));
    public static readonly StyledProperty<Func<HelixGridRequest, CancellationToken, Task<HelixGridPage>>?> ItemsProviderProperty =
        AvaloniaProperty.Register<HelixDataGrid, Func<HelixGridRequest, CancellationToken, Task<HelixGridPage>>?>(nameof(ItemsProvider));
    public static readonly StyledProperty<bool> IsSearchableProperty = AvaloniaProperty.Register<HelixDataGrid, bool>(nameof(IsSearchable));
    public static readonly StyledProperty<string?> SearchTextProperty = AvaloniaProperty.Register<HelixDataGrid, string?>(nameof(SearchText), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<TimeSpan> SearchDelayProperty = AvaloniaProperty.Register<HelixDataGrid, TimeSpan>(nameof(SearchDelay), TimeSpan.FromMilliseconds(300));
    public static readonly StyledProperty<string?> SearchPlaceholderProperty = AvaloniaProperty.Register<HelixDataGrid, string?>(nameof(SearchPlaceholder));
    public static readonly StyledProperty<int> PageSizeProperty = AvaloniaProperty.Register<HelixDataGrid, int>(nameof(PageSize), 10);
    public static readonly StyledProperty<int> PageProperty = AvaloniaProperty.Register<HelixDataGrid, int>(nameof(Page), 1, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<HelixSize> DensityProperty = AvaloniaProperty.Register<HelixDataGrid, HelixSize>(nameof(Density), HelixSize.Md);
    public static readonly StyledProperty<bool> IsStripedProperty = AvaloniaProperty.Register<HelixDataGrid, bool>(nameof(IsStriped));
    public static readonly StyledProperty<string?> EmptyTextProperty = AvaloniaProperty.Register<HelixDataGrid, string?>(nameof(EmptyText));
    public static readonly StyledProperty<bool> ShowColumnChooserProperty = AvaloniaProperty.Register<HelixDataGrid, bool>(nameof(ShowColumnChooser));
    public static readonly StyledProperty<object?> ToolbarProperty = AvaloniaProperty.Register<HelixDataGrid, object?>(nameof(Toolbar));
    public static readonly StyledProperty<string?> GroupByProperty = AvaloniaProperty.Register<HelixDataGrid, string?>(nameof(GroupBy));
    public static readonly StyledProperty<Predicate<object>?> FilterProperty = AvaloniaProperty.Register<HelixDataGrid, Predicate<object>?>(nameof(Filter));
    public static readonly StyledProperty<IDataTemplate?> RowDetailsTemplateProperty = AvaloniaProperty.Register<HelixDataGrid, IDataTemplate?>(nameof(RowDetailsTemplate));
    public static readonly StyledProperty<object?> SelectedItemProperty = AvaloniaProperty.Register<HelixDataGrid, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<bool> IsExportableProperty = AvaloniaProperty.Register<HelixDataGrid, bool>(nameof(IsExportable));
    public static readonly StyledProperty<string> ExportFileNameProperty = AvaloniaProperty.Register<HelixDataGrid, string>(nameof(ExportFileName), "export");
    public static readonly StyledProperty<HelixExportFormat> ExportFormatsProperty = AvaloniaProperty.Register<HelixDataGrid, HelixExportFormat>(nameof(ExportFormats), HelixExportFormat.Csv);
    public static readonly StyledProperty<string?> ExportTitleProperty = AvaloniaProperty.Register<HelixDataGrid, string?>(nameof(ExportTitle));
    public static readonly StyledProperty<HelixExcelStyle?> ExcelStyleProperty = AvaloniaProperty.Register<HelixDataGrid, HelixExcelStyle?>(nameof(ExcelStyle));

    public static readonly DirectProperty<HelixDataGrid, int> TotalCountProperty = AvaloniaProperty.RegisterDirect<HelixDataGrid, int>(nameof(TotalCount), g => g.TotalCount);
    public static readonly DirectProperty<HelixDataGrid, bool> IsLoadingProperty = AvaloniaProperty.RegisterDirect<HelixDataGrid, bool>(nameof(IsLoading), g => g.IsLoading);
    public static readonly DirectProperty<HelixDataGrid, string?> ErrorProperty = AvaloniaProperty.RegisterDirect<HelixDataGrid, string?>(nameof(Error), g => g.Error);
    public static readonly DirectProperty<HelixDataGrid, string?> InfoTextProperty = AvaloniaProperty.RegisterDirect<HelixDataGrid, string?>(nameof(InfoText), g => g.InfoText);
    public static readonly DirectProperty<HelixDataGrid, string?> PageTextProperty = AvaloniaProperty.RegisterDirect<HelixDataGrid, string?>(nameof(PageText), g => g.PageText);
    public static readonly DirectProperty<HelixDataGrid, bool> IsEmptyProperty = AvaloniaProperty.RegisterDirect<HelixDataGrid, bool>(nameof(IsEmpty), g => g.IsEmpty);
    public static readonly DirectProperty<HelixDataGrid, string?> EmptyMessageProperty = AvaloniaProperty.RegisterDirect<HelixDataGrid, string?>(nameof(EmptyMessage), g => g.EmptyMessage);
    public static readonly DirectProperty<HelixDataGrid, bool> HasPagerProperty = AvaloniaProperty.RegisterDirect<HelixDataGrid, bool>(nameof(HasPager), g => g.HasPager);
    public static readonly DirectProperty<HelixDataGrid, bool> HasTotalsProperty = AvaloniaProperty.RegisterDirect<HelixDataGrid, bool>(nameof(HasTotals), g => g.HasTotals);
    public static readonly DirectProperty<HelixDataGrid, bool> HasToolbarProperty = AvaloniaProperty.RegisterDirect<HelixDataGrid, bool>(nameof(HasToolbar), g => g.HasToolbar);

    /// <summary>Total of a column, in the totals row: Sum, Average, Min, Max or Count.</summary>
    public static readonly AttachedProperty<HelixAggregate> AggregateProperty = AvaloniaProperty.RegisterAttached<HelixDataGrid, DataGridColumn, HelixAggregate>("Aggregate");

    /// <summary>Unit of a column, added after its total and to the export header, e.g. "MW".</summary>
    public static readonly AttachedProperty<string?> UnitProperty = AvaloniaProperty.RegisterAttached<HelixDataGrid, DataGridColumn, string?>("Unit");

    /// <summary>.NET format of a column's values and total, e.g. "N1" (also used by the export).</summary>
    public static readonly AttachedProperty<string?> FormatProperty = AvaloniaProperty.RegisterAttached<HelixDataGrid, DataGridColumn, string?>("Format");

    public static HelixAggregate GetAggregate(DataGridColumn column) => column.GetValue(AggregateProperty);
    public static void SetAggregate(DataGridColumn column, HelixAggregate value) => column.SetValue(AggregateProperty, value);
    public static string? GetUnit(DataGridColumn column) => column.GetValue(UnitProperty);
    public static void SetUnit(DataGridColumn column, string? value) => column.SetValue(UnitProperty, value);
    public static string? GetFormat(DataGridColumn column) => column.GetValue(FormatProperty);
    public static void SetFormat(DataGridColumn column, string? value) => column.SetValue(FormatProperty, value);

    private readonly DispatcherTimer _searchTimer;
    private CancellationTokenSource? _cts;
    private string? _sortBy;
    private bool _descending;
    private DataGrid? _grid;
    private Panel? _totals;
    private Button? _export, _columns, _retry, _previous, _next;
    private IReadOnlyList<object> _allRows = [];
    private int _totalCount;
    private bool _isLoading, _isEmpty, _hasPager, _hasTotals, _hasToolbar;
    private string? _error, _infoText, _pageText, _emptyMessage;

    public HelixDataGrid()
    {
        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchTimer.Tick += (_, _) =>
        {
            _searchTimer.Stop();
            SetCurrentValue(PageProperty, 1);
            Reload();
        };
        Columns.CollectionChanged += OnColumnsChanged;
        UpdateToolbar();
    }

    /// <summary>The columns, as in a DataGrid. Set SortMemberPath when a column's binding is not a plain property path.</summary>
    public ObservableCollection<DataGridColumn> Columns { get; } = [];

    /// <summary>All rows (client mode); search, sort, groups and paging run in memory.</summary>
    public IEnumerable? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    /// <summary>Server mode: returns one page for a request. Exceptions show an error with a Retry button.</summary>
    public Func<HelixGridRequest, CancellationToken, Task<HelixGridPage>>? ItemsProvider { get => GetValue(ItemsProviderProperty); set => SetValue(ItemsProviderProperty, value); }

    public bool IsSearchable { get => GetValue(IsSearchableProperty); set => SetValue(IsSearchableProperty, value); }
    public string? SearchText { get => GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public string? SearchPlaceholder { get => GetValue(SearchPlaceholderProperty); set => SetValue(SearchPlaceholderProperty, value); }

    /// <summary>Pause after the last key stroke before searching (300 ms); zero searches at once.</summary>
    public TimeSpan SearchDelay { get => GetValue(SearchDelayProperty); set => SetValue(SearchDelayProperty, value); }

    /// <summary>Rows per page; 0 shows everything (no pager).</summary>
    public int PageSize { get => GetValue(PageSizeProperty); set => SetValue(PageSizeProperty, value); }

    /// <summary>Current page, 1-based, two-way.</summary>
    public int Page { get => GetValue(PageProperty); set => SetValue(PageProperty, value); }

    /// <summary>Row height: Sm 32, Md 40, Lg 48.</summary>
    public HelixSize Density { get => GetValue(DensityProperty); set => SetValue(DensityProperty, value); }
    public bool IsStriped { get => GetValue(IsStripedProperty); set => SetValue(IsStripedProperty, value); }
    public string? EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    /// <summary>A "Columns" button to show or hide columns.</summary>
    public bool ShowColumnChooser { get => GetValue(ShowColumnChooserProperty); set => SetValue(ShowColumnChooserProperty, value); }

    /// <summary>Your content on the left of the toolbar (title, filters, buttons).</summary>
    public object? Toolbar { get => GetValue(ToolbarProperty); set => SetValue(ToolbarProperty, value); }

    /// <summary>Property path that groups the rows (e.g. "Region"): collapsible group headers.</summary>
    public string? GroupBy { get => GetValue(GroupByProperty); set => SetValue(GroupByProperty, value); }

    /// <summary>Keeps only the rows it accepts (client mode, before search and paging).</summary>
    public Predicate<object>? Filter { get => GetValue(FilterProperty); set => SetValue(FilterProperty, value); }
    public IDataTemplate? RowDetailsTemplate { get => GetValue(RowDetailsTemplateProperty); set => SetValue(RowDetailsTemplateProperty, value); }
    public object? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

    /// <summary>An "Export" button in the toolbar, in <see cref="ExportFormats"/>.</summary>
    public bool IsExportable { get => GetValue(IsExportableProperty); set => SetValue(IsExportableProperty, value); }
    public string ExportFileName { get => GetValue(ExportFileNameProperty); set => SetValue(ExportFileNameProperty, value); }

    /// <summary>Formats offered by the Export button: Csv (default), Excel, Word, or several (a menu), e.g. "Excel, Csv" or "All".</summary>
    public HelixExportFormat ExportFormats { get => GetValue(ExportFormatsProperty); set => SetValue(ExportFormatsProperty, value); }

    /// <summary>Title of the Word document and of the Excel sheet.</summary>
    public string? ExportTitle { get => GetValue(ExportTitleProperty); set => SetValue(ExportTitleProperty, value); }

    /// <summary>The look of the Excel file (HelixExcelStyle.Bold, Minimal, Grid or your own).</summary>
    public HelixExcelStyle? ExcelStyle { get => GetValue(ExcelStyleProperty); set => SetValue(ExcelStyleProperty, value); }

    /// <summary>Rows matching the search (all pages).</summary>
    public int TotalCount { get => _totalCount; private set => SetAndRaise(TotalCountProperty, ref _totalCount, value); }
    public bool IsLoading { get => _isLoading; private set => SetAndRaise(IsLoadingProperty, ref _isLoading, value); }
    public string? Error { get => _error; private set => SetAndRaise(ErrorProperty, ref _error, value); }

    /// <summary>"11–20 of 87".</summary>
    public string? InfoText { get => _infoText; private set => SetAndRaise(InfoTextProperty, ref _infoText, value); }

    /// <summary>"2 / 9".</summary>
    public string? PageText { get => _pageText; private set => SetAndRaise(PageTextProperty, ref _pageText, value); }
    public bool IsEmpty { get => _isEmpty; private set => SetAndRaise(IsEmptyProperty, ref _isEmpty, value); }
    public string? EmptyMessage { get => _emptyMessage; private set => SetAndRaise(EmptyMessageProperty, ref _emptyMessage, value); }
    public bool HasPager { get => _hasPager; private set => SetAndRaise(HasPagerProperty, ref _hasPager, value); }
    public bool HasTotals { get => _hasTotals; private set => SetAndRaise(HasTotalsProperty, ref _hasTotals, value); }
    public bool HasToolbar { get => _hasToolbar; private set => SetAndRaise(HasToolbarProperty, ref _hasToolbar, value); }

    public static string ExportText => HelixTexts.Current.Export;
    public static string ColumnsText => HelixTexts.Current.Columns;
    public static string RetryText => HelixTexts.Current.Retry;
    public static string SearchPlaceholderText => HelixTexts.Current.Search;
    public static string EmptyTextDefault => HelixTexts.Current.NoData;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_grid is not null)
        {
            _grid.Sorting -= OnSorting;
            _grid.LayoutUpdated -= OnGridLayout;
            _grid.Columns.Clear();
        }

        if (_export is not null) _export.Click -= OnExportClick;
        if (_columns is not null) _columns.Click -= OnColumnsClick;
        if (_retry is not null) _retry.Click -= OnRetry;
        if (_previous is not null) _previous.Click -= OnPrevious;
        if (_next is not null) _next.Click -= OnNext;
        base.OnApplyTemplate(e);
        _grid = e.NameScope.Find<DataGrid>("PART_Grid");
        _totals = e.NameScope.Find<Panel>("PART_Totals");
        _export = e.NameScope.Find<Button>("PART_Export");
        _columns = e.NameScope.Find<Button>("PART_Columns");
        _retry = e.NameScope.Find<Button>("PART_Retry");
        _previous = e.NameScope.Find<Button>("PART_Previous");
        _next = e.NameScope.Find<Button>("PART_Next");
        if (_grid is not null)
        {
            foreach (var column in Columns) _grid.Columns.Add(column);
            // With a details column, details open only with its chevron, not on selection.
            if (Columns.OfType<HelixDetailsColumn>().Any()) _grid.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
            _grid.Sorting += OnSorting;
            _grid.LayoutUpdated += OnGridLayout;
            ApplyDensity();
        }

        if (_export is not null) _export.Click += OnExportClick;
        if (_columns is not null) _columns.Click += OnColumnsClick;
        if (_retry is not null) _retry.Click += OnRetry;
        if (_previous is not null) _previous.Click += OnPrevious;
        if (_next is not null) _next.Click += OnNext;
        Reload();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == SourceProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= OnSourceCollectionChanged;
            if (change.NewValue is INotifyCollectionChanged newList) newList.CollectionChanged += OnSourceCollectionChanged;
            SetCurrentValue(PageProperty, 1);
            Reload();
        }
        else if (p == SearchTextProperty)
        {
            _searchTimer.Stop();
            if (SearchDelay <= TimeSpan.Zero)
            {
                SetCurrentValue(PageProperty, 1);
                Reload();
            }
            else
            {
                _searchTimer.Interval = SearchDelay;
                _searchTimer.Start();
            }
        }
        else if (p == PageSizeProperty || p == FilterProperty || p == GroupByProperty)
        {
            SetCurrentValue(PageProperty, 1);
            Reload();
        }
        else if (p == PageProperty || p == ItemsProviderProperty)
        {
            Reload();
        }
        else if (p == DensityProperty)
        {
            ApplyDensity();
        }
        else if (p == IsSearchableProperty || p == IsExportableProperty || p == ShowColumnChooserProperty || p == ToolbarProperty)
        {
            UpdateToolbar();
        }

        if (p == IsStripedProperty) PseudoClasses.Set(":striped", IsStriped);
    }

    private void UpdateToolbar() => HasToolbar = IsSearchable || IsExportable || ShowColumnChooser || Toolbar is not null;

    private void ApplyDensity()
    {
        if (_grid is null) return;
        _grid.RowHeight = Density switch { HelixSize.Sm => 32, HelixSize.Lg => 48, _ => 40 };
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_grid is null) return;
        foreach (var c in e.OldItems?.OfType<DataGridColumn>() ?? []) _grid.Columns.Remove(c);
        foreach (var c in e.NewItems?.OfType<DataGridColumn>() ?? []) _grid.Columns.Add(c);
        UpdateTotals();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Reload();

    private void OnRetry(object? sender, RoutedEventArgs e) => Reload();

    private void OnPrevious(object? sender, RoutedEventArgs e) => SetCurrentValue(PageProperty, Math.Max(1, Page - 1));

    private void OnNext(object? sender, RoutedEventArgs e) => SetCurrentValue(PageProperty, Page + 1);

    private void OnColumnsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control button) return;
        var list = new StackPanel { Spacing = 6, Margin = new Thickness(4) };
        foreach (var column in Columns)
        {
            var box = new HelixCheckBox { Content = Header(column), IsChecked = column.IsVisible };
            var c = column;
            box.IsCheckedChanged += (_, _) =>
            {
                // Keep at least one column visible.
                if (box.IsChecked == true) c.IsVisible = true;
                else if (Columns.Count(col => col.IsVisible) > 1) c.IsVisible = false;
                else box.IsChecked = true;
                UpdateTotals();
            };
            list.Children.Add(box);
        }

        var flyout = new Flyout { Content = list, Placement = PlacementMode.BottomEdgeAlignedRight };
        flyout.ShowAt(button);
    }

    /// <summary>Sorting runs here (across all pages), not inside the current page only.</summary>
    private void OnSorting(object? sender, DataGridColumnEventArgs e)
    {
        if (Source is null && ItemsProvider is null) return;
        e.Handled = true;
        if (PathOf(e.Column) is not { } member) return;
        _descending = _sortBy == member && !_descending;
        _sortBy = member;
        Reload();
    }

    /// <summary>Runs the search, sort and paging again (e.g. after changing the source list in place).</summary>
    public void Reload()
    {
        if (ItemsProvider is not null)
        {
            _ = LoadServerAsync();
            return;
        }

        if (Source is null) return;
        IEnumerable<object> rows = Source.Cast<object>();
        if (Filter is { } filter) rows = rows.Where(r => filter(r));

        var search = SearchText?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var paths = Columns.Where(c => c.IsVisible).Select(PathOf).OfType<string>().Distinct().ToList();
            rows = rows.Where(row => paths.Any(path => Value(row, path)?.ToString()?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true));
        }

        if (GroupBy is { Length: > 0 } groupBy)
        {
            // Groups stay together; the sort applies inside each group.
            var grouped = rows.OrderBy(r => Value(r, groupBy), Comparer.Default);
            if (_sortBy is { } by) grouped = _descending ? grouped.ThenByDescending(r => Value(r, by), Comparer.Default) : grouped.ThenBy(r => Value(r, by), Comparer.Default);
            rows = grouped;
        }
        else if (_sortBy is { } sortBy)
        {
            rows = _descending ? rows.OrderByDescending(r => Value(r, sortBy), Comparer.Default) : rows.OrderBy(r => Value(r, sortBy), Comparer.Default);
        }

        var all = rows.ToList();
        _allRows = all;
        var page = ClampPage(all.Count);
        Show(all.Count, PageSize > 0 ? all.Skip((page - 1) * PageSize).Take(PageSize).ToList() : all, search);
    }

    private sealed class Comparer : IComparer<object?>
    {
        public static readonly Comparer Default = new();

        public int Compare(object? x, object? y) => (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            (IComparable a, _) when x.GetType() == y.GetType() => a.CompareTo(y),
            _ => string.Compare(x.ToString(), y.ToString(), StringComparison.CurrentCultureIgnoreCase),
        };
    }

    private int ClampPage(int total)
    {
        var pages = PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)) : 1;
        var page = Math.Clamp(Page, 1, pages);
        if (page != Page) SetCurrentValue(PageProperty, page);
        return page;
    }

    private async Task LoadServerAsync()
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;
        IsLoading = true;
        Error = null;
        try
        {
            var request = new HelixGridRequest(string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(), _sortBy, _descending, Math.Max(1, Page), PageSize);
            var page = await ItemsProvider!(request, cts.Token);
            if (cts.IsCancellationRequested) return;
            _allRows = page.Items;
            Show(page.TotalCount, page.Items, request.Search);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // A newer request replaced this one.
        }
        catch (Exception ex)
        {
            if (!cts.IsCancellationRequested)
            {
                Error = HelixTexts.Current.LoadError;
                if (_grid is not null) _grid.ItemsSource = Array.Empty<object>();
                System.Diagnostics.Trace.TraceError($"HelixDataGrid: {ex}");
            }
        }
        finally
        {
            if (_cts == cts) IsLoading = false;
        }
    }

    private void Show(int total, IReadOnlyList<object> page, string? search)
    {
        if (_grid is not null)
        {
            // A view of the page: shows the sort arrow on the sorted column and the groups.
            var view = new DataGridCollectionView(page as IList ?? page.ToList());
            if (_sortBy is { } by)
            {
                if (GroupBy is { Length: > 0 } g) view.SortDescriptions.Add(DataGridSortDescription.FromPath(g, ListSortDirection.Ascending));
                view.SortDescriptions.Add(DataGridSortDescription.FromPath(by, _descending ? ListSortDirection.Descending : ListSortDirection.Ascending));
            }

            if (GroupBy is { Length: > 0 } groupBy) view.GroupDescriptions.Add(new DataGridPathGroupDescription(groupBy));
            view.MoveCurrentToPosition(-1); // no row selected until the user picks one
            _grid.ItemsSource = view;
            if (SelectedItem is { } selected && page.Contains(selected)) _grid.SelectedItem = selected;
        }

        TotalCount = total;
        IsEmpty = page.Count == 0;
        EmptyMessage = string.IsNullOrEmpty(search) ? EmptyText ?? HelixTexts.Current.NoData : HelixTexts.Format(HelixTexts.Current.NoResultsFor, search);
        var pages = PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)) : 1;
        HasPager = PageSize > 0 && total > PageSize;
        var first = total == 0 ? 0 : (Math.Max(1, Page) - 1) * Math.Max(PageSize, 0) + 1;
        var last = PageSize > 0 ? Math.Min(total, first + PageSize - 1) : total;
        InfoText = HelixTexts.Format(HelixTexts.Current.RangeOf, first, last, total);
        PageText = $"{Math.Clamp(Page, 1, pages)} / {pages}";
        if (_previous is not null) _previous.IsEnabled = Page > 1;
        if (_next is not null) _next.IsEnabled = Page < pages;
        UpdateTotals();
    }

    // ----- Values, formats and totals ----------------------------------------------------------

    /// <summary>The property path of a column: SortMemberPath, else the path of its binding.</summary>
    internal static string? PathOf(DataGridColumn column)
    {
        if (column.SortMemberPath is { Length: > 0 } sort) return sort;
        if (column is not DataGridBoundColumn { Binding: { } binding }) return null;
        var path = binding.GetType().GetProperty("Path")?.GetValue(binding)?.ToString();
        return string.IsNullOrWhiteSpace(path) || path == "." ? null : path;
    }

    private static string? FormatOf(DataGridColumn column)
    {
        if (GetFormat(column) is { Length: > 0 } own) return own;
        if (column is not DataGridBoundColumn { Binding: { } binding }) return null;
        var format = binding.GetType().GetProperty("StringFormat")?.GetValue(binding) as string;
        if (format is null) return null;
        // "{0:N1}" → "N1"; a format with text around keeps the value unformatted for totals and export.
        return format.StartsWith("{0:", StringComparison.Ordinal) && format.EndsWith('}') && format.IndexOf('}') == format.Length - 1 ? format[3..^1] : null;
    }

    private static string Header(DataGridColumn column) => column.Header?.ToString() ?? "";

    private static readonly Dictionary<(Type, string), PropertyInfo?[]> Paths = [];

    /// <summary>Reads a property path ("Customer.Name") with cached reflection.</summary>
    internal static object? Value(object? row, string path)
    {
        if (row is null) return null;
        PropertyInfo?[] chain;
        lock (Paths)
        {
            if (!Paths.TryGetValue((row.GetType(), path), out chain!))
            {
                var type = row.GetType();
                var list = new List<PropertyInfo?>();
                foreach (var name in path.Split('.'))
                {
                    var property = type?.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    list.Add(property);
                    type = property?.PropertyType;
                }

                chain = list.ToArray();
                Paths[(row.GetType(), path)] = chain;
            }
        }

        object? value = row;
        foreach (var property in chain)
        {
            if (value is null || property is null) return null;
            value = property.GetValue(value);
        }

        return value;
    }

    internal static double? Number(object? value) => value switch
    {
        null or bool or string or char or DateTime or DateTimeOffset or Enum => null,
        IConvertible convertible => convertible.ToDouble(CultureInfo.InvariantCulture),
        _ => null,
    };

    private IEnumerable<DataGridColumn> VisibleInOrder => Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayIndex < 0 ? Columns.IndexOf(c) : c.DisplayIndex);

    private string? AggregateText(DataGridColumn column, IEnumerable<object> rows)
    {
        var aggregate = GetAggregate(column);
        if (aggregate == HelixAggregate.None || PathOf(column) is not { } path) return null;
        var culture = CultureInfo.CurrentCulture;
        if (aggregate == HelixAggregate.Count) return rows.Count(r => Value(r, path) is not null).ToString("N0", culture);
        var values = rows.Select(r => Number(Value(r, path))).OfType<double>().ToList();
        if (values.Count == 0) return null;
        var result = aggregate switch
        {
            HelixAggregate.Sum => values.Sum(),
            HelixAggregate.Average => values.Average(),
            HelixAggregate.Min => values.Min(),
            _ => values.Max(),
        };
        var text = result.ToString(FormatOf(column) ?? "#,0.##", culture);
        return GetUnit(column) is { Length: > 0 } unit ? $"{text} {unit}" : text;
    }

    private void UpdateTotals()
    {
        HasTotals = Columns.Any(c => GetAggregate(c) != HelixAggregate.None);
        if (_totals is null) return;
        _totals.Children.Clear();
        if (!HasTotals) return;
        var first = true;
        foreach (var column in VisibleInOrder)
        {
            var text = AggregateText(column, _allRows);
            var cell = new DockPanel { VerticalAlignment = VerticalAlignment.Center };
            if (first)
            {
                var label = new TextBlock
                {
                    Text = HelixTexts.Current.Total.ToUpper(CultureInfo.CurrentCulture), FontSize = 11.5, FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center,
                };
                label.Bind(TextBlock.ForegroundProperty, label.GetResourceObservable(HelixKeys.TextMuted));
                cell.Children.Add(label);
            }

            cell.Children.Add(new TextBlock
            {
                Text = text ?? "", FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = text is not null && GetAggregate(column) != HelixAggregate.Count ? TextAlignment.Right : TextAlignment.Left,
            });
            _totals.Children.Add(new Border { Padding = new Thickness(12, 0), Child = cell, Width = Math.Max(0, column.ActualWidth), Tag = column });
            first = false;
        }
    }

    /// <summary>Keeps the totals cells as wide as their column.</summary>
    private void OnGridLayout(object? sender, EventArgs e)
    {
        if (_totals is null) return;
        foreach (var border in _totals.Children.OfType<Border>())
        {
            if (border.Tag is DataGridColumn column && Math.Abs(border.Width - column.ActualWidth) > 0.5) border.Width = column.ActualWidth;
        }
    }

    // ----- Export ------------------------------------------------------------------------------

    /// <summary>The visible columns as CSV, values as displayed (every filtered row; the loaded page in server mode).</summary>
    public string ToCsv() => HelixCsv.Write(ToExportTable());

    /// <summary>
    /// The visible columns and the filtered rows, ready for Helix.Export: typed values (numbers and dates stay numbers and dates
    /// in Excel), formats and units, totals from Aggregate.
    /// </summary>
    public HelixExportTable ToExportTable()
    {
        var columns = VisibleInOrder.Where(c => PathOf(c) is not null).Select(c =>
        {
            var path = PathOf(c)!;
            return new HelixExportColumn
            {
                Header = GetUnit(c) is { Length: > 0 } unit ? $"{Header(c)} ({unit})" : Header(c),
                Value = row => Value(row, path),
                Format = FormatOf(c),
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
        return new HelixExportTable(columns, _allRows.ToList()) { Title = ExportTitle, TotalLabel = HelixTexts.Current.Total };
    }

    /// <summary>The rows as an Excel, Word or CSV file (bytes), in one format.</summary>
    public byte[] ToFile(HelixExportFormat format) => HelixExport.Write(ToExportTable(), format, ExcelStyle);

    /// <summary>Asks where to save, then writes the rows; without a format, the first of <see cref="ExportFormats"/>.</summary>
    public async Task ExportFileAsync(HelixExportFormat format = HelixExportFormat.None)
    {
        if (format == HelixExportFormat.None) format = HelixExport.Split(ExportFormats).FirstOrDefault(HelixExportFormat.Csv);
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage) return;
        var extension = HelixExport.Extension(format);
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = (string.IsNullOrWhiteSpace(ExportFileName) ? "export" : ExportFileName) + extension,
            DefaultExtension = extension,
            FileTypeChoices = [new FilePickerFileType(HelixExport.Label(format)) { Patterns = ["*" + extension] }],
        });
        if (file is null) return;
        try
        {
            await using var stream = await file.OpenWriteAsync();
            var bytes = ToFile(format);
            await stream.WriteAsync(bytes);
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

    private void OnExportClick(object? sender, RoutedEventArgs e)
    {
        var formats = HelixExport.Split(ExportFormats);
        if (formats.Count <= 1 || sender is not Control button)
        {
            _ = ExportFileAsync();
            return;
        }

        var menu = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedRight };
        foreach (var format in formats)
        {
            var item = new MenuItem { Header = HelixExport.Label(format) };
            var f = format;
            item.Click += (_, _) => _ = ExportFileAsync(f);
            menu.Items.Add(item);
        }

        menu.ShowAt(button);
    }
}
