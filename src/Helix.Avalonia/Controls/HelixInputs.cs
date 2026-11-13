using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Helix.Avalonia;

/// <summary>A text field with label, hint, error, icon, placeholder and clear button.</summary>
[TemplatePart("PART_Input", typeof(TextBox))]
[TemplatePart("PART_Clear", typeof(Button))]
public class HelixTextField : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<HelixTextField, string?>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<HelixTextField, string?>(nameof(Label));
    public static readonly StyledProperty<string?> PlaceholderProperty = AvaloniaProperty.Register<HelixTextField, string?>(nameof(Placeholder));
    public static readonly StyledProperty<string?> HintProperty = AvaloniaProperty.Register<HelixTextField, string?>(nameof(Hint));
    public static readonly StyledProperty<string?> ErrorProperty = AvaloniaProperty.Register<HelixTextField, string?>(nameof(Error));
    public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<HelixTextField, string?>(nameof(Icon));
    public static readonly StyledProperty<bool> IsClearableProperty = AvaloniaProperty.Register<HelixTextField, bool>(nameof(IsClearable));
    public static readonly StyledProperty<bool> IsRequiredProperty = AvaloniaProperty.Register<HelixTextField, bool>(nameof(IsRequired));
    public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<HelixTextField, bool>(nameof(IsReadOnly));
    public static readonly StyledProperty<char> PasswordCharProperty = AvaloniaProperty.Register<HelixTextField, char>(nameof(PasswordChar));
    public static readonly StyledProperty<bool> AcceptsReturnProperty = AvaloniaProperty.Register<HelixTextField, bool>(nameof(AcceptsReturn));
    public static readonly StyledProperty<object?> SuffixProperty = AvaloniaProperty.Register<HelixTextField, object?>(nameof(Suffix));

    private TextBox? _input;
    private Button? _clear;

    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Placeholder { get => GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Help text under the field.</summary>
    public string? Hint { get => GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Error under the field (red border); replaces the hint.</summary>
    public string? Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Built-in icon at the start of the field.</summary>
    public string? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>An × button that empties the field.</summary>
    public bool IsClearable { get => GetValue(IsClearableProperty); set => SetValue(IsClearableProperty, value); }

    /// <summary>A star after the label.</summary>
    public bool IsRequired { get => GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }
    public bool IsReadOnly { get => GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>Hides the text (e.g. '•' for a password).</summary>
    public char PasswordChar { get => GetValue(PasswordCharProperty); set => SetValue(PasswordCharProperty, value); }
    public bool AcceptsReturn { get => GetValue(AcceptsReturnProperty); set => SetValue(AcceptsReturnProperty, value); }

    /// <summary>Content at the end of the field, e.g. a unit "MW".</summary>
    public object? Suffix { get => GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_clear is not null) _clear.Click -= OnClear;
        _input = e.NameScope.Find<TextBox>("PART_Input");
        _clear = e.NameScope.Find<Button>("PART_Clear");
        if (_clear is not null) _clear.Click += OnClear;
        UpdateStates();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ErrorProperty || change.Property == TextProperty || change.Property == IsClearableProperty) UpdateStates();
    }

    private void UpdateStates()
    {
        PseudoClasses.Set(":error", !string.IsNullOrEmpty(Error));
        PseudoClasses.Set(":clearable", IsClearable && !string.IsNullOrEmpty(Text) && !IsReadOnly);
    }

    private void OnClear(object? sender, RoutedEventArgs e)
    {
        SetCurrentValue(TextProperty, string.Empty);
        _input?.Focus();
    }

    /// <summary>Moves the keyboard focus into the field.</summary>
    public new void Focus() => _input?.Focus();
}

/// <summary>A toggle switch with its label; Tone or Accent colors the track when on.</summary>
public class HelixSwitch : ToggleButton
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixSwitch>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixSwitch>();
    public static readonly StyledProperty<IBrush?> TrackBrushProperty = AvaloniaProperty.Register<HelixSwitch, IBrush?>(nameof(TrackBrush));
    public static readonly StyledProperty<IBrush?> ThumbBrushProperty = AvaloniaProperty.Register<HelixSwitch, IBrush?>(nameof(ThumbBrush));
    public static readonly StyledProperty<HelixLabelPosition> LabelPositionProperty =
        AvaloniaProperty.Register<HelixSwitch, HelixLabelPosition>(nameof(LabelPosition));

    protected override Type StyleKeyOverride => typeof(HelixSwitch);

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Track color when on.</summary>
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Track color when off.</summary>
    public IBrush? TrackBrush { get => GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
    public IBrush? ThumbBrush { get => GetValue(ThumbBrushProperty); set => SetValue(ThumbBrushProperty, value); }

    /// <summary>Label after (End) or before (Start) the switch.</summary>
    public HelixLabelPosition LabelPosition { get => GetValue(LabelPositionProperty); set => SetValue(LabelPositionProperty, value); }
}

/// <summary>A checkbox with an animated check; Tone or Accent colors the box when checked.</summary>
public class HelixCheckBox : CheckBox
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixCheckBox>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixCheckBox>();
    public static readonly StyledProperty<IBrush?> OnAccentProperty = HelixAccent.OnAccentProperty.AddOwner<HelixCheckBox>();
    public static readonly StyledProperty<IBrush?> ToneOnBrushProperty = HelixAccent.ToneOnBrushProperty.AddOwner<HelixCheckBox>();
    public static readonly StyledProperty<IBrush?> ResolvedOnAccentProperty = HelixAccent.ResolvedOnAccentProperty.AddOwner<HelixCheckBox>();

    private bool _customAccent;

    protected override Type StyleKeyOverride => typeof(HelixCheckBox);

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Color of the check mark; automatic for a custom Accent.</summary>
    public IBrush? OnAccent { get => GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public IBrush? ToneOnBrush { get => GetValue(ToneOnBrushProperty); set => SetValue(ToneOnBrushProperty, value); }
    public IBrush? ResolvedOnAccent => GetValue(ResolvedOnAccentProperty);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        HelixAccent.OnChanged(this, change, ref _customAccent);
    }
}
