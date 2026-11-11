using System.Globalization;

namespace Velo.Blazor;

/// <summary>A chart point. Y = null leaves a gap in the line.</summary>
public readonly record struct VeloPoint(double X, double? Y)
{
    /// <summary>True when X is a timestamp created with <see cref="At"/>.</summary>
    public bool IsTime { get; init; }

    /// <summary>A point on a time axis.</summary>
    public static VeloPoint At(DateTime time, double? y) => new(VeloTime.ToX(time), y) { IsTime = true };

    public DateTime Time => VeloTime.FromX(X);
}

public enum VeloSeriesType { Line, Area, Step, Bar }

/// <summary>Converts DateTime to chart X values (milliseconds) and back.</summary>
public static class VeloTime
{
    public static double ToX(DateTime time) => time.Ticks / (double)TimeSpan.TicksPerMillisecond;

    public static DateTime FromX(double x) => new((long)(x * TimeSpan.TicksPerMillisecond));
}

public static class VeloPointExtensions
{
    /// <summary>data.ToPoints(d => d.Time, d => d.Value)</summary>
    public static IReadOnlyList<VeloPoint> ToPoints<T>(this IEnumerable<T> source, Func<T, DateTime> time, Func<T, double?> value) =>
        source.Select(item => VeloPoint.At(time(item), value(item))).ToList();

    public static IReadOnlyList<VeloPoint> ToPoints<T>(this IEnumerable<T> source, Func<T, double> x, Func<T, double?> value) =>
        source.Select(item => new VeloPoint(x(item), value(item))).ToList();
}

/// <summary>How a timeline status is drawn: filled (confirmed), outline (sent), dashed (draft).</summary>
public enum VeloTimelineAppearance { Filled, Outline, Dashed }

/// <summary>Staircase: one row per item (reads like a schedule). Packed: items share a row when they don't overlap.</summary>
public enum VeloTimelineLayout { Packed, Staircase }

/// <summary>A status shown in the legend and used to draw items.</summary>
public sealed record VeloTimelineStatus(string Key, string Label, string Color, VeloTimelineAppearance Appearance = VeloTimelineAppearance.Filled);

/// <summary>An item on the timeline.</summary>
public sealed record VeloTimelineItem(string Id, DateTime Start, DateTime End, string Label)
{
    /// <summary>Key of a <see cref="VeloTimelineStatus"/>.</summary>
    public string? Status { get; init; }

    /// <summary>Row group (e.g. a unit or a person). Items without lane share one group.</summary>
    public string? Lane { get; init; }

    /// <summary>Overrides the status color.</summary>
    public string? Color { get; init; }

    /// <summary>Your own object, available in templates and click handlers.</summary>
    public object? Data { get; init; }
}

/// <summary>A proposed edit from a drag or a key press: apply it with item with { Start = …, End = …, Lane = … }.</summary>
public sealed record VeloTimelineChange(VeloTimelineItem Item, DateTime Start, DateTime End, string? Lane)
{
    public VeloTimelineItem Applied => Item with { Start = Start, End = End, Lane = Lane };
}

/// <summary>Axis math shared by the chart and the timeline.</summary>
internal static class VeloScale
{
    /// <summary>"Nice" ticks (1, 2, 2.5, 5 × 10^n) covering [min, max].</summary>
    public static (double Min, double Max, double[] Ticks) Nice(double min, double max, int target = 5)
    {
        if (double.IsNaN(min) || double.IsNaN(max))
        {
            return (0, 1, [0, 1]);
        }

        if (Math.Abs(max - min) < 1e-9)
        {
            var pad = Math.Abs(min) < 1e-9 ? 1 : Math.Abs(min) * 0.1;
            min -= pad;
            max += pad;
        }

        var step = NiceStep((max - min) / target);
        var niceMin = Math.Floor(min / step) * step;
        var niceMax = Math.Ceiling(max / step) * step;
        var ticks = new List<double>();
        for (var v = niceMin; v <= niceMax + step / 2; v += step)
        {
            ticks.Add(Math.Round(v, 10));
        }

        return (niceMin, niceMax, ticks.ToArray());
    }

    private static double NiceStep(double rough)
    {
        var exponent = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        var fraction = rough / exponent;
        var nice = fraction switch { <= 1 => 1, <= 2 => 2, <= 2.5 => 2.5, <= 5 => 5, _ => 10 };
        return nice * exponent;
    }

    private static readonly int[] TimeStepsMinutes = [1, 2, 5, 10, 15, 30, 60, 120, 180, 360, 720, 1440, 2880, 10080];

    /// <summary>Round-time ticks (every 15 min, 1 h, 1 day…) inside [min, max].</summary>
    public static (DateTime[] Ticks, TimeSpan Step) TimeTicks(DateTime min, DateTime max, int maxTicks = 8)
    {
        var span = max - min;
        var stepMinutes = TimeStepsMinutes.FirstOrDefault(m => span.TotalMinutes / m <= maxTicks, TimeStepsMinutes[^1]);
        var step = TimeSpan.FromMinutes(stepMinutes);

        var first = stepMinutes >= 1440
            ? min.Date.AddDays(min.TimeOfDay > TimeSpan.Zero ? 1 : 0)
            : min.Date.AddMinutes(Math.Ceiling(min.TimeOfDay.TotalMinutes / stepMinutes) * stepMinutes);

        var ticks = new List<DateTime>();
        for (var t = first; t <= max; t += step)
        {
            ticks.Add(t);
        }

        return (ticks.ToArray(), step);
    }

    public static string FormatTime(DateTime time, TimeSpan step, CultureInfo culture) =>
        step >= TimeSpan.FromDays(1) ? time.ToString("d MMM", culture)
        : time.TimeOfDay == TimeSpan.Zero && step >= TimeSpan.FromHours(1) ? time.ToString("d MMM", culture)
        : time.ToString("HH:mm", culture);

    public static string Pct(double value) => value.ToString("0.###", CultureInfo.InvariantCulture) + "%";

    public static string Num(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
