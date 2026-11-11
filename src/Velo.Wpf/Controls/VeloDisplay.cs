using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Velo.Wpf;

/// <summary>A small label: Variant (Soft, Solid, Outline), Tone or Accent, Icon, a status dot, CornerRadius.</summary>
public class VeloBadge : ContentControl
{
    static VeloBadge() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloBadge), new FrameworkPropertyMetadata(typeof(VeloBadge)));

    public VeloBadge() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloBadge));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloBadge));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloBadge));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloBadge));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloBadge));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloBadge));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty VariantProperty = VeloProperty.Register<VeloBadge, VeloBadgeVariant>(nameof(Variant), VeloBadgeVariant.Soft);
    public static readonly DependencyProperty IconProperty = VeloProperty.Register<VeloBadge, string?>(nameof(Icon), null);
    public static readonly DependencyProperty ShowDotProperty = VeloProperty.Register<VeloBadge, bool>(nameof(ShowDot), false);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloBadge, CornerRadius>(nameof(CornerRadius), new CornerRadius(999));

    public VeloBadgeVariant Variant { get => (VeloBadgeVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>A colored dot before the text (status).</summary>
    public bool ShowDot { get => (bool)GetValue(ShowDotProperty); set => SetValue(ShowDotProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
}

/// <summary>A message block: Severity picks the color and icon; Title, any content, an optional close button.</summary>
[TemplatePart(Name = "PART_Close", Type = typeof(System.Windows.Controls.Primitives.ButtonBase))]
public class VeloAlert : ContentControl
{
    static VeloAlert() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloAlert), new FrameworkPropertyMetadata(typeof(VeloAlert)));

    public VeloAlert()
    {
        VeloAccent.Init(this);
        SyncTone();
    }

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloAlert));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloAlert));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloAlert));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloAlert));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloAlert));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloAlert));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty SeverityProperty = VeloProperty.Register<VeloAlert, VeloSeverity>(nameof(Severity), VeloSeverity.Info, (d, _) => ((VeloAlert)d).SyncTone());
    public static readonly DependencyProperty TitleProperty = VeloProperty.Register<VeloAlert, string?>(nameof(Title), null);
    public static readonly DependencyProperty IconProperty = VeloProperty.Register<VeloAlert, string?>(nameof(Icon), null);
    public static readonly DependencyProperty IsClosableProperty = VeloProperty.Register<VeloAlert, bool>(nameof(IsClosable), false);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloAlert, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

    private static readonly DependencyPropertyKey EffectiveIconKey = DependencyProperty.RegisterReadOnly(nameof(EffectiveIcon), typeof(string), typeof(VeloAlert), new PropertyMetadata("info"));
    public static readonly DependencyProperty EffectiveIconProperty = EffectiveIconKey.DependencyProperty;

    public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VeloAlert));

    /// <summary>Info, Success, Warning, Danger: color, icon and screen-reader meaning.</summary>
    public VeloSeverity Severity { get => (VeloSeverity)GetValue(SeverityProperty); set => SetValue(SeverityProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Replaces the severity icon.</summary>
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Shows a × button; clicking it raises <see cref="Closed"/> and hides the alert.</summary>
    public bool IsClosable { get => (bool)GetValue(IsClosableProperty); set => SetValue(IsClosableProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public string EffectiveIcon => (string)GetValue(EffectiveIconProperty);

    public event RoutedEventHandler Closed { add => AddHandler(ClosedEvent, value); remove => RemoveHandler(ClosedEvent, value); }

    private System.Windows.Controls.Primitives.ButtonBase? _close;

    public override void OnApplyTemplate()
    {
        if (_close is not null) _close.Click -= OnClose;
        base.OnApplyTemplate();
        _close = GetTemplateChild("PART_Close") as System.Windows.Controls.Primitives.ButtonBase;
        if (_close is not null)
        {
            _close.Click += OnClose;
            _close.ToolTip = VeloTexts.Current.Close;
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
        RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == IconProperty)
        {
            SyncTone();
        }
    }

    private void SyncTone()
    {
        SetCurrentValue(ToneProperty, VeloKeys.ToneOf(Severity));
        SetValue(EffectiveIconKey, Icon ?? Severity switch
        {
            VeloSeverity.Success => "success",
            VeloSeverity.Warning => "warning",
            VeloSeverity.Danger => "danger",
            _ => "info",
        });
    }
}

/// <summary>A card: Title, Subtitle (or your own Header), Actions at the top right, content, Footer; Padding, CornerRadius, IsElevated.</summary>
public class VeloCard : ContentControl
{
    static VeloCard() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloCard), new FrameworkPropertyMetadata(typeof(VeloCard)));

    public static readonly DependencyProperty TitleProperty = VeloProperty.Register<VeloCard, string?>(nameof(Title), null);
    public static readonly DependencyProperty SubtitleProperty = VeloProperty.Register<VeloCard, string?>(nameof(Subtitle), null);
    public static readonly DependencyProperty HeaderProperty = VeloProperty.Register<VeloCard, object?>(nameof(Header), null);
    public static readonly DependencyProperty ActionsProperty = VeloProperty.Register<VeloCard, object?>(nameof(Actions), null);
    public static readonly DependencyProperty FooterProperty = VeloProperty.Register<VeloCard, object?>(nameof(Footer), null);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloCard, CornerRadius>(nameof(CornerRadius), new CornerRadius(14));
    public static readonly DependencyProperty IsElevatedProperty = VeloProperty.Register<VeloCard, bool>(nameof(IsElevated), false);

    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Subtitle { get => (string?)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }

    /// <summary>Your own header content (replaces Title and Subtitle).</summary>
    public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Content at the top right (buttons, a menu…).</summary>
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    /// <summary>Content at the bottom, right-aligned (buttons).</summary>
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>A soft shadow instead of a flat border.</summary>
    public bool IsElevated { get => (bool)GetValue(IsElevatedProperty); set => SetValue(IsElevatedProperty, value); }
}

/// <summary>A progress bar: Accent (bar), TrackBrush, BarHeight, Label and the percentage above it.</summary>
public class VeloProgress : ProgressBar
{
    static VeloProgress() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloProgress), new FrameworkPropertyMetadata(typeof(VeloProgress)));

    public VeloProgress()
    {
        VeloAccent.Init(this);
        ValueChanged += (_, _) => UpdatePercent();
    }

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloProgress));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloProgress));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloProgress));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloProgress));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloProgress));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloProgress));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty TrackBrushProperty = VeloProperty.Register<VeloProgress, Brush?>(nameof(TrackBrush), null);
    public static readonly DependencyProperty BarHeightProperty = VeloProperty.Register<VeloProgress, double>(nameof(BarHeight), 8d);
    public static readonly DependencyProperty LabelProperty = VeloProperty.Register<VeloProgress, string?>(nameof(Label), null);
    public static readonly DependencyProperty ShowValueProperty = VeloProperty.Register<VeloProgress, bool>(nameof(ShowValue), false);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloProgress, CornerRadius>(nameof(CornerRadius), new CornerRadius(999));

    private static readonly DependencyPropertyKey PercentTextKey = DependencyProperty.RegisterReadOnly(nameof(PercentText), typeof(string), typeof(VeloProgress), new PropertyMetadata("0 %"));
    public static readonly DependencyProperty PercentTextProperty = PercentTextKey.DependencyProperty;

    public Brush? TrackBrush { get => (Brush?)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
    public double BarHeight { get => (double)GetValue(BarHeightProperty); set => SetValue(BarHeightProperty, value); }
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Shows the percentage on the right of the label.</summary>
    public bool ShowValue { get => (bool)GetValue(ShowValueProperty); set => SetValue(ShowValueProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public string PercentText => (string)GetValue(PercentTextProperty);

    private void UpdatePercent()
    {
        var range = Maximum - Minimum;
        var percent = range <= 0 ? 0 : (Value - Minimum) / range;
        SetValue(PercentTextKey, percent.ToString("P0", CultureInfo.CurrentCulture));
    }
}

/// <summary>A spinning ring: Accent, Size, Thickness.</summary>
public class VeloSpinner : Control
{
    static VeloSpinner() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloSpinner), new FrameworkPropertyMetadata(typeof(VeloSpinner)));

    public VeloSpinner() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloSpinner));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloSpinner));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloSpinner));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloSpinner));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloSpinner));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloSpinner));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty SizeProperty = VeloProperty.Register<VeloSpinner, double>(nameof(Size), 20d);
    public static readonly DependencyProperty ThicknessProperty = VeloProperty.Register<VeloSpinner, double>(nameof(Thickness), 2.5d);

    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public double Thickness { get => (double)GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }
}

/// <summary>An empty state: Icon in a soft circle, Title, Description and Actions (buttons).</summary>
public class VeloEmptyState : Control
{
    static VeloEmptyState() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloEmptyState), new FrameworkPropertyMetadata(typeof(VeloEmptyState)));

    public VeloEmptyState() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloEmptyState));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloEmptyState));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloEmptyState));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloEmptyState));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloEmptyState));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloEmptyState));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty IconProperty = VeloProperty.Register<VeloEmptyState, string?>(nameof(Icon), "search");
    public static readonly DependencyProperty TitleProperty = VeloProperty.Register<VeloEmptyState, string?>(nameof(Title), null);
    public static readonly DependencyProperty DescriptionProperty = VeloProperty.Register<VeloEmptyState, string?>(nameof(Description), null);
    public static readonly DependencyProperty ActionsProperty = VeloProperty.Register<VeloEmptyState, object?>(nameof(Actions), null);

    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
}

/// <summary>A key figure: Label, Value, Unit and a Delta shown green or red (HigherIsBetter decides which).</summary>
public class VeloStat : Control
{
    static VeloStat() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloStat), new FrameworkPropertyMetadata(typeof(VeloStat)));

    public static readonly DependencyProperty LabelProperty = VeloProperty.Register<VeloStat, string?>(nameof(Label), null);
    public static readonly DependencyProperty ValueProperty = VeloProperty.Register<VeloStat, string?>(nameof(Value), null);
    public static readonly DependencyProperty UnitProperty = VeloProperty.Register<VeloStat, string?>(nameof(Unit), null);
    public static readonly DependencyProperty IconProperty = VeloProperty.Register<VeloStat, string?>(nameof(Icon), null);
    public static readonly DependencyProperty DeltaProperty = VeloProperty.Register<VeloStat, double?>(nameof(Delta), null, (d, _) => ((VeloStat)d).UpdateDelta());
    public static readonly DependencyProperty DeltaFormatProperty = VeloProperty.Register<VeloStat, string>(nameof(DeltaFormat), "+0.#'%';-0.#'%';0'%'", (d, _) => ((VeloStat)d).UpdateDelta());
    public static readonly DependencyProperty HigherIsBetterProperty = VeloProperty.Register<VeloStat, bool>(nameof(HigherIsBetter), true, (d, _) => ((VeloStat)d).UpdateDelta());

    private static readonly DependencyPropertyKey DeltaTextKey = DependencyProperty.RegisterReadOnly(nameof(DeltaText), typeof(string), typeof(VeloStat), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey DeltaStateKey = DependencyProperty.RegisterReadOnly(nameof(DeltaState), typeof(string), typeof(VeloStat), new PropertyMetadata("none"));
    public static readonly DependencyProperty DeltaTextProperty = DeltaTextKey.DependencyProperty;
    public static readonly DependencyProperty DeltaStateProperty = DeltaStateKey.DependencyProperty;

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string? Unit { get => (string?)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Change since the previous period (e.g. 12.5 for +12.5 %).</summary>
    public double? Delta { get => (double?)GetValue(DeltaProperty); set => SetValue(DeltaProperty, value); }

    /// <summary>.NET format of the delta ("+0.#'%';-0.#'%';0'%'" by default).</summary>
    public string DeltaFormat { get => (string)GetValue(DeltaFormatProperty); set => SetValue(DeltaFormatProperty, value); }

    /// <summary>False for costs, errors, delays: a rise is then shown in red.</summary>
    public bool HigherIsBetter { get => (bool)GetValue(HigherIsBetterProperty); set => SetValue(HigherIsBetterProperty, value); }
    public string? DeltaText => (string?)GetValue(DeltaTextProperty);

    /// <summary>"good", "bad", "flat" or "none".</summary>
    public string DeltaState => (string)GetValue(DeltaStateProperty);

    private void UpdateDelta()
    {
        if (Delta is not { } delta)
        {
            SetValue(DeltaTextKey, null);
            SetValue(DeltaStateKey, "none");
            return;
        }

        SetValue(DeltaTextKey, delta.ToString(DeltaFormat, CultureInfo.CurrentCulture));
        SetValue(DeltaStateKey, delta == 0 ? "flat" : (delta > 0) == HigherIsBetter ? "good" : "bad");
    }
}
