using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Helix.Avalonia;

/// <summary>
/// Line, area, step and bar chart on one Y axis (never two). Series in fixed color order, legend toggles series,
/// hover crosshair with a tooltip, reference lines, an optional "now" line for time axes.
/// </summary>
[TemplatePart("PART_Plot", typeof(HelixPlot))]
[TemplatePart("PART_Legend", typeof(Panel))]
public class HelixChart : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixChart, string?>(nameof(Title));
    public static readonly StyledProperty<string?> SubtitleProperty = AvaloniaProperty.Register<HelixChart, string?>(nameof(Subtitle));
    public static readonly StyledProperty<double> PlotHeightProperty = AvaloniaProperty.Register<HelixChart, double>(nameof(PlotHeight), 260);
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<HelixChart, string?>(nameof(Unit));
    public static readonly StyledProperty<string> YFormatProperty = AvaloniaProperty.Register<HelixChart, string>(nameof(YFormat), "#,0.##");
    public static readonly StyledProperty<double?> YMinProperty = AvaloniaProperty.Register<HelixChart, double?>(nameof(YMin));
    public static readonly StyledProperty<double?> YMaxProperty = AvaloniaProperty.Register<HelixChart, double?>(nameof(YMax));
    public static readonly StyledProperty<double?> XMinProperty = AvaloniaProperty.Register<HelixChart, double?>(nameof(XMin));
    public static readonly StyledProperty<double?> XMaxProperty = AvaloniaProperty.Register<HelixChart, double?>(nameof(XMax));
    public static readonly StyledProperty<DateTime?> FromProperty = AvaloniaProperty.Register<HelixChart, DateTime?>(nameof(From));
    public static readonly StyledProperty<DateTime?> ToProperty = AvaloniaProperty.Register<HelixChart, DateTime?>(nameof(To));
    public static readonly StyledProperty<Func<double, string>?> XFormatProperty = AvaloniaProperty.Register<HelixChart, Func<double, string>?>(nameof(XFormat));
    public static readonly StyledProperty<string?> TimeFormatProperty = AvaloniaProperty.Register<HelixChart, string?>(nameof(TimeFormat));
    public static readonly StyledProperty<bool> ShowNowProperty = AvaloniaProperty.Register<HelixChart, bool>(nameof(ShowNow));
    public static readonly StyledProperty<IBrush?> NowBrushProperty = AvaloniaProperty.Register<HelixChart, IBrush?>(nameof(NowBrush));
    public static readonly StyledProperty<bool> ShowLegendProperty = AvaloniaProperty.Register<HelixChart, bool>(nameof(ShowLegend), true);
    public static readonly StyledProperty<string?> EmptyTextProperty = AvaloniaProperty.Register<HelixChart, string?>(nameof(EmptyText));

    private HelixPlot? _plot;
    private Panel? _legend;
    private readonly HashSet<HelixChartSeries> _hidden = [];
    private readonly HashSet<HelixChartSeries> _seenHidden = [];
    private Point? _pointer;
    private DispatcherTimer? _nowTimer;

    public HelixChart()
    {
        Series.CollectionChanged += (_, e) =>
        {
            foreach (var s in e.OldItems?.OfType<HelixChartSeries>() ?? []) { LogicalChildren.Remove(s); s.PropertyChanged -= OnItemChanged; }
            foreach (var s in e.NewItems?.OfType<HelixChartSeries>() ?? []) { LogicalChildren.Add(s); s.PropertyChanged += OnItemChanged; }
            Refresh();
        };
        Lines.CollectionChanged += (_, e) =>
        {
            foreach (var l in e.OldItems?.OfType<HelixChartLine>() ?? []) { LogicalChildren.Remove(l); l.PropertyChanged -= OnItemChanged; }
            foreach (var l in e.NewItems?.OfType<HelixChartLine>() ?? []) { LogicalChildren.Add(l); l.PropertyChanged += OnItemChanged; }
            Refresh();
        };
        ActualThemeVariantChanged += (_, _) => Refresh();
    }

    /// <summary>The series (HelixChartSeries elements in AXAML, or Series.Add in code).</summary>
    [Content]
    public AvaloniaList<HelixChartSeries> Series { get; } = [];

    /// <summary>Reference lines (target, limit, event).</summary>
    public AvaloniaList<HelixChartLine> Lines { get; } = [];

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Subtitle { get => GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }

    /// <summary>Height of the drawing area (260 by default).</summary>
    public double PlotHeight { get => GetValue(PlotHeightProperty); set => SetValue(PlotHeightProperty, value); }

    /// <summary>Unit shown in the tooltip, e.g. "MW".</summary>
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    /// <summary>.NET format of Y values ("#,0.##" by default).</summary>
    public string YFormat { get => GetValue(YFormatProperty); set => SetValue(YFormatProperty, value); }
    public double? YMin { get => GetValue(YMinProperty); set => SetValue(YMinProperty, value); }
    public double? YMax { get => GetValue(YMaxProperty); set => SetValue(YMaxProperty, value); }
    public double? XMin { get => GetValue(XMinProperty); set => SetValue(XMinProperty, value); }
    public double? XMax { get => GetValue(XMaxProperty); set => SetValue(XMaxProperty, value); }

    /// <summary>Start of a time axis (else the first point).</summary>
    public DateTime? From { get => GetValue(FromProperty); set => SetValue(FromProperty, value); }

    /// <summary>End of a time axis (else the last point).</summary>
    public DateTime? To { get => GetValue(ToProperty); set => SetValue(ToProperty, value); }

    /// <summary>Label of a numeric X value (categories: x => months[(int)x]).</summary>
    public Func<double, string>? XFormat { get => GetValue(XFormatProperty); set => SetValue(XFormatProperty, value); }

    /// <summary>Format of time labels ("HH:mm", "dd/MM"…); automatic when null.</summary>
    public string? TimeFormat { get => GetValue(TimeFormatProperty); set => SetValue(TimeFormatProperty, value); }

    /// <summary>A vertical line at the current time (time axis), refreshed every 30 s.</summary>
    public bool ShowNow { get => GetValue(ShowNowProperty); set => SetValue(ShowNowProperty, value); }
    public IBrush? NowBrush { get => GetValue(NowBrushProperty); set => SetValue(NowBrushProperty, value); }

    /// <summary>Legend with series toggles (shown from two series).</summary>
    public bool ShowLegend { get => GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    public string? EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    private void OnItemChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name is not ("DataContext" or "Parent" or "TemplatedParent")) Refresh();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != PlotHeightProperty && change.Property != TitleProperty && change.Property != SubtitleProperty
            && change.Property.OwnerType == typeof(HelixChart))
        {
            Refresh();
        }

        if (change.Property == ShowNowProperty) StartNowTimer();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_plot is not null)
        {
            _plot.PointerMoved -= OnPlotPointerMoved;
            _plot.PointerExited -= OnPlotPointerExited;
        }

        base.OnApplyTemplate(e);
        _plot = e.NameScope.Find<HelixPlot>("PART_Plot");
        _legend = e.NameScope.Find<Panel>("PART_Legend");
        if (_plot is not null)
        {
            _plot.Draw = DrawPlot;
            _plot.PointerMoved += OnPlotPointerMoved;
            _plot.PointerExited += OnPlotPointerExited;
        }

        Refresh();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        StartNowTimer();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _nowTimer?.Stop();
    }

    private void StartNowTimer()
    {
        if (!ShowNow) { _nowTimer?.Stop(); return; }
        _nowTimer ??= new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Background, (_, _) => _plot?.InvalidateVisual());
        _nowTimer.Start();
    }

    private void OnPlotPointerMoved(object? sender, PointerEventArgs e)
    {
        _pointer = e.GetPosition(_plot);
        _plot?.InvalidateVisual();
    }

    private void OnPlotPointerExited(object? sender, PointerEventArgs e)
    {
        _pointer = null;
        _plot?.InvalidateVisual();
    }

    /// <summary>Redraws after a data change made in code (e.g. replacing a series' Points list in place).</summary>
    public void Refresh()
    {
        foreach (var s in Series)
        {
            if (s.IsHidden && _seenHidden.Add(s)) _hidden.Add(s);
        }

        BuildLegend();
        _plot?.InvalidateVisual();
    }

    private IBrush ColorOf(HelixChartSeries series) => HelixDraw.Resolve(this, series.Color) ?? HelixDraw.Series(this, Series.IndexOf(series) + 1);

    private void BuildLegend()
    {
        if (_legend is null) return;
        _legend.Children.Clear();
        _legend.IsVisible = ShowLegend && Series.Count >= 2;
        foreach (var series in Series)
        {
            var hidden = _hidden.Contains(series);
            var key = series.Type is HelixSeriesType.Bar or HelixSeriesType.Area
                ? new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(3), Background = ColorOf(series) }
                : new Border { Width = 14, Height = 2.5, CornerRadius = new CornerRadius(1), Background = ColorOf(series) };
            key.Margin = new Thickness(0, 0, 7, 0);
            key.VerticalAlignment = VerticalAlignment.Center;
            var label = new TextBlock { Text = series.Label, FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center };
            label.Bind(TextBlock.ForegroundProperty, label.GetResourceObservable(hidden ? HelixKeys.TextSubtle : HelixKeys.TextMuted));
            var content = new StackPanel { Orientation = Orientation.Horizontal, Opacity = hidden ? 0.5 : 1, Children = { key, label } };
            var button = new HelixButton
            {
                Content = content, Variant = HelixVariant.Ghost, Tone = HelixTone.Neutral, Size = HelixSize.Sm,
                Height = 26, Padding = new Thickness(8, 0), Margin = new Thickness(0, 0, 4, 0),
            };
            ToolTip.SetTip(button, series.Label);
            var s = series;
            button.Click += (_, _) =>
            {
                if (!_hidden.Remove(s)) _hidden.Add(s);
                Refresh();
            };
            _legend.Children.Add(button);
        }
    }

    private string FormatY(double value) => value.ToString(YFormat, CultureInfo.CurrentCulture);

    private void DrawPlot(DrawingContext dc, Size size)
    {
        var visible = Series.Where(s => !_hidden.Contains(s)).ToList();
        var points = visible.ToDictionary(s => s, s => (s.Points ?? []).OrderBy(p => p.X).ToList());
        var all = points.Values.SelectMany(p => p).ToList();
        var muted = HelixDraw.Brush(this, HelixKeys.TextSubtle, Colors.Gray);
        var grid = HelixDraw.Brush(this, HelixKeys.Border, Colors.LightGray);
        var surface = HelixDraw.Brush(this, HelixKeys.Surface, Colors.White);

        if (all.Count == 0)
        {
            var empty = HelixDraw.Text(this, EmptyText ?? HelixTexts.Current.NoData, 13, muted);
            dc.DrawText(empty, new Point((size.Width - empty.Width) / 2, (size.Height - empty.Height) / 2));
            return;
        }

        var isTime = all.Any(p => p.IsTime) || From is not null || To is not null;
        var hasBars = visible.Any(s => s.Type == HelixSeriesType.Bar);
        var xs = all.Select(p => p.X).Distinct().OrderBy(x => x).ToList();
        var xMin = XMin ?? (From is { } f ? HelixTime.ToX(f) : xs[0]);
        var xMax = XMax ?? (To is { } t ? HelixTime.ToX(t) : xs[^1]);
        var ys = all.Where(p => p.Y is not null).Select(p => p.Y!.Value).ToList();
        foreach (var line in Lines.Where(l => l.Y is not null)) ys.Add(line.Y!.Value);
        var dataMin = ys.Count == 0 ? 0 : ys.Min();
        var dataMax = ys.Count == 0 ? 1 : ys.Max();
        if (hasBars || visible.Any(s => s.Type == HelixSeriesType.Area))
        {
            dataMin = Math.Min(0, dataMin); // bars and areas start at zero
            dataMax = Math.Max(0, dataMax);
        }

        var (yMin, yMax, yStep) = HelixDraw.NiceScale(YMin ?? dataMin, YMax ?? dataMax, Math.Max(2, (int)(size.Height / 55)));
        if (YMin is { } fixedMin) yMin = fixedMin;
        if (YMax is { } fixedMax) yMax = fixedMax;

        // Plot area: the left gutter fits the widest Y label.
        var yLabels = new List<(double Value, FormattedText Text)>();
        for (var v = yMin; v <= yMax + yStep / 1000; v += yStep) yLabels.Add((v, HelixDraw.Text(this, FormatY(v), 11.5, muted)));
        var left = (yLabels.Count == 0 ? 20 : yLabels.Max(l => l.Text.Width)) + 10;
        var area = new Rect(left, 8, Math.Max(10, size.Width - left - 8), Math.Max(10, size.Height - 8 - 24));

        // Bars: one band per distinct X, with half a band of padding at each end.
        var band = 0.0;
        if (hasBars && xs.Count > 0)
        {
            var steps = xs.Zip(xs.Skip(1), (a, b) => b - a).Where(d => d > 0).ToList();
            var unit = steps.Count == 0 ? 1 : steps.Min();
            xMin = Math.Min(xMin, xs[0]) - unit / 2;
            xMax = Math.Max(xMax, xs[^1]) + unit / 2;
            band = unit;
        }

        if (xMax <= xMin) xMax = xMin + 1;
        double X(double x) => area.Left + (x - xMin) / (xMax - xMin) * area.Width;
        double Y(double y) => area.Bottom - (y - yMin) / (yMax - yMin) * area.Height;

        // Grid and Y labels (recessive).
        var gridPen = HelixDraw.Pen(grid, 1);
        foreach (var (value, text) in yLabels)
        {
            var y = Math.Round(Y(value)) + 0.5;
            dc.DrawLine(gridPen, new Point(area.Left, y), new Point(area.Right, y));
            dc.DrawText(text, new Point(left - 8 - text.Width, y - text.Height / 2));
        }

        DrawXLabels(dc, area, xMin, xMax, xs, isTime, hasBars, muted, X);

        using (dc.PushClip(new Rect(area.Left - 1, area.Top - 6, area.Width + 2, area.Height + 7)))
        {
            // Bars (grouped), then areas, then lines on top.
            var bars = visible.Where(s => s.Type == HelixSeriesType.Bar).ToList();
            if (bars.Count > 0)
            {
                var bandPx = band / (xMax - xMin) * area.Width;
                var groupWidth = bandPx * 0.72;
                var gap = bars.Count > 1 ? 2 : 0; // 2px surface gap between adjacent bars
                var barWidth = Math.Max(1, (groupWidth - gap * (bars.Count - 1)) / bars.Count);
                var zero = Y(Math.Clamp(0, yMin, yMax));
                for (var b = 0; b < bars.Count; b++)
                {
                    var brush = ColorOf(bars[b]);
                    foreach (var p in points[bars[b]].Where(p => p.Y is not null))
                    {
                        var x0 = X(p.X) - groupWidth / 2 + b * (barWidth + gap);
                        var y = Y(p.Y!.Value);
                        var rect = new Rect(x0, Math.Min(y, zero), barWidth, Math.Max(1, Math.Abs(zero - y)));
                        dc.DrawGeometry(brush, null, HelixDraw.Bar(rect, 4, p.Y >= 0));
                    }
                }
            }

            foreach (var series in visible.Where(s => s.Type != HelixSeriesType.Bar))
            {
                var brush = ColorOf(series);
                var segments = Segments(points[series], series.Type == HelixSeriesType.Step, X, Y);
                if (series.Type == HelixSeriesType.Area)
                {
                    var fill = HelixDraw.WithOpacity(brush, 0.14);
                    var baseline = Y(Math.Clamp(0, yMin, yMax));
                    foreach (var segment in segments.Where(s => s.Count > 1))
                    {
                        var g = new StreamGeometry();
                        using (var c = g.Open())
                        {
                            c.BeginFigure(new Point(segment[0].X, baseline), true);
                            foreach (var pt in segment) c.LineTo(pt);
                            c.LineTo(new Point(segment[^1].X, baseline));
                            c.EndFigure(true);
                        }

                        dc.DrawGeometry(fill, null, g);
                    }
                }

                var pen = HelixDraw.Pen(brush, 2, series.IsDashed);
                foreach (var segment in segments)
                {
                    if (segment.Count == 1)
                    {
                        dc.DrawEllipse(brush, null, segment[0], 2.5, 2.5);
                        continue;
                    }

                    var g = new StreamGeometry();
                    using (var c = g.Open())
                    {
                        c.BeginFigure(segment[0], false);
                        for (var i = 1; i < segment.Count; i++) c.LineTo(segment[i]);
                        c.EndFigure(false);
                    }

                    dc.DrawGeometry(null, pen, g);
                }

                if (series.ShowMarkers)
                {
                    var ring = HelixDraw.Pen(surface, 2);
                    foreach (var p in points[series].Where(p => p.Y is not null)) dc.DrawEllipse(brush, ring, new Point(X(p.X), Y(p.Y!.Value)), 4.5, 4.5);
                }
            }
        }

        // Reference lines with their label.
        foreach (var line in Lines)
        {
            var brush = HelixDraw.Resolve(this, line.Color) ?? HelixDraw.Brush(this, HelixKeys.TextMuted, Colors.Gray);
            var pen = HelixDraw.Pen(brush, 1.5, line.IsDashed);
            var label = line.Label is null ? null : HelixDraw.Text(this, line.Label, 11.5, brush, FontWeight.SemiBold);
            if (line.Y is { } yv && yv >= yMin && yv <= yMax)
            {
                var y = Y(yv);
                dc.DrawLine(pen, new Point(area.Left, y), new Point(area.Right, y));
                if (label is not null) dc.DrawText(label, new Point(area.Right - label.Width, y - label.Height - 2));
            }
            else if ((line.At is { } at ? HelixTime.ToX(at) : line.X) is { } xv && xv >= xMin && xv <= xMax)
            {
                var x = X(xv);
                dc.DrawLine(pen, new Point(x, area.Top), new Point(x, area.Bottom));
                if (label is not null) dc.DrawText(label, new Point(Math.Min(x + 4, area.Right - label.Width), area.Top));
            }
        }

        // Now line.
        if (ShowNow && isTime)
        {
            var now = HelixTime.ToX(DateTime.Now);
            if (now >= xMin && now <= xMax)
            {
                var brush = NowBrush ?? HelixDraw.Brush(this, HelixKeys.Danger, Colors.OrangeRed);
                var x = X(now);
                dc.DrawLine(HelixDraw.Pen(brush, 1.5), new Point(x, area.Top), new Point(x, area.Bottom));
                var label = HelixDraw.Text(this, DateTime.Now.ToString("HH:mm", CultureInfo.CurrentCulture), 11, Brushes.White, FontWeight.SemiBold);
                var pill = new Rect(x - label.Width / 2 - 5, area.Top - 8, label.Width + 10, label.Height + 2);
                dc.DrawRectangle(brush, null, pill, 4, 4);
                dc.DrawText(label, new Point(pill.Left + 5, pill.Top + 1));
            }
        }

        // Hover: crosshair on the nearest X and a tooltip with every visible series.
        if (_pointer is { } pointer && area.Contains(pointer) && xs.Count > 0)
        {
            var nearest = xs.OrderBy(x => Math.Abs(X(x) - pointer.X)).First();
            var hx = X(nearest);
            dc.DrawLine(HelixDraw.Pen(HelixDraw.Brush(this, HelixKeys.BorderStrong, Colors.Gray), 1), new Point(hx, area.Top), new Point(hx, area.Bottom));
            var rows = new List<(IBrush?, string, string)>();
            foreach (var series in visible)
            {
                var point = points[series].FirstOrDefault(p => p.X == nearest);
                if (point.Y is not { } value) continue;
                var brush = ColorOf(series);
                rows.Add((brush, series.Label, FormatY(value) + (Unit is null ? "" : " " + Unit)));
                if (series.Type != HelixSeriesType.Bar) dc.DrawEllipse(brush, HelixDraw.Pen(surface, 2), new Point(hx, Y(value)), 5, 5);
            }

            if (rows.Count > 0) HelixDraw.Tooltip(dc, this, new Point(hx, pointer.Y), size, LabelOfX(nearest, isTime, xs), rows);
        }
    }

    /// <summary>Line pieces between gaps (Y = null), as screen points.</summary>
    private static List<List<Point>> Segments(List<HelixPoint> points, bool step, Func<double, double> x, Func<double, double> y)
    {
        var result = new List<List<Point>>();
        List<Point>? current = null;
        foreach (var p in points)
        {
            if (p.Y is null)
            {
                current = null;
                continue;
            }

            var pt = new Point(x(p.X), y(p.Y.Value));
            if (current is null)
            {
                current = [];
                result.Add(current);
            }
            else if (step)
            {
                current.Add(new Point(pt.X, current[^1].Y));
            }

            current.Add(pt);
        }

        return result;
    }

    private string LabelOfX(double x, bool isTime, IReadOnlyList<double> xs)
    {
        if (XFormat is { } format) return format(x);
        if (!isTime) return x.ToString("#,0.##", CultureInfo.CurrentCulture);
        var time = HelixTime.FromX(x);
        var span = xs.Count > 1 ? HelixTime.FromX(xs[^1]) - HelixTime.FromX(xs[0]) : TimeSpan.Zero;
        return time.ToString(TimeFormat ?? (span.TotalDays > 2 ? "ddd d MMM HH:mm" : "HH:mm"), CultureInfo.CurrentCulture);
    }

    private void DrawXLabels(DrawingContext dc, Rect area, double xMin, double xMax, IReadOnlyList<double> xs, bool isTime, bool hasBars, IBrush brush, Func<double, double> X)
    {
        var maxLabels = Math.Max(2, (int)(area.Width / 80));
        IEnumerable<double> ticks;
        Func<double, string> label;

        if (hasBars || (!isTime && XFormat is not null))
        {
            // Categories: label every n-th X so labels never collide.
            var every = Math.Max(1, (int)Math.Ceiling(xs.Count / (double)maxLabels));
            ticks = xs.Where((_, i) => i % every == 0);
            label = x => LabelOfX(x, isTime, xs);
        }
        else if (isTime)
        {
            var span = TimeSpan.FromMilliseconds(xMax - xMin);
            TimeSpan[] steps = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30), TimeSpan.FromHours(1),
                TimeSpan.FromHours(3), TimeSpan.FromHours(6), TimeSpan.FromHours(12), TimeSpan.FromDays(1), TimeSpan.FromDays(7), TimeSpan.FromDays(30)];
            var stepSpan = steps.FirstOrDefault(s => span.TotalMilliseconds / s.TotalMilliseconds <= maxLabels, TimeSpan.FromDays(30));
            var start = HelixTime.FromX(xMin);
            var first = new DateTime(start.Ticks / stepSpan.Ticks * stepSpan.Ticks, start.Kind);
            if (first < start) first = first.Add(stepSpan);
            var list = new List<double>();
            for (var t = first; HelixTime.ToX(t) <= xMax && list.Count < 200; t = t.Add(stepSpan)) list.Add(HelixTime.ToX(t));
            ticks = list;
            var fmt = TimeFormat ?? (stepSpan >= TimeSpan.FromDays(1) ? "dd/MM" : "HH:mm");
            label = x => HelixTime.FromX(x).ToString(fmt, CultureInfo.CurrentCulture);
        }
        else
        {
            var (min, max, step) = HelixDraw.NiceScale(xMin, xMax, maxLabels);
            var list = new List<double>();
            for (var v = min; v <= max + step / 1000; v += step) if (v >= xMin && v <= xMax) list.Add(v);
            ticks = list;
            label = x => x.ToString("#,0.##", CultureInfo.CurrentCulture);
        }

        foreach (var tick in ticks)
        {
            var text = HelixDraw.Text(this, label(tick), 11.5, brush);
            var x = Math.Clamp(X(tick) - text.Width / 2, area.Left - 4, area.Right - text.Width + 4);
            dc.DrawText(text, new Point(x, area.Bottom + 6));
        }
    }
}
