using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Helix.Wpf;

/// <summary>
/// The shell shared by every Helix field: label (with * when required), the control, then the error or the hint.
/// Also usable around your own controls: &lt;helix:HelixField Label="Code" Hint="6 digits"&gt;…&lt;/helix:HelixField&gt;.
/// </summary>
public class HelixField : ContentControl
{
    static HelixField() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixField), new FrameworkPropertyMetadata(typeof(HelixField)));

    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixField, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = HelixProperty.Register<HelixField, string?>(nameof(Hint), null, (d, _) => ((HelixField)d).UpdateMessage());
    public static readonly DependencyProperty ErrorProperty = HelixProperty.Register<HelixField, string?>(nameof(Error), null, (d, _) => ((HelixField)d).UpdateMessage());
    public static readonly DependencyProperty IsRequiredProperty = HelixProperty.Register<HelixField, bool>(nameof(IsRequired), false);

    private static readonly DependencyPropertyKey MessageKey = DependencyProperty.RegisterReadOnly(nameof(Message), typeof(string), typeof(HelixField), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(HelixField), new PropertyMetadata(false));
    public static readonly DependencyProperty MessageProperty = MessageKey.DependencyProperty;
    public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Help text under the control.</summary>
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Error message; replaces the hint and turns the field red.</summary>
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public bool IsRequired { get => (bool)GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }

    /// <summary>The error, else the hint.</summary>
    public string? Message => (string?)GetValue(MessageProperty);
    public bool HasError => (bool)GetValue(HasErrorProperty);

    private void UpdateMessage()
    {
        var hasError = !string.IsNullOrEmpty(Error);
        SetValue(HasErrorKey, hasError);
        SetValue(MessageKey, hasError ? Error : string.IsNullOrEmpty(Hint) ? null : Hint);
    }
}

/// <summary>
/// A text field: Label, Hint, Error, Placeholder, Icon, Prefix / Suffix (any content), a clear button,
/// and Accent (focus color), CornerRadius, Background, BorderBrush.
/// </summary>
[TemplatePart(Name = "PART_Clear", Type = typeof(ButtonBase))]
public class HelixTextField : TextBox
{
    static HelixTextField() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixTextField), new FrameworkPropertyMetadata(typeof(HelixTextField)));

    public HelixTextField() => HelixAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixTextField));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixTextField));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixTextField));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixTextField));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixTextField));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixTextField));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);
    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixTextField, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = HelixProperty.Register<HelixTextField, string?>(nameof(Hint), null);
    public static readonly DependencyProperty ErrorProperty = HelixProperty.Register<HelixTextField, string?>(nameof(Error), null,
        (d, e) => d.SetValue(HasErrorKey, !string.IsNullOrEmpty((string?)e.NewValue)));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(HelixTextField), new PropertyMetadata(false));
    public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;

    /// <summary>True while an Error is shown.</summary>
    public bool HasError => (bool)GetValue(HasErrorProperty);
    public static readonly DependencyProperty IsRequiredProperty = HelixProperty.Register<HelixTextField, bool>(nameof(IsRequired), false);
    public static readonly DependencyProperty PrefixProperty = HelixProperty.Register<HelixTextField, object?>(nameof(Prefix), null);
    public static readonly DependencyProperty SuffixProperty = HelixProperty.Register<HelixTextField, object?>(nameof(Suffix), null);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixTextField, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Help text under the field.</summary>
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Error message: replaces the hint and turns the field red.</summary>
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Adds * after the label.</summary>
    public bool IsRequired { get => (bool)GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }

    /// <summary>Content at the start of the input (icon, sign, currency…).</summary>
    public object? Prefix { get => GetValue(PrefixProperty); set => SetValue(PrefixProperty, value); }

    /// <summary>Content at the end of the input (unit, button…).</summary>
    public object? Suffix { get => GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public static readonly DependencyProperty PlaceholderProperty = HelixProperty.Register<HelixTextField, string?>(nameof(Placeholder), null);
    public static readonly DependencyProperty IconProperty = HelixProperty.Register<HelixTextField, string?>(nameof(Icon), null);
    public static readonly DependencyProperty IsClearableProperty = HelixProperty.Register<HelixTextField, bool>(nameof(IsClearable), false);

    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Built-in icon at the start (e.g. "search", "mail").</summary>
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Shows a × button while there is text.</summary>
    public bool IsClearable { get => (bool)GetValue(IsClearableProperty); set => SetValue(IsClearableProperty, value); }

    private ButtonBase? _clear;

    public override void OnApplyTemplate()
    {
        if (_clear is not null)
        {
            _clear.Click -= OnClear;
        }

        base.OnApplyTemplate();
        _clear = GetTemplateChild("PART_Clear") as ButtonBase;
        if (_clear is not null)
        {
            _clear.Click += OnClear;
            _clear.ToolTip = HelixTexts.Current.Clear;
        }
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        SetCurrentValue(TextProperty, string.Empty);
        Focus();
    }
}

/// <summary>
/// A number field: Value (double?, two-way), Min, Max, Step (± buttons and arrow keys), Decimals, Unit,
/// plus everything a text field has (Label, Hint, Error, Prefix, Suffix…).
/// </summary>
[TemplatePart(Name = "PART_Input", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_Up", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_Down", Type = typeof(ButtonBase))]
public class HelixNumberField : Control
{
    static HelixNumberField() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixNumberField), new FrameworkPropertyMetadata(typeof(HelixNumberField)));

    public HelixNumberField() => HelixAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixNumberField));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixNumberField));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixNumberField));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixNumberField));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixNumberField));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixNumberField));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);
    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixNumberField, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = HelixProperty.Register<HelixNumberField, string?>(nameof(Hint), null);
    public static readonly DependencyProperty ErrorProperty = HelixProperty.Register<HelixNumberField, string?>(nameof(Error), null,
        (d, e) => d.SetValue(HasErrorKey, !string.IsNullOrEmpty((string?)e.NewValue)));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(HelixNumberField), new PropertyMetadata(false));
    public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;

    /// <summary>True while an Error is shown.</summary>
    public bool HasError => (bool)GetValue(HasErrorProperty);
    public static readonly DependencyProperty IsRequiredProperty = HelixProperty.Register<HelixNumberField, bool>(nameof(IsRequired), false);
    public static readonly DependencyProperty PrefixProperty = HelixProperty.Register<HelixNumberField, object?>(nameof(Prefix), null);
    public static readonly DependencyProperty SuffixProperty = HelixProperty.Register<HelixNumberField, object?>(nameof(Suffix), null);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixNumberField, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Help text under the field.</summary>
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Error message: replaces the hint and turns the field red.</summary>
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Adds * after the label.</summary>
    public bool IsRequired { get => (bool)GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }

    /// <summary>Content at the start of the input (icon, sign, currency…).</summary>
    public object? Prefix { get => GetValue(PrefixProperty); set => SetValue(PrefixProperty, value); }

    /// <summary>Content at the end of the input (unit, button…).</summary>
    public object? Suffix { get => GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double?), typeof(HelixNumberField),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HelixNumberField)d).ShowValue()));
    public static readonly DependencyProperty MinimumProperty = HelixProperty.Register<HelixNumberField, double?>(nameof(Minimum), null);
    public static readonly DependencyProperty MaximumProperty = HelixProperty.Register<HelixNumberField, double?>(nameof(Maximum), null);
    public static readonly DependencyProperty StepProperty = HelixProperty.Register<HelixNumberField, double>(nameof(Step), 1d);
    public static readonly DependencyProperty DecimalsProperty = HelixProperty.Register<HelixNumberField, int?>(nameof(Decimals), null, (d, _) => ((HelixNumberField)d).ShowValue());
    public static readonly DependencyProperty UnitProperty = HelixProperty.Register<HelixNumberField, string?>(nameof(Unit), null);
    public static readonly DependencyProperty PlaceholderProperty = HelixProperty.Register<HelixNumberField, string?>(nameof(Placeholder), null);
    public static readonly DependencyProperty ShowSteppersProperty = HelixProperty.Register<HelixNumberField, bool>(nameof(ShowSteppers), true);
    public static readonly DependencyProperty IsReadOnlyProperty = HelixProperty.Register<HelixNumberField, bool>(nameof(IsReadOnly), false);
    public static readonly DependencyProperty TextProperty = HelixProperty.Register<HelixNumberField, string>(nameof(Text), "");

    public double? Value { get => (double?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double? Minimum { get => (double?)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double? Maximum { get => (double?)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>Decimals kept and shown (rounding); null keeps what was typed.</summary>
    public int? Decimals { get => (int?)GetValue(DecimalsProperty); set => SetValue(DecimalsProperty, value); }

    /// <summary>Unit shown after the number, e.g. "MW" or "€".</summary>
    public string? Unit { get => (string?)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>The − / + buttons.</summary>
    public bool ShowSteppers { get => (bool)GetValue(ShowSteppersProperty); set => SetValue(ShowSteppersProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>The text in the box (formatted value while not editing).</summary>
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    private TextBox? _input;
    private ButtonBase? _up, _down;

    public override void OnApplyTemplate()
    {
        if (_input is not null)
        {
            _input.LostKeyboardFocus -= OnInputLostFocus;
            _input.PreviewKeyDown -= OnInputKeyDown;
        }

        if (_up is not null) _up.Click -= OnUp;
        if (_down is not null) _down.Click -= OnDown;

        base.OnApplyTemplate();
        _input = GetTemplateChild("PART_Input") as TextBox;
        _up = GetTemplateChild("PART_Up") as ButtonBase;
        _down = GetTemplateChild("PART_Down") as ButtonBase;

        if (_input is not null)
        {
            _input.LostKeyboardFocus += OnInputLostFocus;
            _input.PreviewKeyDown += OnInputKeyDown;
        }

        if (_up is not null)
        {
            _up.Click += OnUp;
            _up.ToolTip = HelixTexts.Current.Increase;
        }

        if (_down is not null)
        {
            _down.Click += OnDown;
            _down.ToolTip = HelixTexts.Current.Decrease;
        }

        ShowValue();
    }

    private void OnUp(object sender, RoutedEventArgs e) => StepBy(1);
    private void OnDown(object sender, RoutedEventArgs e) => StepBy(-1);

    private void OnInputLostFocus(object sender, KeyboardFocusChangedEventArgs e) => Commit();

    private void OnInputKeyDown(object sender, KeyEventArgs e)
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
    private void Commit()
    {
        var text = Text.Trim();
        if (text.Length == 0)
        {
            Set(null);
        }
        else if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value)
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
            ? Decimals is { } d ? v.ToString("F" + d, CultureInfo.CurrentCulture) : v.ToString(CultureInfo.CurrentCulture)
            : "";
        SetCurrentValue(TextProperty, text);
    }
}

/// <summary>
/// A drop-down list with the Helix field shell: Label, Hint, Error, Placeholder, Prefix / Suffix, Accent, CornerRadius.
/// Items, ItemsSource, DisplayMemberPath and SelectedItem work as in a ComboBox.
/// </summary>
public class HelixSelect : ComboBox
{
    static HelixSelect() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixSelect), new FrameworkPropertyMetadata(typeof(HelixSelect)));

    public HelixSelect()
    {
        HelixAccent.Init(this);
        SetCurrentValue(PlaceholderProperty, HelixTexts.Current.SelectPlaceholder);
    }

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixSelect));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixSelect));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixSelect));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixSelect));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixSelect));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixSelect));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);
    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixSelect, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = HelixProperty.Register<HelixSelect, string?>(nameof(Hint), null);
    public static readonly DependencyProperty ErrorProperty = HelixProperty.Register<HelixSelect, string?>(nameof(Error), null,
        (d, e) => d.SetValue(HasErrorKey, !string.IsNullOrEmpty((string?)e.NewValue)));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(HelixSelect), new PropertyMetadata(false));
    public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;

    /// <summary>True while an Error is shown.</summary>
    public bool HasError => (bool)GetValue(HasErrorProperty);
    public static readonly DependencyProperty IsRequiredProperty = HelixProperty.Register<HelixSelect, bool>(nameof(IsRequired), false);
    public static readonly DependencyProperty PrefixProperty = HelixProperty.Register<HelixSelect, object?>(nameof(Prefix), null);
    public static readonly DependencyProperty SuffixProperty = HelixProperty.Register<HelixSelect, object?>(nameof(Suffix), null);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixSelect, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Help text under the field.</summary>
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Error message: replaces the hint and turns the field red.</summary>
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Adds * after the label.</summary>
    public bool IsRequired { get => (bool)GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }

    /// <summary>Content at the start of the input (icon, sign, currency…).</summary>
    public object? Prefix { get => GetValue(PrefixProperty); set => SetValue(PrefixProperty, value); }

    /// <summary>Content at the end of the input (unit, button…).</summary>
    public object? Suffix { get => GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public static readonly DependencyProperty PlaceholderProperty = HelixProperty.Register<HelixSelect, string?>(nameof(Placeholder), null);

    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    public static readonly DependencyProperty IsSearchableProperty = HelixProperty.Register<HelixSelect, bool>(nameof(IsSearchable), false);
    public static readonly DependencyProperty SearchTextProperty = HelixProperty.Register<HelixSelect, string>(nameof(SearchText), "", (d, _) => ((HelixSelect)d).ApplySearch());

    /// <summary>A search box at the top of the list that filters the options as you type (on DisplayMemberPath or ToString).</summary>
    public bool IsSearchable { get => (bool)GetValue(IsSearchableProperty); set => SetValue(IsSearchableProperty, value); }

    /// <summary>The text typed in the search box (emptied when the list closes).</summary>
    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }

    public static string SearchPlaceholderText => HelixTexts.Current.Search;

    private TextBox? _search;

    public override void OnApplyTemplate()
    {
        if (_search is not null) _search.PreviewKeyDown -= OnSearchKey;
        base.OnApplyTemplate();
        _search = GetTemplateChild("PART_Search") as TextBox;
        if (_search is not null) _search.PreviewKeyDown += OnSearchKey;
    }

    protected override void OnDropDownOpened(EventArgs e)
    {
        base.OnDropDownOpened(e);
        if (IsSearchable && _search is not null) Dispatcher.BeginInvoke(() => _search.Focus(), System.Windows.Threading.DispatcherPriority.Input);
    }

    protected override void OnDropDownClosed(EventArgs e)
    {
        base.OnDropDownClosed(e);
        if (SearchText.Length > 0) SetCurrentValue(SearchTextProperty, "");
    }

    /// <summary>Text of an option: DisplayMemberPath, ComboBoxItem content, or ToString.</summary>
    internal static string TextOf(object? item, string? path) => item switch
    {
        null => "",
        ContentControl content => content.Content?.ToString() ?? "",
        _ when !string.IsNullOrEmpty(path) => HelixDataGrid.Value(item, path)?.ToString() ?? "",
        _ => item.ToString() ?? "",
    };

    /// <summary>Filters the options; with ItemsSource the filter is set on its default view while the list is open.</summary>
    private void ApplySearch()
    {
        var term = SearchText?.Trim();
        Items.Filter = string.IsNullOrEmpty(term) ? null : item => TextOf(item, DisplayMemberPath).Contains(term, StringComparison.CurrentCultureIgnoreCase);
    }

    private void OnSearchKey(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (ItemContainerGenerator.ContainerFromIndex(0) is ComboBoxItem first) first.Focus();
                e.Handled = true;
                break;
            case Key.Enter:
                if (Items.Count > 0) SetCurrentValue(SelectedItemProperty, Items[0]);
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

/// <summary>A date picker with the Helix field shell and calendar: Label, Hint, Error, Accent, CornerRadius.</summary>
public class HelixDatePicker : DatePicker
{
    static HelixDatePicker() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixDatePicker), new FrameworkPropertyMetadata(typeof(HelixDatePicker)));

    public HelixDatePicker() => HelixAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixDatePicker));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixDatePicker));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixDatePicker));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixDatePicker));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixDatePicker));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixDatePicker));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);
    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixDatePicker, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = HelixProperty.Register<HelixDatePicker, string?>(nameof(Hint), null);
    public static readonly DependencyProperty ErrorProperty = HelixProperty.Register<HelixDatePicker, string?>(nameof(Error), null,
        (d, e) => d.SetValue(HasErrorKey, !string.IsNullOrEmpty((string?)e.NewValue)));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(HelixDatePicker), new PropertyMetadata(false));
    public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;

    /// <summary>True while an Error is shown.</summary>
    public bool HasError => (bool)GetValue(HasErrorProperty);
    public static readonly DependencyProperty IsRequiredProperty = HelixProperty.Register<HelixDatePicker, bool>(nameof(IsRequired), false);
    public static readonly DependencyProperty PrefixProperty = HelixProperty.Register<HelixDatePicker, object?>(nameof(Prefix), null);
    public static readonly DependencyProperty SuffixProperty = HelixProperty.Register<HelixDatePicker, object?>(nameof(Suffix), null);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixDatePicker, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Help text under the field.</summary>
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Error message: replaces the hint and turns the field red.</summary>
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Adds * after the label.</summary>
    public bool IsRequired { get => (bool)GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }

    /// <summary>Content at the start of the input (icon, sign, currency…).</summary>
    public object? Prefix { get => GetValue(PrefixProperty); set => SetValue(PrefixProperty, value); }

    /// <summary>Content at the end of the input (unit, button…).</summary>
    public object? Suffix { get => GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
}
