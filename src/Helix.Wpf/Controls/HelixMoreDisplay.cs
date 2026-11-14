using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Helix.Export;

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

/// <summary>
/// A line of a <see cref="HelixLogViewer"/>. Only Time, Level and Message are needed; the rest shows in the line's details
/// (click a line): structured properties (click a value to filter on it), the exception, the trace id and the user.
/// </summary>
public sealed record HelixLogEntry(DateTime Time, HelixLogLevel Level, string Message, string? Source = null)
{
    /// <summary>Structured values of the event: { ["CourseId"] = "…", ["EnrollmentId"] = "…" }.</summary>
    public IReadOnlyDictionary<string, string>? Properties { get; init; }

    /// <summary>The exception, with its stack trace.</summary>
    public string? Exception { get; init; }

    /// <summary>The distributed trace id (W3C).</summary>
    public string? TraceId { get; init; }

    /// <summary>Who was doing it (user id or name).</summary>
    public string? User { get; init; }

    public bool HasException => Exception is not null;
    public string LevelText => Level.ToString().ToUpperInvariant();
}

/// <summary>A period of the log viewer's period filter: the last <see cref="Span"/> (null = everything).</summary>
public sealed record HelixLogPeriod(string Label, TimeSpan? Span)
{
    public override string ToString() => Label;
}

/// <summary>A filter on a property value, shown as a chip above the lines.</summary>
public sealed class HelixLogFilter
{
    internal HelixLogFilter(string key, string label, string value, Action<HelixLogFilter> remove)
    {
        Key = key;
        Label = label;
        Value = value;
        RemoveCommand = new HelixCommand(() => remove(this));
    }

    public string Key { get; }
    public string Label { get; }
    public string Value { get; }
    public string ShortValue => Value.Length <= 24 ? Value : Value[..10] + "…" + Value[^8..];
    public System.Windows.Input.ICommand RemoveCommand { get; }
}

/// <summary>
/// A live log: level toggles with counts, search, source and period filters, filters on property values (chips), the details of
/// a line (properties, exception, trace id, user; copy), export of the shown lines to CSV or Excel, and follow mode with a
/// "new lines" button when you read older lines. Virtualized.
/// </summary>
[TemplatePart(Name = "PART_Lines", Type = typeof(ListBox))]
[TemplatePart(Name = "PART_Details", Type = typeof(ContentControl))]
public class HelixLogViewer : Control
{
    static HelixLogViewer() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixLogViewer), new FrameworkPropertyMetadata(typeof(HelixLogViewer)));

    public HelixLogViewer()
    {
        var texts = HelixTexts.Current;
        Periods =
        [
            new(texts.LogAllTime, null), new(texts.LogLast15Minutes, TimeSpan.FromMinutes(15)), new(texts.LogLastHour, TimeSpan.FromHours(1)),
            new(texts.LogLast24Hours, TimeSpan.FromDays(1)), new(texts.LogLast7Days, TimeSpan.FromDays(7)),
        ];
        SetCurrentValue(PeriodProperty, Periods[0]);
        SetCurrentValue(SourceChoiceProperty, texts.LogAllSources);
        SetValue(SourcesKey, new[] { texts.LogAllSources });
        ClearFiltersCommand = new HelixCommand(ClearFilters);
        ExportCsvCommand = new HelixCommand(() => ExportFile(HelixExportFormat.Csv));
        ExportExcelCommand = new HelixCommand(() => ExportFile(HelixExportFormat.Excel));
        JumpToNewestCommand = new HelixCommand(() => SetCurrentValue(IsFollowingProperty, true));
        Filters.CollectionChanged += (_, _) => SetValue(HasFiltersKey, Filters.Count > 0);
        _refresh = new DispatcherTimer(TimeSpan.FromMilliseconds(120), DispatcherPriority.Background, (_, _) => Flush(), Dispatcher) { IsEnabled = false };
        Recount();
    }

    private static void Refilter(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((HelixLogViewer)d).Refilter();

    public static readonly DependencyProperty ItemsSourceProperty = HelixProperty.Register<HelixLogViewer, IEnumerable<HelixLogEntry>?>(nameof(ItemsSource), null, (d, e) => ((HelixLogViewer)d).OnSource(e));
    public static readonly DependencyProperty IsFollowingProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(IsFollowing), true, (d, _) => ((HelixLogViewer)d).OnFollowing());
    public static readonly DependencyProperty SearchTextProperty = HelixProperty.Register<HelixLogViewer, string>(nameof(SearchText), "", Refilter);
    public static readonly DependencyProperty ShowDebugProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(ShowDebug), false, Refilter);
    public static readonly DependencyProperty ShowInfoProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(ShowInfo), true, Refilter);
    public static readonly DependencyProperty ShowWarningProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(ShowWarning), true, Refilter);
    public static readonly DependencyProperty ShowErrorProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(ShowError), true, Refilter);
    public static readonly DependencyProperty TimeFormatProperty = HelixProperty.Register<HelixLogViewer, string>(nameof(TimeFormat), "HH:mm:ss");
    public static readonly DependencyProperty ShowDetailsProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(ShowDetails), true, (d, _) => ((HelixLogViewer)d).ShowSelected());
    public static readonly DependencyProperty IsExportableProperty = HelixProperty.Register<HelixLogViewer, bool>(nameof(IsExportable), true);
    public static readonly DependencyProperty ExportFileNameProperty = HelixProperty.Register<HelixLogViewer, string>(nameof(ExportFileName), "log");
    public static readonly DependencyProperty SourceChoiceProperty = HelixProperty.Register<HelixLogViewer, string?>(nameof(SourceChoice), null, Refilter);
    public static readonly DependencyProperty PeriodProperty = HelixProperty.Register<HelixLogViewer, HelixLogPeriod?>(nameof(Period), null, Refilter);
    public static readonly DependencyProperty SelectedEntryProperty = HelixProperty.Register<HelixLogViewer, HelixLogEntry?>(nameof(SelectedEntry), null, (d, _) => ((HelixLogViewer)d).OnSelected());

    private static readonly DependencyPropertyKey CountsKey = DependencyProperty.RegisterReadOnly(nameof(Counts), typeof(int[]), typeof(HelixLogViewer), new PropertyMetadata(new int[4]));
    private static readonly DependencyPropertyKey ViewKey = DependencyProperty.RegisterReadOnly(nameof(View), typeof(ICollectionView), typeof(HelixLogViewer), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey SourcesKey = DependencyProperty.RegisterReadOnly(nameof(Sources), typeof(IReadOnlyList<string>), typeof(HelixLogViewer), new PropertyMetadata(Array.Empty<string>()));
    private static readonly DependencyPropertyKey HasSourcesKey = DependencyProperty.RegisterReadOnly(nameof(HasSources), typeof(bool), typeof(HelixLogViewer), new PropertyMetadata(false));
    private static readonly DependencyPropertyKey HasFiltersKey = DependencyProperty.RegisterReadOnly(nameof(HasFilters), typeof(bool), typeof(HelixLogViewer), new PropertyMetadata(false));
    private static readonly DependencyPropertyKey StatusTextKey = DependencyProperty.RegisterReadOnly(nameof(StatusText), typeof(string), typeof(HelixLogViewer), new PropertyMetadata(""));
    private static readonly DependencyPropertyKey UnseenTextKey = DependencyProperty.RegisterReadOnly(nameof(UnseenText), typeof(string), typeof(HelixLogViewer), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey HasUnseenKey = DependencyProperty.RegisterReadOnly(nameof(HasUnseen), typeof(bool), typeof(HelixLogViewer), new PropertyMetadata(false));
    public static readonly DependencyProperty CountsProperty = CountsKey.DependencyProperty;
    public static readonly DependencyProperty ViewProperty = ViewKey.DependencyProperty;
    public static readonly DependencyProperty SourcesProperty = SourcesKey.DependencyProperty;
    public static readonly DependencyProperty HasSourcesProperty = HasSourcesKey.DependencyProperty;
    public static readonly DependencyProperty HasFiltersProperty = HasFiltersKey.DependencyProperty;
    public static readonly DependencyProperty StatusTextProperty = StatusTextKey.DependencyProperty;
    public static readonly DependencyProperty UnseenTextProperty = UnseenTextKey.DependencyProperty;
    public static readonly DependencyProperty HasUnseenProperty = HasUnseenKey.DependencyProperty;

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

    /// <summary>A click on a line shows its details (true).</summary>
    public bool ShowDetails { get => (bool)GetValue(ShowDetailsProperty); set => SetValue(ShowDetailsProperty, value); }

    /// <summary>The CSV and Excel buttons, which save the lines shown (true).</summary>
    public bool IsExportable { get => (bool)GetValue(IsExportableProperty); set => SetValue(IsExportableProperty, value); }
    public string ExportFileName { get => (string)GetValue(ExportFileNameProperty); set => SetValue(ExportFileNameProperty, value); }

    /// <summary>The source shown ("All sources" for every one).</summary>
    public string? SourceChoice { get => (string?)GetValue(SourceChoiceProperty); set => SetValue(SourceChoiceProperty, value); }
    public HelixLogPeriod? Period { get => (HelixLogPeriod?)GetValue(PeriodProperty); set => SetValue(PeriodProperty, value); }

    /// <summary>The line whose details are shown.</summary>
    public HelixLogEntry? SelectedEntry { get => (HelixLogEntry?)GetValue(SelectedEntryProperty); set => SetValue(SelectedEntryProperty, value); }

    /// <summary>A testable clock for the period filter (system clock by default).</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>Lines per level: Debug, Info, Warning, Error.</summary>
    public int[] Counts => (int[])GetValue(CountsProperty);

    /// <summary>The filtered lines (template).</summary>
    public ICollectionView? View => (ICollectionView?)GetValue(ViewProperty);

    /// <summary>"All sources" and the sources of the lines (template).</summary>
    public IReadOnlyList<string> Sources => (IReadOnlyList<string>)GetValue(SourcesProperty);
    public bool HasSources => (bool)GetValue(HasSourcesProperty);

    /// <summary>The periods offered (template).</summary>
    public IReadOnlyList<HelixLogPeriod> Periods { get; }

    /// <summary>The property filters (chips).</summary>
    public ObservableCollection<HelixLogFilter> Filters { get; } = [];
    public bool HasFilters => (bool)GetValue(HasFiltersProperty);

    /// <summary>"102 of 412 lines".</summary>
    public string StatusText => (string)GetValue(StatusTextProperty);

    /// <summary>"12 new lines", while not following.</summary>
    public string? UnseenText => (string?)GetValue(UnseenTextProperty);
    public bool HasUnseen => (bool)GetValue(HasUnseenProperty);

    /// <summary>Number of lines shown after the filters.</summary>
    public int VisibleCount => (View as ListCollectionView)?.Count ?? 0;

    /// <summary>The lines shown, after every filter (what the export writes).</summary>
    public IReadOnlyList<HelixLogEntry> VisibleEntries => View?.Cast<HelixLogEntry>().ToList() ?? [];

    public System.Windows.Input.ICommand ClearFiltersCommand { get; }
    public System.Windows.Input.ICommand ExportCsvCommand { get; }
    public System.Windows.Input.ICommand ExportExcelCommand { get; }
    public System.Windows.Input.ICommand JumpToNewestCommand { get; }

    public static string DebugText => HelixTexts.Current.LogDebug;
    public static string InfoText => HelixTexts.Current.LogInfo;
    public static string WarningText => HelixTexts.Current.LogWarning;
    public static string ErrorText => HelixTexts.Current.LogError;
    public static string FollowText => HelixTexts.Current.FollowLog;
    public static string SearchPlaceholderText => HelixTexts.Current.Search;
    public static string ClearFiltersText => HelixTexts.Current.ClearFilters;

    private const string UserKey = "\u0001user";
    private const string TraceKey = "\u0001trace";
    private readonly DispatcherTimer _refresh;
    private ListBox? _lines;
    private ContentControl? _details;
    private int _unseen;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _lines = GetTemplateChild("PART_Lines") as ListBox;
        _details = GetTemplateChild("PART_Details") as ContentControl;
        ShowSelected();
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

    private void OnFollowing()
    {
        if (IsFollowing) SetUnseen(0);
        ScrollToEnd();
    }

    private void OnSelected()
    {
        // Reading a line: stop following, so new lines don't scroll it away.
        if (SelectedEntry is not null) SetCurrentValue(IsFollowingProperty, false);
        ShowSelected();
    }

    /// <summary>Lines may arrive in bursts: counts and scrolling are refreshed at most every 120 ms.</summary>
    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!IsFollowing && e.NewItems is { Count: > 0 } added) SetUnseen(_unseen + added.Count);
        _refresh.IsEnabled = true;
    }

    /// <summary>Applies pending new lines now (the viewer does it every 120 ms).</summary>
    public void Flush()
    {
        _refresh.IsEnabled = false;
        Recount();
        ScrollToEnd();
    }

    private void SetUnseen(int count)
    {
        _unseen = count;
        SetValue(HasUnseenKey, count > 0);
        SetValue(UnseenTextKey, count > 0 ? string.Format(CultureInfo.CurrentCulture, HelixTexts.Current.LogNewLines, count) : null);
    }

    /// <summary>Shows only the lines whose property <paramref name="key"/> equals <paramref name="value"/> (adds to the other filters).</summary>
    public void AddFilter(string key, string value)
    {
        foreach (var same in Filters.Where(f => f.Key == key).ToList()) Filters.Remove(same);
        var label = key switch { UserKey => HelixTexts.Current.LogUser, TraceKey => HelixTexts.Current.LogTraceId, _ => key };
        Filters.Add(new HelixLogFilter(key, label, value, filter => { Filters.Remove(filter); Refilter(); }));
        Refilter();
    }

    /// <summary>Removes the property, source and period filters and the search (levels stay as they are).</summary>
    public void ClearFilters()
    {
        Filters.Clear();
        SetCurrentValue(SearchTextProperty, "");
        SetCurrentValue(SourceChoiceProperty, HelixTexts.Current.LogAllSources);
        SetCurrentValue(PeriodProperty, Periods[0]);
        Refilter();
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
        if (!shown) return false;
        if (SourceChoice is { } source && source != HelixTexts.Current.LogAllSources && entry.Source != source) return false;
        if (Period?.Span is { } span && entry.Time < TimeProvider.GetLocalNow().DateTime - span) return false;
        foreach (var filter in Filters)
        {
            var actual = filter.Key switch
            {
                UserKey => entry.User,
                TraceKey => entry.TraceId,
                _ => entry.Properties?.GetValueOrDefault(filter.Key),
            };
            if (actual != filter.Value) return false;
        }

        var term = SearchText?.Trim();
        return string.IsNullOrEmpty(term)
               || entry.Message.Contains(term, StringComparison.CurrentCultureIgnoreCase)
               || entry.Source?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true
               || entry.User?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true
               || entry.TraceId?.Contains(term, StringComparison.OrdinalIgnoreCase) == true
               || entry.Properties?.Values.Any(v => v.Contains(term, StringComparison.CurrentCultureIgnoreCase)) == true;
    }

    private void Refilter()
    {
        if (Periods is null) return; // during construction
        View?.Refresh();
        Recount();
        ScrollToEnd();
    }

    private void Recount()
    {
        var counts = new int[4];
        var all = ItemsSource?.ToList() ?? [];
        foreach (var entry in all) counts[(int)entry.Level]++;
        SetValue(CountsKey, counts);
        var texts = HelixTexts.Current;
        var sources = all.Select(e => e.Source).OfType<string>().Where(s => s.Length > 0).Distinct().Order(StringComparer.CurrentCulture).ToList();
        if (!sources.SequenceEqual(Sources.Skip(1))) SetValue(SourcesKey, new[] { texts.LogAllSources }.Concat(sources).ToArray());
        SetValue(HasSourcesKey, sources.Count > 1);
        SetValue(StatusTextKey, string.Format(CultureInfo.CurrentCulture, texts.LogShown, VisibleCount, all.Count));
        if (SelectedEntry is { } selected && View is not null && !View.Contains(selected)) SetCurrentValue(SelectedEntryProperty, null);
    }

    private void ScrollToEnd()
    {
        if (!IsFollowing || _lines is null || _lines.Items.Count == 0) return;
        _lines.ScrollIntoView(_lines.Items[^1]);
    }

    private TextBlock Text(string text, double size, string brush, FontWeight weight, FontFamily? font = null)
    {
        var block = new TextBlock { Text = text, FontSize = size, FontWeight = weight, TextWrapping = TextWrapping.Wrap };
        if (font is not null) block.FontFamily = font;
        block.SetResourceReference(TextBlock.ForegroundProperty, brush);
        return block;
    }

    private Button IconButton(string icon, string tip)
    {
        var button = new Button { Content = new HelixIcon { Icon = icon, Size = 14 }, ToolTip = tip, Margin = new Thickness(4, 0, 0, 0) };
        button.SetResourceReference(StyleProperty, "Helix.IconButton");
        return button;
    }

    /// <summary>Builds the details panel of the selected line: each value is a button that filters on it.</summary>
    private void ShowSelected()
    {
        if (_details is null) return;
        if (SelectedEntry is not { } e || !ShowDetails)
        {
            _details.Content = null;
            _details.Visibility = Visibility.Collapsed;
            return;
        }

        var texts = HelixTexts.Current;
        var mono = new FontFamily("Cascadia Mono, Consolas");
        var level = e.Level switch { HelixLogLevel.Info => HelixKeys.Info, HelixLogLevel.Warning => HelixKeys.Warning, HelixLogLevel.Error => HelixKeys.Danger, _ => HelixKeys.TextSubtle };
        var copy = IconButton("copy", texts.LogCopy);
        copy.Click += (_, _) =>
        {
            try { Clipboard.SetText(DetailsText(e)); }
            catch (System.Runtime.InteropServices.COMException) { /* clipboard busy */ }
        };
        var close = IconButton("x", texts.Close);
        close.Click += (_, _) => SetCurrentValue(SelectedEntryProperty, null);
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(copy);
        actions.Children.Add(close);
        DockPanel.SetDock(actions, Dock.Right);
        var title = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var levelText = Text(e.LevelText, 11, level, FontWeights.Bold);
        levelText.Margin = new Thickness(0, 0, 10, 0);
        levelText.VerticalAlignment = VerticalAlignment.Center;
        title.Children.Add(levelText);
        title.Children.Add(Text(e.Time.ToString("F", CultureInfo.CurrentCulture), 12.5, HelixKeys.TextMuted, FontWeights.Normal));
        var head = new DockPanel();
        head.Children.Add(actions);
        head.Children.Add(title);

        var facts = new Grid();
        facts.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        facts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        facts.ColumnDefinitions.Add(new ColumnDefinition());
        void Fact(string label, string value, Action filter)
        {
            var row = facts.RowDefinitions.Count;
            facts.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var name = Text(label, 12.5, HelixKeys.TextMuted, FontWeights.SemiBold);
            name.Margin = new Thickness(0, 2, 0, 2);
            Grid.SetRow(name, row);
            facts.Children.Add(name);
            var button = new Button
            {
                Content = Text(value, 12.5, HelixKeys.Text, FontWeights.Normal, mono),
                Background = Brushes.Transparent, BorderThickness = default, Padding = new Thickness(4, 1, 4, 1), HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = System.Windows.Input.Cursors.Hand, ToolTip = string.Format(CultureInfo.CurrentCulture, texts.LogFilterOn, label, value),
                Template = PlainButton(),
            };
            Grid.SetRow(button, row);
            Grid.SetColumn(button, 2);
            button.Click += (_, _) => filter();
            facts.Children.Add(button);
        }

        if (!string.IsNullOrEmpty(e.Source)) Fact(texts.LogSource, e.Source, () => SetCurrentValue(SourceChoiceProperty, e.Source));
        if (!string.IsNullOrEmpty(e.User)) Fact(texts.LogUser, e.User, () => AddFilter(UserKey, e.User));
        if (!string.IsNullOrEmpty(e.TraceId)) Fact(texts.LogTraceId, e.TraceId, () => AddFilter(TraceKey, e.TraceId));
        foreach (var (key, value) in e.Properties ?? new Dictionary<string, string>()) Fact(key, value, () => AddFilter(key, value));

        var panel = new StackPanel();
        var message = Text(e.Message, 13.5, HelixKeys.Text, FontWeights.Normal);
        message.Margin = new Thickness(0, 8, 0, 8);
        panel.Children.Add(head);
        panel.Children.Add(message);
        panel.Children.Add(facts);
        if (e.Exception is { } exception)
        {
            var header = Text(texts.LogException, 12.5, HelixKeys.Danger, FontWeights.SemiBold);
            header.Margin = new Thickness(0, 10, 0, 6);
            panel.Children.Add(header);
            var stack = new TextBox
            {
                Text = exception, FontFamily = mono, FontSize = 12, TextWrapping = TextWrapping.Wrap, IsReadOnly = true, BorderThickness = default,
                Background = Brushes.Transparent, Padding = default,
            };
            stack.SetResourceReference(ForegroundProperty, HelixKeys.Text);
            var box = new Border { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8, 10, 8), Child = stack };
            box.SetResourceReference(Border.BackgroundProperty, HelixKeys.Surface);
            box.SetResourceReference(Border.BorderBrushProperty, HelixKeys.Border);
            panel.Children.Add(box);
        }

        _details.Content = new ScrollViewer { Content = panel, MaxHeight = 200, Padding = new Thickness(14, 10, 14, 10), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Focusable = false };
        _details.Visibility = Visibility.Visible;
    }

    private static ControlTemplate PlainButton()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        border.SetBinding(Border.PaddingProperty, new Binding(nameof(Padding)) { RelativeSource = RelativeSource.TemplatedParent });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        border.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
        return new ControlTemplate(typeof(Button)) { VisualTree = border };
    }

    private static string DetailsText(HelixLogEntry e)
    {
        var lines = new List<string> { $"{e.Time.ToString("O", CultureInfo.InvariantCulture)} [{e.Level}] {e.Source} {e.Message}" };
        if (e.User is not null) lines.Add($"User: {e.User}");
        if (e.TraceId is not null) lines.Add($"TraceId: {e.TraceId}");
        foreach (var (key, value) in e.Properties ?? new Dictionary<string, string>()) lines.Add($"{key}: {value}");
        if (e.Exception is not null) lines.Add(e.Exception);
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>The lines shown, ready for Helix.Export: time, level, source, message, user, trace id, one column per property, exception.</summary>
    public HelixExportTable ToExportTable()
    {
        var texts = HelixTexts.Current;
        var rows = VisibleEntries;
        var keys = rows.SelectMany(e => e.Properties?.Keys ?? []).Distinct().ToList();
        var columns = new List<HelixExportColumn>
        {
            new() { Header = texts.LogTime, Value = r => ((HelixLogEntry)r).Time, Format = "yyyy-MM-dd HH:mm:ss" },
            new() { Header = texts.LogLevel, Value = r => ((HelixLogEntry)r).Level.ToString() },
            new() { Header = texts.LogSource, Value = r => ((HelixLogEntry)r).Source },
            new() { Header = texts.LogMessage, Value = r => ((HelixLogEntry)r).Message },
            new() { Header = texts.LogUser, Value = r => ((HelixLogEntry)r).User },
            new() { Header = texts.LogTraceId, Value = r => ((HelixLogEntry)r).TraceId },
        };
        columns.AddRange(keys.Select(k => new HelixExportColumn { Header = k, Value = r => ((HelixLogEntry)r).Properties?.GetValueOrDefault(k) }));
        columns.Add(new() { Header = texts.LogException, Value = r => ((HelixLogEntry)r).Exception });
        return new HelixExportTable(columns, rows.Cast<object>());
    }

    /// <summary>The lines shown as a CSV or Excel file (bytes).</summary>
    public byte[] ToFile(HelixExportFormat format) =>
        format == HelixExportFormat.Csv
            ? [.. System.Text.Encoding.UTF8.GetPreamble(), .. System.Text.Encoding.UTF8.GetBytes(HelixCsv.Write(ToExportTable()))]
            : HelixExport.Write(ToExportTable(), format);

    /// <summary>Asks where to save, then writes the lines shown.</summary>
    public void ExportFile(HelixExportFormat format)
    {
        var extension = HelixExport.Extension(format);
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = ExportFileName + extension,
            DefaultExt = extension,
            Filter = $"{HelixExport.Label(format)}|*{extension}",
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try
        {
            System.IO.File.WriteAllBytes(dialog.FileName, ToFile(format));
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        {
            HelixToasts.Error(ex.Message, HelixTexts.Current.Export);
        }
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
