using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Helix.Wpf;

/// <summary>
/// Items on a time axis, by lanes: statuses with a legend (filled, outline, dashed), the live "now" line,
/// past shading, staircase or packed rows. With IsEditable, items can be dragged (move), stretched by their edges,
/// or moved with the arrow keys (Shift + arrows changes the end); <see cref="ItemChanged"/> proposes each change.
/// </summary>
[TemplatePart(Name = "PART_Scroll", Type = typeof(ScrollViewer))]
[TemplatePart(Name = "PART_Canvas", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_Legend", Type = typeof(Panel))]
[TemplatePart(Name = "PART_Toggle", Type = typeof(ButtonBase))]
public class HelixTimeline : Control, IVeloThemeAware
{
    static HelixTimeline() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixTimeline), new FrameworkPropertyMetadata(typeof(HelixTimeline)));

    public HelixTimeline()
    {
        HelixTheme.Watch(this);
        _nowTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _nowTimer.Tick += (_, _) => PlaceNow();
        Loaded += (_, _) => _nowTimer.Start();
        Unloaded += (_, _) => _nowTimer.Stop();
    }

    private static void Rebuild(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((HelixTimeline)d).Build();

    public static readonly DependencyProperty TitleProperty = HelixProperty.Register<HelixTimeline, string?>(nameof(Title), null);
    public static readonly DependencyProperty HeaderContentProperty = HelixProperty.Register<HelixTimeline, object?>(nameof(HeaderContent), null);
    public static readonly DependencyProperty StartProperty = HelixProperty.Register<HelixTimeline, DateTime>(nameof(Start), DateTime.Today, Rebuild);
    public static readonly DependencyProperty EndProperty = HelixProperty.Register<HelixTimeline, DateTime>(nameof(End), DateTime.Today.AddDays(1), Rebuild);
    public static readonly DependencyProperty SlotDurationProperty = HelixProperty.Register<HelixTimeline, TimeSpan>(nameof(SlotDuration), TimeSpan.FromHours(1), Rebuild);
    public static readonly DependencyProperty ItemsProperty = HelixProperty.Register<HelixTimeline, IEnumerable<HelixTimelineItem>?>(nameof(Items), null, Rebuild);
    public static readonly DependencyProperty StatusesProperty = HelixProperty.Register<HelixTimeline, IEnumerable<HelixTimelineStatus>?>(nameof(Statuses), null, Rebuild);
    public static readonly DependencyProperty LayoutProperty = HelixProperty.Register<HelixTimeline, HelixTimelineLayout>(nameof(Layout), HelixTimelineLayout.Packed, Rebuild);
    public static readonly DependencyProperty ShowNowProperty = HelixProperty.Register<HelixTimeline, bool>(nameof(ShowNow), true, Rebuild);
    public static readonly DependencyProperty NowBrushProperty = HelixProperty.Register<HelixTimeline, Brush?>(nameof(NowBrush), null, Rebuild);
    public static readonly DependencyProperty ShadePastProperty = HelixProperty.Register<HelixTimeline, bool>(nameof(ShadePast), true, Rebuild);
    public static readonly DependencyProperty ShowLegendProperty = HelixProperty.Register<HelixTimeline, bool>(nameof(ShowLegend), true, Rebuild);
    public static readonly DependencyProperty IsCollapsibleProperty = HelixProperty.Register<HelixTimeline, bool>(nameof(IsCollapsible), false);
    public static readonly DependencyProperty IsCollapsedProperty = DependencyProperty.Register(nameof(IsCollapsed), typeof(bool), typeof(HelixTimeline),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty SlotMinWidthProperty = HelixProperty.Register<HelixTimeline, double>(nameof(SlotMinWidth), 80d, Rebuild);
    public static readonly DependencyProperty RowHeightProperty = HelixProperty.Register<HelixTimeline, double>(nameof(RowHeight), 34d, Rebuild);
    public static readonly DependencyProperty LaneWidthProperty = HelixProperty.Register<HelixTimeline, double>(nameof(LaneWidth), 140d, Rebuild);
    public static readonly DependencyProperty LaneHeaderProperty = HelixProperty.Register<HelixTimeline, string?>(nameof(LaneHeader), null, Rebuild);
    public static readonly DependencyProperty SlotLabelProperty = HelixProperty.Register<HelixTimeline, Func<DateTime, DateTime, string>?>(nameof(SlotLabel), null, Rebuild);
    public static readonly DependencyProperty EmptyTextProperty = HelixProperty.Register<HelixTimeline, string?>(nameof(EmptyText), null, Rebuild);
    public static readonly DependencyProperty IsEditableProperty = HelixProperty.Register<HelixTimeline, bool>(nameof(IsEditable), false, Rebuild);
    public static readonly DependencyProperty SnapToProperty = HelixProperty.Register<HelixTimeline, TimeSpan?>(nameof(SnapTo), null);
    public static readonly DependencyProperty CanEditProperty = HelixProperty.Register<HelixTimeline, Func<HelixTimelineItem, bool>?>(nameof(CanEdit), null, Rebuild);
    public static readonly DependencyProperty AllowLaneChangeProperty = HelixProperty.Register<HelixTimeline, bool>(nameof(AllowLaneChange), false);
    public static readonly DependencyProperty TimeFormatProperty = HelixProperty.Register<HelixTimeline, string>(nameof(TimeFormat), "HH:mm", Rebuild);

    public static readonly RoutedEvent ItemClickEvent = EventManager.RegisterRoutedEvent(nameof(ItemClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixTimeline));
    public static readonly RoutedEvent ItemChangedEvent = EventManager.RegisterRoutedEvent(nameof(ItemChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixTimeline));

    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Content in the header, before the collapse button (buttons, filters…).</summary>
    public object? HeaderContent { get => GetValue(HeaderContentProperty); set => SetValue(HeaderContentProperty, value); }
    public DateTime Start { get => (DateTime)GetValue(StartProperty); set => SetValue(StartProperty, value); }
    public DateTime End { get => (DateTime)GetValue(EndProperty); set => SetValue(EndProperty, value); }

    /// <summary>Width of a column (15 min, 1 h, 1 day…).</summary>
    public TimeSpan SlotDuration { get => (TimeSpan)GetValue(SlotDurationProperty); set => SetValue(SlotDurationProperty, value); }
    public IEnumerable<HelixTimelineItem>? Items { get => (IEnumerable<HelixTimelineItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Legend and look of each status (key, label, color, appearance).</summary>
    public IEnumerable<HelixTimelineStatus>? Statuses { get => (IEnumerable<HelixTimelineStatus>?)GetValue(StatusesProperty); set => SetValue(StatusesProperty, value); }

    /// <summary>Staircase: one row per item. Packed: items share rows when they don't overlap.</summary>
    public HelixTimelineLayout Layout { get => (HelixTimelineLayout)GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }
    public bool ShowNow { get => (bool)GetValue(ShowNowProperty); set => SetValue(ShowNowProperty, value); }
    public Brush? NowBrush { get => (Brush?)GetValue(NowBrushProperty); set => SetValue(NowBrushProperty, value); }
    public bool ShadePast { get => (bool)GetValue(ShadePastProperty); set => SetValue(ShadePastProperty, value); }
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    public bool IsCollapsible { get => (bool)GetValue(IsCollapsibleProperty); set => SetValue(IsCollapsibleProperty, value); }
    public bool IsCollapsed { get => (bool)GetValue(IsCollapsedProperty); set => SetValue(IsCollapsedProperty, value); }
    public double SlotMinWidth { get => (double)GetValue(SlotMinWidthProperty); set => SetValue(SlotMinWidthProperty, value); }
    public double RowHeight { get => (double)GetValue(RowHeightProperty); set => SetValue(RowHeightProperty, value); }

    /// <summary>Width of the lane names column (when items have lanes).</summary>
    public double LaneWidth { get => (double)GetValue(LaneWidthProperty); set => SetValue(LaneWidthProperty, value); }
    public string? LaneHeader { get => (string?)GetValue(LaneHeaderProperty); set => SetValue(LaneHeaderProperty, value); }

    /// <summary>Text of a slot header from its start and end (default "16:45 – 17:00").</summary>
    public Func<DateTime, DateTime, string>? SlotLabel { get => (Func<DateTime, DateTime, string>?)GetValue(SlotLabelProperty); set => SetValue(SlotLabelProperty, value); }
    public string? EmptyText { get => (string?)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }
    public bool IsEditable { get => (bool)GetValue(IsEditableProperty); set => SetValue(IsEditableProperty, value); }

    /// <summary>Drag and key steps (a third of a slot when null).</summary>
    public TimeSpan? SnapTo { get => (TimeSpan?)GetValue(SnapToProperty); set => SetValue(SnapToProperty, value); }

    /// <summary>Which items can be edited (all when null).</summary>
    public Func<HelixTimelineItem, bool>? CanEdit { get => (Func<HelixTimelineItem, bool>?)GetValue(CanEditProperty); set => SetValue(CanEditProperty, value); }

    /// <summary>Dragging up or down moves the item to another lane.</summary>
    public bool AllowLaneChange { get => (bool)GetValue(AllowLaneChangeProperty); set => SetValue(AllowLaneChangeProperty, value); }
    public string TimeFormat { get => (string)GetValue(TimeFormatProperty); set => SetValue(TimeFormatProperty, value); }

    /// <summary>Raised with <see cref="HelixEventArgs{T}"/> of HelixTimelineItem.</summary>
    public event RoutedEventHandler ItemClick { add => AddHandler(ItemClickEvent, value); remove => RemoveHandler(ItemClickEvent, value); }

    /// <summary>
    /// Raised with <see cref="HelixEventArgs{T}"/> of HelixTimelineChange after a drag or a key press.
    /// The timeline does not change your data: apply <c>change.Applied</c> to your list and set Items again.
    /// </summary>
    public event RoutedEventHandler ItemChanged { add => AddHandler(ItemChangedEvent, value); remove => RemoveHandler(ItemChangedEvent, value); }

    private const double HeaderHeight = 46;
    private ScrollViewer? _scroll;
    private Canvas? _canvas;
    private Panel? _legend;
    private ButtonBase? _toggle;
    private readonly DispatcherTimer _nowTimer;
    private Line? _nowLine;
    private Border? _nowPill;
    private Rectangle? _pastShade;
    private double _slotWidth, _left, _bodyHeight;
    private List<string?> _lanes = [];
    private Dictionary<string, (double Top, double Height)> _laneBands = [];

    /// <summary>Dictionary key of a lane (items without lane share the empty key).</summary>
    private static string K(string? lane) => lane ?? "\u0000";

    // Drag state.
    private HelixTimelineItem? _dragItem;
    private FrameworkElement? _dragElement;
    private string _dragMode = "";
    private Point _dragOrigin;
    private bool _dragMoved;
    private (DateTime Start, DateTime End, string? Lane) _dragResult;

    public override void OnApplyTemplate()
    {
        if (_scroll is not null) _scroll.SizeChanged -= OnScrollSizeChanged;
        if (_toggle is not null) _toggle.Click -= OnToggle;
        base.OnApplyTemplate();
        _scroll = GetTemplateChild("PART_Scroll") as ScrollViewer;
        _canvas = GetTemplateChild("PART_Canvas") as Canvas;
        _legend = GetTemplateChild("PART_Legend") as Panel;
        _toggle = GetTemplateChild("PART_Toggle") as ButtonBase;
        if (_scroll is not null) _scroll.SizeChanged += OnScrollSizeChanged;
        if (_toggle is not null) _toggle.Click += OnToggle;
        Build();
    }

    void IVeloThemeAware.OnThemeChanged() => Build();

    private void OnToggle(object sender, RoutedEventArgs e) => SetCurrentValue(IsCollapsedProperty, !IsCollapsed);

    private void OnScrollSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (Math.Abs(e.PreviousSize.Width - e.NewSize.Width) > 0.5) Build();
    }

    private HelixTimelineStatus? StatusOf(HelixTimelineItem item) => Statuses?.FirstOrDefault(s => s.Key == item.Status);

    private double X(DateTime t) => _left + (t - Start).TotalMinutes / SlotDuration.TotalMinutes * _slotWidth;

    private DateTime TimeAt(double x) => Start.AddMinutes((x - _left) / _slotWidth * SlotDuration.TotalMinutes);

    private TimeSpan Snap => SnapTo ?? TimeSpan.FromMinutes(Math.Max(1, SlotDuration.TotalMinutes / 3));

    private DateTime Round(DateTime t)
    {
        var ticks = Snap.Ticks;
        return new DateTime((long)Math.Round(t.Ticks / (double)ticks) * ticks, t.Kind);
    }

    private void Build()
    {
        if (_canvas is null || _scroll is null)
        {
            return;
        }

        _canvas.Children.Clear();
        BuildLegend();
        var items = (Items ?? []).Where(i => i.End > Start && i.Start < End).OrderBy(i => i.Start).ToList();
        var slots = Math.Max(1, (int)Math.Ceiling((End - Start).TotalMinutes / Math.Max(1, SlotDuration.TotalMinutes)));
        _lanes = items.Select(i => i.Lane).Distinct().OrderBy(l => l is null ? 0 : 1).ToList();
        var hasLanes = _lanes.Any(l => l is not null);
        _left = hasLanes ? LaneWidth : 0;
        var available = Math.Max(200, _scroll.ViewportWidth > 0 ? _scroll.ViewportWidth : _scroll.ActualWidth) - _left;
        _slotWidth = Math.Max(SlotMinWidth, available / slots);
        var width = _left + slots * _slotWidth;

        // Rows per lane.
        var rows = new Dictionary<HelixTimelineItem, int>();
        var rowCount = new Dictionary<string, int>();
        foreach (var lane in _lanes.DefaultIfEmpty(null))
        {
            var laneItems = items.Where(i => i.Lane == lane).ToList();
            var ends = new List<DateTime>();
            foreach (var item in laneItems)
            {
                int row;
                if (Layout == HelixTimelineLayout.Staircase)
                {
                    row = ends.Count;
                    ends.Add(item.End);
                }
                else
                {
                    row = ends.FindIndex(end => end <= item.Start);
                    if (row < 0)
                    {
                        row = ends.Count;
                        ends.Add(item.End);
                    }
                    else
                    {
                        ends[row] = item.End;
                    }
                }

                rows[item] = row;
            }

            rowCount[K(lane)] = Math.Max(1, ends.Count);
        }

        _laneBands = [];
        var top = HeaderHeight;
        foreach (var lane in _lanes.DefaultIfEmpty(null))
        {
            var height = rowCount.GetValueOrDefault(K(lane), 1) * RowHeight + 10;
            _laneBands[K(lane)] = (top, height);
            top += height;
        }

        _bodyHeight = Math.Max(top, HeaderHeight + RowHeight + 10);
        _canvas.Width = width;
        _canvas.Height = _bodyHeight;

        var border = HelixTheme.Brush(this, HelixKeys.Border, Colors.LightGray);
        var subtle = HelixTheme.Brush(this, HelixKeys.TextSubtle, Colors.Gray);
        var muted = HelixTheme.Brush(this, HelixKeys.TextMuted, Colors.Gray);

        // Past shading (under everything).
        _pastShade = new Rectangle { Fill = HelixTheme.Brush(this, HelixKeys.Surface2, Colors.WhiteSmoke), IsHitTestVisible = false };
        Canvas.SetTop(_pastShade, 0);
        _canvas.Children.Add(_pastShade);

        // Slots: vertical lines, major labels (hour or day) and slot labels.
        var major = SlotDuration < TimeSpan.FromHours(1) ? TimeSpan.FromHours(1) : TimeSpan.FromDays(1);
        for (var i = 0; i <= slots; i++)
        {
            var t = Start.Add(SlotDuration * i);
            var x = Math.Round(X(t)) + 0.5;
            var isMajor = new DateTime(t.Ticks / major.Ticks * major.Ticks) == t;
            _canvas.Children.Add(new Line { X1 = x, X2 = x, Y1 = isMajor ? 0 : 22, Y2 = _bodyHeight, Stroke = border, StrokeThickness = 1, IsHitTestVisible = false });
            if (i == slots) break;

            if (isMajor || i == 0)
            {
                var majorText = Label(major >= TimeSpan.FromDays(1) ? t.ToString("ddd d MMM", CultureInfo.CurrentCulture) : t.ToString("HH:00", CultureInfo.CurrentCulture), 11, subtle, FontWeights.SemiBold);
                Place(majorText, x + 6, 4);
            }

            var slotText = SlotLabel?.Invoke(t, t + SlotDuration)
                ?? (SlotDuration >= TimeSpan.FromDays(1)
                    ? t.ToString("ddd d", CultureInfo.CurrentCulture)
                    : $"{t.ToString(TimeFormat, CultureInfo.CurrentCulture)} – {(t + SlotDuration).ToString(TimeFormat, CultureInfo.CurrentCulture)}");
            var slot = Label(slotText, 12, muted);
            slot.Width = _slotWidth;
            slot.TextAlignment = TextAlignment.Center;
            slot.TextTrimming = TextTrimming.CharacterEllipsis;
            Place(slot, X(t), 24);
        }

        _canvas.Children.Add(new Line { X1 = 0, X2 = width, Y1 = HeaderHeight - 0.5, Y2 = HeaderHeight - 0.5, Stroke = border, StrokeThickness = 1, IsHitTestVisible = false });

        // Lanes.
        if (hasLanes)
        {
            var laneBg = new Rectangle { Width = _left, Height = _bodyHeight, Fill = HelixTheme.Brush(this, HelixKeys.Surface, Colors.White) };
            _canvas.Children.Add(laneBg);
            if (LaneHeader is { } lh) Place(Label(lh, 11, subtle, FontWeights.SemiBold), 12, 26);
            foreach (var lane in _lanes)
            {
                var (laneTop, laneHeight) = _laneBands[K(lane)];
                var name = Label(lane ?? "—", 13, HelixTheme.Brush(this, HelixKeys.Text, Colors.Black), FontWeights.Medium);
                name.Width = _left - 20;
                name.TextTrimming = TextTrimming.CharacterEllipsis;
                Place(name, 12, laneTop + 8);
                _canvas.Children.Add(new Line { X1 = 0, X2 = width, Y1 = laneTop + laneHeight - 0.5, Y2 = laneTop + laneHeight - 0.5, Stroke = border, StrokeThickness = 1, IsHitTestVisible = false });
            }

            _canvas.Children.Add(new Line { X1 = _left - 0.5, X2 = _left - 0.5, Y1 = 0, Y2 = _bodyHeight, Stroke = border, StrokeThickness = 1, IsHitTestVisible = false });
        }

        // Items.
        foreach (var item in items)
        {
            var (laneTop, _) = _laneBands[K(item.Lane)];
            var element = ItemElement(item);
            var x1 = Math.Max(_left, X(item.Start));
            var x2 = Math.Min(width, X(item.End));
            element.Width = Math.Max(8, x2 - x1 - 3);
            element.Height = RowHeight - 6;
            Canvas.SetLeft(element, x1 + 1.5);
            Canvas.SetTop(element, laneTop + 5 + rows[item] * RowHeight + 3);
            _canvas.Children.Add(element);
        }

        if (items.Count == 0)
        {
            var empty = Label(EmptyText ?? HelixTexts.Current.NothingPlanned, 13, muted);
            empty.Width = width - _left;
            empty.TextAlignment = TextAlignment.Center;
            Place(empty, _left, HeaderHeight + 8);
        }

        // Now line on top.
        _nowLine = new Line { Stroke = NowBrush ?? HelixTheme.Brush(this, HelixKeys.Danger, Colors.OrangeRed), StrokeThickness = 2, IsHitTestVisible = false, Y1 = 0, Y2 = _bodyHeight };
        _nowPill = new Border
        {
            Background = _nowLine.Stroke,
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(5, 1, 5, 1),
            IsHitTestVisible = false,
            Child = new TextBlock { Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.SemiBold },
        };
        _canvas.Children.Add(_nowLine);
        _canvas.Children.Add(_nowPill);
        PlaceNow();
    }

    private void PlaceNow()
    {
        if (_nowLine is null || _nowPill is null || _pastShade is null) return;
        var now = DateTime.Now;
        var visible = ShowNow && now >= Start && now <= End;
        _nowLine.Visibility = _nowPill.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        var x = X(now);
        _nowLine.X1 = _nowLine.X2 = x;
        ((TextBlock)_nowPill.Child).Text = now.ToString("HH:mm", CultureInfo.CurrentCulture);
        _nowPill.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(_nowPill, x - _nowPill.DesiredSize.Width / 2);
        Canvas.SetTop(_nowPill, 2);

        var shadeEnd = Math.Clamp(x, _left, _canvas?.Width ?? x);
        _pastShade.Visibility = ShadePast && now > Start ? Visibility.Visible : Visibility.Collapsed;
        Canvas.SetLeft(_pastShade, _left);
        _pastShade.Width = Math.Max(0, shadeEnd - _left);
        _pastShade.Height = _bodyHeight;
        _pastShade.Opacity = 0.6;
    }

    private FrameworkElement ItemElement(HelixTimelineItem item)
    {
        var status = StatusOf(item);
        var color = HelixTheme.Resolve(this, item.Color ?? status?.Color) ?? HelixTheme.Brush(this, HelixKeys.Primary, Colors.SteelBlue);
        var c = HelixTheme.ColorOf(color, Colors.SteelBlue);
        var appearance = status?.Appearance ?? HelixTimelineAppearance.Filled;
        var surface = HelixTheme.Brush(this, HelixKeys.Surface, Colors.White);
        var radius = (RowHeight - 6) / 2;

        var grid = new Grid { Cursor = Cursors.Hand, Focusable = true, FocusVisualStyle = null };
        var shape = new Rectangle
        {
            RadiusX = radius,
            RadiusY = radius,
            StrokeThickness = 1.5,
            Stroke = color,
            Fill = appearance == HelixTimelineAppearance.Filled
                ? new SolidColorBrush(HelixTheme.Mix(HelixTheme.ColorOf(surface, Colors.White), c, 0.22))
                : surface,
        };
        if (appearance == HelixTimelineAppearance.Dashed) shape.StrokeDashArray = [3, 2];
        grid.Children.Add(shape);
        var label = new TextBlock
        {
            Text = item.Label,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 10, 0),
        };
        label.SetResourceReference(TextBlock.ForegroundProperty, HelixKeys.Text);
        grid.Children.Add(label);

        var range = $"{item.Start.ToString(TimeFormat, CultureInfo.CurrentCulture)} – {item.End.ToString(TimeFormat, CultureInfo.CurrentCulture)}";
        grid.ToolTip = string.Join(" · ", new[] { item.Label, range, status?.Label }.Where(s => !string.IsNullOrEmpty(s)));
        System.Windows.Automation.AutomationProperties.SetName(grid, (string)grid.ToolTip);

        var editable = IsEditable && (CanEdit?.Invoke(item) ?? true);
        grid.MouseLeftButtonDown += (_, e) => BeginDrag(item, grid, e, editable);
        grid.MouseMove += (_, e) => DragMove(grid, e, editable);
        grid.MouseLeftButtonUp += (_, e) => EndDrag(item, grid, e);
        grid.KeyDown += (_, e) => OnItemKey(item, e, editable);
        grid.GotKeyboardFocus += (_, _) => shape.StrokeThickness = 2.5;
        grid.LostKeyboardFocus += (_, _) => shape.StrokeThickness = 1.5;
        return grid;
    }

    private void BeginDrag(HelixTimelineItem item, FrameworkElement element, MouseButtonEventArgs e, bool editable)
    {
        element.Focus();
        _dragItem = item;
        _dragElement = element;
        _dragOrigin = e.GetPosition(_canvas);
        _dragMoved = false;
        _dragResult = (item.Start, item.End, item.Lane);
        var local = e.GetPosition(element);
        _dragMode = !editable ? "" : local.X < 8 ? "start" : local.X > element.ActualWidth - 8 ? "end" : "move";
        element.CaptureMouse();
        e.Handled = true;
    }

    private void DragMove(FrameworkElement element, MouseEventArgs e, bool editable)
    {
        if (editable && _dragElement is null)
        {
            // Resize cursor on the edges.
            var local = e.GetPosition(element);
            element.Cursor = local.X < 8 || local.X > element.ActualWidth - 8 ? Cursors.SizeWE : Cursors.SizeAll;
        }

        if (_dragElement != element || _dragItem is null || _dragMode == "" || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var p = e.GetPosition(_canvas);
        var dx = p.X - _dragOrigin.X;
        if (!_dragMoved && Math.Abs(dx) < 3 && Math.Abs(p.Y - _dragOrigin.Y) < 3)
        {
            return;
        }

        _dragMoved = true;
        var delta = TimeSpan.FromMinutes(dx / _slotWidth * SlotDuration.TotalMinutes);
        var item = _dragItem;
        var (start, end) = _dragMode switch
        {
            "start" => (Min(Round(item.Start + delta), item.End - Snap), item.End),
            "end" => (item.Start, Max(Round(item.End + delta), item.Start + Snap)),
            _ => (Round(item.Start + delta), Round(item.Start + delta) + (item.End - item.Start)),
        };

        var lane = item.Lane;
        if (_dragMode == "move" && AllowLaneChange)
        {
            var hit = _laneBands.FirstOrDefault(b => p.Y >= b.Value.Top && p.Y < b.Value.Top + b.Value.Height).Key;
            if (hit is not null) lane = hit == K(null) ? null : hit;
            var (laneTop, _) = _laneBands.GetValueOrDefault(K(lane), _laneBands.GetValueOrDefault(K(item.Lane)));
            Canvas.SetTop(element, laneTop + 8);
        }

        _dragResult = (start, end, lane);
        Canvas.SetLeft(element, X(start) + 1.5);
        element.Width = Math.Max(8, X(end) - X(start) - 3);
    }

    private void EndDrag(HelixTimelineItem item, FrameworkElement element, MouseButtonEventArgs e)
    {
        if (_dragElement != element)
        {
            return;
        }

        element.ReleaseMouseCapture();
        _dragElement = null;
        _dragItem = null;
        if (!_dragMoved)
        {
            RaiseEvent(new HelixEventArgs<HelixTimelineItem>(ItemClickEvent, item));
            return;
        }

        var (start, end, lane) = _dragResult;
        if (start != item.Start || end != item.End || lane != item.Lane)
        {
            RaiseEvent(new HelixEventArgs<HelixTimelineChange>(ItemChangedEvent, new HelixTimelineChange(item, start, end, lane)));
        }
        else
        {
            Build();
        }

        e.Handled = true;
    }

    private void OnItemKey(HelixTimelineItem item, KeyEventArgs e, bool editable)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            RaiseEvent(new HelixEventArgs<HelixTimelineItem>(ItemClickEvent, item));
            e.Handled = true;
            return;
        }

        if (!editable || e.Key is not (Key.Left or Key.Right))
        {
            return;
        }

        var step = e.Key == Key.Right ? Snap : -Snap;
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        var change = shift
            ? new HelixTimelineChange(item, item.Start, Max(item.End + step, item.Start + Snap), item.Lane)
            : new HelixTimelineChange(item, item.Start + step, item.End + step, item.Lane);
        RaiseEvent(new HelixEventArgs<HelixTimelineChange>(ItemChangedEvent, change));
        e.Handled = true;
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    private void BuildLegend()
    {
        if (_legend is null) return;
        _legend.Children.Clear();
        var statuses = Statuses?.ToList() ?? [];
        _legend.Visibility = ShowLegend && statuses.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var status in statuses)
        {
            var color = HelixTheme.Resolve(this, status.Color) ?? Brushes.SteelBlue;
            var c = HelixTheme.ColorOf(color, Colors.SteelBlue);
            var key = new Rectangle
            {
                Width = 12,
                Height = 12,
                RadiusX = 6,
                RadiusY = 6,
                Stroke = color,
                StrokeThickness = 1.5,
                Fill = status.Appearance == HelixTimelineAppearance.Filled ? color : Brushes.Transparent,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            if (status.Appearance == HelixTimelineAppearance.Dashed) key.StrokeDashArray = [2, 1.5];
            var text = new TextBlock { Text = status.Label, FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center };
            text.SetResourceReference(TextBlock.ForegroundProperty, HelixKeys.TextMuted);
            var entry = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 16, 6) };
            entry.Children.Add(key);
            entry.Children.Add(text);
            _legend.Children.Add(entry);
            _ = c;
        }
    }

    private TextBlock Label(string text, double size, Brush brush, FontWeight? weight = null) => new()
    {
        Text = text,
        FontSize = size,
        Foreground = brush,
        FontWeight = weight ?? FontWeights.Normal,
        IsHitTestVisible = false,
    };

    private void Place(UIElement element, double x, double y)
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
        _canvas!.Children.Add(element);
    }
}
