using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Helix.Wpf;

/// <summary>
/// Several choices from a list: chips in the field (× removes one), a list with checkboxes that stays open,
/// a search box, "Select all" and "Clear". SelectedItems is updated in place when it is a list (e.g. an ObservableCollection).
/// </summary>
[TemplatePart(Name = "PART_Chrome", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
[TemplatePart(Name = "PART_List", Type = typeof(ListBox))]
[TemplatePart(Name = "PART_Search", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_SelectAll", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_ClearAll", Type = typeof(ButtonBase))]
public class HelixMultiSelect : Control
{
    static HelixMultiSelect() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixMultiSelect), new FrameworkPropertyMetadata(typeof(HelixMultiSelect)));

    public HelixMultiSelect()
    {
        HelixAccent.Init(this);
        SetCurrentValue(PlaceholderProperty, HelixTexts.Current.SelectPlaceholder);
    }

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixMultiSelect));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixMultiSelect));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixMultiSelect));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixMultiSelect));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixMultiSelect));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixMultiSelect));

    public static readonly DependencyProperty ItemsSourceProperty = HelixProperty.Register<HelixMultiSelect, IEnumerable?>(nameof(ItemsSource), null, (d, _) => ((HelixMultiSelect)d).BuildView());
    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(HelixMultiSelect),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((HelixMultiSelect)d).OnSelectedItemsChanged(e)));
    public static readonly DependencyProperty DisplayMemberPathProperty = HelixProperty.Register<HelixMultiSelect, string?>(nameof(DisplayMemberPath), null, (d, _) => ((HelixMultiSelect)d).UpdateChips());
    public static readonly DependencyProperty ItemTemplateProperty = HelixProperty.Register<HelixMultiSelect, DataTemplate?>(nameof(ItemTemplate), null);
    public static readonly DependencyProperty IsSearchableProperty = HelixProperty.Register<HelixMultiSelect, bool>(nameof(IsSearchable), false);
    public static readonly DependencyProperty SearchTextProperty = HelixProperty.Register<HelixMultiSelect, string>(nameof(SearchText), "", (d, _) => ((HelixMultiSelect)d).ApplySearch());
    public static readonly DependencyProperty MaxChipsProperty = HelixProperty.Register<HelixMultiSelect, int>(nameof(MaxChips), 3, (d, _) => ((HelixMultiSelect)d).UpdateChips());
    public static readonly DependencyProperty MaxSelectedProperty = HelixProperty.Register<HelixMultiSelect, int?>(nameof(MaxSelected), null);
    public static readonly DependencyProperty ShowActionsProperty = HelixProperty.Register<HelixMultiSelect, bool>(nameof(ShowActions), true);
    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(HelixMultiSelect),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((HelixMultiSelect)d).OnDropDownChanged((bool)e.NewValue)));

    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixMultiSelect, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = HelixProperty.Register<HelixMultiSelect, string?>(nameof(Hint), null);
    public static readonly DependencyProperty ErrorProperty = HelixProperty.Register<HelixMultiSelect, string?>(nameof(Error), null,
        (d, e) => d.SetValue(HasErrorKey, !string.IsNullOrEmpty((string?)e.NewValue)));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(HelixMultiSelect), new PropertyMetadata(false));
    public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;
    public static readonly DependencyProperty IsRequiredProperty = HelixProperty.Register<HelixMultiSelect, bool>(nameof(IsRequired), false);
    public static readonly DependencyProperty PlaceholderProperty = HelixProperty.Register<HelixMultiSelect, string?>(nameof(Placeholder), null);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixMultiSelect, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

    private static readonly DependencyPropertyKey ChipsKey = DependencyProperty.RegisterReadOnly(nameof(Chips), typeof(IReadOnlyList<HelixChip>), typeof(HelixMultiSelect), new PropertyMetadata(Array.Empty<HelixChip>()));
    private static readonly DependencyPropertyKey MoreTextKey = DependencyProperty.RegisterReadOnly(nameof(MoreText), typeof(string), typeof(HelixMultiSelect), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey HasSelectionKey = DependencyProperty.RegisterReadOnly(nameof(HasSelection), typeof(bool), typeof(HelixMultiSelect), new PropertyMetadata(false));
    public static readonly DependencyProperty ChipsProperty = ChipsKey.DependencyProperty;
    public static readonly DependencyProperty MoreTextProperty = MoreTextKey.DependencyProperty;
    public static readonly DependencyProperty HasSelectionProperty = HasSelectionKey.DependencyProperty;

    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    /// <summary>The options.</summary>
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>The choices, two-way. A list (e.g. ObservableCollection) is updated in place; null gets a new List&lt;object&gt;.</summary>
    public IList? SelectedItems { get => (IList?)GetValue(SelectedItemsProperty); set => SetValue(SelectedItemsProperty, value); }

    /// <summary>Property shown for an option and searched.</summary>
    public string? DisplayMemberPath { get => (string?)GetValue(DisplayMemberPathProperty); set => SetValue(DisplayMemberPathProperty, value); }

    /// <summary>Custom content of an option in the list (chips still show DisplayMemberPath).</summary>
    public DataTemplate? ItemTemplate { get => (DataTemplate?)GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }

    /// <summary>A search box at the top of the list.</summary>
    public bool IsSearchable { get => (bool)GetValue(IsSearchableProperty); set => SetValue(IsSearchableProperty, value); }
    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }

    /// <summary>Chips shown in the field before "+N" (3).</summary>
    public int MaxChips { get => (int)GetValue(MaxChipsProperty); set => SetValue(MaxChipsProperty, value); }

    /// <summary>Most choices allowed (null: no limit).</summary>
    public int? MaxSelected { get => (int?)GetValue(MaxSelectedProperty); set => SetValue(MaxSelectedProperty, value); }

    /// <summary>"Select all" (the options shown) and "Clear" under the list (true).</summary>
    public bool ShowActions { get => (bool)GetValue(ShowActionsProperty); set => SetValue(ShowActionsProperty, value); }
    public bool IsDropDownOpen { get => (bool)GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public bool HasError => (bool)GetValue(HasErrorProperty);
    public bool IsRequired { get => (bool)GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>The first MaxChips choices, shown as chips.</summary>
    public IReadOnlyList<HelixChip> Chips => (IReadOnlyList<HelixChip>)GetValue(ChipsProperty);

    /// <summary>"+N" when more choices than chips.</summary>
    public string? MoreText => (string?)GetValue(MoreTextProperty);
    public bool HasSelection => (bool)GetValue(HasSelectionProperty);

    public static string SearchPlaceholderText => HelixTexts.Current.Search;
    public static string SelectAllText => HelixTexts.Current.SelectAll;
    public static string ClearText => HelixTexts.Current.Clear;

    /// <summary>Removes the chip's item (parameter: the item).</summary>
    public ICommand RemoveCommand => new HelixCommand<object>(Remove);

    private readonly List<object> _chosen = [];
    private ListCollectionView? _view;
    private ListBox? _list;
    private TextBox? _search;
    private FrameworkElement? _chrome;
    private ButtonBase? _selectAll, _clearAll;
    private bool _syncing;

    public override void OnApplyTemplate()
    {
        if (_list is not null) _list.SelectionChanged -= OnListSelection;
        if (_search is not null) _search.PreviewKeyDown -= OnSearchKey;
        if (_chrome is not null) _chrome.MouseLeftButtonUp -= OnChromeClick;
        if (_selectAll is not null) _selectAll.Click -= OnSelectAll;
        if (_clearAll is not null) _clearAll.Click -= OnClearAll;
        base.OnApplyTemplate();
        _list = GetTemplateChild("PART_List") as ListBox;
        _search = GetTemplateChild("PART_Search") as TextBox;
        _chrome = GetTemplateChild("PART_Chrome") as FrameworkElement;
        _selectAll = GetTemplateChild("PART_SelectAll") as ButtonBase;
        _clearAll = GetTemplateChild("PART_ClearAll") as ButtonBase;
        if (_list is not null) _list.SelectionChanged += OnListSelection;
        if (_search is not null) _search.PreviewKeyDown += OnSearchKey;
        if (_chrome is not null) _chrome.MouseLeftButtonUp += OnChromeClick;
        if (_selectAll is not null) _selectAll.Click += OnSelectAll;
        if (_clearAll is not null) _clearAll.Click += OnClearAll;
        if (GetTemplateChild("PART_Popup") is Popup popup) popup.Closed += (_, _) => SetCurrentValue(IsDropDownOpenProperty, false);
        BuildView();
    }

    private string TextOf(object? item) => HelixSelect.TextOf(item, DisplayMemberPath);

    private void BuildView()
    {
        _view = ItemsSource is null ? null : new ListCollectionView(ItemsSource as IList ?? ItemsSource.Cast<object>().ToList());
        if (_list is not null) _list.ItemsSource = _view;
        ApplySearch();
    }

    private void OnSelectedItemsChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= OnBoundListChanged;
        if (e.NewValue is INotifyCollectionChanged newList) newList.CollectionChanged += OnBoundListChanged;
        ReadBoundList();
    }

    private void OnBoundListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_syncing) ReadBoundList();
    }

    private void ReadBoundList()
    {
        _chosen.Clear();
        if (SelectedItems is { } items) _chosen.AddRange(items.Cast<object>());
        UpdateChips();
        SyncListSelection();
    }

    /// <summary>Writes the choices back: in place into the bound list when possible, else a new list.</summary>
    private void Commit()
    {
        _syncing = true;
        try
        {
            if (SelectedItems is { IsReadOnly: false, IsFixedSize: false } list)
            {
                list.Clear();
                foreach (var item in _chosen) list.Add(item);
            }
            else
            {
                SetCurrentValue(SelectedItemsProperty, _chosen.ToList());
            }
        }
        finally
        {
            _syncing = false;
        }

        UpdateChips();
    }

    private void UpdateChips()
    {
        var max = Math.Max(0, MaxChips);
        SetValue(ChipsKey, _chosen.Take(max).Select(i => new HelixChip(i, TextOf(i))).ToList());
        SetValue(MoreTextKey, _chosen.Count > max ? $"+{_chosen.Count - max}" : null);
        SetValue(HasSelectionKey, _chosen.Count > 0);
    }

    private void SyncListSelection()
    {
        if (_list is null || _view is null) return;
        _syncing = true;
        try
        {
            _list.SelectedItems.Clear();
            foreach (var item in _view) if (_chosen.Contains(item)) _list.SelectedItems.Add(item);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnListSelection(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || _view is null) return;
        var changed = false;
        foreach (var item in e.RemovedItems)
        {
            // An option hidden by the search leaves the list's selection, not the choices.
            if (_view.PassesFilter(item) && _chosen.Remove(item)) changed = true;
        }

        foreach (var item in e.AddedItems)
        {
            if (_chosen.Contains(item)) continue;
            if (MaxSelected is { } max && _chosen.Count >= max)
            {
                _syncing = true;
                _list?.SelectedItems.Remove(item);
                _syncing = false;
                continue;
            }

            _chosen.Add(item);
            changed = true;
        }

        if (changed) Commit();
    }

    private void Remove(object item)
    {
        if (_chosen.Remove(item))
        {
            Commit();
            SyncListSelection();
        }
    }

    private void OnSelectAll(object sender, RoutedEventArgs e)
    {
        if (_view is null) return;
        foreach (var item in _view)
        {
            if (MaxSelected is { } max && _chosen.Count >= max) break;
            if (!_chosen.Contains(item)) _chosen.Add(item);
        }

        Commit();
        SyncListSelection();
    }

    private void OnClearAll(object sender, RoutedEventArgs e)
    {
        _chosen.Clear();
        Commit();
        SyncListSelection();
    }

    private void ApplySearch()
    {
        if (_view is null) return;
        var term = SearchText?.Trim();
        _view.Filter = string.IsNullOrEmpty(term) ? null : item => TextOf(item).Contains(term, StringComparison.CurrentCultureIgnoreCase);
        SyncListSelection();
    }

    private void OnChromeClick(object sender, MouseButtonEventArgs e)
    {
        if (!IsEnabled || e.Handled) return;
        SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
        e.Handled = true;
    }

    private void OnDropDownChanged(bool open)
    {
        if (open)
        {
            SyncListSelection();
            Dispatcher.BeginInvoke(() => ((UIElement?)(IsSearchable ? _search : _list))?.Focus(), System.Windows.Threading.DispatcherPriority.Input);
        }
        else if (SearchText.Length > 0)
        {
            SetCurrentValue(SearchTextProperty, "");
        }
    }

    private void OnSearchKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && _list?.ItemContainerGenerator.ContainerFromIndex(0) is ListBoxItem first)
        {
            first.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            Focus();
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;
        if (e.Key == Key.Escape && IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            Focus();
            e.Handled = true;
        }
        else if (!IsDropDownOpen && (e.Key is Key.Enter or Key.Space or Key.Down))
        {
            SetCurrentValue(IsDropDownOpenProperty, true);
            e.Handled = true;
        }
        else if (!IsDropDownOpen && e.Key == Key.Back && _chosen.Count > 0)
        {
            Remove(_chosen[^1]);
            e.Handled = true;
        }
    }
}

/// <summary>A chip of <see cref="HelixMultiSelect"/>: the item and its text.</summary>
public sealed record HelixChip(object Item, string Text);
