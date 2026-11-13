using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Helix.Avalonia;

public enum HelixRadioAppearance { Default, Cards, Segmented }

/// <summary>An option of <see cref="HelixRadioGroup"/>: title, optional description and icon, and the value it stands for.</summary>
public sealed class HelixRadioOption
{
    public HelixRadioOption() { }

    public HelixRadioOption(string title, object? value = null, string? description = null, string? icon = null) =>
        (Title, Value, Description, Icon) = (title, value ?? title, description, icon);

    public string Title { get; set; } = "";
    public string? Description { get; set; }

    /// <summary>Built-in icon (Cards appearance).</summary>
    public string? Icon { get; set; }
    public object? Value { get; set; }
    public bool IsEnabled { get; set; } = true;

    public bool HasDescription => !string.IsNullOrEmpty(Description);
    public bool HasIcon => !string.IsNullOrEmpty(Icon);

    public override string ToString() => Title;
}

/// <summary>
/// One choice among several: classic radio buttons, cards (title, description, icon) or a segmented control.
/// Items are strings or <see cref="HelixRadioOption"/>; SelectedItem / SelectedIndex work as in a ListBox.
/// </summary>
public class HelixRadioGroup : ListBox
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixRadioGroup>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixRadioGroup>();
    public static readonly StyledProperty<HelixRadioAppearance> AppearanceProperty = AvaloniaProperty.Register<HelixRadioGroup, HelixRadioAppearance>(nameof(Appearance));
    public static readonly StyledProperty<string?> LabelProperty = HelixFieldBase.LabelProperty.AddOwner<HelixRadioGroup>();
    public static readonly StyledProperty<string?> HintProperty = HelixFieldBase.HintProperty.AddOwner<HelixRadioGroup>();
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<HelixRadioGroup, Orientation>(nameof(Orientation), Orientation.Vertical);

    protected override Type StyleKeyOverride => typeof(HelixRadioGroup);

    public HelixRadioGroup()
    {
        // HelixRadioOption: title, description and (cards) icon; other items show their text.
        DataTemplates.Add(new global::Avalonia.Controls.Templates.FuncDataTemplate<HelixRadioOption>((option, _) =>
        {
            var title = new TextBlock { Text = option.Title, FontWeight = FontWeight.Medium, TextWrapping = TextWrapping.Wrap };
            var text = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center, Children = { title } };
            if (option.HasDescription && Appearance != HelixRadioAppearance.Segmented)
            {
                var description = new TextBlock { Text = option.Description, FontSize = 12.5, TextWrapping = TextWrapping.Wrap };
                description.Bind(TextBlock.ForegroundProperty, description.GetResourceObservable(HelixKeys.TextMuted));
                text.Children.Add(description);
            }

            if (!option.HasIcon || Appearance != HelixRadioAppearance.Cards) return text;
            var icon = new HelixIcon { Icon = option.Icon, Size = 18, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 1, 0, 0) };
            return new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Children = { icon, text } };
        }));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AppearanceProperty) ApplyItemTheme();
    }

    protected override void OnAttachedToLogicalTree(global::Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        ApplyItemTheme();
    }

    /// <summary>Each appearance has its own item look (radio, card, segment).</summary>
    private void ApplyItemTheme()
    {
        var key = Appearance switch { HelixRadioAppearance.Cards => "HelixRadioCard", HelixRadioAppearance.Segmented => "HelixRadioSegment", _ => "HelixRadioItem" };
        if (this.TryFindResource(key, out var theme) && theme is global::Avalonia.Styling.ControlTheme controlTheme) ItemContainerTheme = controlTheme;
    }

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Default (radio buttons), Cards or Segmented.</summary>
    public HelixRadioAppearance Appearance { get => GetValue(AppearanceProperty); set => SetValue(AppearanceProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Hint { get => GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Vertical (default) or Horizontal; Segmented is always horizontal.</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>The Value of the selected HelixRadioOption, else the selected item.</summary>
    public object? Value => SelectedItem is HelixRadioOption option ? option.Value : SelectedItem;

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (item is HelixRadioOption { IsEnabled: false }) container.IsEnabled = false;
    }
}

/// <summary>A slider with label and value, any tone or accent.</summary>
public class HelixSlider : Slider
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixSlider>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixSlider>();
    public static readonly StyledProperty<IBrush?> TrackBrushProperty = AvaloniaProperty.Register<HelixSlider, IBrush?>(nameof(TrackBrush));
    public static readonly StyledProperty<string?> LabelProperty = HelixFieldBase.LabelProperty.AddOwner<HelixSlider>();
    public static readonly StyledProperty<bool> ShowValueProperty = AvaloniaProperty.Register<HelixSlider, bool>(nameof(ShowValue), true);
    public static readonly StyledProperty<string> FormatProperty = AvaloniaProperty.Register<HelixSlider, string>(nameof(Format), "0.##");
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<HelixSlider, string?>(nameof(Unit));
    public static readonly DirectProperty<HelixSlider, string> ValueTextProperty = AvaloniaProperty.RegisterDirect<HelixSlider, string>(nameof(ValueText), s => s.ValueText);

    private string _valueText = "";

    protected override Type StyleKeyOverride => typeof(HelixSlider);

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public IBrush? TrackBrush { get => GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>The value on the right of the label.</summary>
    public bool ShowValue { get => GetValue(ShowValueProperty); set => SetValue(ShowValueProperty, value); }
    public string Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string ValueText { get => _valueText; private set => SetAndRaise(ValueTextProperty, ref _valueText, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == FormatProperty || change.Property == UnitProperty)
        {
            ValueText = Value.ToString(Format, CultureInfo.CurrentCulture) + (string.IsNullOrEmpty(Unit) ? "" : " " + Unit);
        }
    }
}

/// <summary>A rating with stars or hearts: click to set, hover to preview, optional labels ("Poor"… "Excellent") and count.</summary>
[TemplatePart("PART_Icons", typeof(Panel))]
public class HelixRating : TemplatedControl
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixRating>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixRating>();
    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<HelixRating, double>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<int> MaxProperty = AvaloniaProperty.Register<HelixRating, int>(nameof(Max), 5);
    public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<HelixRating, bool>(nameof(IsReadOnly));
    public static readonly StyledProperty<bool> AllowClearProperty = AvaloniaProperty.Register<HelixRating, bool>(nameof(AllowClear), true);
    public static readonly StyledProperty<string> ShapeProperty = AvaloniaProperty.Register<HelixRating, string>(nameof(Shape), "star");
    public static readonly StyledProperty<HelixSize> SizeProperty = AvaloniaProperty.Register<HelixRating, HelixSize>(nameof(Size), HelixSize.Md);
    public static readonly StyledProperty<IList<string>?> LabelsProperty = AvaloniaProperty.Register<HelixRating, IList<string>?>(nameof(Labels));
    public static readonly StyledProperty<bool> ShowValueProperty = AvaloniaProperty.Register<HelixRating, bool>(nameof(ShowValue));
    public static readonly StyledProperty<int?> CountProperty = AvaloniaProperty.Register<HelixRating, int?>(nameof(Count));
    public static readonly DirectProperty<HelixRating, string?> CaptionProperty = AvaloniaProperty.RegisterDirect<HelixRating, string?>(nameof(Caption), r => r.Caption);

    private Panel? _icons;
    private int? _hover;
    private string? _caption;

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Color of the filled icons (warning / amber by default).</summary>
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>The rating, two-way; decimals are drawn as partly filled icons when read-only.</summary>
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public int Max { get => GetValue(MaxProperty); set => SetValue(MaxProperty, value); }
    public bool IsReadOnly { get => GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>Clicking the current value again clears it (true).</summary>
    public bool AllowClear { get => GetValue(AllowClearProperty); set => SetValue(AllowClearProperty, value); }

    /// <summary>"star" or "heart".</summary>
    public string Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }
    public HelixSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>A word per value, shown on the right ("Poor"… "Excellent").</summary>
    public IList<string>? Labels { get => GetValue(LabelsProperty); set => SetValue(LabelsProperty, value); }

    /// <summary>"4.5" on the right.</summary>
    public bool ShowValue { get => GetValue(ShowValueProperty); set => SetValue(ShowValueProperty, value); }

    /// <summary>Number of reviews, shown as "(128)".</summary>
    public int? Count { get => GetValue(CountProperty); set => SetValue(CountProperty, value); }
    public string? Caption { get => _caption; private set => SetAndRaise(CaptionProperty, ref _caption, value); }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _icons = e.NameScope.Find<Panel>("PART_Icons");
        Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property.OwnerType == typeof(HelixRating) && change.Property != CaptionProperty || change.Property == AccentProperty) Build();
    }

    private void Build()
    {
        var shown = _hover ?? Value;
        var culture = CultureInfo.CurrentCulture;
        var parts = new List<string>();
        if (Labels is { Count: > 0 } labels && shown >= 1) parts.Add(labels[Math.Clamp((int)Math.Ceiling(shown) - 1, 0, labels.Count - 1)]);
        if (ShowValue) parts.Add(shown.ToString("0.#", culture));
        if (Count is { } count) parts.Add($"({count.ToString("N0", culture)})");
        Caption = parts.Count == 0 ? null : string.Join("  ", parts);
        if (_icons is null) return;
        _icons.Children.Clear();
        var size = Size switch { HelixSize.Sm => 16.0, HelixSize.Lg => 30, _ => 22 };
        var muted = HelixDraw.Brush(this, HelixKeys.Surface3, Colors.LightGray);
        for (var i = 1; i <= Math.Max(1, Max); i++)
        {
            var fill = Math.Clamp(shown - (i - 1), 0, 1);
            var star = new HelixRatingIcon { Geometry = HelixIcon.Get(Shape) ?? HelixIcon.Get("star"), Fill = fill, Accent = Accent, Track = muted, Width = size, Height = size };
            if (!IsReadOnly)
            {
                var index = i;
                star.Cursor = new Cursor(StandardCursorType.Hand);
                star.PointerEntered += (_, _) => { _hover = index; Build(); };
                star.PointerExited += (_, _) => { _hover = null; Build(); };
                star.PointerReleased += (_, _) => SetCurrentValue(ValueProperty, AllowClear && Math.Abs(Value - index) < 0.01 ? 0 : index);
            }

            _icons.Children.Add(star);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (IsReadOnly) return;
        if (e.Key is Key.Right or Key.Up) { SetCurrentValue(ValueProperty, Math.Min(Max, Math.Floor(Value) + 1)); e.Handled = true; }
        else if (e.Key is Key.Left or Key.Down) { SetCurrentValue(ValueProperty, Math.Max(0, Math.Ceiling(Value) - 1)); e.Handled = true; }
    }
}

/// <summary>One icon of a rating, filled from the left by <see cref="Fill"/> (0 to 1).</summary>
public sealed class HelixRatingIcon : Control
{
    public Geometry? Geometry { get; set; }
    public double Fill { get; set; }
    public IBrush? Accent { get; set; }
    public IBrush? Track { get; set; }

    public override void Render(DrawingContext context)
    {
        if (Geometry is null) return;
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        var scale = Bounds.Width / 24.0;
        using (context.PushTransform(Matrix.CreateScale(scale, scale)))
        {
            context.DrawGeometry(Track, new Pen(Track, 1.5, lineJoin: PenLineJoin.Round), Geometry);
            if (Fill <= 0 || Accent is null) return;
            using (context.PushClip(new Rect(0, 0, 24 * Fill, 24)))
            {
                context.DrawGeometry(Accent, new Pen(Accent, 1.5, lineJoin: PenLineJoin.Round), Geometry);
            }
        }
    }
}
