using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Helix.Wpf;

/// <summary>A drawing surface: the owning chart draws on it and listens to the mouse.</summary>
public sealed class HelixPlot : FrameworkElement
{
    internal Action<DrawingContext, Size>? Draw { get; set; }

    public HelixPlot()
    {
        SnapsToDevicePixels = true;
        ClipToBounds = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        // Transparent background so the whole area receives mouse moves.
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        Draw?.Invoke(dc, RenderSize);
    }
}

/// <summary>Text and number helpers shared by the charts.</summary>
internal static class HelixDraw
{
    public static FormattedText Text(FrameworkElement scope, string text, double size, Brush brush, FontWeight? weight = null) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(HelixTheme.Font(scope), FontStyles.Normal, weight ?? FontWeights.Normal, FontStretches.Normal),
            size, brush, VisualTreeHelper.GetDpi(scope).PixelsPerDip);

    public static Pen Pen(Brush brush, double thickness, bool dashed = false)
    {
        var pen = new Pen(brush, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        if (dashed)
        {
            pen.DashStyle = new DashStyle([3, 2.2], 0);
            pen.DashCap = PenLineCap.Flat;
        }

        pen.Freeze();
        return pen;
    }

    /// <summary>"Nice" axis ticks (1, 2, 2.5, 5 × 10^n) covering [min, max].</summary>
    public static (double Min, double Max, double Step) NiceScale(double min, double max, int targetTicks = 5)
    {
        if (double.IsNaN(min) || double.IsNaN(max))
        {
            return (0, 1, 0.2);
        }

        if (Math.Abs(max - min) < 1e-9)
        {
            var pad = Math.Abs(max) < 1e-9 ? 1 : Math.Abs(max) * 0.1;
            (min, max) = (min - pad, max + pad);
        }

        var raw = (max - min) / Math.Max(1, targetTicks);
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        var normalized = raw / magnitude;
        var step = (normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 2.5 ? 2.5 : normalized <= 5 ? 5 : 10) * magnitude;
        return (Math.Floor(min / step) * step, Math.Ceiling(max / step) * step, step);
    }

    /// <summary>Rectangle with only the top corners rounded (data end), anchored on the baseline.</summary>
    public static Geometry Bar(Rect r, double radius, bool roundTop)
    {
        radius = Math.Min(radius, Math.Min(r.Width / 2, r.Height));
        if (radius <= 0.5)
        {
            return new RectangleGeometry(r);
        }

        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            if (roundTop)
            {
                c.BeginFigure(new Point(r.Left, r.Bottom), true, true);
                c.LineTo(new Point(r.Left, r.Top + radius), true, false);
                c.ArcTo(new Point(r.Left + radius, r.Top), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
                c.LineTo(new Point(r.Right - radius, r.Top), true, false);
                c.ArcTo(new Point(r.Right, r.Top + radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
                c.LineTo(new Point(r.Right, r.Bottom), true, false);
            }
            else
            {
                c.BeginFigure(new Point(r.Left, r.Top), true, true);
                c.LineTo(new Point(r.Right, r.Top), true, false);
                c.LineTo(new Point(r.Right, r.Bottom - radius), true, false);
                c.ArcTo(new Point(r.Right - radius, r.Bottom), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
                c.LineTo(new Point(r.Left + radius, r.Bottom), true, false);
                c.ArcTo(new Point(r.Left, r.Bottom - radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
            }
        }

        g.Freeze();
        return g;
    }

    /// <summary>A tooltip card drawn in the plot (follows the pointer, stays inside the plot).</summary>
    public static void Tooltip(DrawingContext dc, FrameworkElement scope, Point anchor, Size bounds, string? title, IReadOnlyList<(Brush? Swatch, string Text, string Value)> rows)
    {
        var text = HelixTheme.Brush(scope, HelixKeys.Text, Colors.Black);
        var muted = HelixTheme.Brush(scope, HelixKeys.TextMuted, Colors.Gray);
        var surface = HelixTheme.Brush(scope, HelixKeys.Surface, Colors.White);
        var border = HelixTheme.Brush(scope, HelixKeys.Border, Colors.LightGray);

        var titleText = title is null ? null : Text(scope, title, 12, muted);
        var lines = rows.Select(r => (r.Swatch, Label: Text(scope, r.Text, 12.5, text), Value: Text(scope, r.Value, 12.5, text, FontWeights.SemiBold))).ToList();
        var labelWidth = lines.Count == 0 ? 0 : lines.Max(l => l.Label.Width);
        var valueWidth = lines.Count == 0 ? 0 : lines.Max(l => l.Value.Width);
        var swatch = lines.Any(l => l.Swatch is not null) ? 14 : 0;
        var width = Math.Max(titleText?.Width ?? 0, swatch + labelWidth + 16 + valueWidth) + 20;
        var height = (titleText is null ? 0 : 20) + lines.Count * 19 + 14;

        var x = anchor.X + 14;
        if (x + width > bounds.Width) x = anchor.X - 14 - width;
        var y = Math.Clamp(anchor.Y - height / 2, 0, Math.Max(0, bounds.Height - height));
        var box = new Rect(Math.Max(0, x), y, width, height);

        dc.PushOpacity(0.98);
        dc.DrawRoundedRectangle(surface, Pen(border, 1), box, 9, 9);
        dc.Pop();
        var top = box.Top + 8;
        if (titleText is not null)
        {
            dc.DrawText(titleText, new Point(box.Left + 10, top));
            top += 20;
        }

        foreach (var (sw, label, value) in lines)
        {
            if (sw is not null)
            {
                dc.DrawRoundedRectangle(sw, null, new Rect(box.Left + 10, top + 4, 9, 9), 2.5, 2.5);
            }

            dc.DrawText(label, new Point(box.Left + 10 + swatch, top));
            dc.DrawText(value, new Point(box.Right - 10 - value.Width, top));
            top += 19;
        }
    }
}

/// <summary>
/// Line, area, step and bar chart on one Y axis (never two). Series in fixed color order, legend toggles series,
/// hover crosshair with a tooltip, reference lines, an optional "now" line for time axes.
/// </summary>
[TemplatePart(Name = "PART_Plot", Type = typeof(HelixPlot))]
[TemplatePart(Name = "PART_Legend", Type = typeof(Panel))]
[System.Windows.Markup.ContentProperty(nameof(Series))]
public class HelixChart : Control, IVeloThemeAware
{
    static HelixChart() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixChart), new FrameworkPropertyMetadata(typeof(HelixChart)));

    public HelixChart()
    {
        SetValue(SeriesPropertyKey, new FreezableCollection<HelixChartSeries>());
        SetValue(LinesPropertyKey, new FreezableCollection<HelixChartLine>());
        Series.Changed += (_, _) => Refresh();
        Lines.Changed += (_, _) => Refresh();
        HelixTheme.Watch(this);
        Loaded += (_, _) => StartNowTimer();
        Unloaded += (_, _) => _nowTimer?.Stop();
    }

    private static readonly DependencyPropertyKey SeriesPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Series), typeof(FreezableCollection<HelixChartSeries>), typeof(HelixChart), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey LinesPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Lines), typeof(FreezableCollection<HelixChartLine>), typeof(HelixChart), new PropertyMetadata(null));
    public static readonly DependencyProperty SeriesProperty = SeriesPropertyKey.DependencyProperty;
    public static readonly DependencyProperty LinesProperty = LinesPropertyKey.DependencyProperty;

    public static readonly DependencyProperty TitleProperty = HelixProperty.Register<HelixChart, string?>(nameof(Title), null);
    public static readonly DependencyProperty SubtitleProperty = HelixProperty.Register<HelixChart, string?>(nameof(Subtitle), null);
    public static readonly DependencyProperty PlotHeightProperty = HelixProperty.Register<HelixChart, double>(nameof(PlotHeight), 260d);
    public static readonly DependencyProperty UnitProperty = HelixProperty.Register<HelixChart, string?>(nameof(Unit), null, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty YFormatProperty = HelixProperty.Register<HelixChart, string>(nameof(YFormat), "#,0.##", (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty YMinProperty = HelixProperty.Register<HelixChart, double?>(nameof(YMin), null, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty YMaxProperty = HelixProperty.Register<HelixChart, double?>(nameof(YMax), null, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty XMinProperty = HelixProperty.Register<HelixChart, double?>(nameof(XMin), null, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty XMaxProperty = HelixProperty.Register<HelixChart, double?>(nameof(XMax), null, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty FromProperty = HelixProperty.Register<HelixChart, DateTime?>(nameof(From), null, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty ToProperty = HelixProperty.Register<HelixChart, DateTime?>(nameof(To), null, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty XFormatProperty = HelixProperty.Register<HelixChart, Func<double, string>?>(nameof(XFormat), null, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty TimeFormatProperty = HelixProperty.Register<HelixChart, string?>(nameof(TimeFormat), null, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty ShowNowProperty = HelixProperty.Register<HelixChart, bool>(nameof(ShowNow), false, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty NowBrushProperty = HelixProperty.Register<HelixChart, Brush?>(nameof(NowBrush), null, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty ShowLegendProperty = HelixProperty.Register<HelixChart, bool>(nameof(ShowLegend), true, (d, _) => ((HelixChart)d).Refresh());
    public static readonly DependencyProperty EmptyTextProperty = HelixProperty.Register<HelixChart, string?>(nameof(EmptyText), null, (d, _) => ((HelixChart)d).Refresh());

    /// <summary>The series (HelixChartSeries elements in XAML, or Series.Add in code).</summary>
    public FreezableCollection<HelixChartSeries> Series => (FreezableCollection<HelixChartSeries>)GetValue(SeriesProperty);

    /// <summary>Reference lines (target, limit, event).</summary>
    public FreezableCollection<HelixChartLine> Lines => (FreezableCollection<HelixChartLine>)GetValue(LinesProperty);

    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Subtitle { get => (string?)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }

    /// <summary>Height of the drawing area (260 by default).</summary>
    public double PlotHeight { get => (double)GetValue(PlotHeightProperty); set => SetValue(PlotHeightProperty, value); }

    /// <summary>Unit shown in the tooltip, e.g. "MW".</summary>
    public string? Unit { get => (string?)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

    /// <summary>.NET format of Y values ("#,0.##" by default).</summary>
    public string YFormat { get => (string)GetValue(YFormatProperty); set => SetValue(YFormatProperty, value); }
    public double? YMin { get => (double?)GetValue(YMinProperty); set => SetValue(YMinProperty, value); }
    public double? YMax { get => (double?)GetValue(YMaxProperty); set => SetValue(YMaxProperty, value); }
    public double? XMin { get => (double?)GetValue(XMinProperty); set => SetValue(XMinProperty, value); }
    public double? XMax { get => (double?)GetValue(XMaxProperty); set => SetValue(XMaxProperty, value); }

    /// <summary>Start of a time axis (else the first point).</summary>
    public DateTime? From { get => (DateTime?)GetValue(FromProperty); set => SetValue(FromProperty, value); }

    /// <summary>End of a time axis (else the last point).</summary>
    public DateTime? To { get => (DateTime?)GetValue(ToProperty); set => SetValue(ToProperty, value); }

    /// <summary>Label of a numeric X value (categories: x => months[(int)x]).</summary>
    public Func<double, string>? XFormat { get => (Func<double, string>?)GetValue(XFormatProperty); set => SetValue(XFormatProperty, value); }

    /// <summary>Format of time labels ("HH:mm", "dd/MM"…); automatic when null.</summary>
    public string? TimeFormat { get => (string?)GetValue(TimeFormatProperty); set => SetValue(TimeFormatProperty, value); }

    /// <summary>A vertical line at the current time (time axis), refreshed every 30 s.</summary>
    public bool ShowNow { get => (bool)GetValue(ShowNowProperty); set => SetValue(ShowNowProperty, value); }
    public Brush? NowBrush { get => (Brush?)GetValue(NowBrushProperty); set => SetValue(NowBrushProperty, value); }

    /// <summary>Legend with series toggles (shown from two series).</summary>
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    public string? EmptyText { get => (string?)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    private HelixPlot? _plot;
    private Panel? _legend;
    private readonly HashSet<HelixChartSeries> _hidden = [];
    private readonly HashSet<HelixChartSeries> _seenHidden = [];
    private Point? _pointer;
    private System.Windows.Threading.DispatcherTimer? _nowTimer;

    public override void OnApplyTemplate()
    {
        if (_plot is not null)
        {
            _plot.MouseMove -= OnPlotMouseMove;
            _plot.MouseLeave -= OnPlotMouseLeave;
        }

        base.OnApplyTemplate();
        _plot = GetTemplateChild("PART_Plot") as HelixPlot;
        _legend = GetTemplateChild("PART_Legend") as Panel;
        if (_plot is not null)
        {
            _plot.Draw = DrawPlot;
            _plot.MouseMove += OnPlotMouseMove;
            _plot.MouseLeave += OnPlotMouseLeave;
        }

        Refresh();
    }

    void IVeloThemeAware.OnThemeChanged() => Refresh();

    private void StartNowTimer()
    {
        if (!ShowNow || _nowTimer is not null)
        {
            _nowTimer?.Start();
            return;
        }

        _nowTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _nowTimer.Tick += (_, _) => _plot?.InvalidateVisual();
        _nowTimer.Start();
    }

    private void OnPlotMouseMove(object sender, MouseEventArgs e)
    {
        _pointer = e.GetPosition(_plot);
        _plot?.InvalidateVisual();
    }

    private void OnPlotMouseLeave(object sender, MouseEventArgs e)
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

    private Brush ColorOf(HelixChartSeries series) =>
        HelixTheme.Resolve(this, series.Color) ?? HelixTheme.Series(this, Series.IndexOf(series) + 1);

    private void BuildLegend()
    {
        if (_legend is null)
        {
            return;
        }

        _legend.Children.Clear();
        _legend.Visibility = ShowLegend && Series.Count >= 2 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var series in Series)
        {
            var hidden = _hidden.Contains(series);
            var key = series.Type is HelixSeriesType.Bar or HelixSeriesType.Area
                ? (FrameworkElement)new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(3), Background = ColorOf(series) }
                : new Border { Width = 14, Height = 2.5, CornerRadius = new CornerRadius(1), Background = ColorOf(series) };
            key.Margin = new Thickness(0, 0, 7, 0);
            key.VerticalAlignment = VerticalAlignment.Center;
            var label = new TextBlock { Text = series.Name, FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center };
            label.SetResourceReference(TextBlock.ForegroundProperty, hidden ? HelixKeys.TextSubtle : HelixKeys.TextMuted);
            var content = new StackPanel { Orientation = Orientation.Horizontal, Opacity = hidden ? 0.5 : 1 };
            content.Children.Add(key);
            content.Children.Add(label);
            var button = new HelixButton
            {
                Content = content,
                Variant = HelixVariant.Ghost,
                Tone = HelixTone.Neutral,
                Size = HelixSize.Sm,
                Height = 26,
                Padding = new Thickness(8, 0, 8, 0),
                Margin = new Thickness(0, 0, 4, 0),
                ToolTip = series.Name,
            };
            System.Windows.Automation.AutomationProperties.SetName(button, series.Name);
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
        var muted = HelixTheme.Brush(this, HelixKeys.TextSubtle, Colors.Gray);
        var grid = HelixTheme.Brush(this, HelixKeys.Border, Colors.LightGray);
        var surface = HelixTheme.Brush(this, HelixKeys.Surface, Colors.White);

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

        // Plot area: left gutter fits the widest Y label.
        var yLabels = new List<(double Value, FormattedText Text)>();
        for (var v = yMin; v <= yMax + yStep / 1000; v += yStep)
        {
            yLabels.Add((v, HelixDraw.Text(this, FormatY(v), 11.5, muted)));
        }

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

        // X labels.
        DrawXLabels(dc, area, xMin, xMax, xs, isTime, hasBars, muted, X);

        dc.PushClip(new RectangleGeometry(new Rect(area.Left - 1, area.Top - 6, area.Width + 2, area.Height + 7)));

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
                var fill = brush.Clone();
                fill.Opacity = 0.14;
                fill.Freeze();
                var baseline = Y(Math.Clamp(0, yMin, yMax));
                foreach (var segment in segments.Where(s => s.Count > 1))
                {
                    var g = new StreamGeometry();
                    using (var c = g.Open())
                    {
                        c.BeginFigure(new Point(segment[0].X, baseline), true, true);
                        foreach (var pt in segment) c.LineTo(pt, true, true);
                        c.LineTo(new Point(segment[^1].X, baseline), true, false);
                    }

                    g.Freeze();
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
                    c.BeginFigure(segment[0], false, false);
                    for (var i = 1; i < segment.Count; i++) c.LineTo(segment[i], true, true);
                }

                g.Freeze();
                dc.DrawGeometry(null, pen, g);
            }

            if (series.ShowMarkers)
            {
                var ring = HelixDraw.Pen(surface, 2);
                foreach (var p in points[series].Where(p => p.Y is not null))
                {
                    dc.DrawEllipse(brush, ring, new Point(X(p.X), Y(p.Y!.Value)), 4.5, 4.5);
                }
            }
        }

        dc.Pop();

        // Reference lines with their label.
        foreach (var line in Lines)
        {
            var brush = HelixTheme.Resolve(this, line.Color) ?? HelixTheme.Brush(this, HelixKeys.TextMuted, Colors.Gray);
            var pen = HelixDraw.Pen(brush, 1.5, line.IsDashed);
            var label = line.Label is null ? null : HelixDraw.Text(this, line.Label, 11.5, brush, FontWeights.SemiBold);
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
                var brush = NowBrush ?? HelixTheme.Brush(this, HelixKeys.Danger, Colors.OrangeRed);
                var x = X(now);
                dc.DrawLine(HelixDraw.Pen(brush, 1.5), new Point(x, area.Top), new Point(x, area.Bottom));
                var label = HelixDraw.Text(this, DateTime.Now.ToString("HH:mm", CultureInfo.CurrentCulture), 11, Brushes.White, FontWeights.SemiBold);
                var pill = new Rect(x - label.Width / 2 - 5, area.Top - 8, label.Width + 10, label.Height + 2);
                dc.DrawRoundedRectangle(brush, null, pill, 4, 4);
                dc.DrawText(label, new Point(pill.Left + 5, pill.Top + 1));
            }
        }

        // Hover: crosshair on the nearest X and a tooltip with every visible series.
        if (_pointer is { } pointer && area.Contains(pointer) && xs.Count > 0)
        {
            var nearest = xs.OrderBy(x => Math.Abs(X(x) - pointer.X)).First();
            var hx = X(nearest);
            dc.DrawLine(HelixDraw.Pen(HelixTheme.Brush(this, HelixKeys.BorderStrong, Colors.Gray), 1), new Point(hx, area.Top), new Point(hx, area.Bottom));
            var rows = new List<(Brush?, string, string)>();
            foreach (var series in visible)
            {
                var point = points[series].FirstOrDefault(p => p.X == nearest);
                if (point.Y is not { } value) continue;
                var brush = ColorOf(series);
                rows.Add((brush, series.Name, FormatY(value) + (Unit is null ? "" : " " + Unit)));
                if (series.Type != HelixSeriesType.Bar)
                {
                    dc.DrawEllipse(brush, HelixDraw.Pen(surface, 2), new Point(hx, Y(value)), 5, 5);
                }
            }

            if (rows.Count > 0)
            {
                HelixDraw.Tooltip(dc, this, new Point(hx, pointer.Y), size, LabelOfX(nearest, isTime, xs), rows);
            }
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

    private void DrawXLabels(DrawingContext dc, Rect area, double xMin, double xMax, IReadOnlyList<double> xs, bool isTime, bool hasBars, Brush brush, Func<double, double> X)
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
