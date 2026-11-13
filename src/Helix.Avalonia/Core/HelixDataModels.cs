using Avalonia;
using Avalonia.Interactivity;

namespace Helix.Avalonia;

// Same models as Helix for Blazor and WPF, so data code can be shared.

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

/// <summary>One series of a <see cref="HelixChart"/>; a StyledElement, so its properties can be bound in AXAML (it takes the chart's DataContext).</summary>
public sealed class HelixChartSeries : StyledElement
{
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<HelixChartSeries, string>(nameof(Label), "");
    public static readonly StyledProperty<IEnumerable<HelixPoint>?> PointsProperty = AvaloniaProperty.Register<HelixChartSeries, IEnumerable<HelixPoint>?>(nameof(Points));
    public static readonly StyledProperty<HelixSeriesType> TypeProperty = AvaloniaProperty.Register<HelixChartSeries, HelixSeriesType>(nameof(Type));
    public static readonly StyledProperty<string?> ColorProperty = AvaloniaProperty.Register<HelixChartSeries, string?>(nameof(Color));
    public static readonly StyledProperty<bool> ShowMarkersProperty = AvaloniaProperty.Register<HelixChartSeries, bool>(nameof(ShowMarkers));
    public static readonly StyledProperty<bool> IsDashedProperty = AvaloniaProperty.Register<HelixChartSeries, bool>(nameof(IsDashed));
    public static readonly StyledProperty<bool> IsHiddenProperty = AvaloniaProperty.Register<HelixChartSeries, bool>(nameof(IsHidden));

    /// <summary>Name of the series in the legend and the tooltip (Name in Helix for WPF; in Avalonia, Name is the element's x:Name).</summary>
    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public IEnumerable<HelixPoint>? Points { get => GetValue(PointsProperty); set => SetValue(PointsProperty, value); }
    public HelixSeriesType Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>"series-2", "danger", "#FF8A00"… Null: the next series color, in order.</summary>
    public string? Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public bool ShowMarkers { get => GetValue(ShowMarkersProperty); set => SetValue(ShowMarkersProperty, value); }

    /// <summary>Dashed line (forecast, target…), so it is not told apart by color alone.</summary>
    public bool IsDashed { get => GetValue(IsDashedProperty); set => SetValue(IsDashedProperty, value); }

    /// <summary>Hidden at start; the legend can show it.</summary>
    public bool IsHidden { get => GetValue(IsHiddenProperty); set => SetValue(IsHiddenProperty, value); }
}

/// <summary>A reference line on a chart: horizontal (Y), vertical (X or At), with a label.</summary>
public sealed class HelixChartLine : StyledElement
{
    public static readonly StyledProperty<double?> YProperty = AvaloniaProperty.Register<HelixChartLine, double?>(nameof(Y));
    public static readonly StyledProperty<double?> XProperty = AvaloniaProperty.Register<HelixChartLine, double?>(nameof(X));
    public static readonly StyledProperty<DateTime?> AtProperty = AvaloniaProperty.Register<HelixChartLine, DateTime?>(nameof(At));
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<HelixChartLine, string?>(nameof(Label));
    public static readonly StyledProperty<string?> ColorProperty = AvaloniaProperty.Register<HelixChartLine, string?>(nameof(Color));
    public static readonly StyledProperty<bool> IsDashedProperty = AvaloniaProperty.Register<HelixChartLine, bool>(nameof(IsDashed), true);

    public double? Y { get => GetValue(YProperty); set => SetValue(YProperty, value); }
    public double? X { get => GetValue(XProperty); set => SetValue(XProperty, value); }
    public DateTime? At { get => GetValue(AtProperty); set => SetValue(AtProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public bool IsDashed { get => GetValue(IsDashedProperty); set => SetValue(IsDashedProperty, value); }
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

/// <summary>From this value on, the gauge takes this color and label (e.g. 80 → "Critical").</summary>
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

/// <summary>A routed event that carries the item it is about (clicked part, cell, row…).</summary>
public sealed class HelixEventArgs<T>(RoutedEvent routedEvent, T item) : RoutedEventArgs(routedEvent)
{
    public T Item { get; } = item;
}
