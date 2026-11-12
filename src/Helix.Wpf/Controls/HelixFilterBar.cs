using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Helix.Wpf;

/// <summary>A named set of filter values (key → encoded value) saved by the user in a <see cref="HelixFilterBar"/>.</summary>
public sealed record HelixFilterPreset(string Name, IReadOnlyDictionary<string, string> State);

/// <summary>
/// A row of filters above a list or a grid. Its State is a key → text dictionary (easy to save or send to a server).
/// With <see cref="Target"/>, the grid is filtered directly; users can save a set of filters under a name.
/// </summary>
[TemplatePart(Name = "PART_Clear", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_Saved", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_SavePopup", Type = typeof(Popup))]
[TemplatePart(Name = "PART_SaveName", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_SaveConfirm", Type = typeof(ButtonBase))]
public class HelixFilterBar : ItemsControl
{
    static HelixFilterBar() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixFilterBar), new FrameworkPropertyMetadata(typeof(HelixFilterBar)));

    public static readonly DependencyProperty TargetProperty = HelixProperty.Register<HelixFilterBar, HelixDataGrid?>(nameof(Target), null, (d, _) => ((HelixFilterBar)d).Publish());
    public static readonly DependencyProperty SourceProperty = HelixProperty.Register<HelixFilterBar, IEnumerable?>(nameof(Source), null);
    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(nameof(State), typeof(IReadOnlyDictionary<string, string>), typeof(HelixFilterBar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((HelixFilterBar)d).OnStateChanged(e)));
    public static readonly DependencyProperty PresetsProperty = DependencyProperty.Register(nameof(Presets), typeof(IList<HelixFilterPreset>), typeof(HelixFilterBar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HelixFilterBar)d).UpdateTexts()));

    private static readonly DependencyPropertyKey ActiveCountKey = DependencyProperty.RegisterReadOnly(nameof(ActiveCount), typeof(int), typeof(HelixFilterBar), new PropertyMetadata(0));
    private static readonly DependencyPropertyKey SavedTextKey = DependencyProperty.RegisterReadOnly(nameof(SavedText), typeof(string), typeof(HelixFilterBar), new PropertyMetadata(null));
    public static readonly DependencyProperty ActiveCountProperty = ActiveCountKey.DependencyProperty;
    public static readonly DependencyProperty SavedTextProperty = SavedTextKey.DependencyProperty;

    /// <summary>The grid to filter (its Filter is set by the bar).</summary>
    public HelixDataGrid? Target { get => (HelixDataGrid?)GetValue(TargetProperty); set => SetValue(TargetProperty, value); }

    /// <summary>The rows, for the options of choice filters (Target.Source by default).</summary>
    public IEnumerable? Source { get => (IEnumerable?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    /// <summary>The values, key → text (two-way).</summary>
    public IReadOnlyDictionary<string, string>? State { get => (IReadOnlyDictionary<string, string>?)GetValue(StateProperty); set => SetValue(StateProperty, value); }

    /// <summary>Saved filters of the menu. A list (e.g. ObservableCollection) is updated in place when the user saves or deletes one.</summary>
    public IList<HelixFilterPreset>? Presets { get => (IList<HelixFilterPreset>?)GetValue(PresetsProperty); set => SetValue(PresetsProperty, value); }

    /// <summary>Number of filters with a value.</summary>
    public int ActiveCount => (int)GetValue(ActiveCountProperty);

    /// <summary>Text of the saved-filters button: the active saved filter's name, else "Saved filters".</summary>
    public string? SavedText => (string?)GetValue(SavedTextProperty);

    public static string ClearText => HelixTexts.Current.ClearFilters;
    public static string SaveText => HelixTexts.Current.Save;
    public static string NameText => HelixTexts.Current.Name;

    /// <summary>Raised when a filter value changes.</summary>
    public event EventHandler? FiltersChanged;

    private readonly Dictionary<string, string> _state = [];
    private bool _syncing;
    private ButtonBase? _clear, _saved, _saveConfirm;
    private Popup? _savePopup;
    private TextBox? _saveName;

    public HelixFilterBar() => UpdateTexts();

    /// <summary>The rows that pass every active filter.</summary>
    public Predicate<object> Predicate
    {
        get
        {
            var rules = Filters.Where(f => _state.ContainsKey(f.Key)).Select(f => f.CreatePredicate(_state[f.Key])).OfType<Predicate<object>>().ToArray();
            return item => rules.All(rule => rule(item));
        }
    }

    internal IEnumerable? Rows => Source ?? Target?.Source;

    private IEnumerable<HelixFilter> Filters => Items.OfType<HelixFilter>();

    internal string? ValueOf(string key) => _state.GetValueOrDefault(key);

    public override void OnApplyTemplate()
    {
        if (_clear is not null) _clear.Click -= OnClear;
        if (_saved is not null) _saved.Click -= OnSavedMenu;
        if (_saveConfirm is not null) _saveConfirm.Click -= OnSaveConfirm;
        if (_saveName is not null) _saveName.KeyDown -= OnSaveNameKey;
        base.OnApplyTemplate();
        _clear = GetTemplateChild("PART_Clear") as ButtonBase;
        _saved = GetTemplateChild("PART_Saved") as ButtonBase;
        _savePopup = GetTemplateChild("PART_SavePopup") as Popup;
        _saveName = GetTemplateChild("PART_SaveName") as TextBox;
        _saveConfirm = GetTemplateChild("PART_SaveConfirm") as ButtonBase;
        if (_clear is not null) _clear.Click += OnClear;
        if (_saved is not null) _saved.Click += OnSavedMenu;
        if (_saveConfirm is not null) _saveConfirm.Click += OnSaveConfirm;
        if (_saveName is not null) _saveName.KeyDown += OnSaveNameKey;
    }

    /// <summary>Sets (or clears, with null or empty) the value of a filter.</summary>
    public void SetFilterValue(string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (!_state.Remove(key)) return;
        }
        else
        {
            if (_state.TryGetValue(key, out var old) && old == value) return;
            _state[key] = value;
        }

        Commit();
    }

    private void Commit()
    {
        _syncing = true;
        SetCurrentValue(StateProperty, new Dictionary<string, string>(_state));
        _syncing = false;
        Publish();
    }

    private void OnStateChanged(DependencyPropertyChangedEventArgs e)
    {
        if (_syncing) return;
        _state.Clear();
        if (e.NewValue is IReadOnlyDictionary<string, string> state)
        {
            foreach (var (key, value) in state) _state[key] = value;
        }

        foreach (var filter in Filters) filter.Refresh();
        Publish();
    }

    private void Publish()
    {
        SetValue(ActiveCountKey, Filters.Count(f => _state.ContainsKey(f.Key)));
        UpdateTexts();
        if (Target is { } grid) grid.Filter = _state.Count == 0 ? null : Predicate;
        FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    private HelixFilterPreset? CurrentPreset =>
        Presets?.FirstOrDefault(p => p.State.Count == _state.Count && p.State.All(kv => _state.TryGetValue(kv.Key, out var v) && v == kv.Value));

    private void UpdateTexts() => SetValue(SavedTextKey, CurrentPreset?.Name ?? HelixTexts.Current.SavedFilters);

    private void OnClear(object sender, RoutedEventArgs e)
    {
        _state.Clear();
        foreach (var filter in Filters) filter.Refresh();
        Commit();
    }

    private void Apply(HelixFilterPreset preset)
    {
        _state.Clear();
        foreach (var (key, value) in preset.State) _state[key] = value;
        foreach (var filter in Filters) filter.Refresh();
        Commit();
    }

    private void OnSavedMenu(object sender, RoutedEventArgs e)
    {
        var texts = HelixTexts.Current;
        var menu = new HelixContextMenu { PlacementTarget = _saved, Placement = PlacementMode.Bottom, VerticalOffset = 4 };
        menu.Items.Add(new HelixMenuLabel { Text = texts.SavedFilters });
        var current = CurrentPreset;
        if (Presets is not { Count: > 0 })
        {
            menu.Items.Add(new HelixMenuItem { Header = texts.NoSavedFilters, IsEnabled = false });
        }
        else
        {
            foreach (var preset in Presets)
            {
                var item = new HelixMenuItem { Header = preset.Name, IsCheckable = true, IsChecked = ReferenceEquals(preset, current) };
                item.Click += (_, _) => Apply(preset);
                menu.Items.Add(item);
            }
        }

        menu.Items.Add(new HelixMenuSeparator());
        var save = new HelixMenuItem { Header = texts.SaveFilters, IconName = "plus", IsEnabled = _state.Count > 0 };
        save.Click += (_, _) => OpenSave(current?.Name);
        menu.Items.Add(save);
        if (current is not null)
        {
            var delete = new HelixMenuItem { Header = HelixTexts.Format(texts.DeleteSavedFilter, current.Name), IconName = "trash", IsDanger = true };
            delete.Click += (_, _) =>
            {
                if (Presets is { IsReadOnly: false } list) list.Remove(current);
                else SetCurrentValue(PresetsProperty, Presets?.Where(p => !ReferenceEquals(p, current)).ToList());
                UpdateTexts();
            };
            menu.Items.Add(delete);
        }

        menu.IsOpen = true;
    }

    private void OpenSave(string? name)
    {
        if (_savePopup is null || _saveName is null) return;
        _saveName.Text = name ?? "";
        _savePopup.IsOpen = true;
        Dispatcher.BeginInvoke(() => { _saveName.Focus(); _saveName.SelectAll(); }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void OnSaveNameKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OnSaveConfirm(sender, e);
        else if (e.Key == Key.Escape && _savePopup is not null) _savePopup.IsOpen = false;
    }

    private void OnSaveConfirm(object sender, RoutedEventArgs e)
    {
        var name = _saveName?.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;
        var preset = new HelixFilterPreset(name, new Dictionary<string, string>(_state));
        if (Presets is { IsReadOnly: false } list)
        {
            foreach (var same in list.Where(p => string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase)).ToList()) list.Remove(same);
            list.Add(preset);
        }
        else
        {
            SetCurrentValue(PresetsProperty, (Presets ?? []).Where(p => !string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase)).Append(preset).ToList());
        }

        if (_savePopup is not null) _savePopup.IsOpen = false;
        UpdateTexts();
    }
}

/// <summary>Base of the filters of a <see cref="HelixFilterBar"/>: a key, a label, and the rule for an encoded value.</summary>
public abstract class HelixFilter : Control
{
    public static readonly DependencyProperty KeyProperty = HelixProperty.Register<HelixFilter, string>(nameof(Key), "");
    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixFilter, string?>(nameof(Label), null);

    /// <summary>Stable key of the value in the bar's State and in saved filters.</summary>
    public string Key { get => (string)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    protected HelixFilterBar? Bar => ItemsControl.ItemsControlFromItemContainer(this) as HelixFilterBar ?? Parent as HelixFilterBar;

    protected bool Updating { get; set; }

    protected HelixFilter() => Loaded += (_, _) => Refresh();

    /// <summary>The rule for an encoded value; null keeps every row.</summary>
    public abstract Predicate<object>? CreatePredicate(string value);

    /// <summary>Shows the bar's current value in the filter's field.</summary>
    internal void Refresh()
    {
        Updating = true;
        try { Show(Bar?.ValueOf(Key)); }
        finally { Updating = false; }
    }

    protected abstract void Show(string? value);

    protected void Set(string? value)
    {
        if (!Updating) Bar?.SetFilterValue(Key, value);
    }

    protected static object? ValueAt(object row, string path) => HelixDataGrid.Value(row, path);

    protected static string EncodeList(IEnumerable<string> values) => JsonSerializer.Serialize(values);

    protected static List<string> DecodeList(string? value)
    {
        if (string.IsNullOrEmpty(value)) return [];
        try { return JsonSerializer.Deserialize<List<string>>(value) ?? []; }
        catch (JsonException) { return [value]; }
    }

    protected static string KeyOf(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
}

/// <summary>Text found in any of the <see cref="Paths"/> (comma-separated property paths).</summary>
public class HelixTextFilter : HelixFilter
{
    static HelixTextFilter() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixTextFilter), new FrameworkPropertyMetadata(typeof(HelixTextFilter)));

    public HelixTextFilter() => SetCurrentValue(PlaceholderProperty, HelixTexts.Current.Search);

    public static readonly DependencyProperty PathsProperty = HelixProperty.Register<HelixTextFilter, string>(nameof(Paths), "");
    public static readonly DependencyProperty PlaceholderProperty = HelixProperty.Register<HelixTextFilter, string?>(nameof(Placeholder), null);
    public static readonly DependencyProperty TextProperty = HelixProperty.Register<HelixTextFilter, string>(nameof(Text), "", (d, e) => ((HelixTextFilter)d).Set((string)e.NewValue));

    /// <summary>Property paths searched, e.g. "Site,Product".</summary>
    public string Paths { get => (string)GetValue(PathsProperty); set => SetValue(PathsProperty, value); }
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>The text typed (bound to the field of the template).</summary>
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    protected override void Show(string? value) => SetCurrentValue(TextProperty, value ?? "");

    public override Predicate<object>? CreatePredicate(string value)
    {
        var term = value.Trim();
        var paths = Paths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return term.Length == 0 || paths.Length == 0 ? null
            : row => paths.Any(p => ValueAt(row, p)?.ToString()?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true);
    }
}

/// <summary>Rows whose <see cref="Path"/> value is one of the chosen options (from <see cref="Options"/> or the existing values).</summary>
public class HelixChoiceFilter : HelixFilter
{
    static HelixChoiceFilter() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixChoiceFilter), new FrameworkPropertyMetadata(typeof(HelixChoiceFilter)));

    public static readonly DependencyProperty PathProperty = HelixProperty.Register<HelixChoiceFilter, string>(nameof(Path), "");
    public static readonly DependencyProperty OptionsProperty = HelixProperty.Register<HelixChoiceFilter, IEnumerable?>(nameof(Options), null);
    public static readonly DependencyProperty IsSearchableProperty = HelixProperty.Register<HelixChoiceFilter, bool>(nameof(IsSearchable), false);

    private static readonly DependencyPropertyKey ChoicesKey = DependencyProperty.RegisterReadOnly(nameof(Choices), typeof(IList), typeof(HelixChoiceFilter), new PropertyMetadata(null));
    public static readonly DependencyProperty ChoicesProperty = ChoicesKey.DependencyProperty;
    public static readonly DependencyProperty ChosenProperty = HelixProperty.Register<HelixChoiceFilter, IList?>(nameof(Chosen), null);

    /// <summary>Property path compared with the options, e.g. "Status".</summary>
    public string Path { get => (string)GetValue(PathProperty); set => SetValue(PathProperty, value); }

    /// <summary>The options; null: the distinct values of the rows.</summary>
    public IEnumerable? Options { get => (IEnumerable?)GetValue(OptionsProperty); set => SetValue(OptionsProperty, value); }
    public bool IsSearchable { get => (bool)GetValue(IsSearchableProperty); set => SetValue(IsSearchableProperty, value); }

    /// <summary>The options shown (template).</summary>
    public IList? Choices => (IList?)GetValue(ChoicesProperty);

    /// <summary>The chosen options (template, two-way with the multi-select).</summary>
    public IList? Chosen { get => (IList?)GetValue(ChosenProperty); set => SetValue(ChosenProperty, value); }

    private ObservableCollection<object>? _chosen;

    protected override void Show(string? value)
    {
        var options = (Options ?? Bar?.Rows?.Cast<object>().Select(r => ValueAt(r, Path)).Where(v => v is not null).Distinct().OrderBy(v => v?.ToString()) ?? Enumerable.Empty<object?>())
            .Cast<object>().ToList();
        SetValue(ChoicesKey, options);
        var keys = DecodeList(value).ToHashSet();
        if (_chosen is not null) _chosen.CollectionChanged -= OnChosenChanged;
        _chosen = new ObservableCollection<object>(options.Where(o => keys.Contains(KeyOf(o))));
        _chosen.CollectionChanged += OnChosenChanged;
        SetCurrentValue(ChosenProperty, _chosen);
    }

    private void OnChosenChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
        Set(_chosen is { Count: > 0 } chosen ? EncodeList(chosen.Select(KeyOf)) : null);

    public override Predicate<object>? CreatePredicate(string value)
    {
        var keys = DecodeList(value).ToHashSet();
        return keys.Count == 0 ? null : row => keys.Contains(KeyOf(ValueAt(row, Path)));
    }
}

/// <summary>Rows whose <see cref="Path"/> date is in the chosen period (both days included).</summary>
public class HelixDateFilter : HelixFilter
{
    static HelixDateFilter() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixDateFilter), new FrameworkPropertyMetadata(typeof(HelixDateFilter)));

    public static readonly DependencyProperty PathProperty = HelixProperty.Register<HelixDateFilter, string>(nameof(Path), "");
    public static readonly DependencyProperty MaxDaysProperty = HelixProperty.Register<HelixDateFilter, int?>(nameof(MaxDays), null);
    public static readonly DependencyProperty RangeProperty = HelixProperty.Register<HelixDateFilter, HelixDateRange?>(nameof(Range), null,
        (d, e) => ((HelixDateFilter)d).Set(e.NewValue is HelixDateRange r ? Encode(r) : null));

    /// <summary>Property path of the date, e.g. "Date" (DateTime, DateTime? or DateOnly).</summary>
    public string Path { get => (string)GetValue(PathProperty); set => SetValue(PathProperty, value); }
    public int? MaxDays { get => (int?)GetValue(MaxDaysProperty); set => SetValue(MaxDaysProperty, value); }

    /// <summary>The chosen period (template).</summary>
    public HelixDateRange? Range { get => (HelixDateRange?)GetValue(RangeProperty); set => SetValue(RangeProperty, value); }

    private static string Encode(HelixDateRange range) =>
        $"{range.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}/{range.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

    private static HelixDateRange? Decode(string? value)
    {
        var parts = value?.Split('/');
        return parts is { Length: 2 }
            && DateTime.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
            && DateTime.TryParseExact(parts[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)
            ? new HelixDateRange(start, end) : null;
    }

    protected override void Show(string? value) => SetCurrentValue(RangeProperty, Decode(value));

    public override Predicate<object>? CreatePredicate(string value)
    {
        if (Decode(value) is not { } range) return null;
        return row => ValueAt(row, Path) switch
        {
            DateTime date => range.Contains(date),
            DateOnly day => range.Contains(day.ToDateTime(TimeOnly.MinValue)),
            DateTimeOffset offset => range.Contains(offset.LocalDateTime),
            _ => false,
        };
    }
}

/// <summary>While the switch is on, keeps the rows whose <see cref="Path"/> is true (or that pass <see cref="Rule"/>).</summary>
public class HelixToggleFilter : HelixFilter
{
    static HelixToggleFilter() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixToggleFilter), new FrameworkPropertyMetadata(typeof(HelixToggleFilter)));

    public static readonly DependencyProperty PathProperty = HelixProperty.Register<HelixToggleFilter, string?>(nameof(Path), null);
    public static readonly DependencyProperty IsOnProperty = HelixProperty.Register<HelixToggleFilter, bool>(nameof(IsOn), false, (d, e) => ((HelixToggleFilter)d).Set((bool)e.NewValue ? "1" : null));

    /// <summary>Property path of a bool, e.g. "Late".</summary>
    public string? Path { get => (string?)GetValue(PathProperty); set => SetValue(PathProperty, value); }

    /// <summary>Your rule, used instead of Path.</summary>
    public Predicate<object>? Rule { get; set; }

    /// <summary>The switch (template).</summary>
    public bool IsOn { get => (bool)GetValue(IsOnProperty); set => SetValue(IsOnProperty, value); }

    protected override void Show(string? value) => SetCurrentValue(IsOnProperty, value == "1");

    public override Predicate<object>? CreatePredicate(string value) =>
        value != "1" ? null : Rule ?? (Path is { Length: > 0 } path ? row => ValueAt(row, path) is true : null);
}
