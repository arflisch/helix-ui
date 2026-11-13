using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Helix.Avalonia;

/// <summary>Label, hint, error and required star: the shell shared by the Helix fields.</summary>
public abstract class HelixFieldBase : TemplatedControl
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixFieldBase>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixFieldBase>();
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<HelixFieldBase, string?>(nameof(Label));
    public static readonly StyledProperty<string?> HintProperty = AvaloniaProperty.Register<HelixFieldBase, string?>(nameof(Hint));
    public static readonly StyledProperty<string?> ErrorProperty = AvaloniaProperty.Register<HelixFieldBase, string?>(nameof(Error));
    public static readonly StyledProperty<bool> IsRequiredProperty = AvaloniaProperty.Register<HelixFieldBase, bool>(nameof(IsRequired));
    public static readonly StyledProperty<string?> PlaceholderProperty = AvaloniaProperty.Register<HelixFieldBase, string?>(nameof(Placeholder));
    public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<HelixFieldBase, string?>(nameof(Icon));
    public static readonly StyledProperty<CultureInfo?> CultureProperty = AvaloniaProperty.Register<HelixFieldBase, CultureInfo?>(nameof(Culture));

    /// <summary>Focus ring and selection color.</summary>
    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Help text under the field.</summary>
    public string? Hint { get => GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Error message: replaces the hint and turns the field red.</summary>
    public string? Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Adds * after the label.</summary>
    public bool IsRequired { get => GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }
    public string? Placeholder { get => GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Built-in icon at the start of the field.</summary>
    public string? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Culture of dates and numbers (the current culture by default).</summary>
    public CultureInfo? Culture { get => GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    protected CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ErrorProperty) PseudoClasses.Set(":error", !string.IsNullOrEmpty(Error));
    }
}

/// <summary>A number with − / + buttons, bounds, step, decimals and unit; optionally calculations ("120*0.8") and a signed look.</summary>
[TemplatePart("PART_Input", typeof(TextBox))]
[TemplatePart("PART_Up", typeof(Button))]
[TemplatePart("PART_Down", typeof(Button))]
public class HelixNumberField : HelixFieldBase
{
    public static readonly StyledProperty<double?> ValueProperty = AvaloniaProperty.Register<HelixNumberField, double?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double?> MinimumProperty = AvaloniaProperty.Register<HelixNumberField, double?>(nameof(Minimum));
    public static readonly StyledProperty<double?> MaximumProperty = AvaloniaProperty.Register<HelixNumberField, double?>(nameof(Maximum));
    public static readonly StyledProperty<double> StepProperty = AvaloniaProperty.Register<HelixNumberField, double>(nameof(Step), 1);
    public static readonly StyledProperty<int?> DecimalsProperty = AvaloniaProperty.Register<HelixNumberField, int?>(nameof(Decimals));
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<HelixNumberField, string?>(nameof(Unit));
    public static readonly StyledProperty<bool> ShowSteppersProperty = AvaloniaProperty.Register<HelixNumberField, bool>(nameof(ShowSteppers), true);
    public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<HelixNumberField, bool>(nameof(IsReadOnly));
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<HelixNumberField, string?>(nameof(Text), "");
    public static readonly StyledProperty<bool> IsSignedProperty = AvaloniaProperty.Register<HelixNumberField, bool>(nameof(IsSigned));
    public static readonly StyledProperty<bool> HigherIsBetterProperty = AvaloniaProperty.Register<HelixNumberField, bool>(nameof(HigherIsBetter), true);
    public static readonly StyledProperty<bool> AllowExpressionsProperty = AvaloniaProperty.Register<HelixNumberField, bool>(nameof(AllowExpressions));

    private TextBox? _input;
    private Button? _up, _down;

    public double? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double? Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double? Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Step { get => GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>Rounds and shows this many decimals.</summary>
    public int? Decimals { get => GetValue(DecimalsProperty); set => SetValue(DecimalsProperty, value); }

    /// <summary>Unit shown after the number, e.g. "MW" or "€".</summary>
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    /// <summary>The − / + buttons.</summary>
    public bool ShowSteppers { get => GetValue(ShowSteppersProperty); set => SetValue(ShowSteppersProperty, value); }
    public bool IsReadOnly { get => GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>The text in the box (formatted value while not editing).</summary>
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>"+12" in green, "−4" in red (HigherIsBetter=false swaps the colors).</summary>
    public bool IsSigned { get => GetValue(IsSignedProperty); set => SetValue(IsSignedProperty, value); }
    public bool HigherIsBetter { get => GetValue(HigherIsBetterProperty); set => SetValue(HigherIsBetterProperty, value); }

    /// <summary>Accepts calculations: "120*0.8", "=1000/4+20", "(3+4)*2" (evaluated on Enter or when leaving).</summary>
    public bool AllowExpressions { get => GetValue(AllowExpressionsProperty); set => SetValue(AllowExpressionsProperty, value); }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_input is not null)
        {
            _input.LostFocus -= OnInputLostFocus;
            _input.KeyDown -= OnInputKeyDown;
        }

        if (_up is not null) _up.Click -= OnUp;
        if (_down is not null) _down.Click -= OnDown;
        base.OnApplyTemplate(e);
        _input = e.NameScope.Find<TextBox>("PART_Input");
        _up = e.NameScope.Find<Button>("PART_Up");
        _down = e.NameScope.Find<Button>("PART_Down");
        if (_input is not null)
        {
            _input.LostFocus += OnInputLostFocus;
            _input.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
            if (AllowExpressions) ToolTip.SetTip(_input, HelixTexts.Current.ExpressionHint);
        }

        if (_up is not null) { _up.Click += OnUp; ToolTip.SetTip(_up, HelixTexts.Current.Increase); }
        if (_down is not null) { _down.Click += OnDown; ToolTip.SetTip(_down, HelixTexts.Current.Decrease); }
        ShowValue();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == DecimalsProperty || change.Property == IsSignedProperty
            || change.Property == HigherIsBetterProperty || change.Property == CultureProperty)
        {
            ShowValue();
        }
    }

    private void OnUp(object? sender, RoutedEventArgs e) => StepBy(1);
    private void OnDown(object? sender, RoutedEventArgs e) => StepBy(-1);
    private void OnInputLostFocus(object? sender, RoutedEventArgs e) => Commit();

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                Commit();
                break;
            case Key.Up when !IsReadOnly:
                StepBy(1);
                e.Handled = true;
                break;
            case Key.Down when !IsReadOnly:
                StepBy(-1);
                e.Handled = true;
                break;
        }
    }

    private void StepBy(int direction)
    {
        Commit();
        Set((Value ?? Minimum ?? 0) + direction * Step);
    }

    /// <summary>Reads the typed text: current culture first ("12,5" in French), then invariant ("12.5").</summary>
    public void Commit()
    {
        var text = (Text ?? "").Trim();
        if (text.StartsWith('+')) text = text[1..];
        if (text.Length == 0)
        {
            Set(null);
        }
        else if (AllowExpressions ? HelixExpression.TryEvaluate(text, EffectiveCulture, out var value)
                 : double.TryParse(text, NumberStyles.Float, EffectiveCulture, out value)
                   || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            Set(value);
        }
        else
        {
            ShowValue(); // invalid text: back to the last value
        }
    }

    private void Set(double? value)
    {
        if (value is { } v)
        {
            if (Minimum is { } min) v = Math.Max(min, v);
            if (Maximum is { } max) v = Math.Min(max, v);
            if (Decimals is { } d) v = Math.Round(v, d);
            value = v;
        }

        SetCurrentValue(ValueProperty, value);
        ShowValue();
    }

    private void ShowValue()
    {
        var text = Value is { } v
            ? Decimals is { } d ? v.ToString("F" + d, EffectiveCulture) : v.ToString(EffectiveCulture)
            : "";
        if (IsSigned && Value > 0) text = "+" + text;
        SetCurrentValue(TextProperty, text);
        var direction = !IsSigned || Value is null or 0 ? 0 : Math.Sign(Value.Value);
        PseudoClasses.Set(":good", direction != 0 && direction > 0 == HigherIsBetter);
        PseudoClasses.Set(":bad", direction != 0 && direction > 0 != HigherIsBetter);
    }
}

/// <summary>
/// A drop-down list with the Helix field shell: Label, Hint, Error, Placeholder, Icon, optional search box.
/// Items, ItemsSource, DisplayMemberBinding and SelectedItem work as in a ComboBox.
/// </summary>
[TemplatePart("PART_Search", typeof(TextBox))]
public class HelixSelect : ComboBox
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixSelect>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixSelect>();
    public static readonly StyledProperty<string?> LabelProperty = HelixFieldBase.LabelProperty.AddOwner<HelixSelect>();
    public static readonly StyledProperty<string?> HintProperty = HelixFieldBase.HintProperty.AddOwner<HelixSelect>();
    public static readonly StyledProperty<string?> ErrorProperty = HelixFieldBase.ErrorProperty.AddOwner<HelixSelect>();
    public static readonly StyledProperty<bool> IsRequiredProperty = HelixFieldBase.IsRequiredProperty.AddOwner<HelixSelect>();
    public static readonly StyledProperty<string?> IconProperty = HelixFieldBase.IconProperty.AddOwner<HelixSelect>();
    public static readonly StyledProperty<bool> IsSearchableProperty = AvaloniaProperty.Register<HelixSelect, bool>(nameof(IsSearchable));
    public static readonly StyledProperty<string?> SearchTextProperty = AvaloniaProperty.Register<HelixSelect, string?>(nameof(SearchText));
    public static readonly StyledProperty<string?> DisplayMemberPathProperty = AvaloniaProperty.Register<HelixSelect, string?>(nameof(DisplayMemberPath));

    private TextBox? _search;

    protected override Type StyleKeyOverride => typeof(HelixSelect);

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Hint { get => GetValue(HintProperty); set => SetValue(HintProperty, value); }
    public string? Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public bool IsRequired { get => GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }
    public string? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>A search box at the top of the list that filters the options as you type.</summary>
    public bool IsSearchable { get => GetValue(IsSearchableProperty); set => SetValue(IsSearchableProperty, value); }

    /// <summary>The text typed in the search box (emptied when the list closes).</summary>
    public string? SearchText { get => GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }

    /// <summary>Property shown for each item (e.g. "Name"); sets DisplayMemberBinding and is used by the search.</summary>
    public string? DisplayMemberPath { get => GetValue(DisplayMemberPathProperty); set => SetValue(DisplayMemberPathProperty, value); }

    public static string SearchPlaceholderText => HelixTexts.Current.Search;
    public static string SelectPlaceholderText => HelixTexts.Current.SelectPlaceholder;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_search is not null) _search.KeyDown -= OnSearchKey;
        base.OnApplyTemplate(e);
        _search = e.NameScope.Find<TextBox>("PART_Search");
        if (_search is not null) _search.AddHandler(KeyDownEvent, OnSearchKey, RoutingStrategies.Tunnel);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ErrorProperty) PseudoClasses.Set(":error", !string.IsNullOrEmpty(Error));
        else if (change.Property == SearchTextProperty) ApplySearch();
        else if (change.Property == DisplayMemberPathProperty)
        {
            DisplayMemberBinding = string.IsNullOrEmpty(DisplayMemberPath) ? null : new Binding(DisplayMemberPath);
        }
        else if (change.Property == IsDropDownOpenProperty)
        {
            if (IsDropDownOpen && IsSearchable) Dispatcher.UIThread.Post(() => _search?.Focus(), DispatcherPriority.Input);
            if (!IsDropDownOpen && !string.IsNullOrEmpty(SearchText)) SetCurrentValue(SearchTextProperty, "");
        }
    }

    /// <summary>Text of an option: DisplayMemberPath, ComboBoxItem content, or ToString.</summary>
    internal static string TextOf(object? item, string? path) => item switch
    {
        null => "",
        ContentControl content => content.Content?.ToString() ?? "",
        _ when !string.IsNullOrEmpty(path) => HelixDataGrid.Value(item, path)?.ToString() ?? "",
        _ => item.ToString() ?? "",
    };

    /// <summary>Hides the options that do not contain the search text.</summary>
    private void ApplySearch()
    {
        var term = SearchText?.Trim();
        for (var i = 0; i < ItemCount; i++)
        {
            if (ContainerFromIndex(i) is { } container)
            {
                container.IsVisible = string.IsNullOrEmpty(term) || TextOf(Items[i], DisplayMemberPath).Contains(term, StringComparison.CurrentCultureIgnoreCase);
            }
        }
    }

    private void OnSearchKey(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                for (var i = 0; i < ItemCount; i++)
                {
                    if (ContainerFromIndex(i) is { IsVisible: true })
                    {
                        SetCurrentValue(SelectedIndexProperty, i);
                        break;
                    }
                }

                SetCurrentValue(IsDropDownOpenProperty, false);
                e.Handled = true;
                break;
            case Key.Escape:
                SetCurrentValue(IsDropDownOpenProperty, false);
                e.Handled = true;
                break;
        }
    }
}

/// <summary>A chip of <see cref="HelixMultiSelect"/>: the item and its text.</summary>
public sealed record HelixChip(object Item, string Text, System.Windows.Input.ICommand? RemoveCommand = null);

/// <summary>Several choices in one field: chips in the field, checkboxes in the list, optional search, "select all" and "clear".</summary>
[TemplatePart("PART_List", typeof(Panel))]
[TemplatePart("PART_Search", typeof(TextBox))]
[TemplatePart("PART_SelectAll", typeof(Button))]
[TemplatePart("PART_Clear", typeof(Button))]
public class HelixMultiSelect : HelixFieldBase
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty = AvaloniaProperty.Register<HelixMultiSelect, IEnumerable?>(nameof(ItemsSource));
    public static readonly StyledProperty<IList?> SelectedItemsProperty = AvaloniaProperty.Register<HelixMultiSelect, IList?>(nameof(SelectedItems), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<string?> DisplayMemberPathProperty = AvaloniaProperty.Register<HelixMultiSelect, string?>(nameof(DisplayMemberPath));
    public static readonly StyledProperty<bool> IsSearchableProperty = AvaloniaProperty.Register<HelixMultiSelect, bool>(nameof(IsSearchable));
    public static readonly StyledProperty<string?> SearchTextProperty = AvaloniaProperty.Register<HelixMultiSelect, string?>(nameof(SearchText));
    public static readonly StyledProperty<int> MaxChipsProperty = AvaloniaProperty.Register<HelixMultiSelect, int>(nameof(MaxChips), 3);
    public static readonly StyledProperty<int?> MaxSelectedProperty = AvaloniaProperty.Register<HelixMultiSelect, int?>(nameof(MaxSelected));
    public static readonly StyledProperty<bool> ShowActionsProperty = AvaloniaProperty.Register<HelixMultiSelect, bool>(nameof(ShowActions), true);
    public static readonly StyledProperty<bool> IsDropDownOpenProperty = AvaloniaProperty.Register<HelixMultiSelect, bool>(nameof(IsDropDownOpen), defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<HelixMultiSelect, IReadOnlyList<HelixChip>> ChipsProperty =
        AvaloniaProperty.RegisterDirect<HelixMultiSelect, IReadOnlyList<HelixChip>>(nameof(Chips), m => m.Chips);
    public static readonly DirectProperty<HelixMultiSelect, string?> MoreTextProperty = AvaloniaProperty.RegisterDirect<HelixMultiSelect, string?>(nameof(MoreText), m => m.MoreText);
    public static readonly DirectProperty<HelixMultiSelect, bool> HasSelectionProperty = AvaloniaProperty.RegisterDirect<HelixMultiSelect, bool>(nameof(HasSelection), m => m.HasSelection);

    private Panel? _list;
    private TextBox? _search;
    private Button? _selectAll, _clear;
    private IReadOnlyList<HelixChip> _chips = [];
    private string? _moreText;
    private bool _hasSelection;

    public HelixMultiSelect() => SetCurrentValue(SelectedItemsProperty, new AvaloniaList<object>());

    public IEnumerable? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>The chosen items (two-way); a new list is assigned on each change, so a binding sees it.</summary>
    public IList? SelectedItems { get => GetValue(SelectedItemsProperty); set => SetValue(SelectedItemsProperty, value); }
    public string? DisplayMemberPath { get => GetValue(DisplayMemberPathProperty); set => SetValue(DisplayMemberPathProperty, value); }
    public bool IsSearchable { get => GetValue(IsSearchableProperty); set => SetValue(IsSearchableProperty, value); }
    public string? SearchText { get => GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }

    /// <summary>Chips shown in the field before "+N".</summary>
    public int MaxChips { get => GetValue(MaxChipsProperty); set => SetValue(MaxChipsProperty, value); }

    /// <summary>At most this many choices; the other options are disabled when reached.</summary>
    public int? MaxSelected { get => GetValue(MaxSelectedProperty); set => SetValue(MaxSelectedProperty, value); }

    /// <summary>"Select all" and "Clear" at the bottom of the list.</summary>
    public bool ShowActions { get => GetValue(ShowActionsProperty); set => SetValue(ShowActionsProperty, value); }
    public bool IsDropDownOpen { get => GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }
    public IReadOnlyList<HelixChip> Chips { get => _chips; private set => SetAndRaise(ChipsProperty, ref _chips, value); }

    /// <summary>"+2" when more items are chosen than MaxChips.</summary>
    public string? MoreText { get => _moreText; private set => SetAndRaise(MoreTextProperty, ref _moreText, value); }
    public bool HasSelection { get => _hasSelection; private set => SetAndRaise(HasSelectionProperty, ref _hasSelection, value); }

    public static string SelectAllText => HelixTexts.Current.SelectAll;
    public static string ClearText => HelixTexts.Current.Clear;
    public static string SearchPlaceholderText => HelixTexts.Current.Search;
    public static string SelectPlaceholderText => HelixTexts.Current.SelectPlaceholder;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_selectAll is not null) _selectAll.Click -= OnSelectAll;
        if (_clear is not null) _clear.Click -= OnClear;
        base.OnApplyTemplate(e);
        _list = e.NameScope.Find<Panel>("PART_List");
        _search = e.NameScope.Find<TextBox>("PART_Search");
        _selectAll = e.NameScope.Find<Button>("PART_SelectAll");
        _clear = e.NameScope.Find<Button>("PART_ClearAll");
        if (_selectAll is not null) _selectAll.Click += OnSelectAll;
        if (_clear is not null) _clear.Click += OnClear;
        BuildList();
        UpdateChips();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == ItemsSourceProperty || p == DisplayMemberPathProperty || p == MaxSelectedProperty) { BuildList(); UpdateChips(); }
        else if (p == SelectedItemsProperty || p == MaxChipsProperty) { UpdateChecks(); UpdateChips(); }
        else if (p == SearchTextProperty) ApplySearch();
        else if (p == IsDropDownOpenProperty)
        {
            if (IsDropDownOpen && IsSearchable) Dispatcher.UIThread.Post(() => _search?.Focus(), DispatcherPriority.Input);
            if (!IsDropDownOpen && !string.IsNullOrEmpty(SearchText)) SetCurrentValue(SearchTextProperty, "");
        }
    }

    private IReadOnlyList<object> Options => ItemsSource?.Cast<object>().ToList() ?? [];

    private string TextOf(object item) => HelixSelect.TextOf(item, DisplayMemberPath);

    private bool IsSelected(object item) => SelectedItems?.Contains(item) == true;

    private void BuildList()
    {
        if (_list is null) return;
        _list.Children.Clear();
        foreach (var item in Options)
        {
            var box = new HelixCheckBox { Content = TextOf(item), Tag = item, Margin = new Thickness(10, 6), IsChecked = IsSelected(item), HorizontalAlignment = HorizontalAlignment.Stretch };
            box.IsCheckedChanged += (_, _) => Toggle(item, box.IsChecked == true);
            _list.Children.Add(box);
        }

        UpdateChecks();
    }

    private void Toggle(object item, bool selected)
    {
        if (selected == IsSelected(item)) return;
        var list = (SelectedItems?.Cast<object>() ?? []).ToList();
        if (selected) list.Add(item); else list.Remove(item);
        // Keeps the options' order, so the chips read like the list.
        var ordered = Options.Where(list.Contains).ToList();
        SetCurrentValue(SelectedItemsProperty, new AvaloniaList<object>(ordered));
    }

    private void UpdateChecks()
    {
        if (_list is null) return;
        var count = SelectedItems?.Count ?? 0;
        foreach (var box in _list.Children.OfType<HelixCheckBox>())
        {
            var selected = box.Tag is { } item && IsSelected(item);
            if (box.IsChecked != selected) box.IsChecked = selected;
            box.IsEnabled = selected || MaxSelected is not { } max || count < max;
        }
    }

    private void UpdateChips()
    {
        var selected = (SelectedItems?.Cast<object>() ?? []).ToList();
        HasSelection = selected.Count > 0;
        var max = Math.Max(0, MaxChips);
        Chips = selected.Take(max).Select(i => new HelixChip(i, TextOf(i), new HelixCommand(() => Remove(i)))).ToList();
        MoreText = selected.Count > max ? $"+{selected.Count - max}" : null;
    }

    private void ApplySearch()
    {
        if (_list is null) return;
        var term = SearchText?.Trim();
        foreach (var box in _list.Children.OfType<HelixCheckBox>())
        {
            box.IsVisible = string.IsNullOrEmpty(term) || (box.Content?.ToString() ?? "").Contains(term, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    private void OnSelectAll(object? sender, RoutedEventArgs e)
    {
        var visible = _list?.Children.OfType<HelixCheckBox>().Where(b => b.IsVisible).Select(b => b.Tag!).ToList() ?? [];
        var list = (SelectedItems?.Cast<object>() ?? []).Union(visible).ToList();
        if (MaxSelected is { } max) list = list.Take(max).ToList();
        SetCurrentValue(SelectedItemsProperty, new AvaloniaList<object>(Options.Where(list.Contains)));
    }

    private void OnClear(object? sender, RoutedEventArgs e) => SetCurrentValue(SelectedItemsProperty, new AvaloniaList<object>());

    /// <summary>Removes one chosen item (the × of a chip).</summary>
    public void Remove(object item) => Toggle(item, false);
}
