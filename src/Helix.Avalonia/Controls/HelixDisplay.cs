using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Helix.Avalonia;

/// <summary>Minimal ICommand for the templates.</summary>
internal sealed class HelixCommand(Action execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute();
}

/// <summary>A short status label: Soft, Solid or Outline, any tone or accent, optional icon or dot.</summary>
public class HelixBadge : ContentControl
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixBadge>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixBadge>();
    public static readonly StyledProperty<IBrush?> OnAccentProperty = HelixAccent.OnAccentProperty.AddOwner<HelixBadge>();
    public static readonly StyledProperty<IBrush?> ToneOnBrushProperty = HelixAccent.ToneOnBrushProperty.AddOwner<HelixBadge>();
    public static readonly StyledProperty<IBrush?> ResolvedOnAccentProperty = HelixAccent.ResolvedOnAccentProperty.AddOwner<HelixBadge>();
    public static readonly StyledProperty<HelixBadgeVariant> VariantProperty = AvaloniaProperty.Register<HelixBadge, HelixBadgeVariant>(nameof(Variant));
    public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<HelixBadge, string?>(nameof(Icon));
    public static readonly StyledProperty<bool> ShowDotProperty = AvaloniaProperty.Register<HelixBadge, bool>(nameof(ShowDot));

    private bool _customAccent;

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public IBrush? OnAccent { get => GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public IBrush? ToneOnBrush { get => GetValue(ToneOnBrushProperty); set => SetValue(ToneOnBrushProperty, value); }
    public IBrush? ResolvedOnAccent => GetValue(ResolvedOnAccentProperty);
    public HelixBadgeVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public string? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>A small colored dot before the text.</summary>
    public bool ShowDot { get => GetValue(ShowDotProperty); set => SetValue(ShowDotProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        HelixAccent.OnChanged(this, change, ref _customAccent);
    }
}

/// <summary>A contextual message: severity (info, success, warning, danger), title, icon, optional close button.</summary>
[TemplatePart("PART_Close", typeof(Button))]
public class HelixAlert : ContentControl
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixAlert>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixAlert>();
    public static readonly StyledProperty<HelixSeverity> SeverityProperty = AvaloniaProperty.Register<HelixAlert, HelixSeverity>(nameof(Severity));
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixAlert, string?>(nameof(Title));
    public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<HelixAlert, string?>(nameof(Icon));
    public static readonly StyledProperty<bool> IsClosableProperty = AvaloniaProperty.Register<HelixAlert, bool>(nameof(IsClosable));
    public static readonly DirectProperty<HelixAlert, string> ResolvedIconProperty =
        AvaloniaProperty.RegisterDirect<HelixAlert, string>(nameof(ResolvedIcon), a => a.ResolvedIcon);

    public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent = RoutedEvent.Register<HelixAlert, RoutedEventArgs>(nameof(Closed), RoutingStrategies.Bubble);

    private string _resolvedIcon = "info";
    private Button? _close;

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Sets the tone and the icon at once.</summary>
    public HelixSeverity Severity { get => GetValue(SeverityProperty); set => SetValue(SeverityProperty, value); }
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Replaces the severity icon.</summary>
    public string? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public bool IsClosable { get => GetValue(IsClosableProperty); set => SetValue(IsClosableProperty, value); }
    public string ResolvedIcon { get => _resolvedIcon; private set => SetAndRaise(ResolvedIconProperty, ref _resolvedIcon, value); }

    /// <summary>Raised when the close button hides the alert.</summary>
    public event EventHandler<RoutedEventArgs>? Closed { add => AddHandler(ClosedEvent, value); remove => RemoveHandler(ClosedEvent, value); }

    public HelixAlert() => Update();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SeverityProperty || change.Property == IconProperty) Update();
    }

    private void Update()
    {
        SetCurrentValue(ToneProperty, HelixKeys.ToneOf(Severity));
        ResolvedIcon = Icon ?? Severity switch
        {
            HelixSeverity.Success => "success",
            HelixSeverity.Warning => "warning",
            HelixSeverity.Danger => "danger",
            _ => "info",
        };
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_close is not null) _close.Click -= OnClose;
        _close = e.NameScope.Find<Button>("PART_Close");
        if (_close is not null) _close.Click += OnClose;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        IsVisible = false;
        RaiseEvent(new RoutedEventArgs(ClosedEvent));
    }
}

/// <summary>A surface with optional title, subtitle, header actions and footer.</summary>
public class HelixCard : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixCard, string?>(nameof(Title));
    public static readonly StyledProperty<string?> SubtitleProperty = AvaloniaProperty.Register<HelixCard, string?>(nameof(Subtitle));
    public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<HelixCard, object?>(nameof(Header));
    public static readonly StyledProperty<object?> ActionsProperty = AvaloniaProperty.Register<HelixCard, object?>(nameof(Actions));
    public static readonly StyledProperty<object?> FooterProperty = AvaloniaProperty.Register<HelixCard, object?>(nameof(Footer));
    public static readonly StyledProperty<bool> IsElevatedProperty = AvaloniaProperty.Register<HelixCard, bool>(nameof(IsElevated));
    public static readonly DirectProperty<HelixCard, bool> HasHeaderProperty = AvaloniaProperty.RegisterDirect<HelixCard, bool>(nameof(HasHeader), c => c.HasHeader);

    private bool _hasHeader;

    /// <summary>True when there is a title, a header or actions.</summary>
    public bool HasHeader { get => _hasHeader; private set => SetAndRaise(HasHeaderProperty, ref _hasHeader, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty || change.Property == HeaderProperty || change.Property == ActionsProperty)
        {
            HasHeader = !string.IsNullOrEmpty(Title) || Header is not null || Actions is not null;
        }
    }

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Subtitle { get => GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }

    /// <summary>Your own header, replaces Title and Subtitle.</summary>
    public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Buttons on the right of the header.</summary>
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>A shadow instead of the border.</summary>
    public bool IsElevated { get => GetValue(IsElevatedProperty); set => SetValue(IsElevatedProperty, value); }
}

/// <summary>A progress bar with an optional label and percentage.</summary>
public class HelixProgress : ProgressBar
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixProgress>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixProgress>();
    public static readonly StyledProperty<IBrush?> TrackBrushProperty = AvaloniaProperty.Register<HelixProgress, IBrush?>(nameof(TrackBrush));
    public static readonly StyledProperty<double> BarHeightProperty = AvaloniaProperty.Register<HelixProgress, double>(nameof(BarHeight), 8);
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<HelixProgress, string?>(nameof(Label));
    public static readonly StyledProperty<bool> ShowValueProperty = AvaloniaProperty.Register<HelixProgress, bool>(nameof(ShowValue));
    public static readonly DirectProperty<HelixProgress, string> PercentTextProperty =
        AvaloniaProperty.RegisterDirect<HelixProgress, string>(nameof(PercentText), p => p.PercentText);

    private string _percentText = "0 %";
    private Control? _track, _fill;

    protected override Type StyleKeyOverride => typeof(HelixProgress);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_track is not null) _track.SizeChanged -= OnTrackSized;
        _track = e.NameScope.Find<Control>("Track");
        _fill = e.NameScope.Find<Control>("Fill");
        if (_track is not null) _track.SizeChanged += OnTrackSized;
        LayoutFill();
    }

    private void OnTrackSized(object? sender, SizeChangedEventArgs e) => LayoutFill();

    private void LayoutFill()
    {
        if (_track is null || _fill is null) return;
        var share = Maximum <= Minimum ? 0 : Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0, 1);
        _fill.Width = _track.Bounds.Width * share;
    }

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public IBrush? TrackBrush { get => GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
    public double BarHeight { get => GetValue(BarHeightProperty); set => SetValue(BarHeightProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>The percentage on the right of the label.</summary>
    public bool ShowValue { get => GetValue(ShowValueProperty); set => SetValue(ShowValueProperty, value); }
    public string PercentText { get => _percentText; private set => SetAndRaise(PercentTextProperty, ref _percentText, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == MinimumProperty || change.Property == MaximumProperty)
        {
            var share = Maximum <= Minimum ? 0 : (Value - Minimum) / (Maximum - Minimum);
            PercentText = share.ToString("P0", CultureInfo.CurrentCulture);
            LayoutFill();
        }
    }
}

/// <summary>A spinning ring for work in progress.</summary>
public class HelixSpinner : Control
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixSpinner>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixSpinner>();
    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<HelixSpinner, double>(nameof(Size), 20);
    public static readonly StyledProperty<double> ThicknessProperty = AvaloniaProperty.Register<HelixSpinner, double>(nameof(Thickness), 2.5);

    static HelixSpinner()
    {
        AffectsRender<HelixSpinner>(AccentProperty, SizeProperty, ThicknessProperty);
        AffectsMeasure<HelixSpinner>(SizeProperty);
    }

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public double Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    public override void Render(DrawingContext context)
    {
        if (Accent is null) return;
        var radius = (Size - Thickness) / 2;
        var center = new Point(Size / 2, Size / 2);
        using (context.PushOpacity(0.2))
        {
            context.DrawEllipse(null, new Pen(Accent, Thickness), center, radius, radius);
        }

        var arc = new StreamGeometry();
        using (var g = arc.Open())
        {
            g.BeginFigure(new Point(center.X, center.Y - radius), false);
            g.ArcTo(new Point(center.X + radius, center.Y), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            g.EndFigure(false);
        }

        context.DrawGeometry(null, new Pen(Accent, Thickness, lineCap: PenLineCap.Round), arc);
    }
}

/// <summary>An empty screen that explains and offers an action.</summary>
public class HelixEmptyState : TemplatedControl
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixEmptyState>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixEmptyState>();
    public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<HelixEmptyState, string?>(nameof(Icon), "search");
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixEmptyState, string?>(nameof(Title));
    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<HelixEmptyState, string?>(nameof(Description));
    public static readonly StyledProperty<object?> ActionsProperty = AvaloniaProperty.Register<HelixEmptyState, object?>(nameof(Actions));

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public string? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
}

/// <summary>A key figure: label, value with unit, icon and a colored change.</summary>
public class HelixStat : TemplatedControl
{
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<HelixStat, string?>(nameof(Label));
    public static readonly StyledProperty<string?> ValueProperty = AvaloniaProperty.Register<HelixStat, string?>(nameof(Value));
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<HelixStat, string?>(nameof(Unit));
    public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<HelixStat, string?>(nameof(Icon));
    public static readonly StyledProperty<double?> DeltaProperty = AvaloniaProperty.Register<HelixStat, double?>(nameof(Delta));
    public static readonly StyledProperty<string> DeltaFormatProperty = AvaloniaProperty.Register<HelixStat, string>(nameof(DeltaFormat), "+0.#'%';-0.#'%';0'%'");
    public static readonly StyledProperty<bool> HigherIsBetterProperty = AvaloniaProperty.Register<HelixStat, bool>(nameof(HigherIsBetter), true);
    public static readonly DirectProperty<HelixStat, string?> DeltaTextProperty = AvaloniaProperty.RegisterDirect<HelixStat, string?>(nameof(DeltaText), s => s.DeltaText);
    public static readonly DirectProperty<HelixStat, string?> DeltaIconProperty = AvaloniaProperty.RegisterDirect<HelixStat, string?>(nameof(DeltaIcon), s => s.DeltaIcon);

    private string? _deltaText, _deltaIcon;

    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>The change, e.g. 12.5 for +12.5 %.</summary>
    public double? Delta { get => GetValue(DeltaProperty); set => SetValue(DeltaProperty, value); }
    public string DeltaFormat { get => GetValue(DeltaFormatProperty); set => SetValue(DeltaFormatProperty, value); }

    /// <summary>False when a decrease is the good news (costs, delays…).</summary>
    public bool HigherIsBetter { get => GetValue(HigherIsBetterProperty); set => SetValue(HigherIsBetterProperty, value); }
    public string? DeltaText { get => _deltaText; private set => SetAndRaise(DeltaTextProperty, ref _deltaText, value); }
    public string? DeltaIcon { get => _deltaIcon; private set => SetAndRaise(DeltaIconProperty, ref _deltaIcon, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != DeltaProperty && change.Property != DeltaFormatProperty && change.Property != HigherIsBetterProperty) return;
        DeltaText = Delta?.ToString(DeltaFormat, CultureInfo.CurrentCulture);
        DeltaIcon = Delta switch { > 0 => "arrow-up", < 0 => "arrow-down", _ => null };
        var good = Delta is { } d && d != 0 ? d > 0 == HigherIsBetter : (bool?)null;
        PseudoClasses.Set(":good", good == true);
        PseudoClasses.Set(":bad", good == false);
    }
}
