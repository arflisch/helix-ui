using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace Helix.Wpf;

/// <summary>A "loading" veil over its content while <see cref="IsBusy"/>, shown after <see cref="Delay"/> so quick work doesn't flicker.</summary>
public class HelixBusy : ContentControl
{
    static HelixBusy() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixBusy), new FrameworkPropertyMetadata(typeof(HelixBusy)));

    public static readonly DependencyProperty IsBusyProperty = HelixProperty.Register<HelixBusy, bool>(nameof(IsBusy), false, (d, _) => ((HelixBusy)d).OnBusyChanged());
    public static readonly DependencyProperty TextProperty = HelixProperty.Register<HelixBusy, string?>(nameof(Text), null);
    public static readonly DependencyProperty DelayProperty = HelixProperty.Register<HelixBusy, TimeSpan>(nameof(Delay), TimeSpan.FromMilliseconds(150));
    private static readonly DependencyPropertyKey IsVeilShownKey = DependencyProperty.RegisterReadOnly(nameof(IsVeilShown), typeof(bool), typeof(HelixBusy), new PropertyMetadata(false));
    public static readonly DependencyProperty IsVeilShownProperty = IsVeilShownKey.DependencyProperty;

    /// <summary>Shows the veil (after Delay) and blocks the content.</summary>
    public bool IsBusy { get => (bool)GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }

    /// <summary>Text under the spinner, e.g. "Saving…".</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Wait before showing the veil (150 ms).</summary>
    public TimeSpan Delay { get => (TimeSpan)GetValue(DelayProperty); set => SetValue(DelayProperty, value); }

    /// <summary>True while the veil is visible.</summary>
    public bool IsVeilShown => (bool)GetValue(IsVeilShownProperty);

    private DispatcherTimer? _timer;

    private void OnBusyChanged()
    {
        _timer?.Stop();
        if (!IsBusy)
        {
            SetValue(IsVeilShownKey, false);
            return;
        }

        _timer = new DispatcherTimer { Interval = Delay };
        _timer.Tick += (_, _) =>
        {
            _timer?.Stop();
            SetValue(IsVeilShownKey, IsBusy);
        };
        _timer.Start();
    }
}

/// <summary>A zone behind a <see cref="HelixTargetBar"/>, e.g. the tolerance around the target.</summary>
public sealed record HelixTargetBand(double From, double To, HelixTone Tone, string? Label = null);

/// <summary>A value against its target: the bar, a mark at the target, optional bands; with Tolerance, green / orange / red.</summary>
[TemplatePart(Name = "PART_Track", Type = typeof(HelixTargetTrack))]
public class HelixTargetBar : Control
{
    static HelixTargetBar() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixTargetBar), new FrameworkPropertyMetadata(typeof(HelixTargetBar)));

    private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((HelixTargetBar)d).Update();

    public static readonly DependencyProperty ValueProperty = HelixProperty.Register<HelixTargetBar, double?>(nameof(Value), null, Changed);
    public static readonly DependencyProperty TargetProperty = HelixProperty.Register<HelixTargetBar, double?>(nameof(Target), null, Changed);
    public static readonly DependencyProperty MinimumProperty = HelixProperty.Register<HelixTargetBar, double>(nameof(Minimum), 0d, Changed);
    public static readonly DependencyProperty MaximumProperty = HelixProperty.Register<HelixTargetBar, double>(nameof(Maximum), 100d, Changed);
    public static readonly DependencyProperty ToleranceProperty = HelixProperty.Register<HelixTargetBar, double?>(nameof(Tolerance), null, Changed);
    public static readonly DependencyProperty BandsProperty = HelixProperty.Register<HelixTargetBar, IList<HelixTargetBand>?>(nameof(Bands), null, Changed);
    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixTargetBar, string?>(nameof(Label), null);
    public static readonly DependencyProperty UnitProperty = HelixProperty.Register<HelixTargetBar, string?>(nameof(Unit), null, Changed);
    public static readonly DependencyProperty NumberFormatProperty = HelixProperty.Register<HelixTargetBar, string>(nameof(NumberFormat), "N0", Changed);
    public static readonly DependencyProperty ShowValuesProperty = HelixProperty.Register<HelixTargetBar, bool>(nameof(ShowValues), true);
    public static readonly DependencyProperty TrackHeightProperty = HelixProperty.Register<HelixTargetBar, double>(nameof(TrackHeight), 14d);

    private static readonly DependencyPropertyKey ValueTextKey = DependencyProperty.RegisterReadOnly(nameof(ValueText), typeof(string), typeof(HelixTargetBar), new PropertyMetadata(""));
    private static readonly DependencyPropertyKey TargetTextKey = DependencyProperty.RegisterReadOnly(nameof(TargetText), typeof(string), typeof(HelixTargetBar), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey DeltaTextKey = DependencyProperty.RegisterReadOnly(nameof(DeltaText), typeof(string), typeof(HelixTargetBar), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey StateKey = DependencyProperty.RegisterReadOnly(nameof(State), typeof(string), typeof(HelixTargetBar), new PropertyMetadata("none"));
    public static readonly DependencyProperty ValueTextProperty = ValueTextKey.DependencyProperty;
    public static readonly DependencyProperty TargetTextProperty = TargetTextKey.DependencyProperty;
    public static readonly DependencyProperty DeltaTextProperty = DeltaTextKey.DependencyProperty;
    public static readonly DependencyProperty StateProperty = StateKey.DependencyProperty;

    public double? Value { get => (double?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The target, shown as a mark across the bar.</summary>
    public double? Target { get => (double?)GetValue(TargetProperty); set => SetValue(TargetProperty, value); }
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>Allowed gap: green within it, orange beyond, red beyond twice.</summary>
    public double? Tolerance { get => (double?)GetValue(ToleranceProperty); set => SetValue(ToleranceProperty, value); }

    /// <summary>Zones behind the bar (From, To, Tone, Label).</summary>
    public IList<HelixTargetBand>? Bands { get => (IList<HelixTargetBand>?)GetValue(BandsProperty); set => SetValue(BandsProperty, value); }
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Unit { get => (string?)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string NumberFormat { get => (string)GetValue(NumberFormatProperty); set => SetValue(NumberFormatProperty, value); }
    public bool ShowValues { get => (bool)GetValue(ShowValuesProperty); set => SetValue(ShowValuesProperty, value); }
    public double TrackHeight { get => (double)GetValue(TrackHeightProperty); set => SetValue(TrackHeightProperty, value); }

    public string ValueText => (string)GetValue(ValueTextProperty);

    /// <summary>"/ 120 MW".</summary>
    public string? TargetText => (string?)GetValue(TargetTextProperty);

    /// <summary>"+12 MW" (gap to the target).</summary>
    public string? DeltaText => (string?)GetValue(DeltaTextProperty);

    /// <summary>"ok", "warn", "off" with Tolerance, else "none": drives the bar color.</summary>
    public string State => (string)GetValue(StateProperty);

    private HelixTargetTrack? _track;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _track = GetTemplateChild("PART_Track") as HelixTargetTrack;
        if (_track is not null) _track.Owner = this;
        Update();
    }

    private string Format(double value) =>
        value.ToString(NumberFormat, CultureInfo.CurrentCulture) + (string.IsNullOrEmpty(Unit) ? "" : " " + Unit);

    private void Update()
    {
        SetValue(ValueTextKey, Value is { } v ? Format(v) : "—");
        SetValue(TargetTextKey, Target is { } t ? "/ " + Format(t) : null);
        SetValue(DeltaTextKey, Value is { } a && Target is { } b ? (a - b >= 0 ? "+" : "") + Format(a - b) : null);
        SetValue(StateKey, Tolerance is not { } tol || Value is not { } val || Target is not { } tar ? "none"
            : Math.Abs(val - tar) <= tol ? "ok" : Math.Abs(val - tar) <= tol * 2 ? "warn" : "off");
        _track?.InvalidateVisual();
    }
}

/// <summary>The drawn track of a <see cref="HelixTargetBar"/>: bands, bar and target mark.</summary>
public class HelixTargetTrack : FrameworkElement
{
    internal HelixTargetBar? Owner { get; set; }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 200 : availableSize.Width, (Owner?.TrackHeight ?? 14) + 8);

    private Brush Res(string key, Brush fallback) => TryFindResource(key) as Brush ?? fallback;

    protected override void OnRender(DrawingContext dc)
    {
        if (Owner is not { } bar) return;
        var width = ActualWidth;
        var height = bar.TrackHeight;
        var top = 4.0;
        double X(double value) => bar.Maximum <= bar.Minimum ? 0 : Math.Clamp((value - bar.Minimum) / (bar.Maximum - bar.Minimum), 0, 1) * width;

        var radius = height / 2;
        dc.DrawRoundedRectangle(Res(HelixKeys.Surface2, Brushes.LightGray), null, new Rect(0, top, width, height), radius, radius);
        if (bar.Bands is { } bands)
        {
            dc.PushClip(new RectangleGeometry(new Rect(0, top, width, height), radius, radius));
            foreach (var band in bands)
            {
                var brush = Res(HelixKeys.Of(band.Tone), Brushes.Gray).Clone();
                brush.Opacity = 0.22;
                dc.DrawRectangle(brush, null, new Rect(X(band.From), top, Math.Max(0, X(band.To) - X(band.From)), height));
            }

            dc.Pop();
        }

        if (bar.Value is { } value)
        {
            var color = bar.State switch
            {
                "ok" => Res(HelixKeys.Success, Brushes.Green),
                "warn" => Res(HelixKeys.Warning, Brushes.Orange),
                "off" => Res(HelixKeys.Danger, Brushes.Red),
                _ => Res(HelixKeys.Primary, Brushes.SteelBlue),
            };
            var barHeight = height / 2;
            dc.DrawRoundedRectangle(color, null, new Rect(0, top + height / 4, X(value), barHeight), barHeight / 2, barHeight / 2);
        }

        if (bar.Target is { } target)
        {
            var x = X(target);
            dc.DrawRoundedRectangle(Res(HelixKeys.Text, Brushes.Black), null, new Rect(Math.Clamp(x - 1.5, 0, width - 3), 0, 3, height + 8), 1.5, 1.5);
        }
    }
}

/// <summary>Label / value pairs in <see cref="Columns"/> columns (detail screens, summaries).</summary>
public class HelixDescriptionList : ItemsControl
{
    static HelixDescriptionList() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixDescriptionList), new FrameworkPropertyMetadata(typeof(HelixDescriptionList)));

    public static readonly DependencyProperty ColumnsProperty = HelixProperty.Register<HelixDescriptionList, int>(nameof(Columns), 1);
    public static readonly DependencyProperty IsInlineProperty = HelixProperty.Register<HelixDescriptionList, bool>(nameof(IsInline), false);
    public static readonly DependencyProperty IsBorderedProperty = HelixProperty.Register<HelixDescriptionList, bool>(nameof(IsBordered), false);
    public static readonly DependencyProperty LabelWidthProperty = HelixProperty.Register<HelixDescriptionList, double>(nameof(LabelWidth), 140d);

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>Label on the left of the value instead of above it.</summary>
    public bool IsInline { get => (bool)GetValue(IsInlineProperty); set => SetValue(IsInlineProperty, value); }

    /// <summary>A line under each item.</summary>
    public bool IsBordered { get => (bool)GetValue(IsBorderedProperty); set => SetValue(IsBorderedProperty, value); }
    public double LabelWidth { get => (double)GetValue(LabelWidthProperty); set => SetValue(LabelWidthProperty, value); }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is HelixDescription;

    protected override DependencyObject GetContainerForItemOverride() => new HelixDescription();
}

/// <summary>One pair of a <see cref="HelixDescriptionList"/>: Label and any content as value.</summary>
public class HelixDescription : ContentControl
{
    static HelixDescription() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixDescription), new FrameworkPropertyMetadata(typeof(HelixDescription)));

    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixDescription, string?>(nameof(Label), null);
    public static readonly DependencyProperty IconProperty = HelixProperty.Register<HelixDescription, string?>(nameof(Icon), null);
    public static readonly DependencyProperty SpanProperty = HelixProperty.Register<HelixDescription, int>(nameof(Span), 1, null,
        FrameworkPropertyMetadataOptions.AffectsParentMeasure);

    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Columns taken (e.g. 2 for a long text).</summary>
    public int Span { get => (int)GetValue(SpanProperty); set => SetValue(SpanProperty, value); }
}

/// <summary>Lays the items of a <see cref="HelixDescriptionList"/> in columns, each taking its Span.</summary>
public class HelixDescriptionPanel : Panel
{
    private int Columns => Math.Max(1, (ItemsControl.GetItemsOwner(this) as HelixDescriptionList)?.Columns ?? 1);
    private const double GapX = 28, GapY = 16;

    private IEnumerable<List<(UIElement Child, int Span)>> Rows()
    {
        var row = new List<(UIElement, int)>();
        var used = 0;
        foreach (UIElement child in InternalChildren)
        {
            var span = Math.Clamp(child is HelixDescription d ? d.Span : 1, 1, Columns);
            if (used + span > Columns && row.Count > 0)
            {
                yield return row;
                row = [];
                used = 0;
            }

            row.Add((child, span));
            used += span;
        }

        if (row.Count > 0) yield return row;
    }

    private double Cell(double width) => Math.Max(0, (width - GapX * (Columns - 1)) / Columns);

    protected override Size MeasureOverride(Size available)
    {
        var width = double.IsInfinity(available.Width) ? 600 : available.Width;
        var cell = Cell(width);
        var height = 0.0;
        var first = true;
        foreach (var row in Rows())
        {
            var rowHeight = 0.0;
            foreach (var (child, span) in row)
            {
                child.Measure(new Size(cell * span + GapX * (span - 1), double.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            }

            height += rowHeight + (first ? 0 : GapY);
            first = false;
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size final)
    {
        var cell = Cell(final.Width);
        var y = 0.0;
        foreach (var row in Rows())
        {
            var rowHeight = row.Max(r => r.Child.DesiredSize.Height);
            var x = 0.0;
            foreach (var (child, span) in row)
            {
                var w = cell * span + GapX * (span - 1);
                child.Arrange(new Rect(x, y, w, rowHeight));
                x += w + GapX;
            }

            y += rowHeight + GapY;
        }

        return final;
    }
}

public enum HelixLogLevel { Debug, Info, Warning, Error }

/// <summary>A line of a <see cref="HelixLogViewer"/>.</summary>
public sealed record HelixLogEntry(DateTime Time, HelixLogLevel Level, string Message, string? Source = null);

/// <summary>A live log: level filters with counts, search, follow mode (stays on the newest line), virtualized.</summary>
[TemplatePart(Name = "PART_Lines", Type = typeof(ListBox))]
public class HelixLogViewer : Control
{
    static HelixLogViewer() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixLogViewer), new FrameworkPropertyMetadata(typeof(HelixLogViewer)));

    public HelixLogViewer() => _refresh = new DispatcherTimer(TimeSpan.FromMilliseconds(120), DispatcherPriority.Background, (_, _) => Flush(), Dispatcher) { IsEnabled = false };

    public static readonly DependencyProperty ItemsSourceProperty = HelixProperty.Register<HelixLogViewer, IEnumerable<HelixLogEntry>?>(nameof(ItemsSource), null, (d, e) => ((HelixLogViewer)d).OnSource(e));
    public static readonly DependencyProperty IsFollowingProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(IsFollowing), true, (d, _) => ((HelixLogViewer)d).ScrollToEnd());
    public static readonly DependencyProperty SearchTextProperty = HelixProperty.Register<HelixLogViewer, string>(nameof(SearchText), "", (d, _) => ((HelixLogViewer)d).Refilter());
    public static readonly DependencyProperty ShowDebugProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(ShowDebug), false, (d, _) => ((HelixLogViewer)d).Refilter());
    public static readonly DependencyProperty ShowInfoProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(ShowInfo), true, (d, _) => ((HelixLogViewer)d).Refilter());
    public static readonly DependencyProperty ShowWarningProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(ShowWarning), true, (d, _) => ((HelixLogViewer)d).Refilter());
    public static readonly DependencyProperty ShowErrorProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(ShowError), true, (d, _) => ((HelixLogViewer)d).Refilter());
    public static readonly DependencyProperty TimeFormatProperty = HelixProperty.Register<HelixLogViewer, string>(nameof(TimeFormat), "HH:mm:ss");

    private static readonly DependencyPropertyKey CountsKey = DependencyProperty.RegisterReadOnly(nameof(Counts), typeof(int[]), typeof(HelixLogViewer), new PropertyMetadata(new int[4]));
    private static readonly DependencyPropertyKey ViewKey = DependencyProperty.RegisterReadOnly(nameof(View), typeof(ICollectionView), typeof(HelixLogViewer), new PropertyMetadata(null));
    public static readonly DependencyProperty CountsProperty = CountsKey.DependencyProperty;
    public static readonly DependencyProperty ViewProperty = ViewKey.DependencyProperty;

    /// <summary>The lines, oldest first; an ObservableCollection is followed live.</summary>
    public IEnumerable<HelixLogEntry>? ItemsSource { get => (IEnumerable<HelixLogEntry>?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>Stays on the newest line (true).</summary>
    public bool IsFollowing { get => (bool)GetValue(IsFollowingProperty); set => SetValue(IsFollowingProperty, value); }
    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public bool ShowDebug { get => (bool)GetValue(ShowDebugProperty); set => SetValue(ShowDebugProperty, value); }
    public bool ShowInfo { get => (bool)GetValue(ShowInfoProperty); set => SetValue(ShowInfoProperty, value); }
    public bool ShowWarning { get => (bool)GetValue(ShowWarningProperty); set => SetValue(ShowWarningProperty, value); }
    public bool ShowError { get => (bool)GetValue(ShowErrorProperty); set => SetValue(ShowErrorProperty, value); }
    public string TimeFormat { get => (string)GetValue(TimeFormatProperty); set => SetValue(TimeFormatProperty, value); }

    /// <summary>Lines per level: Debug, Info, Warning, Error.</summary>
    public int[] Counts => (int[])GetValue(CountsProperty);

    /// <summary>The filtered lines (template).</summary>
    public ICollectionView? View => (ICollectionView?)GetValue(ViewProperty);

    public static string DebugText => HelixTexts.Current.LogDebug;
    public static string InfoText => HelixTexts.Current.LogInfo;
    public static string WarningText => HelixTexts.Current.LogWarning;
    public static string ErrorText => HelixTexts.Current.LogError;
    public static string FollowText => HelixTexts.Current.FollowLog;
    public static string SearchPlaceholderText => HelixTexts.Current.Search;

    private readonly DispatcherTimer _refresh;
    private ListBox? _lines;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _lines = GetTemplateChild("PART_Lines") as ListBox;
        ScrollToEnd();
    }

    private void OnSource(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= OnLinesChanged;
        if (e.NewValue is INotifyCollectionChanged newList) newList.CollectionChanged += OnLinesChanged;
        var view = e.NewValue is IEnumerable<HelixLogEntry> lines ? new ListCollectionView(lines as IList ?? lines.ToList()) : null;
        if (view is not null) view.Filter = Accept;
        SetValue(ViewKey, view);
        Recount();
        ScrollToEnd();
    }

    /// <summary>Lines may arrive in bursts: counts and scrolling are refreshed at most every 120 ms.</summary>
    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e) => _refresh.IsEnabled = true;

    private void Flush()
    {
        _refresh.IsEnabled = false;
        Recount();
        ScrollToEnd();
    }

    private bool Accept(object item)
    {
        if (item is not HelixLogEntry entry) return false;
        var shown = entry.Level switch
        {
            HelixLogLevel.Debug => ShowDebug,
            HelixLogLevel.Info => ShowInfo,
            HelixLogLevel.Warning => ShowWarning,
            _ => ShowError,
        };
        var term = SearchText?.Trim();
        return shown && (string.IsNullOrEmpty(term) || entry.Message.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || entry.Source?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true);
    }

    private void Refilter()
    {
        View?.Refresh();
        ScrollToEnd();
    }

    private void Recount()
    {
        var counts = new int[4];
        foreach (var entry in ItemsSource ?? []) counts[(int)entry.Level]++;
        SetValue(CountsKey, counts);
    }

    private void ScrollToEnd()
    {
        if (!IsFollowing || _lines is null || _lines.Items.Count == 0) return;
        _lines.ScrollIntoView(_lines.Items[^1]);
    }
}

/// <summary>Formats a log time with the viewer's TimeFormat (MultiBinding: time, format).</summary>
public sealed class HelixLogTimeConverter : IMultiValueConverter
{
    public static readonly HelixLogTimeConverter Instance = new();

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [DateTime time, string format, ..] ? time.ToString(format, CultureInfo.CurrentCulture) : "";

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
