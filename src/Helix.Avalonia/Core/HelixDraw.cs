using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Helix.Avalonia;

/// <summary>Theme lookups and drawing helpers shared by the charts.</summary>
public static class HelixDraw
{
    /// <summary>A theme brush by key, for the current light/dark variant of the control.</summary>
    public static IBrush Brush(Control scope, string key, Color fallback) =>
        scope.TryFindResource(key, scope.ActualThemeVariant, out var value) && value is IBrush brush ? brush : new ImmutableSolidColorBrush(fallback);

    /// <summary>The color of a theme brush.</summary>
    public static Color ColorOf(Control scope, string key, Color fallback) => Brush(scope, key, fallback) is ISolidColorBrush s ? s.Color : fallback;

    /// <summary>
    /// A brush from a color text: a Helix name ("primary", "danger", "muted", "subtle", "series-3", "heat-high"…),
    /// a hex color ("#FF8A00") or a color name. Null when empty or unknown.
    /// </summary>
    public static IBrush? Resolve(Control scope, string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        var key = color.Trim().ToLowerInvariant() switch
        {
            "primary" => HelixKeys.Primary,
            "neutral" => HelixKeys.Neutral,
            "success" => HelixKeys.Success,
            "warning" => HelixKeys.Warning,
            "danger" => HelixKeys.Danger,
            "info" => HelixKeys.Info,
            "text" => HelixKeys.Text,
            "muted" => HelixKeys.TextMuted,
            "subtle" => HelixKeys.TextSubtle,
            "border" => HelixKeys.Border,
            "surface" => HelixKeys.Surface,
            "heat-low" => HelixKeys.HeatLow,
            "heat-high" => HelixKeys.HeatHigh,
            var s when s.StartsWith("series-", StringComparison.Ordinal) && int.TryParse(s[7..], out var n) => HelixKeys.Series(n),
            _ => null,
        };
        if (key is not null) return Brush(scope, key, Colors.Gray);
        return Color.TryParse(color, out var parsed) ? new ImmutableSolidColorBrush(parsed) : null;
    }

    /// <summary>Data series brush n (1-based, 8 colors in a fixed order).</summary>
    public static IBrush Series(Control scope, int index) => Brush(scope, HelixKeys.Series(index), Colors.SteelBlue);

    public static IBrush Solid(Color color, double opacity = 1) => new ImmutableSolidColorBrush(color, opacity);

    /// <summary>The same brush, more transparent (area fills).</summary>
    public static IBrush WithOpacity(IBrush brush, double opacity) =>
        brush is ISolidColorBrush s ? new ImmutableSolidColorBrush(s.Color, opacity) : brush;

    public static Color Mix(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        byte M(byte x, byte y) => (byte)Math.Round(x + (y - x) * t);
        return Color.FromArgb(M(a.A, b.A), M(a.R, b.R), M(a.G, b.G), M(a.B, b.B));
    }

    public static FormattedText Text(Control scope, string text, double size, IBrush brush, FontWeight weight = FontWeight.Normal)
    {
        var family = scope.TryFindResource(HelixKeys.FontFamily, scope.ActualThemeVariant, out var f) && f is FontFamily ff ? ff : FontFamily.Default;
        return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(family, FontStyle.Normal, weight), size, brush);
    }

    public static Pen Pen(IBrush brush, double thickness, bool dashed = false) =>
        new(brush, thickness, dashed ? new DashStyle([3, 2.2], 0) : null, dashed ? PenLineCap.Flat : PenLineCap.Round, PenLineJoin.Round);

    /// <summary>"Nice" axis ticks (1, 2, 2.5, 5 × 10^n) covering [min, max].</summary>
    public static (double Min, double Max, double Step) NiceScale(double min, double max, int targetTicks = 5)
    {
        if (double.IsNaN(min) || double.IsNaN(max)) return (0, 1, 0.2);
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

    /// <summary>Rectangle with only the data end rounded, anchored on the baseline.</summary>
    public static Geometry Bar(Rect r, double radius, bool roundTop)
    {
        radius = Math.Min(radius, Math.Min(r.Width / 2, r.Height));
        if (radius <= 0.5) return new RectangleGeometry(r);
        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            if (roundTop)
            {
                c.BeginFigure(new Point(r.Left, r.Bottom), true);
                c.LineTo(new Point(r.Left, r.Top + radius));
                c.ArcTo(new Point(r.Left + radius, r.Top), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                c.LineTo(new Point(r.Right - radius, r.Top));
                c.ArcTo(new Point(r.Right, r.Top + radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                c.LineTo(new Point(r.Right, r.Bottom));
            }
            else
            {
                c.BeginFigure(new Point(r.Left, r.Top), true);
                c.LineTo(new Point(r.Right, r.Top));
                c.LineTo(new Point(r.Right, r.Bottom - radius));
                c.ArcTo(new Point(r.Right - radius, r.Bottom), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                c.LineTo(new Point(r.Left + radius, r.Bottom));
                c.ArcTo(new Point(r.Left, r.Bottom - radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            }

            c.EndFigure(true);
        }

        return g;
    }

    /// <summary>Arc from startDeg (0 = top, clockwise) over sweepDeg.</summary>
    public static Geometry Arc(Point center, double radius, double startDeg, double sweepDeg)
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
            c.BeginFigure(At(startDeg), false);
            c.ArcTo(At(startDeg + sweepDeg), new Size(radius, radius), 0, sweepDeg > 180, SweepDirection.Clockwise);
            c.EndFigure(false);
        }

        return g;
    }

    /// <summary>A tooltip card drawn in the plot (follows the pointer, stays inside the plot).</summary>
    public static void Tooltip(DrawingContext dc, Control scope, Point anchor, Size bounds, string? title, IReadOnlyList<(IBrush? Swatch, string Text, string Value)> rows)
    {
        var text = Brush(scope, HelixKeys.Text, Colors.Black);
        var muted = Brush(scope, HelixKeys.TextMuted, Colors.Gray);
        var surface = Brush(scope, HelixKeys.Surface, Colors.White);
        var border = Brush(scope, HelixKeys.Border, Colors.LightGray);

        var titleText = title is null ? null : Text(scope, title, 12, muted);
        var lines = rows.Select(r => (r.Swatch, Label: Text(scope, r.Text, 12.5, text), Value: Text(scope, r.Value, 12.5, text, FontWeight.SemiBold))).ToList();
        var labelWidth = lines.Count == 0 ? 0 : lines.Max(l => l.Label.Width);
        var valueWidth = lines.Count == 0 ? 0 : lines.Max(l => l.Value.Width);
        var swatch = lines.Any(l => l.Swatch is not null) ? 14 : 0;
        var width = Math.Max(titleText?.Width ?? 0, swatch + labelWidth + 16 + valueWidth) + 20;
        var height = (titleText is null ? 0 : 20) + lines.Count * 19 + 14;

        var x = anchor.X + 14;
        if (x + width > bounds.Width) x = anchor.X - 14 - width;
        var y = Math.Clamp(anchor.Y - height / 2, 0, Math.Max(0, bounds.Height - height));
        var box = new Rect(Math.Max(0, x), y, width, height);

        dc.DrawRectangle(surface, Pen(border, 1), box, 9, 9, new BoxShadows(new BoxShadow { OffsetY = 6, Blur = 18, Color = Color.FromArgb(0x26, 0, 0, 0) }));
        var top = box.Top + 8;
        if (titleText is not null)
        {
            dc.DrawText(titleText, new Point(box.Left + 10, top));
            top += 20;
        }

        foreach (var (sw, label, value) in lines)
        {
            if (sw is not null) dc.DrawRectangle(sw, null, new Rect(box.Left + 10, top + 4, 9, 9), 2.5, 2.5);
            dc.DrawText(label, new Point(box.Left + 10 + swatch, top));
            dc.DrawText(value, new Point(box.Right - 10 - value.Width, top));
            top += 19;
        }
    }
}

/// <summary>A drawing surface: the owning chart draws on it and listens to the pointer.</summary>
public sealed class HelixPlot : Control
{
    internal Action<DrawingContext, Size>? Draw { get; set; }

    public override void Render(DrawingContext context)
    {
        // Transparent background so the whole area receives pointer moves.
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        Draw?.Invoke(context, Bounds.Size);
    }
}
