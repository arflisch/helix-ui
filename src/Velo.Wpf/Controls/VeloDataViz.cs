using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Velo.Wpf;

/// <summary>
/// Share of a whole: a donut (or a segmented bar). At most MaxParts parts (6): the rest folds into "Others".
/// Hover shows the part in the center; the legend lists values and shares.
/// </summary>
[TemplatePart(Name = "PART_Plot", Type = typeof(VeloPlot))]
[TemplatePart(Name = "PART_Legend", Type = typeof(Panel))]
public class VeloDonut : Control, IVeloThemeAware
{
    static VeloDonut() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloDonut), new FrameworkPropertyMetadata(typeof(VeloDonut)));

    public VeloDonut() => VeloTheme.Watch(this);

    public static readonly DependencyProperty ItemsProperty = VeloProperty.Register<VeloDonut, IEnumerable<VeloPart>?>(nameof(Items), null, (d, _) => ((VeloDonut)d).Refresh());
    public static readonly DependencyProperty TitleProperty = VeloProperty.Register<VeloDonut, string?>(nameof(Title), null);
    public static readonly DependencyProperty SubtitleProperty = VeloProperty.Register<VeloDonut, string?>(nameof(Subtitle), null);
    public static readonly DependencyProperty ShapeProperty = VeloProperty.Register<VeloDonut, VeloPartShape>(nameof(Shape), VeloPartShape.Donut, (d, _) => ((VeloDonut)d).Refresh());
    public static readonly DependencyProperty MaxPartsProperty = VeloProperty.Register<VeloDonut, int>(nameof(MaxParts), 6, (d, _) => ((VeloDonut)d).Refresh());
    public static readonly DependencyProperty OtherLabelProperty = VeloProperty.Register<VeloDonut, string?>(nameof(OtherLabel), null, (d, _) => ((VeloDonut)d).Refresh());
    public static readonly DependencyProperty SortProperty = VeloProperty.Register<VeloDonut, bool>(nameof(Sort), true, (d, _) => ((VeloDonut)d).Refresh());
    public static readonly DependencyProperty CenterLabelProperty = VeloProperty.Register<VeloDonut, string?>(nameof(CenterLabel), null, (d, _) => ((VeloDonut)d).Refresh());
    public static readonly DependencyProperty UnitProperty = VeloProperty.Register<VeloDonut, string?>(nameof(Unit), null, (d, _) => ((VeloDonut)d).Refresh());
    public static readonly DependencyProperty ValueFormatProperty = VeloProperty.Register<VeloDonut, string>(nameof(ValueFormat), "#,0.##", (d, _) => ((VeloDonut)d).Refresh());
    public static readonly DependencyProperty ThicknessProperty = VeloProperty.Register<VeloDonut, double>(nameof(Thickness), 22d, (d, _) => ((VeloDonut)d).Refresh());
    public static readonly DependencyProperty DonutSizeProperty = VeloProperty.Register<VeloDonut, double>(nameof(DonutSize), 180d);
    public static readonly DependencyProperty EmptyTextProperty = VeloProperty.Register<VeloDonut, string?>(nameof(EmptyText), null, (d, _) => ((VeloDonut)d).Refresh());
    public static readonly RoutedEvent ItemClickEvent = EventManager.RegisterRoutedEvent(nameof(ItemClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VeloDonut));

    public IEnumerable<VeloPart>? Items { get => (IEnumerable<VeloPart>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Subtitle { get => (string?)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public VeloPartShape Shape { get => (VeloPartShape)GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    /// <summary>Parts drawn (6 by default); smaller ones fold into "Others".</summary>
    public int MaxParts { get => (int)GetValue(MaxPartsProperty); set => SetValue(MaxPartsProperty, value); }
    public string? OtherLabel { get => (string?)GetValue(OtherLabelProperty); set => SetValue(OtherLabelProperty, value); }

    /// <summary>Largest part first.</summary>
    public bool Sort { get => (bool)GetValue(SortProperty); set => SetValue(SortProperty, value); }

    /// <summary>Text under the total in the center ("Total" by default).</summary>
    public string? CenterLabel { get => (string?)GetValue(CenterLabelProperty); set => SetValue(CenterLabelProperty, value); }
    public string? Unit { get => (string?)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string ValueFormat { get => (string)GetValue(ValueFormatProperty); set => SetValue(ValueFormatProperty, value); }

    /// <summary>Ring thickness (or bar height).</summary>
    public double Thickness { get => (double)GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    /// <summary>Diameter of the donut.</summary>
    public double DonutSize { get => (double)GetValue(DonutSizeProperty); set => SetValue(DonutSizeProperty, value); }
    public string? EmptyText { get => (string?)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    /// <summary>Raised with <see cref="VeloEventArgs{T}"/> of VeloPart.</summary>
    public event RoutedEventHandler ItemClick { add => AddHandler(ItemClickEvent, value); remove => RemoveHandler(ItemClickEvent, value); }

    private VeloPlot? _plot;
    private Panel? _legend;
    private List<(VeloPart Part, Brush Brush)> _parts = [];
    private int? _hover;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _plot = GetTemplateChild("PART_Plot") as VeloPlot;
        _legend = GetTemplateChild("PART_Legend") as Panel;
        if (_plot is not null)
        {
            _plot.Draw = Draw;
            _plot.MouseMove += (_, e) => SetHover(HitTest(e.GetPosition(_plot)));
            _plot.MouseLeave += (_, _) => SetHover(null);
            _plot.MouseLeftButtonUp += (_, e) =>
            {
                if (HitTest(e.GetPosition(_plot)) is { } i) RaiseEvent(new VeloEventArgs<VeloPart>(ItemClickEvent, _parts[i].Part));
            };
        }

        Refresh();
    }

    void IVeloThemeAware.OnThemeChanged() => Refresh();

    private void SetHover(int? index)
    {
        if (index == _hover) return;
        _hover = index;
        if (_plot is not null) _plot.Cursor = index is null ? null : Cursors.Hand;
        _plot?.InvalidateVisual();
        BuildLegend();
    }

    private double Total => _parts.Sum(p => Math.Max(0, p.Part.Value));

    private string Format(double value) => value.ToString(ValueFormat, CultureInfo.CurrentCulture) + (Unit is null ? "" : " " + Unit);

    private void Refresh()
    {
        var items = (Items ?? []).Where(p => p.Value > 0).ToList();
        if (Sort) items = items.OrderByDescending(p => p.Value).ToList();
        var max = Math.Max(2, MaxParts);
        if (items.Count > max)
        {
            var rest = items.Skip(max - 1).Sum(p => p.Value);
            items = items.Take(max - 1).Append(new VeloPart(OtherLabel ?? VeloTexts.Current.Others, rest, "subtle")).ToList();
        }

        _parts = items.Select((p, i) => (p, VeloTheme.Resolve(this, p.Color) ?? VeloTheme.Series(this, i + 1))).ToList();
        if (_plot is not null)
        {
            _plot.Width = Shape == VeloPartShape.Donut ? DonutSize : double.NaN;
            _plot.Height = Shape == VeloPartShape.Donut ? DonutSize : Thickness + 8;
            _plot.InvalidateVisual();
        }

        BuildLegend();
    }

    private void BuildLegend()
    {
        if (_legend is null) return;
        _legend.Children.Clear();
        var total = Total;
        for (var i = 0; i < _parts.Count; i++)
        {
            var (part, brush) = _parts[i];
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8), Opacity = _hover is { } h && h != i ? 0.45 : 1, Background = Brushes.Transparent };
            var index = i;
            row.MouseEnter += (_, _) => SetHover(index);
            row.MouseLeave += (_, _) => SetHover(null);
            var share = new TextBlock { Text = (part.Value / total).ToString("P0", CultureInfo.CurrentCulture), FontSize = 13, Width = 46, TextAlignment = TextAlignment.Right };
            share.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.TextMuted);
            DockPanel.SetDock(share, Dock.Right);
            var value = new TextBlock { Text = Format(part.Value), FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(16, 0, 0, 0) };
            value.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.Text);
            DockPanel.SetDock(value, Dock.Right);
            var swatch = new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(3), Background = brush, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(swatch, Dock.Left);
            var label = new TextBlock { Text = part.Label, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis };
            label.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.Text);
            row.Children.Add(share);
            row.Children.Add(value);
            row.Children.Add(swatch);
            row.Children.Add(label);
            _legend.Children.Add(row);
        }
    }

    private int? HitTest(Point p)
    {
        if (_plot is null || _parts.Count == 0) return null;
        var size = _plot.RenderSize;
        var total = Total;
        if (Shape == VeloPartShape.SegmentedBar)
        {
            var x = 0.0;
            for (var i = 0; i < _parts.Count; i++)
            {
                var w = _parts[i].Part.Value / total * size.Width;
                if (p.X >= x && p.X < x + w) return i;
                x += w;
            }

            return null;
        }

        var center = new Point(size.Width / 2, size.Height / 2);
        var radius = Math.Min(size.Width, size.Height) / 2;
        var distance = (p - center).Length;
        if (distance > radius || distance < radius - Thickness - 4) return null;
        var angle = (Math.Atan2(p.Y - center.Y, p.X - center.X) * 180 / Math.PI + 90 + 360) % 360;
        var start = 0.0;
        for (var i = 0; i < _parts.Count; i++)
        {
            var sweep = _parts[i].Part.Value / total * 360;
            if (angle >= start && angle < start + sweep) return i;
            start += sweep;
        }

        return null;
    }

    private void Draw(DrawingContext dc, Size size)
    {
        var text = VeloTheme.Brush(this, VeloKeys.Text, Colors.Black);
        var muted = VeloTheme.Brush(this, VeloKeys.TextMuted, Colors.Gray);
        var total = Total;
        if (_parts.Count == 0 || total <= 0)
        {
            var empty = VeloDraw.Text(this, EmptyText ?? VeloTexts.Current.NoData, 13, muted);
            dc.DrawText(empty, new Point((size.Width - empty.Width) / 2, (size.Height - empty.Height) / 2));
            return;
        }

        if (Shape == VeloPartShape.SegmentedBar)
        {
            var x = 0.0;
            for (var i = 0; i < _parts.Count; i++)
            {
                var w = _parts[i].Part.Value / total * size.Width;
                var gap = i < _parts.Count - 1 ? 2 : 0; // 2px surface gap between fills
                dc.PushOpacity(_hover is { } h && h != i ? 0.4 : 1);
                dc.DrawRoundedRectangle(_parts[i].Brush, null, new Rect(x, 4, Math.Max(1, w - gap), Thickness), 4, 4);
                dc.Pop();
                x += w;
            }

            return;
        }

        var center = new Point(size.Width / 2, size.Height / 2);
        var radius = Math.Min(size.Width, size.Height) / 2 - Thickness / 2;
        var gapDegrees = _parts.Count > 1 ? 2 / radius * 180 / Math.PI : 0; // ≈ 2px gap
        var start = 0.0;
        for (var i = 0; i < _parts.Count; i++)
        {
            var sweep = _parts[i].Part.Value / total * 360;
            var pen = new Pen(_parts[i].Brush, _hover == i ? Thickness + 4 : Thickness) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
            dc.PushOpacity(_hover is { } h && h != i ? 0.4 : 1);
            dc.DrawGeometry(null, pen, Arc(center, radius, start + gapDegrees / 2, Math.Max(0.5, sweep - gapDegrees)));
            dc.Pop();
            start += sweep;
        }

        // Center: the hovered part, else the total.
        var (big, small) = _hover is { } hover
            ? (Format(_parts[hover].Part.Value), $"{_parts[hover].Part.Label} · {(_parts[hover].Part.Value / total).ToString("P0", CultureInfo.CurrentCulture)}")
            : (Format(total), CenterLabel ?? VeloTexts.Current.Total);
        var bigText = VeloDraw.Text(this, big, Math.Max(14, radius / 3.6), text, FontWeights.SemiBold);
        var smallText = VeloDraw.Text(this, small, 12, muted);
        smallText.MaxTextWidth = Math.Max(20, radius * 1.5);
        smallText.TextAlignment = TextAlignment.Center;
        dc.DrawText(bigText, new Point(center.X - bigText.Width / 2, center.Y - bigText.Height / 2 - 7));
        dc.DrawText(smallText, new Point(center.X - smallText.MaxTextWidth / 2, center.Y + bigText.Height / 2 - 6));
    }

    /// <summary>Arc from startDeg (0 = top, clockwise) over sweepDeg.</summary>
    internal static Geometry Arc(Point center, double radius, double startDeg, double sweepDeg)
    {
        sweepDeg = Math.Min(sweepDeg, 359.99);
        Point At(double deg)
        {
            var rad = (deg - 90) * Math.PI / 180;
            return new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
        }

        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            c.BeginFigure(At(startDeg), false, false);
            c.ArcTo(At(startDeg + sweepDeg), new Size(radius, radius), 0, sweepDeg > 180, SweepDirection.Clockwise, true, false);
        }

        g.Freeze();
        return g;
    }
}

/// <summary>A dial for one value between Min and Max. Thresholds color the arc and name the zone (icon-free text, not color alone).</summary>
public class VeloGauge : FrameworkElement, IVeloThemeAware
{
    public VeloGauge() => VeloTheme.Watch(this);

    public static readonly DependencyProperty ValueProperty = VeloProperty.Register<VeloGauge, double>(nameof(Value), 0d);
    public static readonly DependencyProperty MinimumProperty = VeloProperty.Register<VeloGauge, double>(nameof(Minimum), 0d);
    public static readonly DependencyProperty MaximumProperty = VeloProperty.Register<VeloGauge, double>(nameof(Maximum), 100d);
    public static readonly DependencyProperty LabelProperty = VeloProperty.Register<VeloGauge, string?>(nameof(Label), null);
    public static readonly DependencyProperty UnitProperty = VeloProperty.Register<VeloGauge, string?>(nameof(Unit), null);
    public static readonly DependencyProperty FormatProperty = VeloProperty.Register<VeloGauge, string>(nameof(Format), "0.#");
    public static readonly DependencyProperty ThresholdsProperty = VeloProperty.Register<VeloGauge, IEnumerable<VeloGaugeThreshold>?>(nameof(Thresholds), null);
    public static readonly DependencyProperty AccentProperty = VeloProperty.Register<VeloGauge, Brush?>(nameof(Accent), null);
    public static readonly DependencyProperty SizeProperty = VeloProperty.Register<VeloGauge, double>(nameof(Size), 180d);

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Unit { get => (string?)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>From each Value on, the arc takes this Color and the zone this Label.</summary>
    public IEnumerable<VeloGaugeThreshold>? Thresholds { get => (IEnumerable<VeloGaugeThreshold>?)GetValue(ThresholdsProperty); set => SetValue(ThresholdsProperty, value); }

    /// <summary>Arc color without thresholds (primary by default).</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    void IVeloThemeAware.OnThemeChanged() => InvalidateVisual();

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size * 0.86);

    protected override void OnRender(DrawingContext dc)
    {
        var center = new Point(Size / 2, Size / 2);
        var thickness = Math.Max(8, Size / 13);
        var radius = Size / 2 - thickness / 2 - 2;
        var range = Math.Max(1e-9, Maximum - Minimum);
        var t = Math.Clamp((Value - Minimum) / range, 0, 1);
        var zone = (Thresholds ?? []).Where(th => Value >= th.Value).OrderBy(th => th.Value).LastOrDefault();
        var color = VeloTheme.Resolve(this, zone?.Color) ?? Accent ?? VeloTheme.Brush(this, VeloKeys.Primary, Colors.SteelBlue);
        var track = VeloTheme.Brush(this, VeloKeys.Surface3, Colors.LightGray);
        var text = VeloTheme.Brush(this, VeloKeys.Text, Colors.Black);
        var muted = VeloTheme.Brush(this, VeloKeys.TextMuted, Colors.Gray);

        const double start = -120, sweep = 240;
        dc.DrawGeometry(null, VeloDraw.Pen(track, thickness), VeloDonut.Arc(center, radius, start, sweep));
        if (t > 0)
        {
            dc.DrawGeometry(null, VeloDraw.Pen(color, thickness), VeloDonut.Arc(center, radius, start, Math.Max(0.5, sweep * t)));
        }

        // Threshold ticks outside the arc.
        foreach (var th in Thresholds ?? [])
        {
            var deg = start + sweep * Math.Clamp((th.Value - Minimum) / range, 0, 1);
            var rad = (deg - 90) * Math.PI / 180;
            var outer = radius + thickness / 2 + 1;
            var a = new Point(center.X + outer * Math.Cos(rad), center.Y + outer * Math.Sin(rad));
            var b = new Point(center.X + (outer - thickness - 2) * Math.Cos(rad), center.Y + (outer - thickness - 2) * Math.Sin(rad));
            dc.DrawLine(VeloDraw.Pen(VeloTheme.Brush(this, VeloKeys.Surface, Colors.White), 2), a, b);
        }

        var value = VeloDraw.Text(this, Value.ToString(Format, CultureInfo.CurrentCulture), Size / 6, text, FontWeights.SemiBold);
        dc.DrawText(value, new Point(center.X - value.Width / 2, center.Y - value.Height / 2 - 4));
        var below = string.Join(" · ", new[] { Unit, zone?.Label ?? Label }.Where(s => !string.IsNullOrEmpty(s)));
        if (below.Length > 0)
        {
            var small = VeloDraw.Text(this, below, 12.5, zone is null ? muted : color, zone is null ? FontWeights.Normal : FontWeights.SemiBold);
            dc.DrawText(small, new Point(center.X - small.Width / 2, center.Y + value.Height / 2 - 4));
        }

        var min = VeloDraw.Text(this, Minimum.ToString(Format, CultureInfo.CurrentCulture), 11, muted);
        var max = VeloDraw.Text(this, Maximum.ToString(Format, CultureInfo.CurrentCulture), 11, muted);
        var y = center.Y + radius * 0.5 + thickness / 2 + 4;
        dc.DrawText(min, new Point(center.X - radius * 0.87 - min.Width / 2, y));
        dc.DrawText(max, new Point(center.X + radius * 0.87 - max.Width / 2, y));
    }
}

/// <summary>
/// A grid of colored cells: sequential (one hue, light → dark) or diverging around a midpoint (two hues, neutral middle).
/// Hover shows the exact value; ShowValues writes it in the cells.
/// </summary>
[TemplatePart(Name = "PART_Plot", Type = typeof(VeloPlot))]
public class VeloHeatmap : Control, IVeloThemeAware
{
    static VeloHeatmap() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloHeatmap), new FrameworkPropertyMetadata(typeof(VeloHeatmap)));

    public VeloHeatmap() => VeloTheme.Watch(this);

    public static readonly DependencyProperty CellsProperty = VeloProperty.Register<VeloHeatmap, IEnumerable<VeloHeatCell>?>(nameof(Cells), null, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty RowsProperty = VeloProperty.Register<VeloHeatmap, IList<string>?>(nameof(Rows), null, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty ColumnsProperty = VeloProperty.Register<VeloHeatmap, IList<string>?>(nameof(Columns), null, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty TitleProperty = VeloProperty.Register<VeloHeatmap, string?>(nameof(Title), null);
    public static readonly DependencyProperty UnitProperty = VeloProperty.Register<VeloHeatmap, string?>(nameof(Unit), null, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty FormatProperty = VeloProperty.Register<VeloHeatmap, string>(nameof(Format), "0.#", (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty IsDivergingProperty = VeloProperty.Register<VeloHeatmap, bool>(nameof(IsDiverging), false, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty MidpointProperty = VeloProperty.Register<VeloHeatmap, double>(nameof(Midpoint), 0d, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty MinimumProperty = VeloProperty.Register<VeloHeatmap, double?>(nameof(Minimum), null, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty MaximumProperty = VeloProperty.Register<VeloHeatmap, double?>(nameof(Maximum), null, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty ShowValuesProperty = VeloProperty.Register<VeloHeatmap, bool>(nameof(ShowValues), false, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty ShowLegendProperty = VeloProperty.Register<VeloHeatmap, bool>(nameof(ShowLegend), true, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty ColumnLabelEveryProperty = VeloProperty.Register<VeloHeatmap, int>(nameof(ColumnLabelEvery), 1, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly DependencyProperty CellHeightProperty = VeloProperty.Register<VeloHeatmap, double>(nameof(CellHeight), 28d, (d, _) => ((VeloHeatmap)d).Refresh());
    public static readonly RoutedEvent CellClickEvent = EventManager.RegisterRoutedEvent(nameof(CellClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VeloHeatmap));

    public IEnumerable<VeloHeatCell>? Cells { get => (IEnumerable<VeloHeatCell>?)GetValue(CellsProperty); set => SetValue(CellsProperty, value); }

    /// <summary>Row order (else the order of first appearance).</summary>
    public IList<string>? Rows { get => (IList<string>?)GetValue(RowsProperty); set => SetValue(RowsProperty, value); }

    /// <summary>Column order (else the order of first appearance).</summary>
    public IList<string>? Columns { get => (IList<string>?)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Unit { get => (string?)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>Two hues around Midpoint (gains/losses, above/below target).</summary>
    public bool IsDiverging { get => (bool)GetValue(IsDivergingProperty); set => SetValue(IsDivergingProperty, value); }
    public double Midpoint { get => (double)GetValue(MidpointProperty); set => SetValue(MidpointProperty, value); }
    public double? Minimum { get => (double?)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double? Maximum { get => (double?)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public bool ShowValues { get => (bool)GetValue(ShowValuesProperty); set => SetValue(ShowValuesProperty, value); }
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }

    /// <summary>Label every n-th column (e.g. 3 for hours of the day).</summary>
    public int ColumnLabelEvery { get => (int)GetValue(ColumnLabelEveryProperty); set => SetValue(ColumnLabelEveryProperty, value); }
    public double CellHeight { get => (double)GetValue(CellHeightProperty); set => SetValue(CellHeightProperty, value); }

    /// <summary>Raised with <see cref="VeloEventArgs{T}"/> of VeloHeatCell.</summary>
    public event RoutedEventHandler CellClick { add => AddHandler(CellClickEvent, value); remove => RemoveHandler(CellClickEvent, value); }

    private VeloPlot? _plot;
    private List<string> _rows = [], _cols = [];
    private Dictionary<(string, string), VeloHeatCell> _map = [];
    private double _min, _max;
    private Point? _pointer;
    private Rect _grid;
    private const double TopLabels = 22, LegendHeight = 34;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _plot = GetTemplateChild("PART_Plot") as VeloPlot;
        if (_plot is not null)
        {
            _plot.Draw = Draw;
            _plot.MouseMove += (_, e) => { _pointer = e.GetPosition(_plot); _plot.InvalidateVisual(); };
            _plot.MouseLeave += (_, _) => { _pointer = null; _plot.InvalidateVisual(); };
            _plot.MouseLeftButtonUp += (_, e) =>
            {
                if (CellAt(e.GetPosition(_plot)) is { } cell) RaiseEvent(new VeloEventArgs<VeloHeatCell>(CellClickEvent, cell));
            };
        }

        Refresh();
    }

    void IVeloThemeAware.OnThemeChanged() => _plot?.InvalidateVisual();

    private void Refresh()
    {
        var cells = (Cells ?? []).ToList();
        _rows = Rows?.ToList() ?? cells.Select(c => c.Row).Distinct().ToList();
        _cols = Columns?.ToList() ?? cells.Select(c => c.Column).Distinct().ToList();
        _map = cells.GroupBy(c => (c.Row, c.Column)).ToDictionary(g => g.Key, g => g.Last());
        var values = cells.Where(c => c.Value is not null).Select(c => c.Value!.Value).ToList();
        _min = Minimum ?? (values.Count == 0 ? 0 : values.Min());
        _max = Maximum ?? (values.Count == 0 ? 1 : values.Max());
        if (IsDiverging)
        {
            var spread = Math.Max(Math.Abs(_max - Midpoint), Math.Abs(Midpoint - _min));
            (_min, _max) = (Midpoint - spread, Midpoint + spread); // symmetric, so equal distances look equal
        }

        if (_plot is not null)
        {
            _plot.Height = TopLabels + _rows.Count * CellHeight + (ShowLegend ? LegendHeight : 0) + 4;
            _plot.InvalidateVisual();
        }
    }

    private Color ColorOf(double value)
    {
        var t = _max - _min < 1e-9 ? 1 : (value - _min) / (_max - _min);
        if (!IsDiverging)
        {
            return VeloTheme.Mix(HeatColor(VeloKeys.HeatLow), HeatColor(VeloKeys.HeatHigh), t);
        }

        return t < 0.5
            ? VeloTheme.Mix(HeatColor(VeloKeys.HeatNegative), HeatColor(VeloKeys.HeatMid), t * 2)
            : VeloTheme.Mix(HeatColor(VeloKeys.HeatMid), HeatColor(VeloKeys.HeatPositive), (t - 0.5) * 2);
    }

    private Color HeatColor(string key) => VeloTheme.ColorOf(TryFindResource(key) as Brush, Colors.SteelBlue);

    private string FormatValue(double? value) => value is { } v ? v.ToString(Format, CultureInfo.CurrentCulture) + (Unit is null ? "" : " " + Unit) : "—";

    private VeloHeatCell? CellAt(Point p)
    {
        if (!_grid.Contains(p) || _rows.Count == 0 || _cols.Count == 0) return null;
        var c = (int)((p.X - _grid.Left) / (_grid.Width / _cols.Count));
        var r = (int)((p.Y - _grid.Top) / CellHeight);
        if (r < 0 || r >= _rows.Count || c < 0 || c >= _cols.Count) return null;
        return _map.TryGetValue((_rows[r], _cols[c]), out var cell) ? cell : new VeloHeatCell(_rows[r], _cols[c], null);
    }

    private void Draw(DrawingContext dc, Size size)
    {
        var muted = VeloTheme.Brush(this, VeloKeys.TextMuted, Colors.Gray);
        var subtle = VeloTheme.Brush(this, VeloKeys.TextSubtle, Colors.Gray);
        if (_rows.Count == 0 || _cols.Count == 0)
        {
            var empty = VeloDraw.Text(this, VeloTexts.Current.NoData, 13, muted);
            dc.DrawText(empty, new Point((size.Width - empty.Width) / 2, 10));
            return;
        }

        var rowLabels = _rows.Select(r => VeloDraw.Text(this, r, 12, muted)).ToList();
        var left = rowLabels.Max(l => l.Width) + 10;
        _grid = new Rect(left, TopLabels, Math.Max(10, size.Width - left), _rows.Count * CellHeight);
        var cellWidth = _grid.Width / _cols.Count;
        var every = Math.Max(1, ColumnLabelEvery);
        var empty2 = VeloTheme.Brush(this, VeloKeys.Surface2, Colors.WhiteSmoke);

        for (var c = 0; c < _cols.Count; c += every)
        {
            var label = VeloDraw.Text(this, _cols[c], 11.5, subtle);
            dc.DrawText(label, new Point(left + c * cellWidth + (cellWidth - label.Width) / 2, 2));
        }

        for (var r = 0; r < _rows.Count; r++)
        {
            dc.DrawText(rowLabels[r], new Point(left - 10 - rowLabels[r].Width, _grid.Top + r * CellHeight + (CellHeight - rowLabels[r].Height) / 2));
            for (var c = 0; c < _cols.Count; c++)
            {
                var rect = new Rect(left + c * cellWidth + 1, _grid.Top + r * CellHeight + 1, Math.Max(1, cellWidth - 2), CellHeight - 2); // 2px gaps
                var value = _map.TryGetValue((_rows[r], _cols[c]), out var cell) ? cell.Value : null;
                if (value is { } v)
                {
                    var color = ColorOf(v);
                    dc.DrawRoundedRectangle(VeloTheme.Frozen(color), null, rect, 4, 4);
                    if (ShowValues && cellWidth > 26)
                    {
                        var ink = VeloAccent.IsLight(color) ? VeloTheme.Frozen(Color.FromRgb(0x18, 0x18, 0x1b)) : Brushes.White;
                        var t = VeloDraw.Text(this, v.ToString(Format, CultureInfo.CurrentCulture), 11, ink);
                        dc.DrawText(t, new Point(rect.Left + (rect.Width - t.Width) / 2, rect.Top + (rect.Height - t.Height) / 2));
                    }
                }
                else
                {
                    dc.DrawRoundedRectangle(empty2, null, rect, 4, 4);
                }
            }
        }

        if (ShowLegend)
        {
            var top = _grid.Bottom + 12;
            var width = Math.Min(220, _grid.Width);
            var gradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
            for (var i = 0; i <= 10; i++)
            {
                gradient.GradientStops.Add(new GradientStop(ColorOf(_min + (_max - _min) * i / 10), i / 10.0));
            }

            gradient.Freeze();
            var min = VeloDraw.Text(this, FormatValue(_min), 11, subtle);
            var max = VeloDraw.Text(this, FormatValue(_max), 11, subtle);
            dc.DrawText(min, new Point(left, top - 1));
            var bar = new Rect(left + min.Width + 8, top + 3, width, 8);
            dc.DrawRoundedRectangle(gradient, null, bar, 4, 4);
            dc.DrawText(max, new Point(bar.Right + 8, top - 1));
        }

        if (_pointer is { } p && CellAt(p) is { } hovered)
        {
            var r = _rows.IndexOf(hovered.Row);
            var c = _cols.IndexOf(hovered.Column);
            var rect = new Rect(left + c * cellWidth + 1, _grid.Top + r * CellHeight + 1, Math.Max(1, cellWidth - 2), CellHeight - 2);
            dc.DrawRoundedRectangle(null, VeloDraw.Pen(VeloTheme.Brush(this, VeloKeys.Text, Colors.Black), 2), rect, 4, 4);
            VeloDraw.Tooltip(dc, this, new Point(rect.Right, rect.Top + rect.Height / 2), size, $"{hovered.Row} · {hovered.Column}",
                [(hovered.Value is { } hv ? VeloTheme.Frozen(ColorOf(hv)) : null, Title ?? "", FormatValue(hovered.Value))]);
        }
    }
}

/// <summary>A ranked list with proportional bars behind the labels (top pages, sources, countries…).</summary>
[TemplatePart(Name = "PART_Rows", Type = typeof(Panel))]
public class VeloBarList : Control, IVeloThemeAware
{
    static VeloBarList() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloBarList), new FrameworkPropertyMetadata(typeof(VeloBarList)));

    public VeloBarList()
    {
        VeloAccent.Init(this);
        VeloTheme.Watch(this);
    }

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloBarList));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloBarList));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloBarList));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloBarList));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloBarList));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloBarList));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty ItemsProperty = VeloProperty.Register<VeloBarList, IEnumerable<VeloBarItem>?>(nameof(Items), null, (d, _) => ((VeloBarList)d).Build());
    public static readonly DependencyProperty SortProperty = VeloProperty.Register<VeloBarList, bool>(nameof(Sort), true, (d, _) => ((VeloBarList)d).Build());
    public static readonly DependencyProperty LimitProperty = VeloProperty.Register<VeloBarList, int?>(nameof(Limit), null, (d, _) => ((VeloBarList)d).Build());
    public static readonly DependencyProperty MaxValueProperty = VeloProperty.Register<VeloBarList, double?>(nameof(MaxValue), null, (d, _) => ((VeloBarList)d).Build());
    public static readonly DependencyProperty ValueFormatProperty = VeloProperty.Register<VeloBarList, string>(nameof(ValueFormat), "#,0.##", (d, _) => ((VeloBarList)d).Build());
    public static readonly DependencyProperty UnitProperty = VeloProperty.Register<VeloBarList, string?>(nameof(Unit), null, (d, _) => ((VeloBarList)d).Build());
    public static readonly DependencyProperty LabelHeaderProperty = VeloProperty.Register<VeloBarList, string?>(nameof(LabelHeader), null, (d, _) => ((VeloBarList)d).Build());
    public static readonly DependencyProperty ValueHeaderProperty = VeloProperty.Register<VeloBarList, string?>(nameof(ValueHeader), null, (d, _) => ((VeloBarList)d).Build());
    public static readonly RoutedEvent ItemClickEvent = EventManager.RegisterRoutedEvent(nameof(ItemClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VeloBarList));

    public IEnumerable<VeloBarItem>? Items { get => (IEnumerable<VeloBarItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public bool Sort { get => (bool)GetValue(SortProperty); set => SetValue(SortProperty, value); }

    /// <summary>Rows shown before "Show all".</summary>
    public int? Limit { get => (int?)GetValue(LimitProperty); set => SetValue(LimitProperty, value); }

    /// <summary>Value of a full bar (else the largest value).</summary>
    public double? MaxValue { get => (double?)GetValue(MaxValueProperty); set => SetValue(MaxValueProperty, value); }
    public string ValueFormat { get => (string)GetValue(ValueFormatProperty); set => SetValue(ValueFormatProperty, value); }
    public string? Unit { get => (string?)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string? LabelHeader { get => (string?)GetValue(LabelHeaderProperty); set => SetValue(LabelHeaderProperty, value); }
    public string? ValueHeader { get => (string?)GetValue(ValueHeaderProperty); set => SetValue(ValueHeaderProperty, value); }

    /// <summary>Raised with <see cref="VeloEventArgs{T}"/> of VeloBarItem; rows become clickable.</summary>
    public event RoutedEventHandler ItemClick
    {
        add { AddHandler(ItemClickEvent, value); _clickable = true; Build(); }
        remove => RemoveHandler(ItemClickEvent, value);
    }

    private Panel? _rows;
    private bool _expanded, _clickable;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _rows = GetTemplateChild("PART_Rows") as Panel;
        Build();
    }

    void IVeloThemeAware.OnThemeChanged() => Build();

    private void Build()
    {
        if (_rows is null) return;
        _rows.Children.Clear();
        var items = (Items ?? []).ToList();
        if (Sort) items = items.OrderByDescending(i => i.Value).ToList();
        var max = MaxValue ?? (items.Count == 0 ? 1 : Math.Max(1e-9, items.Max(i => i.Value)));
        var shown = !_expanded && Limit is { } limit ? items.Take(limit).ToList() : items;

        if (LabelHeader is not null || ValueHeader is not null)
        {
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            var vh = new TextBlock { Text = ValueHeader ?? "", FontSize = 12, FontWeight = FontWeights.SemiBold };
            vh.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.TextSubtle);
            DockPanel.SetDock(vh, Dock.Right);
            var lh = new TextBlock { Text = LabelHeader ?? "", FontSize = 12, FontWeight = FontWeights.SemiBold };
            lh.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.TextSubtle);
            header.Children.Add(vh);
            header.Children.Add(lh);
            _rows.Children.Add(header);
        }

        foreach (var item in shown)
        {
            var share = Math.Clamp(item.Value / max, 0, 1);
            var row = new Grid { Margin = new Thickness(0, 0, 0, 4), MinHeight = 32, Background = Brushes.Transparent };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var track = new Grid();
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(share, GridUnitType.Star) });
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - share, GridUnitType.Star) });
            var bar = new Border { CornerRadius = new CornerRadius(6), Background = Accent, Opacity = 0.16 };
            track.Children.Add(bar);
            row.Children.Add(track);

            var label = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            if (item.Icon is { } icon)
            {
                var ic = new VeloIcon { Icon = icon, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
                ic.SetResourceReference(VeloIcon.ForegroundProperty, VeloKeys.TextMuted);
                label.Children.Add(ic);
            }

            var text = new TextBlock { Text = item.Label, FontSize = 13.5, TextTrimming = TextTrimming.CharacterEllipsis };
            text.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.Text);
            label.Children.Add(text);
            row.Children.Add(label);

            var value = new TextBlock { Text = item.Value.ToString(ValueFormat, CultureInfo.CurrentCulture) + (Unit is null ? "" : " " + Unit), FontSize = 13.5, FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            value.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.Text);
            Grid.SetColumn(value, 1);
            row.Children.Add(value);

            if (_clickable)
            {
                row.Cursor = Cursors.Hand;
                row.MouseEnter += (_, _) => bar.Opacity = 0.26;
                row.MouseLeave += (_, _) => bar.Opacity = 0.16;
                var it = item;
                row.MouseLeftButtonUp += (_, _) => RaiseEvent(new VeloEventArgs<VeloBarItem>(ItemClickEvent, it));
            }

            _rows.Children.Add(row);
        }

        if (Limit is { } l && items.Count > l)
        {
            var more = new VeloButton
            {
                Content = _expanded ? VeloTexts.Current.ShowLess : VeloTexts.Format(VeloTexts.Current.ShowAll, items.Count),
                IconEnd = _expanded ? "chevron-down" : "chevron-down",
                Variant = VeloVariant.Ghost,
                Tone = VeloTone.Neutral,
                Size = VeloSize.Sm,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 4, 0, 0),
            };
            more.Click += (_, _) => { _expanded = !_expanded; Build(); };
            _rows.Children.Add(more);
        }
    }
}
