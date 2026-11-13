using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Helix.Avalonia;

/// <summary>
/// Share of a whole: a donut (or a segmented bar). At most MaxParts parts (6): the rest folds into "Others".
/// Hover shows the part in the center; the legend lists values and shares.
/// </summary>
[TemplatePart("PART_Plot", typeof(HelixPlot))]
[TemplatePart("PART_Legend", typeof(Panel))]
public class HelixDonut : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable<HelixPart>?> ItemsProperty = AvaloniaProperty.Register<HelixDonut, IEnumerable<HelixPart>?>(nameof(Items));
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixDonut, string?>(nameof(Title));
    public static readonly StyledProperty<string?> SubtitleProperty = AvaloniaProperty.Register<HelixDonut, string?>(nameof(Subtitle));
    public static readonly StyledProperty<HelixPartShape> ShapeProperty = AvaloniaProperty.Register<HelixDonut, HelixPartShape>(nameof(Shape));
    public static readonly StyledProperty<int> MaxPartsProperty = AvaloniaProperty.Register<HelixDonut, int>(nameof(MaxParts), 6);
    public static readonly StyledProperty<string?> OtherLabelProperty = AvaloniaProperty.Register<HelixDonut, string?>(nameof(OtherLabel));
    public static readonly StyledProperty<bool> SortProperty = AvaloniaProperty.Register<HelixDonut, bool>(nameof(Sort), true);
    public static readonly StyledProperty<string?> CenterLabelProperty = AvaloniaProperty.Register<HelixDonut, string?>(nameof(CenterLabel));
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<HelixDonut, string?>(nameof(Unit));
    public static readonly StyledProperty<string> ValueFormatProperty = AvaloniaProperty.Register<HelixDonut, string>(nameof(ValueFormat), "#,0.##");
    public static readonly StyledProperty<double> ThicknessProperty = AvaloniaProperty.Register<HelixDonut, double>(nameof(Thickness), 22);
    public static readonly StyledProperty<double> DonutSizeProperty = AvaloniaProperty.Register<HelixDonut, double>(nameof(DonutSize), 180);
    public static readonly StyledProperty<string?> EmptyTextProperty = AvaloniaProperty.Register<HelixDonut, string?>(nameof(EmptyText));
    public static readonly RoutedEvent<HelixEventArgs<HelixPart>> ItemClickEvent =
        RoutedEvent.Register<HelixDonut, HelixEventArgs<HelixPart>>(nameof(ItemClick), RoutingStrategies.Bubble);

    public IEnumerable<HelixPart>? Items { get => GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Subtitle { get => GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public HelixPartShape Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    /// <summary>Parts drawn (6 by default); smaller ones fold into "Others".</summary>
    public int MaxParts { get => GetValue(MaxPartsProperty); set => SetValue(MaxPartsProperty, value); }
    public string? OtherLabel { get => GetValue(OtherLabelProperty); set => SetValue(OtherLabelProperty, value); }

    /// <summary>Largest part first.</summary>
    public bool Sort { get => GetValue(SortProperty); set => SetValue(SortProperty, value); }

    /// <summary>Text under the total in the center ("Total" by default).</summary>
    public string? CenterLabel { get => GetValue(CenterLabelProperty); set => SetValue(CenterLabelProperty, value); }
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string ValueFormat { get => GetValue(ValueFormatProperty); set => SetValue(ValueFormatProperty, value); }

    /// <summary>Ring thickness (or bar height).</summary>
    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    /// <summary>Diameter of the donut.</summary>
    public double DonutSize { get => GetValue(DonutSizeProperty); set => SetValue(DonutSizeProperty, value); }
    public string? EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    public event EventHandler<HelixEventArgs<HelixPart>>? ItemClick { add => AddHandler(ItemClickEvent, value); remove => RemoveHandler(ItemClickEvent, value); }

    private HelixPlot? _plot;
    private Panel? _legend;
    private List<(HelixPart Part, IBrush Brush)> _parts = [];
    private int? _hover;

    public HelixDonut()
    {
        ActualThemeVariantChanged += (_, _) => Refresh();
        SizeChanged += (_, _) =>
        {
            // The segmented bar is as wide as the control (the donut has its own size).
            if (_plot is not null && Shape == HelixPartShape.SegmentedBar) _plot.Width = Math.Max(0, Bounds.Width);
        };
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _plot = e.NameScope.Find<HelixPlot>("PART_Plot");
        _legend = e.NameScope.Find<Panel>("PART_Legend");
        if (_plot is not null)
        {
            _plot.Draw = Draw;
            _plot.PointerMoved += (_, a) => SetHover(HitTest(a.GetPosition(_plot)));
            _plot.PointerExited += (_, _) => SetHover(null);
            _plot.PointerReleased += (_, a) =>
            {
                if (HitTest(a.GetPosition(_plot)) is { } i) RaiseEvent(new HelixEventArgs<HelixPart>(ItemClickEvent, _parts[i].Part));
            };
        }

        Refresh();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property.OwnerType == typeof(HelixDonut) && change.Property != TitleProperty && change.Property != SubtitleProperty) Refresh();
    }

    private void SetHover(int? index)
    {
        if (index == _hover) return;
        _hover = index;
        if (_plot is not null) _plot.Cursor = index is null ? null : new Cursor(StandardCursorType.Hand);
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
            items = items.Take(max - 1).Append(new HelixPart(OtherLabel ?? HelixTexts.Current.Others, rest, "subtle")).ToList();
        }

        _parts = items.Select((p, i) => (p, HelixDraw.Resolve(this, p.Color) ?? HelixDraw.Series(this, i + 1))).ToList();
        if (_plot is not null)
        {
            _plot.Width = Shape == HelixPartShape.Donut ? DonutSize : Math.Max(0, Bounds.Width);
            _plot.Height = Shape == HelixPartShape.Donut ? DonutSize : Thickness + 8;
            _plot.InvalidateVisual();
        }

        BuildLegend();
    }

    private TextBlock Label(string text, string key, double size = 13, FontWeight weight = FontWeight.Normal)
    {
        var block = new TextBlock { Text = text, FontSize = size, FontWeight = weight, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        block.Bind(TextBlock.ForegroundProperty, block.GetResourceObservable(key));
        return block;
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
            row.PointerEntered += (_, _) => SetHover(index);
            row.PointerExited += (_, _) => SetHover(null);
            var share = Label((part.Value / total).ToString("P0", CultureInfo.CurrentCulture), HelixKeys.TextMuted);
            share.Width = 46;
            share.TextAlignment = TextAlignment.Right;
            DockPanel.SetDock(share, Dock.Right);
            var value = Label(Format(part.Value), HelixKeys.Text, weight: FontWeight.SemiBold);
            value.Margin = new Thickness(16, 0, 0, 0);
            DockPanel.SetDock(value, Dock.Right);
            var swatch = new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(3), Background = brush, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(swatch, Dock.Left);
            row.Children.Add(share);
            row.Children.Add(value);
            row.Children.Add(swatch);
            row.Children.Add(Label(part.Label, HelixKeys.Text));
            _legend.Children.Add(row);
        }
    }

    private int? HitTest(Point p)
    {
        if (_plot is null || _parts.Count == 0) return null;
        var size = _plot.Bounds.Size;
        var total = Total;
        if (Shape == HelixPartShape.SegmentedBar)
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
        var distance = Math.Sqrt(Math.Pow(p.X - center.X, 2) + Math.Pow(p.Y - center.Y, 2));
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
        var text = HelixDraw.Brush(this, HelixKeys.Text, Colors.Black);
        var muted = HelixDraw.Brush(this, HelixKeys.TextMuted, Colors.Gray);
        var total = Total;
        if (_parts.Count == 0 || total <= 0)
        {
            var empty = HelixDraw.Text(this, EmptyText ?? HelixTexts.Current.NoData, 13, muted);
            dc.DrawText(empty, new Point((size.Width - empty.Width) / 2, (size.Height - empty.Height) / 2));
            return;
        }

        if (Shape == HelixPartShape.SegmentedBar)
        {
            var x = 0.0;
            for (var i = 0; i < _parts.Count; i++)
            {
                var w = _parts[i].Part.Value / total * size.Width;
                var gap = i < _parts.Count - 1 ? 2 : 0; // 2px surface gap between fills
                using (dc.PushOpacity(_hover is { } h && h != i ? 0.4 : 1))
                {
                    dc.DrawRectangle(_parts[i].Brush, null, new Rect(x, 4, Math.Max(1, w - gap), Thickness), 4, 4);
                }

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
            var pen = new Pen(_parts[i].Brush, _hover == i ? Thickness + 4 : Thickness, lineCap: PenLineCap.Flat);
            using (dc.PushOpacity(_hover is { } h && h != i ? 0.4 : 1))
            {
                dc.DrawGeometry(null, pen, HelixDraw.Arc(center, radius, start + gapDegrees / 2, Math.Max(0.5, sweep - gapDegrees)));
            }

            start += sweep;
        }

        // Center: the hovered part, else the total.
        var (big, small) = _hover is { } hover
            ? (Format(_parts[hover].Part.Value), $"{_parts[hover].Part.Label} · {(_parts[hover].Part.Value / total).ToString("P0", CultureInfo.CurrentCulture)}")
            : (Format(total), CenterLabel ?? HelixTexts.Current.Total);
        var bigText = HelixDraw.Text(this, big, Math.Max(14, radius / 3.6), text, FontWeight.SemiBold);
        var smallText = HelixDraw.Text(this, small, 12, muted);
        smallText.MaxTextWidth = Math.Max(20, radius * 1.5);
        smallText.TextAlignment = TextAlignment.Center;
        dc.DrawText(bigText, new Point(center.X - bigText.Width / 2, center.Y - bigText.Height / 2 - 7));
        dc.DrawText(smallText, new Point(center.X - smallText.MaxTextWidth / 2, center.Y + bigText.Height / 2 - 6));
    }
}

/// <summary>A dial for one value between Minimum and Maximum. Thresholds color the arc and name the zone (text, not color alone).</summary>
public class HelixGauge : Control
{
    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<HelixGauge, double>(nameof(Value));
    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<HelixGauge, double>(nameof(Minimum));
    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<HelixGauge, double>(nameof(Maximum), 100);
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<HelixGauge, string?>(nameof(Label));
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<HelixGauge, string?>(nameof(Unit));
    public static readonly StyledProperty<string> FormatProperty = AvaloniaProperty.Register<HelixGauge, string>(nameof(Format), "0.#");
    public static readonly StyledProperty<IEnumerable<HelixGaugeThreshold>?> ThresholdsProperty = AvaloniaProperty.Register<HelixGauge, IEnumerable<HelixGaugeThreshold>?>(nameof(Thresholds));
    public static readonly StyledProperty<IBrush?> AccentProperty = AvaloniaProperty.Register<HelixGauge, IBrush?>(nameof(Accent));
    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<HelixGauge, double>(nameof(Size), 180);

    static HelixGauge()
    {
        AffectsRender<HelixGauge>(ValueProperty, MinimumProperty, MaximumProperty, LabelProperty, UnitProperty, FormatProperty, ThresholdsProperty, AccentProperty, SizeProperty);
        AffectsMeasure<HelixGauge>(SizeProperty);
    }

    public HelixGauge() => ActualThemeVariantChanged += (_, _) => InvalidateVisual();

    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>From each Value on, the arc takes this Color and the zone this Label.</summary>
    public IEnumerable<HelixGaugeThreshold>? Thresholds { get => GetValue(ThresholdsProperty); set => SetValue(ThresholdsProperty, value); }

    /// <summary>Arc color without thresholds (primary by default).</summary>
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public double Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size * 0.86);

    public override void Render(DrawingContext dc)
    {
        var center = new Point(Size / 2, Size / 2);
        var thickness = Math.Max(8, Size / 13);
        var radius = Size / 2 - thickness / 2 - 2;
        var range = Math.Max(1e-9, Maximum - Minimum);
        var t = Math.Clamp((Value - Minimum) / range, 0, 1);
        var zone = (Thresholds ?? []).Where(th => Value >= th.Value).OrderBy(th => th.Value).LastOrDefault();
        var color = HelixDraw.Resolve(this, zone?.Color) ?? Accent ?? HelixDraw.Brush(this, HelixKeys.Primary, Colors.SteelBlue);
        var track = HelixDraw.Brush(this, HelixKeys.Surface3, Colors.LightGray);
        var text = HelixDraw.Brush(this, HelixKeys.Text, Colors.Black);
        var muted = HelixDraw.Brush(this, HelixKeys.TextMuted, Colors.Gray);

        const double start = -120, sweep = 240;
        dc.DrawGeometry(null, HelixDraw.Pen(track, thickness), HelixDraw.Arc(center, radius, start, sweep));
        if (t > 0) dc.DrawGeometry(null, HelixDraw.Pen(color, thickness), HelixDraw.Arc(center, radius, start, Math.Max(0.5, sweep * t)));

        // Threshold ticks across the arc.
        foreach (var th in Thresholds ?? [])
        {
            var deg = start + sweep * Math.Clamp((th.Value - Minimum) / range, 0, 1);
            var rad = (deg - 90) * Math.PI / 180;
            var outer = radius + thickness / 2 + 1;
            var a = new Point(center.X + outer * Math.Cos(rad), center.Y + outer * Math.Sin(rad));
            var b = new Point(center.X + (outer - thickness - 2) * Math.Cos(rad), center.Y + (outer - thickness - 2) * Math.Sin(rad));
            dc.DrawLine(HelixDraw.Pen(HelixDraw.Brush(this, HelixKeys.Surface, Colors.White), 2), a, b);
        }

        var value = HelixDraw.Text(this, Value.ToString(Format, CultureInfo.CurrentCulture), Size / 6, text, FontWeight.SemiBold);
        dc.DrawText(value, new Point(center.X - value.Width / 2, center.Y - value.Height / 2 - 4));
        var below = string.Join(" · ", new[] { Unit, zone?.Label ?? Label }.Where(s => !string.IsNullOrEmpty(s)));
        if (below.Length > 0)
        {
            var small = HelixDraw.Text(this, below, 12.5, zone is null ? muted : color, zone is null ? FontWeight.Normal : FontWeight.SemiBold);
            dc.DrawText(small, new Point(center.X - small.Width / 2, center.Y + value.Height / 2 - 4));
        }

        var min = HelixDraw.Text(this, Minimum.ToString(Format, CultureInfo.CurrentCulture), 11, muted);
        var max = HelixDraw.Text(this, Maximum.ToString(Format, CultureInfo.CurrentCulture), 11, muted);
        var y = center.Y + radius * 0.5 + thickness / 2 + 4;
        dc.DrawText(min, new Point(center.X - radius * 0.87 - min.Width / 2, y));
        dc.DrawText(max, new Point(center.X + radius * 0.87 - max.Width / 2, y));
    }
}

/// <summary>
/// A grid of colored cells: sequential (one hue, light → dark) or diverging around a midpoint (two hues, neutral middle).
/// Hover shows the exact value; ShowValues writes it in the cells.
/// </summary>
public class HelixHeatmap : Control
{
    public static readonly StyledProperty<IEnumerable<HelixHeatCell>?> CellsProperty = AvaloniaProperty.Register<HelixHeatmap, IEnumerable<HelixHeatCell>?>(nameof(Cells));
    public static readonly StyledProperty<IList<string>?> RowsProperty = AvaloniaProperty.Register<HelixHeatmap, IList<string>?>(nameof(Rows));
    public static readonly StyledProperty<IList<string>?> ColumnsProperty = AvaloniaProperty.Register<HelixHeatmap, IList<string>?>(nameof(Columns));
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixHeatmap, string?>(nameof(Title));
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<HelixHeatmap, string?>(nameof(Unit));
    public static readonly StyledProperty<string> FormatProperty = AvaloniaProperty.Register<HelixHeatmap, string>(nameof(Format), "0.#");
    public static readonly StyledProperty<bool> IsDivergingProperty = AvaloniaProperty.Register<HelixHeatmap, bool>(nameof(IsDiverging));
    public static readonly StyledProperty<double> MidpointProperty = AvaloniaProperty.Register<HelixHeatmap, double>(nameof(Midpoint));
    public static readonly StyledProperty<double?> MinimumProperty = AvaloniaProperty.Register<HelixHeatmap, double?>(nameof(Minimum));
    public static readonly StyledProperty<double?> MaximumProperty = AvaloniaProperty.Register<HelixHeatmap, double?>(nameof(Maximum));
    public static readonly StyledProperty<bool> ShowValuesProperty = AvaloniaProperty.Register<HelixHeatmap, bool>(nameof(ShowValues));
    public static readonly StyledProperty<bool> ShowLegendProperty = AvaloniaProperty.Register<HelixHeatmap, bool>(nameof(ShowLegend), true);
    public static readonly StyledProperty<int> ColumnLabelEveryProperty = AvaloniaProperty.Register<HelixHeatmap, int>(nameof(ColumnLabelEvery), 1);
    public static readonly StyledProperty<double> CellHeightProperty = AvaloniaProperty.Register<HelixHeatmap, double>(nameof(CellHeight), 28);
    public static readonly RoutedEvent<HelixEventArgs<HelixHeatCell>> CellClickEvent =
        RoutedEvent.Register<HelixHeatmap, HelixEventArgs<HelixHeatCell>>(nameof(CellClick), RoutingStrategies.Bubble);

    public IEnumerable<HelixHeatCell>? Cells { get => GetValue(CellsProperty); set => SetValue(CellsProperty, value); }

    /// <summary>Row order (else the order of first appearance).</summary>
    public IList<string>? Rows { get => GetValue(RowsProperty); set => SetValue(RowsProperty, value); }

    /// <summary>Column order (else the order of first appearance).</summary>
    public IList<string>? Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    /// <summary>Two hues around Midpoint (gains/losses, above/below target).</summary>
    public bool IsDiverging { get => GetValue(IsDivergingProperty); set => SetValue(IsDivergingProperty, value); }
    public double Midpoint { get => GetValue(MidpointProperty); set => SetValue(MidpointProperty, value); }
    public double? Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double? Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public bool ShowValues { get => GetValue(ShowValuesProperty); set => SetValue(ShowValuesProperty, value); }
    public bool ShowLegend { get => GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }

    /// <summary>Label every n-th column (e.g. 3 for hours of the day).</summary>
    public int ColumnLabelEvery { get => GetValue(ColumnLabelEveryProperty); set => SetValue(ColumnLabelEveryProperty, value); }
    public double CellHeight { get => GetValue(CellHeightProperty); set => SetValue(CellHeightProperty, value); }

    public event EventHandler<HelixEventArgs<HelixHeatCell>>? CellClick { add => AddHandler(CellClickEvent, value); remove => RemoveHandler(CellClickEvent, value); }

    private List<string> _rows = [], _cols = [];
    private Dictionary<(string, string), HelixHeatCell> _map = [];
    private double _min, _max;
    private Point? _pointer;
    private Rect _grid;
    private const double TopLabels = 22, LegendHeight = 34;

    public HelixHeatmap()
    {
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
        PointerMoved += (_, e) => { _pointer = e.GetPosition(this); InvalidateVisual(); };
        PointerExited += (_, _) => { _pointer = null; InvalidateVisual(); };
        PointerReleased += (_, e) =>
        {
            if (CellAt(e.GetPosition(this)) is { } cell) RaiseEvent(new HelixEventArgs<HelixHeatCell>(CellClickEvent, cell));
        };
        Refresh();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property.OwnerType == typeof(HelixHeatmap)) Refresh();
    }

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

        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 400 : availableSize.Width, TopLabels + _rows.Count * CellHeight + (ShowLegend ? LegendHeight : 0) + 4);

    private Color ColorOf(double value)
    {
        var t = _max - _min < 1e-9 ? 1 : (value - _min) / (_max - _min);
        if (!IsDiverging) return HelixDraw.Mix(Heat(HelixKeys.HeatLow), Heat(HelixKeys.HeatHigh), t);
        return t < 0.5
            ? HelixDraw.Mix(Heat(HelixKeys.HeatNegative), Heat(HelixKeys.HeatMid), t * 2)
            : HelixDraw.Mix(Heat(HelixKeys.HeatMid), Heat(HelixKeys.HeatPositive), (t - 0.5) * 2);
    }

    private Color Heat(string key) => HelixDraw.ColorOf(this, key, Colors.SteelBlue);

    private string FormatValue(double? value) => value is { } v ? v.ToString(Format, CultureInfo.CurrentCulture) + (Unit is null ? "" : " " + Unit) : "—";

    private HelixHeatCell? CellAt(Point p)
    {
        if (!_grid.Contains(p) || _rows.Count == 0 || _cols.Count == 0) return null;
        var c = (int)((p.X - _grid.Left) / (_grid.Width / _cols.Count));
        var r = (int)((p.Y - _grid.Top) / CellHeight);
        if (r < 0 || r >= _rows.Count || c < 0 || c >= _cols.Count) return null;
        return _map.TryGetValue((_rows[r], _cols[c]), out var cell) ? cell : new HelixHeatCell(_rows[r], _cols[c], null);
    }

    public override void Render(DrawingContext dc)
    {
        var size = Bounds.Size;
        dc.FillRectangle(Brushes.Transparent, new Rect(size));
        var muted = HelixDraw.Brush(this, HelixKeys.TextMuted, Colors.Gray);
        var subtle = HelixDraw.Brush(this, HelixKeys.TextSubtle, Colors.Gray);
        if (_rows.Count == 0 || _cols.Count == 0)
        {
            var empty = HelixDraw.Text(this, HelixTexts.Current.NoData, 13, muted);
            dc.DrawText(empty, new Point((size.Width - empty.Width) / 2, 10));
            return;
        }

        var rowLabels = _rows.Select(r => HelixDraw.Text(this, r, 12, muted)).ToList();
        var left = rowLabels.Max(l => l.Width) + 10;
        _grid = new Rect(left, TopLabels, Math.Max(10, size.Width - left), _rows.Count * CellHeight);
        var cellWidth = _grid.Width / _cols.Count;
        var every = Math.Max(1, ColumnLabelEvery);
        var emptyFill = HelixDraw.Brush(this, HelixKeys.Surface2, Colors.WhiteSmoke);

        for (var c = 0; c < _cols.Count; c += every)
        {
            var label = HelixDraw.Text(this, _cols[c], 11.5, subtle);
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
                    dc.DrawRectangle(HelixDraw.Solid(color), null, rect, 4, 4);
                    if (ShowValues && cellWidth > 26)
                    {
                        var ink = HelixAccent.IsLight(color) ? HelixDraw.Solid(Color.FromRgb(0x18, 0x18, 0x1b)) : Brushes.White;
                        var t = HelixDraw.Text(this, v.ToString(Format, CultureInfo.CurrentCulture), 11, ink);
                        dc.DrawText(t, new Point(rect.Left + (rect.Width - t.Width) / 2, rect.Top + (rect.Height - t.Height) / 2));
                    }
                }
                else
                {
                    dc.DrawRectangle(emptyFill, null, rect, 4, 4);
                }
            }
        }

        if (ShowLegend)
        {
            var top = _grid.Bottom + 12;
            var width = Math.Min(220, _grid.Width);
            var gradient = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative) };
            for (var i = 0; i <= 10; i++) gradient.GradientStops.Add(new GradientStop(ColorOf(_min + (_max - _min) * i / 10), i / 10.0));
            var min = HelixDraw.Text(this, FormatValue(_min), 11, subtle);
            var max = HelixDraw.Text(this, FormatValue(_max), 11, subtle);
            dc.DrawText(min, new Point(left, top - 1));
            var bar = new Rect(left + min.Width + 8, top + 3, width, 8);
            dc.DrawRectangle(gradient, null, bar, 4, 4);
            dc.DrawText(max, new Point(bar.Right + 8, top - 1));
        }

        if (_pointer is { } p && CellAt(p) is { } hovered)
        {
            var r = _rows.IndexOf(hovered.Row);
            var c = _cols.IndexOf(hovered.Column);
            var rect = new Rect(left + c * cellWidth + 1, _grid.Top + r * CellHeight + 1, Math.Max(1, cellWidth - 2), CellHeight - 2);
            dc.DrawRectangle(null, HelixDraw.Pen(HelixDraw.Brush(this, HelixKeys.Text, Colors.Black), 2), rect, 4, 4);
            HelixDraw.Tooltip(dc, this, new Point(rect.Right, rect.Top + rect.Height / 2), size, $"{hovered.Row} · {hovered.Column}",
                [(hovered.Value is { } hv ? HelixDraw.Solid(ColorOf(hv)) : null, Title ?? "", FormatValue(hovered.Value))]);
        }
    }
}

/// <summary>A ranked list with proportional bars behind the labels (top pages, sources, countries…).</summary>
[TemplatePart("PART_Rows", typeof(Panel))]
public class HelixBarList : TemplatedControl
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixBarList>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixBarList>();
    public static readonly StyledProperty<IEnumerable<HelixBarItem>?> ItemsProperty = AvaloniaProperty.Register<HelixBarList, IEnumerable<HelixBarItem>?>(nameof(Items));
    public static readonly StyledProperty<bool> SortProperty = AvaloniaProperty.Register<HelixBarList, bool>(nameof(Sort), true);
    public static readonly StyledProperty<int?> LimitProperty = AvaloniaProperty.Register<HelixBarList, int?>(nameof(Limit));
    public static readonly StyledProperty<double?> MaxValueProperty = AvaloniaProperty.Register<HelixBarList, double?>(nameof(MaxValue));
    public static readonly StyledProperty<string> ValueFormatProperty = AvaloniaProperty.Register<HelixBarList, string>(nameof(ValueFormat), "#,0.##");
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<HelixBarList, string?>(nameof(Unit));
    public static readonly StyledProperty<string?> LabelHeaderProperty = AvaloniaProperty.Register<HelixBarList, string?>(nameof(LabelHeader));
    public static readonly StyledProperty<string?> ValueHeaderProperty = AvaloniaProperty.Register<HelixBarList, string?>(nameof(ValueHeader));
    public static readonly RoutedEvent<HelixEventArgs<HelixBarItem>> ItemClickEvent =
        RoutedEvent.Register<HelixBarList, HelixEventArgs<HelixBarItem>>(nameof(ItemClick), RoutingStrategies.Bubble);

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public IEnumerable<HelixBarItem>? Items { get => GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public bool Sort { get => GetValue(SortProperty); set => SetValue(SortProperty, value); }

    /// <summary>Rows shown before "Show all".</summary>
    public int? Limit { get => GetValue(LimitProperty); set => SetValue(LimitProperty, value); }

    /// <summary>Value of a full bar (else the largest value).</summary>
    public double? MaxValue { get => GetValue(MaxValueProperty); set => SetValue(MaxValueProperty, value); }
    public string ValueFormat { get => GetValue(ValueFormatProperty); set => SetValue(ValueFormatProperty, value); }
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string? LabelHeader { get => GetValue(LabelHeaderProperty); set => SetValue(LabelHeaderProperty, value); }
    public string? ValueHeader { get => GetValue(ValueHeaderProperty); set => SetValue(ValueHeaderProperty, value); }

    /// <summary>Raised with the clicked HelixBarItem; rows become clickable.</summary>
    public event EventHandler<HelixEventArgs<HelixBarItem>>? ItemClick
    {
        add { AddHandler(ItemClickEvent, value); _clickable = true; Build(); }
        remove => RemoveHandler(ItemClickEvent, value);
    }

    private Panel? _rows;
    private bool _expanded, _clickable;

    public HelixBarList() => ActualThemeVariantChanged += (_, _) => Build();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _rows = e.NameScope.Find<Panel>("PART_Rows");
        Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property.OwnerType == typeof(HelixBarList) || change.Property == AccentProperty) Build();
    }

    private TextBlock Text(string text, string key, double size, FontWeight weight = FontWeight.Normal)
    {
        var block = new TextBlock { Text = text, FontSize = size, FontWeight = weight, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        block.Bind(TextBlock.ForegroundProperty, block.GetResourceObservable(key));
        return block;
    }

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
            var vh = Text(ValueHeader ?? "", HelixKeys.TextSubtle, 12, FontWeight.SemiBold);
            DockPanel.SetDock(vh, Dock.Right);
            header.Children.Add(vh);
            header.Children.Add(Text(LabelHeader ?? "", HelixKeys.TextSubtle, 12, FontWeight.SemiBold));
            _rows.Children.Add(header);
        }

        foreach (var item in shown)
        {
            var share = Math.Clamp(item.Value / max, 0, 1);
            var row = new Grid { Margin = new Thickness(0, 0, 0, 4), MinHeight = 32, Background = Brushes.Transparent, ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            var track = new Grid { ColumnDefinitions = { new ColumnDefinition(share, GridUnitType.Star), new ColumnDefinition(1 - share, GridUnitType.Star) } };
            var bar = new Border { CornerRadius = new CornerRadius(6), Background = Accent, Opacity = 0.16 };
            track.Children.Add(bar);
            row.Children.Add(track);

            var label = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0), VerticalAlignment = VerticalAlignment.Center, Spacing = 8 };
            if (item.Icon is { } icon)
            {
                var ic = new HelixIcon { Icon = icon, VerticalAlignment = VerticalAlignment.Center };
                ic.Bind(HelixIcon.ForegroundProperty, ic.GetResourceObservable(HelixKeys.TextMuted));
                label.Children.Add(ic);
            }

            label.Children.Add(Text(item.Label, HelixKeys.Text, 13.5));
            row.Children.Add(label);
            var value = Text(item.Value.ToString(ValueFormat, CultureInfo.CurrentCulture) + (Unit is null ? "" : " " + Unit), HelixKeys.Text, 13.5, FontWeight.SemiBold);
            value.Margin = new Thickness(12, 0, 0, 0);
            Grid.SetColumn(value, 1);
            row.Children.Add(value);

            if (_clickable)
            {
                row.Cursor = new Cursor(StandardCursorType.Hand);
                row.PointerEntered += (_, _) => bar.Opacity = 0.26;
                row.PointerExited += (_, _) => bar.Opacity = 0.16;
                var it = item;
                row.PointerReleased += (_, _) => RaiseEvent(new HelixEventArgs<HelixBarItem>(ItemClickEvent, it));
            }

            _rows.Children.Add(row);
        }

        if (Limit is { } l && items.Count > l)
        {
            var more = new HelixButton
            {
                Content = _expanded ? HelixTexts.Current.ShowLess : HelixTexts.Format(HelixTexts.Current.ShowAll, items.Count),
                IconEnd = "chevron-down", Variant = HelixVariant.Ghost, Tone = HelixTone.Neutral, Size = HelixSize.Sm,
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 0),
            };
            more.Click += (_, _) => { _expanded = !_expanded; Build(); };
            _rows.Children.Add(more);
        }
    }
}
