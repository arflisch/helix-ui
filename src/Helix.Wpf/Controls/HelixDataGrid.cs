using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;

namespace Helix.Wpf;

/// <summary>What a server-side grid asks for: one page, already searched and sorted.</summary>
public sealed record HelixGridRequest(string? Search, string? SortBy, bool Descending, int Page, int PageSize);

/// <summary>One page of server data and the total number of matching rows.</summary>
public sealed record HelixGridPage(IReadOnlyList<object> Items, int TotalCount);

/// <summary>
/// A DataGrid with the Helix look, a search box, paging, a column chooser, density, stripes and states (loading, empty, error).
/// Client mode: bind <see cref="Source"/>; search, sort and paging run in memory.
/// Server mode: set <see cref="ItemsProvider"/>; it receives each request (cancelled when a newer one starts).
/// Columns work as in a DataGrid (DataGridTextColumn, DataGridTemplateColumn…).
/// </summary>
[TemplatePart(Name = "PART_Columns", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_ColumnPopup", Type = typeof(Popup))]
[TemplatePart(Name = "PART_ColumnList", Type = typeof(Panel))]
[TemplatePart(Name = "PART_Retry", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_Export", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_Totals", Type = typeof(Panel))]
public partial class HelixDataGrid : DataGrid
{
    static HelixDataGrid() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixDataGrid), new FrameworkPropertyMetadata(typeof(HelixDataGrid)));

    public HelixDataGrid()
    {
        SetCurrentValue(SearchPlaceholderProperty, HelixTexts.Current.Search);
        SetCurrentValue(EmptyTextProperty, HelixTexts.Current.NoData);
        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchTimer.Tick += (_, _) =>
        {
            _searchTimer.Stop();
            SetCurrentValue(PageProperty, 1);
            Reload();
        };
        Unloaded += (_, _) => _cts?.Cancel();
        InitExtras();
    }

    public static readonly DependencyProperty SourceProperty = HelixProperty.Register<HelixDataGrid, IEnumerable?>(nameof(Source), null, (d, e) => ((HelixDataGrid)d).OnSourceChanged(e));
    public static readonly DependencyProperty ItemsProviderProperty = HelixProperty.Register<HelixDataGrid, Func<HelixGridRequest, CancellationToken, Task<HelixGridPage>>?>(nameof(ItemsProvider), null, (d, _) => ((HelixDataGrid)d).Reload());
    public static readonly DependencyProperty IsSearchableProperty = HelixProperty.Register<HelixDataGrid, bool>(nameof(IsSearchable), false);
    public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(HelixDataGrid),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HelixDataGrid)d).OnSearchChanged()));
    public static readonly DependencyProperty SearchPlaceholderProperty = HelixProperty.Register<HelixDataGrid, string?>(nameof(SearchPlaceholder), null);
    public static readonly DependencyProperty PageSizeProperty = HelixProperty.Register<HelixDataGrid, int>(nameof(PageSize), 10, (d, _) => ((HelixDataGrid)d).OnPagingChanged());
    public static readonly DependencyProperty PageSizesProperty = HelixProperty.Register<HelixDataGrid, IList<int>?>(nameof(PageSizes), new List<int> { 10, 25, 50 });
    public static readonly DependencyProperty PageProperty = DependencyProperty.Register(nameof(Page), typeof(int), typeof(HelixDataGrid),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HelixDataGrid)d).Reload()));
    public static readonly DependencyProperty DensityProperty = HelixProperty.Register<HelixDataGrid, HelixSize>(nameof(Density), HelixSize.Md);
    public static readonly DependencyProperty IsStripedProperty = HelixProperty.Register<HelixDataGrid, bool>(nameof(IsStriped), false, (d, e) => ((HelixDataGrid)d).AlternationCount = (bool)e.NewValue ? 2 : 0);
    public static readonly DependencyProperty EmptyTextProperty = HelixProperty.Register<HelixDataGrid, string?>(nameof(EmptyText), null);
    public static readonly DependencyProperty ShowColumnChooserProperty = HelixProperty.Register<HelixDataGrid, bool>(nameof(ShowColumnChooser), false);
    public static readonly DependencyProperty ToolbarProperty = HelixProperty.Register<HelixDataGrid, object?>(nameof(Toolbar), null);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixDataGrid, CornerRadius>(nameof(CornerRadius), new CornerRadius(14));

    private static readonly DependencyPropertyKey TotalCountKey = DependencyProperty.RegisterReadOnly(nameof(TotalCount), typeof(int), typeof(HelixDataGrid), new PropertyMetadata(0));
    private static readonly DependencyPropertyKey IsLoadingKey = DependencyProperty.RegisterReadOnly(nameof(IsLoading), typeof(bool), typeof(HelixDataGrid), new PropertyMetadata(false));
    private static readonly DependencyPropertyKey ErrorKey = DependencyProperty.RegisterReadOnly(nameof(Error), typeof(string), typeof(HelixDataGrid), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey InfoTextKey = DependencyProperty.RegisterReadOnly(nameof(InfoText), typeof(string), typeof(HelixDataGrid), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey IsEmptyKey = DependencyProperty.RegisterReadOnly(nameof(IsEmpty), typeof(bool), typeof(HelixDataGrid), new PropertyMetadata(false));
    private static readonly DependencyPropertyKey EmptyMessageKey = DependencyProperty.RegisterReadOnly(nameof(EmptyMessage), typeof(string), typeof(HelixDataGrid), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey HasPagerKey = DependencyProperty.RegisterReadOnly(nameof(HasPager), typeof(bool), typeof(HelixDataGrid), new PropertyMetadata(false));
    public static readonly DependencyProperty TotalCountProperty = TotalCountKey.DependencyProperty;
    public static readonly DependencyProperty IsLoadingProperty = IsLoadingKey.DependencyProperty;
    public static readonly DependencyProperty ErrorProperty = ErrorKey.DependencyProperty;
    public static readonly DependencyProperty InfoTextProperty = InfoTextKey.DependencyProperty;
    public static readonly DependencyProperty IsEmptyProperty = IsEmptyKey.DependencyProperty;
    public static readonly DependencyProperty EmptyMessageProperty = EmptyMessageKey.DependencyProperty;
    public static readonly DependencyProperty HasPagerProperty = HasPagerKey.DependencyProperty;

    /// <summary>All rows (client mode). Use it instead of ItemsSource to get search and paging.</summary>
    public IEnumerable? Source { get => (IEnumerable?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    /// <summary>Server mode: returns one page for a request. Exceptions show an error with a Retry button.</summary>
    public Func<HelixGridRequest, CancellationToken, Task<HelixGridPage>>? ItemsProvider
    {
        get => (Func<HelixGridRequest, CancellationToken, Task<HelixGridPage>>?)GetValue(ItemsProviderProperty);
        set => SetValue(ItemsProviderProperty, value);
    }

    public bool IsSearchable { get => (bool)GetValue(IsSearchableProperty); set => SetValue(IsSearchableProperty, value); }
    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public string? SearchPlaceholder { get => (string?)GetValue(SearchPlaceholderProperty); set => SetValue(SearchPlaceholderProperty, value); }

    /// <summary>Rows per page; 0 shows everything (no pager).</summary>
    public int PageSize { get => (int)GetValue(PageSizeProperty); set => SetValue(PageSizeProperty, value); }

    /// <summary>Choices of the page-size selector (hidden with fewer than 2).</summary>
    public IList<int>? PageSizes { get => (IList<int>?)GetValue(PageSizesProperty); set => SetValue(PageSizesProperty, value); }

    /// <summary>Current page, 1-based, two-way.</summary>
    public int Page { get => (int)GetValue(PageProperty); set => SetValue(PageProperty, value); }

    /// <summary>Row height: Sm 32, Md 40, Lg 48.</summary>
    public HelixSize Density { get => (HelixSize)GetValue(DensityProperty); set => SetValue(DensityProperty, value); }
    public bool IsStriped { get => (bool)GetValue(IsStripedProperty); set => SetValue(IsStripedProperty, value); }
    public string? EmptyText { get => (string?)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    /// <summary>A "Columns" button to show or hide columns.</summary>
    public bool ShowColumnChooser { get => (bool)GetValue(ShowColumnChooserProperty); set => SetValue(ShowColumnChooserProperty, value); }

    /// <summary>Your content on the left of the toolbar (title, filters, buttons).</summary>
    public object? Toolbar { get => GetValue(ToolbarProperty); set => SetValue(ToolbarProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>Rows matching the search (all pages).</summary>
    public int TotalCount => (int)GetValue(TotalCountProperty);
    public bool IsLoading => (bool)GetValue(IsLoadingProperty);
    public string? Error => (string?)GetValue(ErrorProperty);

    /// <summary>"11–20 of 87".</summary>
    public string? InfoText => (string?)GetValue(InfoTextProperty);
    public bool IsEmpty => (bool)GetValue(IsEmptyProperty);
    public string? EmptyMessage => (string?)GetValue(EmptyMessageProperty);
    public bool HasPager => (bool)GetValue(HasPagerProperty);

    /// <summary>Built-in texts used by the template.</summary>
    public static string ColumnsText => HelixTexts.Current.Columns;
    public static string VisibleColumnsText => HelixTexts.Current.VisibleColumns;
    public static string RetryText => HelixTexts.Current.Retry;

    private readonly DispatcherTimer _searchTimer;
    private CancellationTokenSource? _cts;
    private string? _sortBy;
    private bool _descending;
    private ButtonBase? _columnsButton, _retry;
    private Popup? _columnPopup;
    private Panel? _columnList;

    public override void OnApplyTemplate()
    {
        if (_columnsButton is not null) _columnsButton.Click -= OnColumnsClick;
        if (_retry is not null) _retry.Click -= OnRetry;
        base.OnApplyTemplate();
        _columnsButton = GetTemplateChild("PART_Columns") as ButtonBase;
        _columnPopup = GetTemplateChild("PART_ColumnPopup") as Popup;
        _columnList = GetTemplateChild("PART_ColumnList") as Panel;
        _retry = GetTemplateChild("PART_Retry") as ButtonBase;
        if (_columnsButton is not null) _columnsButton.Click += OnColumnsClick;
        if (_retry is not null) _retry.Click += OnRetry;
        ApplyExtrasTemplate();
    }

    private void OnRetry(object sender, RoutedEventArgs e) => Reload();

    private void OnColumnsClick(object sender, RoutedEventArgs e)
    {
        if (_columnPopup is null || _columnList is null) return;
        _columnList.Children.Clear();
        foreach (var column in Columns)
        {
            var box = new HelixCheckBox
            {
                Content = column.Header?.ToString() ?? "",
                IsChecked = column.Visibility == Visibility.Visible,
                Margin = new Thickness(4, 4, 12, 4),
            };
            var c = column;
            box.Checked += (_, _) => c.Visibility = Visibility.Visible;
            box.Unchecked += (_, _) =>
            {
                // Keep at least one column visible.
                if (Columns.Count(col => col.Visibility == Visibility.Visible) > 1) c.Visibility = Visibility.Collapsed;
                else box.IsChecked = true;
            };
            _columnList.Children.Add(box);
        }

        _columnPopup.IsOpen = true;
    }

    private void OnSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        // An ObservableCollection source reloads by itself when rows are added or removed.
        if (e.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldList) oldList.CollectionChanged -= OnSourceCollectionChanged;
        if (e.NewValue is System.Collections.Specialized.INotifyCollectionChanged newList) newList.CollectionChanged += OnSourceCollectionChanged;
        SetCurrentValue(PageProperty, 1);
        Reload();
    }

    private void OnSourceCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => Reload();

    private void OnSearchChanged()
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void OnPagingChanged()
    {
        SetCurrentValue(PageProperty, 1);
        Reload();
    }

    /// <summary>Sorting is done here (across all pages), not by the default view of the current page.</summary>
    protected override void OnSorting(DataGridSortingEventArgs eventArgs)
    {
        if (Source is null && ItemsProvider is null)
        {
            base.OnSorting(eventArgs);
            return;
        }

        eventArgs.Handled = true;
        var column = eventArgs.Column;
        var member = column.SortMemberPath;
        if (string.IsNullOrEmpty(member) && column is DataGridBoundColumn { Binding: Binding binding })
        {
            member = binding.Path?.Path;
        }

        if (string.IsNullOrEmpty(member))
        {
            return;
        }

        _descending = _sortBy == member && !_descending;
        _sortBy = member;
        foreach (var c in Columns) c.SortDirection = null;
        column.SortDirection = _descending ? ListSortDirection.Descending : ListSortDirection.Ascending;
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

        if (Source is null)
        {
            return;
        }

        IEnumerable<object> rows = Source.Cast<object>();
        if (Filter is { } filter)
        {
            rows = rows.Where(r => filter(r));
        }

        var search = SearchText?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var paths = SearchPaths();
            rows = rows.Where(row => paths.Any(path => Value(row, path)?.ToString()?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true));
        }

        if (GroupBy is { Length: > 0 } groupBy)
        {
            // Groups stay together; the sort applies inside each group.
            var grouped = rows.OrderBy(r => Value(r, groupBy), Comparer.Default);
            if (_sortBy is { } by)
            {
                grouped = _descending ? grouped.ThenByDescending(r => Value(r, by), Comparer.Default) : grouped.ThenBy(r => Value(r, by), Comparer.Default);
            }

            rows = grouped;
        }
        else if (_sortBy is { } sortBy)
        {
            rows = _descending ? rows.OrderByDescending(r => Value(r, sortBy), Comparer.Default) : rows.OrderBy(r => Value(r, sortBy), Comparer.Default);
        }

        var all = rows.ToList();
        _allRows = all;
        Show(all.Count, PageSize > 0 ? all.Skip((ClampPage(all.Count) - 1) * PageSize).Take(PageSize).ToList() : all, search);
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
        SetValue(IsLoadingKey, true);
        SetValue(ErrorKey, null);
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
                SetValue(ErrorKey, HelixTexts.Current.LoadError);
                ItemsSource = Array.Empty<object>();
                System.Diagnostics.Trace.TraceError($"HelixDataGrid: {ex}");
            }
        }
        finally
        {
            if (_cts == cts) SetValue(IsLoadingKey, false);
        }
    }

    private void Show(int total, IReadOnlyList<object> page, string? search)
    {
        ItemsSource = Grouped(page);
        SetValue(TotalCountKey, total);
        SetValue(IsEmptyKey, page.Count == 0);
        SetValue(EmptyMessageKey, string.IsNullOrEmpty(search) ? EmptyText : HelixTexts.Format(HelixTexts.Current.NoResultsFor, search));
        SetValue(HasPagerKey, PageSize > 0 && total > PageSize);
        var first = total == 0 ? 0 : (Math.Max(1, Page) - 1) * Math.Max(PageSize, 0) + 1;
        var last = PageSize > 0 ? Math.Min(total, first + PageSize - 1) : total;
        SetValue(InfoTextKey, HelixTexts.Format(HelixTexts.Current.RangeOf, first, last, total));
        UpdateTotals();
    }

    /// <summary>Binding paths of the visible bound columns (what the search looks into).</summary>
    private List<string> SearchPaths() => Columns
        .Where(c => c.Visibility == Visibility.Visible)
        .Select(c => c is DataGridBoundColumn { Binding: Binding b } ? b.Path?.Path : c.SortMemberPath)
        .Where(p => !string.IsNullOrEmpty(p))
        .Select(p => p!)
        .Distinct()
        .ToList();

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
}
