using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Helix.Avalonia;

/// <summary>
/// Items on a time axis, by lanes: statuses with a legend (filled, outline, dashed), the live "now" line,
/// past shading, staircase or packed rows. With IsEditable, items can be dragged (move), stretched by their edges,
/// or moved with the arrow keys (Shift + arrows changes the end); <see cref="ItemChanged"/> proposes each change.
/// </summary>
[TemplatePart("PART_Scroll", typeof(ScrollViewer))]
[TemplatePart("PART_Canvas", typeof(Canvas))]
[TemplatePart("PART_Legend", typeof(Panel))]
[TemplatePart("PART_Toggle", typeof(Button))]
public class HelixTimeline : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixTimeline, string?>(nameof(Title));
    public static readonly StyledProperty<object?> HeaderContentProperty = AvaloniaProperty.Register<HelixTimeline, object?>(nameof(HeaderContent));
    public static readonly StyledProperty<DateTime> StartProperty = AvaloniaProperty.Register<HelixTimeline, DateTime>(nameof(Start), DateTime.Today);
    public static readonly StyledProperty<DateTime> EndProperty = AvaloniaProperty.Register<HelixTimeline, DateTime>(nameof(End), DateTime.Today.AddDays(1));
    public static readonly StyledProperty<TimeSpan> SlotDurationProperty = AvaloniaProperty.Register<HelixTimeline, TimeSpan>(nameof(SlotDuration), TimeSpan.FromHours(1));
    public static readonly StyledProperty<IEnumerable<HelixTimelineItem>?> ItemsProperty = AvaloniaProperty.Register<HelixTimeline, IEnumerable<HelixTimelineItem>?>(nameof(Items));
    public static readonly StyledProperty<IEnumerable<HelixTimelineStatus>?> StatusesProperty = AvaloniaProperty.Register<HelixTimeline, IEnumerable<HelixTimelineStatus>?>(nameof(Statuses));
    public static readonly StyledProperty<HelixTimelineLayout> LayoutProperty = AvaloniaProperty.Register<HelixTimeline, HelixTimelineLayout>(nameof(Layout));
    public static readonly StyledProperty<bool> ShowNowProperty = AvaloniaProperty.Register<HelixTimeline, bool>(nameof(ShowNow), true);
    public static readonly StyledProperty<IBrush?> NowBrushProperty = AvaloniaProperty.Register<HelixTimeline, IBrush?>(nameof(NowBrush));
    public static readonly StyledProperty<bool> ShadePastProperty = AvaloniaProperty.Register<HelixTimeline, bool>(nameof(ShadePast), true);
    public static readonly StyledProperty<bool> ShowLegendProperty = AvaloniaProperty.Register<HelixTimeline, bool>(nameof(ShowLegend), true);
    public static readonly StyledProperty<bool> IsCollapsibleProperty = AvaloniaProperty.Register<HelixTimeline, bool>(nameof(IsCollapsible));
    public static readonly StyledProperty<bool> IsCollapsedProperty = AvaloniaProperty.Register<HelixTimeline, bool>(nameof(IsCollapsed), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<double> SlotMinWidthProperty = AvaloniaProperty.Register<HelixTimeline, double>(nameof(SlotMinWidth), 80);
    public static readonly StyledProperty<double> RowHeightProperty = AvaloniaProperty.Register<HelixTimeline, double>(nameof(RowHeight), 34);
    public static readonly StyledProperty<double> LaneWidthProperty = AvaloniaProperty.Register<HelixTimeline, double>(nameof(LaneWidth), 140);
    public static readonly StyledProperty<string?> LaneHeaderProperty = AvaloniaProperty.Register<HelixTimeline, string?>(nameof(LaneHeader));
    public static readonly StyledProperty<Func<DateTime, DateTime, string>?> SlotLabelProperty = AvaloniaProperty.Register<HelixTimeline, Func<DateTime, DateTime, string>?>(nameof(SlotLabel));
    public static readonly StyledProperty<string?> EmptyTextProperty = AvaloniaProperty.Register<HelixTimeline, string?>(nameof(EmptyText));
    public static readonly StyledProperty<bool> IsEditableProperty = AvaloniaProperty.Register<HelixTimeline, bool>(nameof(IsEditable));
    public static readonly StyledProperty<TimeSpan?> SnapToProperty = AvaloniaProperty.Register<HelixTimeline, TimeSpan?>(nameof(SnapTo));
    public static readonly StyledProperty<Func<HelixTimelineItem, bool>?> CanEditProperty = AvaloniaProperty.Register<HelixTimeline, Func<HelixTimelineItem, bool>?>(nameof(CanEdit));
    public static readonly StyledProperty<bool> AllowLaneChangeProperty = AvaloniaProperty.Register<HelixTimeline, bool>(nameof(AllowLaneChange));
    public static readonly StyledProperty<string> TimeFormatProperty = AvaloniaProperty.Register<HelixTimeline, string>(nameof(TimeFormat), "HH:mm");

    public static readonly RoutedEvent<HelixEventArgs<HelixTimelineItem>> ItemClickEvent =
        RoutedEvent.Register<HelixTimeline, HelixEventArgs<HelixTimelineItem>>(nameof(ItemClick), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<HelixEventArgs<HelixTimelineChange>> ItemChangedEvent =
        RoutedEvent.Register<HelixTimeline, HelixEventArgs<HelixTimelineChange>>(nameof(ItemChanged), RoutingStrategies.Bubble);

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Content in the header, before the collapse button (buttons, filters…).</summary>
    public object? HeaderContent { get => GetValue(HeaderContentProperty); set => SetValue(HeaderContentProperty, value); }
    public DateTime Start { get => GetValue(StartProperty); set => SetValue(StartProperty, value); }
    public DateTime End { get => GetValue(EndProperty); set => SetValue(EndProperty, value); }

    /// <summary>Width of a column (15 min, 1 h, 1 day…).</summary>
    public TimeSpan SlotDuration { get => GetValue(SlotDurationProperty); set => SetValue(SlotDurationProperty, value); }
    public IEnumerable<HelixTimelineItem>? Items { get => GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Legend and look of each status (key, label, color, appearance).</summary>
    public IEnumerable<HelixTimelineStatus>? Statuses { get => GetValue(StatusesProperty); set => SetValue(StatusesProperty, value); }

    /// <summary>Staircase: one row per item. Packed: items share rows when they don't overlap.</summary>
    public HelixTimelineLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }
    public bool ShowNow { get => GetValue(ShowNowProperty); set => SetValue(ShowNowProperty, value); }
    public IBrush? NowBrush { get => GetValue(NowBrushProperty); set => SetValue(NowBrushProperty, value); }
    public bool ShadePast { get => GetValue(ShadePastProperty); set => SetValue(ShadePastProperty, value); }
    public bool ShowLegend { get => GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    public bool IsCollapsible { get => GetValue(IsCollapsibleProperty); set => SetValue(IsCollapsibleProperty, value); }
    public bool IsCollapsed { get => GetValue(IsCollapsedProperty); set => SetValue(IsCollapsedProperty, value); }
    public double SlotMinWidth { get => GetValue(SlotMinWidthProperty); set => SetValue(SlotMinWidthProperty, value); }
    public double RowHeight { get => GetValue(RowHeightProperty); set => SetValue(RowHeightProperty, value); }

    /// <summary>Width of the lane names column (when items have lanes).</summary>
    public double LaneWidth { get => GetValue(LaneWidthProperty); set => SetValue(LaneWidthProperty, value); }
    public string? LaneHeader { get => GetValue(LaneHeaderProperty); set => SetValue(LaneHeaderProperty, value); }

    /// <summary>Text of a slot header from its start and end (default "16:45 – 17:00").</summary>
    public Func<DateTime, DateTime, string>? SlotLabel { get => GetValue(SlotLabelProperty); set => SetValue(SlotLabelProperty, value); }
    public string? EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }
    public bool IsEditable { get => GetValue(IsEditableProperty); set => SetValue(IsEditableProperty, value); }

    /// <summary>Drag and key steps (a third of a slot when null).</summary>
    public TimeSpan? SnapTo { get => GetValue(SnapToProperty); set => SetValue(SnapToProperty, value); }

    /// <summary>Which items can be edited (all when null).</summary>
    public Func<HelixTimelineItem, bool>? CanEdit { get => GetValue(CanEditProperty); set => SetValue(CanEditProperty, value); }

    /// <summary>Dragging up or down moves the item to another lane.</summary>
    public bool AllowLaneChange { get => GetValue(AllowLaneChangeProperty); set => SetValue(AllowLaneChangeProperty, value); }
    public string TimeFormat { get => GetValue(TimeFormatProperty); set => SetValue(TimeFormatProperty, value); }

    public event EventHandler<HelixEventArgs<HelixTimelineItem>>? ItemClick { add => AddHandler(ItemClickEvent, value); remove => RemoveHandler(ItemClickEvent, value); }

    /// <summary>
    /// Raised after a drag or a key press with the proposed change. The timeline does not change your data:
    /// apply <c>change.Applied</c> to your list and set Items again.
    /// </summary>
    public event EventHandler<HelixEventArgs<HelixTimelineChange>>? ItemChanged { add => AddHandler(ItemChangedEvent, value); remove => RemoveHandler(ItemChangedEvent, value); }

    public static string ToggleText => HelixTexts.Current.Collapse;

    private const double HeaderHeight = 46;
    private ScrollViewer? _scroll;
    private Canvas? _canvas;
    private Panel? _legend;
    private Button? _toggle;
    private readonly DispatcherTimer _nowTimer;
    private Line? _nowLine;
    private Border? _nowPill;
    private Rectangle? _pastShade;
    private double _slotWidth, _left, _bodyHeight, _lastWidth;
    private List<string?> _lanes = [];
    private Dictionary<string, (double Top, double Height)> _laneBands = [];

    // Drag state.
    private HelixTimelineItem? _dragItem;
    private Control? _dragElement;
    private string _dragMode = "";
    private Point _dragOrigin;
    private bool _dragMoved;
    private (DateTime Start, DateTime End, string? Lane) _dragResult;

    public HelixTimeline()
    {
        _nowTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _nowTimer.Tick += (_, _) => PlaceNow();
        ActualThemeVariantChanged += (_, _) => Build();
    }

    /// <summary>Dictionary key of a lane (items without lane share the empty key).</summary>
    private static string K(string? lane) => lane ?? "\u0000";

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _nowTimer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _nowTimer.Stop();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_scroll is not null) _scroll.SizeChanged -= OnScrollSizeChanged;
        if (_toggle is not null) _toggle.Click -= OnToggle;
        base.OnApplyTemplate(e);
        _scroll = e.NameScope.Find<ScrollViewer>("PART_Scroll");
        _canvas = e.NameScope.Find<Canvas>("PART_Canvas");
        _legend = e.NameScope.Find<Panel>("PART_Legend");
        _toggle = e.NameScope.Find<Button>("PART_Toggle");
        if (_scroll is not null) _scroll.SizeChanged += OnScrollSizeChanged;
        if (_toggle is not null) _toggle.Click += OnToggle;
        Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p.OwnerType == typeof(HelixTimeline) && p != TitleProperty && p != HeaderContentProperty && p != IsCollapsedProperty
            && p != IsCollapsibleProperty && p != SnapToProperty && p != AllowLaneChangeProperty)
        {
            Build();
        }
    }

    private void OnToggle(object? sender, RoutedEventArgs e) => SetCurrentValue(IsCollapsedProperty, !IsCollapsed);

    private void OnScrollSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (Math.Abs(_lastWidth - e.NewSize.Width) > 0.5) Build();
    }

    private HelixTimelineStatus? StatusOf(HelixTimelineItem item) => Statuses?.FirstOrDefault(s => s.Key == item.Status);

    private double X(DateTime t) => _left + (t - Start).TotalMinutes / SlotDuration.TotalMinutes * _slotWidth;

    private TimeSpan Snap => SnapTo ?? TimeSpan.FromMinutes(Math.Max(1, SlotDuration.TotalMinutes / 3));

    private DateTime Round(DateTime t)
    {
        var ticks = Snap.Ticks;
        return new DateTime((long)Math.Round(t.Ticks / (double)ticks) * ticks, t.Kind);
    }

    private IBrush Res(string key, Color fallback) => HelixDraw.Brush(this, key, fallback);

    private void Build()
    {
        if (_canvas is null || _scroll is null) return;
        _canvas.Children.Clear();
        BuildLegend();
        var culture = CultureInfo.CurrentCulture;
        var items = (Items ?? []).Where(i => i.End > Start && i.Start < End).OrderBy(i => i.Start).ToList();
        var slots = Math.Max(1, (int)Math.Ceiling((End - Start).TotalMinutes / Math.Max(1, SlotDuration.TotalMinutes)));
        _lanes = items.Select(i => i.Lane).Distinct().OrderBy(l => l is null ? 0 : 1).ToList();
        var hasLanes = _lanes.Any(l => l is not null);
        _left = hasLanes ? LaneWidth : 0;
        _lastWidth = _scroll.Bounds.Width;
        var available = Math.Max(200, _scroll.Viewport.Width > 0 ? _scroll.Viewport.Width : _scroll.Bounds.Width) - _left;
        _slotWidth = Math.Max(SlotMinWidth, available / slots);
        var width = _left + slots * _slotWidth;

        // Rows per lane.
        var rows = new Dictionary<HelixTimelineItem, int>();
        var rowCount = new Dictionary<string, int>();
        foreach (var lane in _lanes.DefaultIfEmpty(null))
        {
            var ends = new List<DateTime>();
            foreach (var item in items.Where(i => i.Lane == lane))
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

        var border = Res(HelixKeys.Border, Colors.LightGray);
        var subtle = Res(HelixKeys.TextSubtle, Colors.Gray);
        var muted = Res(HelixKeys.TextMuted, Colors.Gray);

        // Past shading (under everything).
        _pastShade = new Rectangle { Fill = Res(HelixKeys.Surface2, Colors.WhiteSmoke), IsHitTestVisible = false, Opacity = 0.6 };
        _canvas.Children.Add(_pastShade);

        // Slots: vertical lines, major labels (hour or day) and slot labels.
        var major = SlotDuration < TimeSpan.FromHours(1) ? TimeSpan.FromHours(1) : TimeSpan.FromDays(1);
        for (var i = 0; i <= slots; i++)
        {
            var t = Start.Add(SlotDuration * i);
            var x = Math.Round(X(t)) + 0.5;
            var isMajor = new DateTime(t.Ticks / major.Ticks * major.Ticks) == t;
            AddLine(x, isMajor ? 0 : 22, x, _bodyHeight, border);
            if (i == slots) break;
            if (isMajor || i == 0)
            {
                Place(Label(major >= TimeSpan.FromDays(1) ? t.ToString("ddd d MMM", culture) : t.ToString("HH:00", culture), 11, subtle, FontWeight.SemiBold), x + 6, 4);
            }

            var slotText = SlotLabel?.Invoke(t, t + SlotDuration)
                ?? (SlotDuration >= TimeSpan.FromDays(1)
                    ? t.ToString("ddd d", culture)
                    : $"{t.ToString(TimeFormat, culture)} – {(t + SlotDuration).ToString(TimeFormat, culture)}");
            var slot = Label(slotText, 12, muted);
            slot.Width = _slotWidth;
            slot.TextAlignment = TextAlignment.Center;
            slot.TextTrimming = TextTrimming.CharacterEllipsis;
            Place(slot, X(t), 24);
        }

        AddLine(0, HeaderHeight - 0.5, width, HeaderHeight - 0.5, border);

        // Lanes.
        if (hasLanes)
        {
            _canvas.Children.Add(new Rectangle { Width = _left, Height = _bodyHeight, Fill = Res(HelixKeys.Surface, Colors.White) });
            if (LaneHeader is { } lh) Place(Label(lh, 11, subtle, FontWeight.SemiBold), 12, 26);
            foreach (var lane in _lanes)
            {
                var (laneTop, laneHeight) = _laneBands[K(lane)];
                var name = Label(lane ?? "—", 13, Res(HelixKeys.Text, Colors.Black), FontWeight.Medium);
                name.Width = _left - 20;
                name.TextTrimming = TextTrimming.CharacterEllipsis;
                Place(name, 12, laneTop + 8);
                AddLine(0, laneTop + laneHeight - 0.5, width, laneTop + laneHeight - 0.5, border);
            }

            AddLine(_left - 0.5, 0, _left - 0.5, _bodyHeight, border);
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
        var nowBrush = NowBrush ?? Res(HelixKeys.Danger, Colors.OrangeRed);
        _nowLine = new Line { Stroke = nowBrush, StrokeThickness = 2, IsHitTestVisible = false };
        _nowPill = new Border
        {
            Background = nowBrush, CornerRadius = new CornerRadius(5), Padding = new Thickness(5, 1), IsHitTestVisible = false,
            Child = new TextBlock { Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeight.SemiBold },
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
        _nowLine.IsVisible = _nowPill.IsVisible = visible;
        var x = X(now);
        _nowLine.StartPoint = new Point(x, 0);
        _nowLine.EndPoint = new Point(x, _bodyHeight);
        ((TextBlock)_nowPill.Child!).Text = now.ToString("HH:mm", CultureInfo.CurrentCulture);
        _nowPill.Measure(Size.Infinity);
        Canvas.SetLeft(_nowPill, x - _nowPill.DesiredSize.Width / 2);
        Canvas.SetTop(_nowPill, 2);
        var shadeEnd = Math.Clamp(x, _left, double.IsNaN(_canvas?.Width ?? double.NaN) ? x : _canvas!.Width);
        _pastShade.IsVisible = ShadePast && now > Start;
        Canvas.SetLeft(_pastShade, _left);
        _pastShade.Width = Math.Max(0, shadeEnd - _left);
        _pastShade.Height = _bodyHeight;
    }

    private Control ItemElement(HelixTimelineItem item)
    {
        var status = StatusOf(item);
        var color = HelixDraw.Resolve(this, item.Color ?? status?.Color) ?? Res(HelixKeys.Primary, Colors.SteelBlue);
        var c = color is ISolidColorBrush s ? s.Color : Colors.SteelBlue;
        var appearance = status?.Appearance ?? HelixTimelineAppearance.Filled;
        var surface = Res(HelixKeys.Surface, Colors.White);
        var surfaceColor = surface is ISolidColorBrush ss ? ss.Color : Colors.White;
        var radius = (RowHeight - 6) / 2;

        var grid = new Panel { Cursor = new Cursor(StandardCursorType.Hand), Focusable = true, Tag = item };
        var shape = new Border
        {
            CornerRadius = new CornerRadius(radius),
            BorderThickness = new Thickness(appearance == HelixTimelineAppearance.Dashed ? 0 : 1.5),
            BorderBrush = color,
            Background = appearance == HelixTimelineAppearance.Filled ? HelixDraw.Solid(HelixDraw.Mix(surfaceColor, c, 0.22)) : surface,
        };
        grid.Children.Add(shape);
        if (appearance == HelixTimelineAppearance.Dashed)
        {
            // A dashed outline: a rounded rectangle drawn with a dash pattern.
            grid.Children.Add(new Rectangle { RadiusX = radius, RadiusY = radius, Stroke = color, StrokeThickness = 1.5, StrokeDashArray = [3, 2] });
        }

        var label = new TextBlock
        {
            Text = item.Label, FontSize = 12, FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0),
        };
        label.Bind(TextBlock.ForegroundProperty, label.GetResourceObservable(HelixKeys.Text));
        grid.Children.Add(label);

        var culture = CultureInfo.CurrentCulture;
        var range = $"{item.Start.ToString(TimeFormat, culture)} – {item.End.ToString(TimeFormat, culture)}";
        var tip = string.Join(" · ", new[] { item.Label, range, status?.Label }.Where(t => !string.IsNullOrEmpty(t)));
        ToolTip.SetTip(grid, tip);
        global::Avalonia.Automation.AutomationProperties.SetName(grid, tip);

        var editable = IsEditable && (CanEdit?.Invoke(item) ?? true);
        grid.PointerPressed += (_, e) => BeginDrag(item, grid, e, editable);
        grid.PointerMoved += (_, e) => DragMove(grid, e, editable);
        grid.PointerReleased += (_, e) => EndDrag(item, grid, e);
        grid.KeyDown += (_, e) => OnItemKey(item, e, editable);
        grid.GotFocus += (_, _) => shape.BorderThickness = new Thickness(2.5);
        grid.LostFocus += (_, _) => shape.BorderThickness = new Thickness(appearance == HelixTimelineAppearance.Dashed ? 0 : 1.5);
        return grid;
    }

    private void BeginDrag(HelixTimelineItem item, Control element, PointerPressedEventArgs e, bool editable)
    {
        if (!e.GetCurrentPoint(element).Properties.IsLeftButtonPressed) return;
        element.Focus();
        _dragItem = item;
        _dragElement = element;
        _dragOrigin = e.GetPosition(_canvas);
        _dragMoved = false;
        _dragResult = (item.Start, item.End, item.Lane);
        var local = e.GetPosition(element);
        _dragMode = !editable ? "" : local.X < 8 ? "start" : local.X > element.Bounds.Width - 8 ? "end" : "move";
        e.Pointer.Capture(element);
        e.Handled = true;
    }

    private void DragMove(Control element, PointerEventArgs e, bool editable)
    {
        if (editable && _dragElement is null)
        {
            // Resize cursor on the edges.
            var local = e.GetPosition(element);
            element.Cursor = new Cursor(local.X < 8 || local.X > element.Bounds.Width - 8 ? StandardCursorType.SizeWestEast : StandardCursorType.SizeAll);
        }

        if (_dragElement != element || _dragItem is null || _dragMode == "") return;
        var p = e.GetPosition(_canvas);
        var dx = p.X - _dragOrigin.X;
        if (!_dragMoved && Math.Abs(dx) < 3 && Math.Abs(p.Y - _dragOrigin.Y) < 3) return;
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

    private void EndDrag(HelixTimelineItem item, Control element, PointerReleasedEventArgs e)
    {
        if (_dragElement != element) return;
        e.Pointer.Capture(null);
        _dragElement = null;
        _dragItem = null;
        if (!_dragMoved)
        {
            RaiseEvent(new HelixEventArgs<HelixTimelineItem>(ItemClickEvent, item));
            return;
        }

        var (start, end, lane) = _dragResult;
        if (start != item.Start || end != item.End || lane != item.Lane) Propose(new HelixTimelineChange(item, start, end, lane));
        else Build();
        e.Handled = true;
    }

    /// <summary>Raises ItemChanged with a change (what a drag or a key press does).</summary>
    public void Propose(HelixTimelineChange change) => RaiseEvent(new HelixEventArgs<HelixTimelineChange>(ItemChangedEvent, change));

    private void OnItemKey(HelixTimelineItem item, KeyEventArgs e, bool editable)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            RaiseEvent(new HelixEventArgs<HelixTimelineItem>(ItemClickEvent, item));
            e.Handled = true;
            return;
        }

        if (!editable || e.Key is not (Key.Left or Key.Right)) return;
        var step = e.Key == Key.Right ? Snap : -Snap;
        Propose(e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            ? new HelixTimelineChange(item, item.Start, Max(item.End + step, item.Start + Snap), item.Lane)
            : new HelixTimelineChange(item, item.Start + step, item.End + step, item.Lane));
        e.Handled = true;
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    private void BuildLegend()
    {
        if (_legend is null) return;
        _legend.Children.Clear();
        var statuses = Statuses?.ToList() ?? [];
        _legend.IsVisible = ShowLegend && statuses.Count > 0;
        foreach (var status in statuses)
        {
            var color = HelixDraw.Resolve(this, status.Color) ?? Brushes.SteelBlue;
            var key = new Rectangle
            {
                Width = 12, Height = 12, RadiusX = 6, RadiusY = 6, Stroke = color, StrokeThickness = 1.5,
                Fill = status.Appearance == HelixTimelineAppearance.Filled ? color : Brushes.Transparent,
                Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center,
            };
            if (status.Appearance == HelixTimelineAppearance.Dashed) key.StrokeDashArray = [2, 1.5];
            var text = new TextBlock { Text = status.Label, FontSize = 12.5, VerticalAlignment = VerticalAlignment.Center };
            text.Bind(TextBlock.ForegroundProperty, text.GetResourceObservable(HelixKeys.TextMuted));
            _legend.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 16, 6), Children = { key, text } });
        }
    }

    private static TextBlock Label(string text, double size, IBrush brush, FontWeight weight = FontWeight.Normal) =>
        new() { Text = text, FontSize = size, Foreground = brush, FontWeight = weight, IsHitTestVisible = false };

    private void AddLine(double x1, double y1, double x2, double y2, IBrush brush) =>
        _canvas!.Children.Add(new Line { StartPoint = new Point(x1, y1), EndPoint = new Point(x2, y2), Stroke = brush, StrokeThickness = 1, IsHitTestVisible = false });

    private void Place(Control element, double x, double y)
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
        _canvas!.Children.Add(element);
    }
}
