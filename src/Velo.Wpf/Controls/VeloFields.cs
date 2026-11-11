using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Velo.Wpf;

/// <summary>
/// The shell shared by every Velo field: label (with * when required), the control, then the error or the hint.
/// Also usable around your own controls: &lt;velo:VeloField Label="Code" Hint="6 digits"&gt;…&lt;/velo:VeloField&gt;.
/// </summary>
public class VeloField : ContentControl
{
    static VeloField() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloField), new FrameworkPropertyMetadata(typeof(VeloField)));

    public static readonly DependencyProperty LabelProperty = VeloProperty.Register<VeloField, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = VeloProperty.Register<VeloField, string?>(nameof(Hint), null, (d, _) => ((VeloField)d).UpdateMessage());
    public static readonly DependencyProperty ErrorProperty = VeloProperty.Register<VeloField, string?>(nameof(Error), null, (d, _) => ((VeloField)d).UpdateMessage());
    public static readonly DependencyProperty IsRequiredProperty = VeloProperty.Register<VeloField, bool>(nameof(IsRequired), false);

    private static readonly DependencyPropertyKey MessageKey = DependencyProperty.RegisterReadOnly(nameof(Message), typeof(string), typeof(VeloField), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(VeloField), new PropertyMetadata(false));
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
public class VeloTextField : TextBox
{
    static VeloTextField() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloTextField), new FrameworkPropertyMetadata(typeof(VeloTextField)));

    public VeloTextField() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloTextField));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloTextField));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloTextField));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloTextField));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloTextField));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloTextField));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);
    public static readonly DependencyProperty LabelProperty = VeloProperty.Register<VeloTextField, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = VeloProperty.Register<VeloTextField, string?>(nameof(Hint), null);
    public static readonly DependencyProperty ErrorProperty = VeloProperty.Register<VeloTextField, string?>(nameof(Error), null,
        (d, e) => d.SetValue(HasErrorKey, !string.IsNullOrEmpty((string?)e.NewValue)));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(VeloTextField), new PropertyMetadata(false));
    public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;

    /// <summary>True while an Error is shown.</summary>
    public bool HasError => (bool)GetValue(HasErrorProperty);
    public static readonly DependencyProperty IsRequiredProperty = VeloProperty.Register<VeloTextField, bool>(nameof(IsRequired), false);
    public static readonly DependencyProperty PrefixProperty = VeloProperty.Register<VeloTextField, object?>(nameof(Prefix), null);
    public static readonly DependencyProperty SuffixProperty = VeloProperty.Register<VeloTextField, object?>(nameof(Suffix), null);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloTextField, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

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

    public static readonly DependencyProperty PlaceholderProperty = VeloProperty.Register<VeloTextField, string?>(nameof(Placeholder), null);
    public static readonly DependencyProperty IconProperty = VeloProperty.Register<VeloTextField, string?>(nameof(Icon), null);
    public static readonly DependencyProperty IsClearableProperty = VeloProperty.Register<VeloTextField, bool>(nameof(IsClearable), false);

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
            _clear.ToolTip = VeloTexts.Current.Clear;
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
public class VeloNumberField : Control
{
    static VeloNumberField() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloNumberField), new FrameworkPropertyMetadata(typeof(VeloNumberField)));

    public VeloNumberField() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloNumberField));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloNumberField));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloNumberField));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloNumberField));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloNumberField));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloNumberField));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);
    public static readonly DependencyProperty LabelProperty = VeloProperty.Register<VeloNumberField, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = VeloProperty.Register<VeloNumberField, string?>(nameof(Hint), null);
    public static readonly DependencyProperty ErrorProperty = VeloProperty.Register<VeloNumberField, string?>(nameof(Error), null,
        (d, e) => d.SetValue(HasErrorKey, !string.IsNullOrEmpty((string?)e.NewValue)));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(VeloNumberField), new PropertyMetadata(false));
    public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;

    /// <summary>True while an Error is shown.</summary>
    public bool HasError => (bool)GetValue(HasErrorProperty);
    public static readonly DependencyProperty IsRequiredProperty = VeloProperty.Register<VeloNumberField, bool>(nameof(IsRequired), false);
    public static readonly DependencyProperty PrefixProperty = VeloProperty.Register<VeloNumberField, object?>(nameof(Prefix), null);
    public static readonly DependencyProperty SuffixProperty = VeloProperty.Register<VeloNumberField, object?>(nameof(Suffix), null);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloNumberField, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

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

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double?), typeof(VeloNumberField),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((VeloNumberField)d).ShowValue()));
    public static readonly DependencyProperty MinimumProperty = VeloProperty.Register<VeloNumberField, double?>(nameof(Minimum), null);
    public static readonly DependencyProperty MaximumProperty = VeloProperty.Register<VeloNumberField, double?>(nameof(Maximum), null);
    public static readonly DependencyProperty StepProperty = VeloProperty.Register<VeloNumberField, double>(nameof(Step), 1d);
    public static readonly DependencyProperty DecimalsProperty = VeloProperty.Register<VeloNumberField, int?>(nameof(Decimals), null, (d, _) => ((VeloNumberField)d).ShowValue());
    public static readonly DependencyProperty UnitProperty = VeloProperty.Register<VeloNumberField, string?>(nameof(Unit), null);
    public static readonly DependencyProperty PlaceholderProperty = VeloProperty.Register<VeloNumberField, string?>(nameof(Placeholder), null);
    public static readonly DependencyProperty ShowSteppersProperty = VeloProperty.Register<VeloNumberField, bool>(nameof(ShowSteppers), true);
    public static readonly DependencyProperty IsReadOnlyProperty = VeloProperty.Register<VeloNumberField, bool>(nameof(IsReadOnly), false);
    public static readonly DependencyProperty TextProperty = VeloProperty.Register<VeloNumberField, string>(nameof(Text), "");

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
            _up.ToolTip = VeloTexts.Current.Increase;
        }

        if (_down is not null)
        {
            _down.Click += OnDown;
            _down.ToolTip = VeloTexts.Current.Decrease;
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
/// A drop-down list with the Velo field shell: Label, Hint, Error, Placeholder, Prefix / Suffix, Accent, CornerRadius.
/// Items, ItemsSource, DisplayMemberPath and SelectedItem work as in a ComboBox.
/// </summary>
public class VeloSelect : ComboBox
{
    static VeloSelect() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloSelect), new FrameworkPropertyMetadata(typeof(VeloSelect)));

    public VeloSelect()
    {
        VeloAccent.Init(this);
        SetCurrentValue(PlaceholderProperty, VeloTexts.Current.SelectPlaceholder);
    }

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloSelect));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloSelect));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloSelect));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloSelect));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloSelect));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloSelect));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);
    public static readonly DependencyProperty LabelProperty = VeloProperty.Register<VeloSelect, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = VeloProperty.Register<VeloSelect, string?>(nameof(Hint), null);
    public static readonly DependencyProperty ErrorProperty = VeloProperty.Register<VeloSelect, string?>(nameof(Error), null,
        (d, e) => d.SetValue(HasErrorKey, !string.IsNullOrEmpty((string?)e.NewValue)));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(VeloSelect), new PropertyMetadata(false));
    public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;

    /// <summary>True while an Error is shown.</summary>
    public bool HasError => (bool)GetValue(HasErrorProperty);
    public static readonly DependencyProperty IsRequiredProperty = VeloProperty.Register<VeloSelect, bool>(nameof(IsRequired), false);
    public static readonly DependencyProperty PrefixProperty = VeloProperty.Register<VeloSelect, object?>(nameof(Prefix), null);
    public static readonly DependencyProperty SuffixProperty = VeloProperty.Register<VeloSelect, object?>(nameof(Suffix), null);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloSelect, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

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

    public static readonly DependencyProperty PlaceholderProperty = VeloProperty.Register<VeloSelect, string?>(nameof(Placeholder), null);

    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
}

/// <summary>A date picker with the Velo field shell and calendar: Label, Hint, Error, Accent, CornerRadius.</summary>
public class VeloDatePicker : DatePicker
{
    static VeloDatePicker() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloDatePicker), new FrameworkPropertyMetadata(typeof(VeloDatePicker)));

    public VeloDatePicker() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloDatePicker));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloDatePicker));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloDatePicker));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloDatePicker));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloDatePicker));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloDatePicker));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);
    public static readonly DependencyProperty LabelProperty = VeloProperty.Register<VeloDatePicker, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = VeloProperty.Register<VeloDatePicker, string?>(nameof(Hint), null);
    public static readonly DependencyProperty ErrorProperty = VeloProperty.Register<VeloDatePicker, string?>(nameof(Error), null,
        (d, e) => d.SetValue(HasErrorKey, !string.IsNullOrEmpty((string?)e.NewValue)));
    private static readonly DependencyPropertyKey HasErrorKey = DependencyProperty.RegisterReadOnly(nameof(HasError), typeof(bool), typeof(VeloDatePicker), new PropertyMetadata(false));
    public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;

    /// <summary>True while an Error is shown.</summary>
    public bool HasError => (bool)GetValue(HasErrorProperty);
    public static readonly DependencyProperty IsRequiredProperty = VeloProperty.Register<VeloDatePicker, bool>(nameof(IsRequired), false);
    public static readonly DependencyProperty PrefixProperty = VeloProperty.Register<VeloDatePicker, object?>(nameof(Prefix), null);
    public static readonly DependencyProperty SuffixProperty = VeloProperty.Register<VeloDatePicker, object?>(nameof(Suffix), null);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloDatePicker, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

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
