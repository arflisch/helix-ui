using System.Collections;
using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace Helix.Avalonia;

/// <summary>A named set of filter values (key → encoded value) saved by the user in a <see cref="HelixFilterBar"/>.</summary>
public sealed record HelixFilterPreset(string Name, IReadOnlyDictionary<string, string> State);

/// <summary>
/// A row of filters above a list or a grid. Its State is a key → text dictionary (easy to save or send to a server).
/// With <see cref="Target"/>, the grid is filtered directly; users can save a set of filters under a name.
/// </summary>
[TemplatePart("PART_Clear", typeof(Button))]
[TemplatePart("PART_Saved", typeof(Button))]
[TemplatePart("PART_SavePopup", typeof(Popup))]
[TemplatePart("PART_SaveName", typeof(TextBox))]
[TemplatePart("PART_SaveConfirm", typeof(Button))]
public class HelixFilterBar : ItemsControl
{
    public static readonly StyledProperty<HelixDataGrid?> TargetProperty = AvaloniaProperty.Register<HelixFilterBar, HelixDataGrid?>(nameof(Target));
    public static readonly StyledProperty<IEnumerable?> SourceProperty = AvaloniaProperty.Register<HelixFilterBar, IEnumerable?>(nameof(Source));
    public static readonly StyledProperty<IReadOnlyDictionary<string, string>?> StateProperty =
        AvaloniaProperty.Register<HelixFilterBar, IReadOnlyDictionary<string, string>?>(nameof(State), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<IList<HelixFilterPreset>?> PresetsProperty =
        AvaloniaProperty.Register<HelixFilterBar, IList<HelixFilterPreset>?>(nameof(Presets), defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<HelixFilterBar, int> ActiveCountProperty = AvaloniaProperty.RegisterDirect<HelixFilterBar, int>(nameof(ActiveCount), b => b.ActiveCount);
    public static readonly DirectProperty<HelixFilterBar, string?> SavedTextProperty = AvaloniaProperty.RegisterDirect<HelixFilterBar, string?>(nameof(SavedText), b => b.SavedText);
    public static readonly RoutedEvent<RoutedEventArgs> FiltersChangedEvent = RoutedEvent.Register<HelixFilterBar, RoutedEventArgs>(nameof(FiltersChanged), RoutingStrategies.Bubble);

    protected override Type StyleKeyOverride => typeof(HelixFilterBar);

    /// <summary>The grid to filter (its Filter is set by the bar).</summary>
    public HelixDataGrid? Target { get => GetValue(TargetProperty); set => SetValue(TargetProperty, value); }

    /// <summary>The rows, for the options of choice filters (Target.Source by default).</summary>
    public IEnumerable? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    /// <summary>The values, key → text (two-way).</summary>
    public IReadOnlyDictionary<string, string>? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }

    /// <summary>Saved filters of the menu. A list (e.g. ObservableCollection) is updated in place when the user saves or deletes one.</summary>
    public IList<HelixFilterPreset>? Presets { get => GetValue(PresetsProperty); set => SetValue(PresetsProperty, value); }

    private int _activeCount;
    private string? _savedText = HelixTexts.Current.SavedFilters;

    /// <summary>Number of filters with a value.</summary>
    public int ActiveCount { get => _activeCount; private set => SetAndRaise(ActiveCountProperty, ref _activeCount, value); }

    /// <summary>Text of the saved-filters button: the active saved filter's name, else "Saved filters".</summary>
    public string? SavedText { get => _savedText; private set => SetAndRaise(SavedTextProperty, ref _savedText, value); }

    public static string ClearText => HelixTexts.Current.ClearFilters;
    public static string SaveText => HelixTexts.Current.Save;
    public static string NameText => HelixTexts.Current.Name;

    /// <summary>Raised when a filter value changes.</summary>
    public event EventHandler<RoutedEventArgs>? FiltersChanged { add => AddHandler(FiltersChangedEvent, value); remove => RemoveHandler(FiltersChangedEvent, value); }

    private readonly Dictionary<string, string> _state = [];
    private bool _syncing;
    private Button? _clear, _saved, _saveConfirm;
    private Popup? _savePopup;
    private TextBox? _saveName;

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

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_clear is not null) _clear.Click -= OnClear;
        if (_saved is not null) _saved.Click -= OnSavedMenu;
        if (_saveConfirm is not null) _saveConfirm.Click -= OnSaveConfirm;
        if (_saveName is not null) _saveName.KeyDown -= OnSaveNameKey;
        base.OnApplyTemplate(e);
        _clear = e.NameScope.Find<Button>("PART_Clear");
        _saved = e.NameScope.Find<Button>("PART_Saved");
        _savePopup = e.NameScope.Find<Popup>("PART_SavePopup");
        _saveName = e.NameScope.Find<TextBox>("PART_SaveName");
        _saveConfirm = e.NameScope.Find<Button>("PART_SaveConfirm");
        if (_clear is not null) _clear.Click += OnClear;
        if (_saved is not null) _saved.Click += OnSavedMenu;
        if (_saveConfirm is not null) _saveConfirm.Click += OnSaveConfirm;
        if (_saveName is not null) _saveName.KeyDown += OnSaveNameKey;
        foreach (var filter in Filters) filter.Refresh();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty && !_syncing)
        {
            _state.Clear();
            if (State is { } state)
            {
                foreach (var (key, value) in state) _state[key] = value;
            }

            foreach (var filter in Filters) filter.Refresh();
            Publish();
        }
        else if (change.Property == TargetProperty) Publish();
        else if (change.Property == PresetsProperty) UpdateTexts();
        else if (change.Property == SourceProperty) foreach (var filter in Filters) filter.Refresh();
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

    /// <summary>Removes every filter value.</summary>
    public void ClearFilters()
    {
        _state.Clear();
        foreach (var filter in Filters) filter.Refresh();
        Commit();
    }

    /// <summary>Applies a saved filter.</summary>
    public void Apply(HelixFilterPreset preset)
    {
        _state.Clear();
        foreach (var (key, value) in preset.State) _state[key] = value;
        foreach (var filter in Filters) filter.Refresh();
        Commit();
    }

    /// <summary>Saves the current values under a name (replaces a saved filter with the same name).</summary>
    public HelixFilterPreset SavePreset(string name)
    {
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

        UpdateTexts();
        return preset;
    }

    private void Commit()
    {
        _syncing = true;
        SetCurrentValue(StateProperty, new Dictionary<string, string>(_state));
        _syncing = false;
        Publish();
    }

    private void Publish()
    {
        ActiveCount = Filters.Count(f => _state.ContainsKey(f.Key));
        UpdateTexts();
        if (Target is { } grid) grid.Filter = _state.Count == 0 ? null : Predicate;
        RaiseEvent(new RoutedEventArgs(FiltersChangedEvent, this));
    }

    private HelixFilterPreset? CurrentPreset =>
        Presets?.FirstOrDefault(p => p.State.Count == _state.Count && p.State.All(kv => _state.TryGetValue(kv.Key, out var v) && v == kv.Value));

    private void UpdateTexts() => SavedText = CurrentPreset?.Name ?? HelixTexts.Current.SavedFilters;

    private void OnClear(object? sender, RoutedEventArgs e) => ClearFilters();

    private void OnSavedMenu(object? sender, RoutedEventArgs e)
    {
        if (_saved is null) return;
        var texts = HelixTexts.Current;
        var menu = new HelixContextMenu { Placement = PlacementMode.BottomEdgeAlignedRight, VerticalOffset = 4 };
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
                var item = new HelixMenuItem { Header = preset.Name, IconName = ReferenceEquals(preset, current) ? "check" : "star" };
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

        menu.Open(_saved);
    }

    private void OpenSave(string? name)
    {
        if (_savePopup is null || _saveName is null) return;
        _saveName.Text = name ?? "";
        _savePopup.IsOpen = true;
        Dispatcher.UIThread.Post(() => { _saveName.Focus(); _saveName.SelectAll(); }, DispatcherPriority.Input);
    }

    private void OnSaveNameKey(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OnSaveConfirm(sender, e);
        else if (e.Key == Key.Escape && _savePopup is not null) _savePopup.IsOpen = false;
    }

    private void OnSaveConfirm(object? sender, RoutedEventArgs e)
    {
        var name = _saveName?.Text?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        SavePreset(name);
        if (_savePopup is not null) _savePopup.IsOpen = false;
    }
}

/// <summary>Base of the filters of a <see cref="HelixFilterBar"/>: a key, a label, and the rule for an encoded value.</summary>
public abstract class HelixFilter : TemplatedControl
{
    public static readonly StyledProperty<string> KeyProperty = AvaloniaProperty.Register<HelixFilter, string>(nameof(Key), "");
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<HelixFilter, string?>(nameof(Label));

    /// <summary>Stable key of the value in the bar's State and in saved filters.</summary>
    public string Key { get => GetValue(KeyProperty); set => SetValue(KeyProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    protected HelixFilterBar? Bar => Parent as HelixFilterBar ?? this.FindLogicalAncestorOfType<HelixFilterBar>();

    protected bool Updating { get; set; }

    protected override void OnAttachedToLogicalTree(global::Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        Refresh();
    }

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
    public static readonly StyledProperty<string> PathsProperty = AvaloniaProperty.Register<HelixTextFilter, string>(nameof(Paths), "");
    public static readonly StyledProperty<string?> PlaceholderProperty = AvaloniaProperty.Register<HelixTextFilter, string?>(nameof(Placeholder));
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<HelixTextFilter, string?>(nameof(Text), "", defaultBindingMode: BindingMode.TwoWay);

    public HelixTextFilter() => SetCurrentValue(PlaceholderProperty, HelixTexts.Current.Search);

    /// <summary>Property paths searched, e.g. "Site,Product".</summary>
    public string Paths { get => GetValue(PathsProperty); set => SetValue(PathsProperty, value); }
    public string? Placeholder { get => GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>The text typed (bound to the field of the template).</summary>
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty) Set(Text);
    }

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
    public static readonly StyledProperty<string> PathProperty = AvaloniaProperty.Register<HelixChoiceFilter, string>(nameof(Path), "");
    public static readonly StyledProperty<IEnumerable?> OptionsProperty = AvaloniaProperty.Register<HelixChoiceFilter, IEnumerable?>(nameof(Options));
    public static readonly StyledProperty<bool> IsSearchableProperty = AvaloniaProperty.Register<HelixChoiceFilter, bool>(nameof(IsSearchable));
    public static readonly StyledProperty<IList?> ChosenProperty = AvaloniaProperty.Register<HelixChoiceFilter, IList?>(nameof(Chosen), defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<HelixChoiceFilter, IList?> ChoicesProperty = AvaloniaProperty.RegisterDirect<HelixChoiceFilter, IList?>(nameof(Choices), f => f.Choices);

    private IList? _choices;

    /// <summary>Property path compared with the options, e.g. "Status".</summary>
    public string Path { get => GetValue(PathProperty); set => SetValue(PathProperty, value); }

    /// <summary>The options; null: the distinct values of the rows.</summary>
    public IEnumerable? Options { get => GetValue(OptionsProperty); set => SetValue(OptionsProperty, value); }
    public bool IsSearchable { get => GetValue(IsSearchableProperty); set => SetValue(IsSearchableProperty, value); }

    /// <summary>The options shown (template).</summary>
    public IList? Choices { get => _choices; private set => SetAndRaise(ChoicesProperty, ref _choices, value); }

    /// <summary>The chosen options (template, two-way with the multi-select).</summary>
    public IList? Chosen { get => GetValue(ChosenProperty); set => SetValue(ChosenProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChosenProperty)
        {
            Set(Chosen is { Count: > 0 } chosen ? EncodeList(chosen.Cast<object>().Select(KeyOf)) : null);
        }
        else if (change.Property == OptionsProperty || change.Property == PathProperty)
        {
            Refresh();
        }
    }

    protected override void Show(string? value)
    {
        var options = (Options?.Cast<object?>() ?? Bar?.Rows?.Cast<object>().Select(r => ValueAt(r, Path)).Where(v => v is not null).Distinct().OrderBy(v => v?.ToString())
                ?? Enumerable.Empty<object?>())
            .OfType<object>().ToList();
        Choices = options;
        var keys = DecodeList(value).ToHashSet();
        SetCurrentValue(ChosenProperty, new AvaloniaList<object>(options.Where(o => keys.Contains(KeyOf(o)))));
    }

    public override Predicate<object>? CreatePredicate(string value)
    {
        var keys = DecodeList(value).ToHashSet();
        return keys.Count == 0 ? null : row => keys.Contains(KeyOf(ValueAt(row, Path)));
    }
}

/// <summary>Rows whose <see cref="Path"/> date is in the chosen period (both days included).</summary>
public class HelixDateFilter : HelixFilter
{
    public static readonly StyledProperty<string> PathProperty = AvaloniaProperty.Register<HelixDateFilter, string>(nameof(Path), "");
    public static readonly StyledProperty<int?> MaxDaysProperty = AvaloniaProperty.Register<HelixDateFilter, int?>(nameof(MaxDays));
    public static readonly StyledProperty<HelixDateRange?> RangeProperty = AvaloniaProperty.Register<HelixDateFilter, HelixDateRange?>(nameof(Range), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Property path of the date, e.g. "Date" (DateTime, DateTime? or DateOnly).</summary>
    public string Path { get => GetValue(PathProperty); set => SetValue(PathProperty, value); }
    public int? MaxDays { get => GetValue(MaxDaysProperty); set => SetValue(MaxDaysProperty, value); }

    /// <summary>The chosen period (template).</summary>
    public HelixDateRange? Range { get => GetValue(RangeProperty); set => SetValue(RangeProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RangeProperty) Set(Range is { } r ? Encode(r) : null);
    }

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
    public static readonly StyledProperty<string?> PathProperty = AvaloniaProperty.Register<HelixToggleFilter, string?>(nameof(Path));
    public static readonly StyledProperty<bool> IsOnProperty = AvaloniaProperty.Register<HelixToggleFilter, bool>(nameof(IsOn), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Property path of a bool, e.g. "Late".</summary>
    public string? Path { get => GetValue(PathProperty); set => SetValue(PathProperty, value); }

    /// <summary>Your rule, used instead of Path.</summary>
    public Predicate<object>? Rule { get; set; }

    /// <summary>The switch (template).</summary>
    public bool IsOn { get => GetValue(IsOnProperty); set => SetValue(IsOnProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOnProperty) Set(IsOn ? "1" : null);
    }

    protected override void Show(string? value) => SetCurrentValue(IsOnProperty, value == "1");

    public override Predicate<object>? CreatePredicate(string value) =>
        value != "1" ? null : Rule ?? (Path is { Length: > 0 } path ? row => ValueAt(row, path) is true : null);
}
