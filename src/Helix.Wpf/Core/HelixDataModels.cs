using System.Windows;

namespace Helix.Wpf;

// Same models as Helix for Blazor, so data code can be shared. Classes with setters can be written in XAML.

/// <summary>A chart point. Y = null leaves a gap in the line.</summary>
public readonly record struct HelixPoint(double X, double? Y)
{
    /// <summary>True when X is a timestamp created with <see cref="At"/>.</summary>
    public bool IsTime { get; init; }

    /// <summary>A point on a time axis.</summary>
    public static HelixPoint At(DateTime time, double? y) => new(HelixTime.ToX(time), y) { IsTime = true };

    public DateTime Time => HelixTime.FromX(X);
}

/// <summary>Converts DateTime to chart X values (milliseconds) and back.</summary>
public static class HelixTime
{
    public static double ToX(DateTime time) => time.Ticks / (double)TimeSpan.TicksPerMillisecond;

    public static DateTime FromX(double x) => new((long)(x * TimeSpan.TicksPerMillisecond));
}

public static class HelixPointExtensions
{
    /// <summary>data.ToPoints(d => d.Time, d => d.Value)</summary>
    public static IReadOnlyList<HelixPoint> ToPoints<T>(this IEnumerable<T> source, Func<T, DateTime> time, Func<T, double?> value) =>
        source.Select(item => HelixPoint.At(time(item), value(item))).ToList();

    public static IReadOnlyList<HelixPoint> ToPoints<T>(this IEnumerable<T> source, Func<T, double> x, Func<T, double?> value) =>
        source.Select(item => new HelixPoint(x(item), value(item))).ToList();
}

public enum HelixSeriesType { Line, Area, Step, Bar }

public enum HelixPartShape { Donut, SegmentedBar }

public enum HelixTreeLayout { List, Diagram }

public enum HelixDrawerPosition { Right, Left, Bottom }

public enum HelixTabsVariant { Underline, Pills, Segmented }

public enum HelixAvatarStatus { None, Online, Away, Busy, Offline }

/// <summary>One series of a <see cref="HelixChart"/>. A Freezable, so its properties can be bound in XAML.</summary>
public sealed class HelixChartSeries : Freezable
{
    public static readonly DependencyProperty NameProperty = DependencyProperty.Register(nameof(Name), typeof(string), typeof(HelixChartSeries), new PropertyMetadata(""));
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(nameof(Points), typeof(IEnumerable<HelixPoint>), typeof(HelixChartSeries), new PropertyMetadata(null));
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(HelixSeriesType), typeof(HelixChartSeries), new PropertyMetadata(HelixSeriesType.Line));
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(string), typeof(HelixChartSeries), new PropertyMetadata(null));
    public static readonly DependencyProperty ShowMarkersProperty = DependencyProperty.Register(nameof(ShowMarkers), typeof(bool), typeof(HelixChartSeries), new PropertyMetadata(false));
    public static readonly DependencyProperty IsDashedProperty = DependencyProperty.Register(nameof(IsDashed), typeof(bool), typeof(HelixChartSeries), new PropertyMetadata(false));
    public static readonly DependencyProperty IsHiddenProperty = DependencyProperty.Register(nameof(IsHidden), typeof(bool), typeof(HelixChartSeries), new PropertyMetadata(false));

    public string Name { get => (string)GetValue(NameProperty); set => SetValue(NameProperty, value); }
    public IEnumerable<HelixPoint>? Points { get => (IEnumerable<HelixPoint>?)GetValue(PointsProperty); set => SetValue(PointsProperty, value); }
    public HelixSeriesType Type { get => (HelixSeriesType)GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>"series-2", "danger", "#FF8A00"… Null: the next series color, in order.</summary>
    public string? Color { get => (string?)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public bool ShowMarkers { get => (bool)GetValue(ShowMarkersProperty); set => SetValue(ShowMarkersProperty, value); }

    /// <summary>Dashed line (forecast, target…), so it is not told apart by color alone.</summary>
    public bool IsDashed { get => (bool)GetValue(IsDashedProperty); set => SetValue(IsDashedProperty, value); }

    /// <summary>Hidden at start; the legend can show it.</summary>
    public bool IsHidden { get => (bool)GetValue(IsHiddenProperty); set => SetValue(IsHiddenProperty, value); }

    protected override Freezable CreateInstanceCore() => new HelixChartSeries();
}

/// <summary>A reference line on a chart: horizontal (Y), vertical (X or At), with a label.</summary>
public sealed class HelixChartLine : Freezable
{
    public static readonly DependencyProperty YProperty = DependencyProperty.Register(nameof(Y), typeof(double?), typeof(HelixChartLine), new PropertyMetadata(null));
    public static readonly DependencyProperty XProperty = DependencyProperty.Register(nameof(X), typeof(double?), typeof(HelixChartLine), new PropertyMetadata(null));
    public static readonly DependencyProperty AtProperty = DependencyProperty.Register(nameof(At), typeof(DateTime?), typeof(HelixChartLine), new PropertyMetadata(null));
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(HelixChartLine), new PropertyMetadata(null));
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(string), typeof(HelixChartLine), new PropertyMetadata(null));
    public static readonly DependencyProperty IsDashedProperty = DependencyProperty.Register(nameof(IsDashed), typeof(bool), typeof(HelixChartLine), new PropertyMetadata(true));

    public double? Y { get => (double?)GetValue(YProperty); set => SetValue(YProperty, value); }
    public double? X { get => (double?)GetValue(XProperty); set => SetValue(XProperty, value); }
    public DateTime? At { get => (DateTime?)GetValue(AtProperty); set => SetValue(AtProperty, value); }
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Color { get => (string?)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public bool IsDashed { get => (bool)GetValue(IsDashedProperty); set => SetValue(IsDashedProperty, value); }

    protected override Freezable CreateInstanceCore() => new HelixChartLine();
}

/// <summary>A part of a <see cref="HelixDonut"/>.</summary>
public sealed class HelixPart
{
    public HelixPart() { }

    public HelixPart(string label, double value, string? color = null) => (Label, Value, Color) = (label, value, color);

    public string Label { get; set; } = "";
    public double Value { get; set; }

    /// <summary>Null: the series colors in order.</summary>
    public string? Color { get; set; }
}

/// <summary>A row of a <see cref="HelixBarList"/>.</summary>
public sealed class HelixBarItem
{
    public HelixBarItem() { }

    public HelixBarItem(string label, double value) => (Label, Value) = (label, value);

    public string Label { get; set; } = "";
    public double Value { get; set; }
    public string? Icon { get; set; }
}

/// <summary>From this value on, the gauge takes this color and label (e.g. 80 → "Critique").</summary>
public sealed class HelixGaugeThreshold
{
    public HelixGaugeThreshold() { }

    public HelixGaugeThreshold(double value, string color, string label) => (Value, Color, Label) = (value, color, label);

    public double Value { get; set; }
    public string Color { get; set; } = "primary";
    public string? Label { get; set; }
}

/// <summary>A heatmap cell. Value = null is drawn as "no data".</summary>
public sealed record HelixHeatCell(string Row, string Column, double? Value);

public sealed class HelixBreadcrumbItem
{
    public HelixBreadcrumbItem() { }

    public HelixBreadcrumbItem(string text, string? icon = null) => (Text, Icon) = (text, icon);

    public string Text { get; set; } = "";
    public string? Icon { get; set; }

    /// <summary>Your own value (a route, a folder…), given back by ItemClick.</summary>
    public object? Tag { get; set; }
}

public sealed class HelixStep
{
    public HelixStep() { }

    public HelixStep(string title, string? description = null) => (Title, Description) = (title, description);

    public string Title { get; set; } = "";
    public string? Description { get; set; }
}

public sealed class HelixKanbanColumn
{
    public HelixKanbanColumn() { }

    public HelixKanbanColumn(string key, string title, string? color = null, int? limit = null) => (Key, Title, Color, Limit) = (key, title, color, limit);

    public string Key { get; set; } = "";
    public string Title { get; set; } = "";

    /// <summary>Dot color: "series-1", "success", "#FF8A00"…</summary>
    public string? Color { get; set; }

    /// <summary>WIP limit: the count turns red above it.</summary>
    public int? Limit { get; set; }
}

/// <summary>A card was dropped in a column (possibly the same one) at Index.</summary>
public sealed class HelixKanbanMoveEventArgs(RoutedEvent routedEvent, object item, string from, string to, int index) : RoutedEventArgs(routedEvent)
{
    public object Item { get; } = item;
    public string From { get; } = from;
    public string To { get; } = to;
    public int Index { get; } = index;
}

/// <summary>Position, width and visibility of a dashboard tile (save it to restore the user's layout).</summary>
/// <param name="Column">Columns layout: first column of the tile (-1 = automatic).</param>
public sealed record HelixTileState(string Id, int Span = 1, bool Hidden = false, int Column = -1);

/// <summary>How a timeline status is drawn: filled (confirmed), outline (sent), dashed (draft).</summary>
public enum HelixTimelineAppearance { Filled, Outline, Dashed }

/// <summary>Staircase: one row per item (reads like a schedule). Packed: items share a row when they don't overlap.</summary>
public enum HelixTimelineLayout { Packed, Staircase }

/// <summary>A status shown in the legend and used to draw items.</summary>
public sealed class HelixTimelineStatus
{
    public HelixTimelineStatus() { }

    public HelixTimelineStatus(string key, string label, string color, HelixTimelineAppearance appearance = HelixTimelineAppearance.Filled) =>
        (Key, Label, Color, Appearance) = (key, label, color, appearance);

    public string Key { get; set; } = "";
    public string Label { get; set; } = "";

    /// <summary>"series-1", "danger", "#F15A24"…</summary>
    public string Color { get; set; } = "primary";
    public HelixTimelineAppearance Appearance { get; set; }
}

/// <summary>An item on the timeline.</summary>
public sealed record HelixTimelineItem(string Id, DateTime Start, DateTime End, string Label)
{
    /// <summary>Key of a <see cref="HelixTimelineStatus"/>.</summary>
    public string? Status { get; init; }

    /// <summary>Row group (e.g. a unit or a person). Items without lane share one group.</summary>
    public string? Lane { get; init; }

    /// <summary>Overrides the status color.</summary>
    public string? Color { get; init; }

    /// <summary>Your own object, available in click and change handlers.</summary>
    public object? Data { get; init; }
}

/// <summary>A proposed edit from a drag or a key press: apply it with item with { Start = …, End = …, Lane = … }.</summary>
public sealed record HelixTimelineChange(HelixTimelineItem Item, DateTime Start, DateTime End, string? Lane)
{
    public HelixTimelineItem Applied => Item with { Start = Start, End = End, Lane = Lane };
}

/// <summary>Event data carrying one value.</summary>
public sealed class HelixEventArgs<T>(RoutedEvent routedEvent, T value) : RoutedEventArgs(routedEvent)
{
    public T Value { get; } = value;
}
