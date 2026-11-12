using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Helix.Wpf;

/// <summary>A small label: Variant (Soft, Solid, Outline), Tone or Accent, Icon, a status dot, CornerRadius.</summary>
public class HelixBadge : ContentControl
{
    static HelixBadge() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixBadge), new FrameworkPropertyMetadata(typeof(HelixBadge)));

    public HelixBadge() => HelixAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixBadge));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixBadge));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixBadge));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixBadge));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixBadge));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixBadge));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty VariantProperty = HelixProperty.Register<HelixBadge, HelixBadgeVariant>(nameof(Variant), HelixBadgeVariant.Soft);
    public static readonly DependencyProperty IconProperty = HelixProperty.Register<HelixBadge, string?>(nameof(Icon), null);
    public static readonly DependencyProperty ShowDotProperty = HelixProperty.Register<HelixBadge, bool>(nameof(ShowDot), false);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixBadge, CornerRadius>(nameof(CornerRadius), new CornerRadius(999));

    public HelixBadgeVariant Variant { get => (HelixBadgeVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>A colored dot before the text (status).</summary>
    public bool ShowDot { get => (bool)GetValue(ShowDotProperty); set => SetValue(ShowDotProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
}

/// <summary>A message block: Severity picks the color and icon; Title, any content, an optional close button.</summary>
[TemplatePart(Name = "PART_Close", Type = typeof(System.Windows.Controls.Primitives.ButtonBase))]
public class HelixAlert : ContentControl
{
    static HelixAlert() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixAlert), new FrameworkPropertyMetadata(typeof(HelixAlert)));

    public HelixAlert()
    {
        HelixAccent.Init(this);
        SyncTone();
    }

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixAlert));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixAlert));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixAlert));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixAlert));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixAlert));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixAlert));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty SeverityProperty = HelixProperty.Register<HelixAlert, HelixSeverity>(nameof(Severity), HelixSeverity.Info, (d, _) => ((HelixAlert)d).SyncTone());
    public static readonly DependencyProperty TitleProperty = HelixProperty.Register<HelixAlert, string?>(nameof(Title), null);
    public static readonly DependencyProperty IconProperty = HelixProperty.Register<HelixAlert, string?>(nameof(Icon), null);
    public static readonly DependencyProperty IsClosableProperty = HelixProperty.Register<HelixAlert, bool>(nameof(IsClosable), false);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixAlert, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

    private static readonly DependencyPropertyKey EffectiveIconKey = DependencyProperty.RegisterReadOnly(nameof(EffectiveIcon), typeof(string), typeof(HelixAlert), new PropertyMetadata("info"));
    public static readonly DependencyProperty EffectiveIconProperty = EffectiveIconKey.DependencyProperty;

    public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixAlert));

    /// <summary>Info, Success, Warning, Danger: color, icon and screen-reader meaning.</summary>
    public HelixSeverity Severity { get => (HelixSeverity)GetValue(SeverityProperty); set => SetValue(SeverityProperty, value); }
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
            _close.ToolTip = HelixTexts.Current.Close;
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
        SetCurrentValue(ToneProperty, HelixKeys.ToneOf(Severity));
        SetValue(EffectiveIconKey, Icon ?? Severity switch
        {
            HelixSeverity.Success => "success",
            HelixSeverity.Warning => "warning",
            HelixSeverity.Danger => "danger",
            _ => "info",
        });
    }
}

/// <summary>A card: Title, Subtitle (or your own Header), Actions at the top right, content, Footer; Padding, CornerRadius, IsElevated.</summary>
public class HelixCard : ContentControl
{
    static HelixCard() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixCard), new FrameworkPropertyMetadata(typeof(HelixCard)));

    public static readonly DependencyProperty TitleProperty = HelixProperty.Register<HelixCard, string?>(nameof(Title), null);
    public static readonly DependencyProperty SubtitleProperty = HelixProperty.Register<HelixCard, string?>(nameof(Subtitle), null);
    public static readonly DependencyProperty HeaderProperty = HelixProperty.Register<HelixCard, object?>(nameof(Header), null);
    public static readonly DependencyProperty ActionsProperty = HelixProperty.Register<HelixCard, object?>(nameof(Actions), null);
    public static readonly DependencyProperty FooterProperty = HelixProperty.Register<HelixCard, object?>(nameof(Footer), null);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixCard, CornerRadius>(nameof(CornerRadius), new CornerRadius(14));
    public static readonly DependencyProperty IsElevatedProperty = HelixProperty.Register<HelixCard, bool>(nameof(IsElevated), false);

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
public class HelixProgress : ProgressBar
{
    static HelixProgress() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixProgress), new FrameworkPropertyMetadata(typeof(HelixProgress)));

    public HelixProgress()
    {
        HelixAccent.Init(this);
        ValueChanged += (_, _) => UpdatePercent();
    }

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixProgress));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixProgress));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixProgress));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixProgress));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixProgress));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixProgress));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty TrackBrushProperty = HelixProperty.Register<HelixProgress, Brush?>(nameof(TrackBrush), null);
    public static readonly DependencyProperty BarHeightProperty = HelixProperty.Register<HelixProgress, double>(nameof(BarHeight), 8d);
    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixProgress, string?>(nameof(Label), null);
    public static readonly DependencyProperty ShowValueProperty = HelixProperty.Register<HelixProgress, bool>(nameof(ShowValue), false);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixProgress, CornerRadius>(nameof(CornerRadius), new CornerRadius(999));

    private static readonly DependencyPropertyKey PercentTextKey = DependencyProperty.RegisterReadOnly(nameof(PercentText), typeof(string), typeof(HelixProgress), new PropertyMetadata("0 %"));
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
public class HelixSpinner : Control
{
    static HelixSpinner() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixSpinner), new FrameworkPropertyMetadata(typeof(HelixSpinner)));

    public HelixSpinner() => HelixAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixSpinner));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixSpinner));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixSpinner));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixSpinner));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixSpinner));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixSpinner));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty SizeProperty = HelixProperty.Register<HelixSpinner, double>(nameof(Size), 20d);
    public static readonly DependencyProperty ThicknessProperty = HelixProperty.Register<HelixSpinner, double>(nameof(Thickness), 2.5d);

    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public double Thickness { get => (double)GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }
}

/// <summary>An empty state: Icon in a soft circle, Title, Description and Actions (buttons).</summary>
public class HelixEmptyState : Control
{
    static HelixEmptyState() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixEmptyState), new FrameworkPropertyMetadata(typeof(HelixEmptyState)));

    public HelixEmptyState() => HelixAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixEmptyState));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixEmptyState));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixEmptyState));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixEmptyState));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixEmptyState));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixEmptyState));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty IconProperty = HelixProperty.Register<HelixEmptyState, string?>(nameof(Icon), "search");
    public static readonly DependencyProperty TitleProperty = HelixProperty.Register<HelixEmptyState, string?>(nameof(Title), null);
    public static readonly DependencyProperty DescriptionProperty = HelixProperty.Register<HelixEmptyState, string?>(nameof(Description), null);
    public static readonly DependencyProperty ActionsProperty = HelixProperty.Register<HelixEmptyState, object?>(nameof(Actions), null);

    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
}

/// <summary>A key figure: Label, Value, Unit and a Delta shown green or red (HigherIsBetter decides which).</summary>
public class HelixStat : Control
{
    static HelixStat() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixStat), new FrameworkPropertyMetadata(typeof(HelixStat)));

    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixStat, string?>(nameof(Label), null);
    public static readonly DependencyProperty ValueProperty = HelixProperty.Register<HelixStat, string?>(nameof(Value), null);
    public static readonly DependencyProperty UnitProperty = HelixProperty.Register<HelixStat, string?>(nameof(Unit), null);
    public static readonly DependencyProperty IconProperty = HelixProperty.Register<HelixStat, string?>(nameof(Icon), null);
    public static readonly DependencyProperty DeltaProperty = HelixProperty.Register<HelixStat, double?>(nameof(Delta), null, (d, _) => ((HelixStat)d).UpdateDelta());
    public static readonly DependencyProperty DeltaFormatProperty = HelixProperty.Register<HelixStat, string>(nameof(DeltaFormat), "+0.#'%';-0.#'%';0'%'", (d, _) => ((HelixStat)d).UpdateDelta());
    public static readonly DependencyProperty HigherIsBetterProperty = HelixProperty.Register<HelixStat, bool>(nameof(HigherIsBetter), true, (d, _) => ((HelixStat)d).UpdateDelta());

    private static readonly DependencyPropertyKey DeltaTextKey = DependencyProperty.RegisterReadOnly(nameof(DeltaText), typeof(string), typeof(HelixStat), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey DeltaStateKey = DependencyProperty.RegisterReadOnly(nameof(DeltaState), typeof(string), typeof(HelixStat), new PropertyMetadata("none"));
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
