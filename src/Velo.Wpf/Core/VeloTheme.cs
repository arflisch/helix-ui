using System.Windows;
using System.Windows.Media;

namespace Velo.Wpf;

/// <summary>Implemented by controls that draw with theme colors in code and must redraw when the theme changes.</summary>
internal interface IVeloThemeAware
{
    void OnThemeChanged();
}

/// <summary>Theme helpers for controls drawn in code: color names, series colors, and theme-change notification.</summary>
public static class VeloTheme
{
    // Holds a reference to a theme brush: VeloResources replaces every brush on a theme change,
    // so this property changes too and tells the control to redraw. No event subscription to leak.
    internal static readonly DependencyProperty StampProperty = DependencyProperty.RegisterAttached(
        "Stamp", typeof(object), typeof(VeloTheme),
        new PropertyMetadata(null, (d, _) => (d as IVeloThemeAware)?.OnThemeChanged()));

    /// <summary>Call from the constructor of a control drawn in code.</summary>
    internal static void Watch(FrameworkElement element) => element.SetResourceReference(StampProperty, VeloKeys.Text);

    /// <summary>
    /// A brush from a color text: a Velo name ("primary", "danger", "muted", "series-3", "heat-high"…),
    /// a hex color ("#FF8A00") or a WPF color name. Null when empty.
    /// </summary>
    public static Brush? Resolve(FrameworkElement scope, string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var key = color.Trim().ToLowerInvariant() switch
        {
            "primary" => VeloKeys.Primary,
            "neutral" => VeloKeys.Neutral,
            "success" => VeloKeys.Success,
            "warning" => VeloKeys.Warning,
            "danger" => VeloKeys.Danger,
            "info" => VeloKeys.Info,
            "text" => VeloKeys.Text,
            "muted" => VeloKeys.TextMuted,
            "subtle" => VeloKeys.TextSubtle,
            "border" => VeloKeys.Border,
            "surface" => VeloKeys.Surface,
            "heat-low" => VeloKeys.HeatLow,
            "heat-high" => VeloKeys.HeatHigh,
            var s when s.StartsWith("series-", StringComparison.Ordinal) && int.TryParse(s[7..], out var n) => VeloKeys.Series(n),
            _ => null,
        };

        if (key is not null)
        {
            return scope.TryFindResource(key) as Brush;
        }

        try
        {
            return new BrushConverter().ConvertFromInvariantString(color) as Brush;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>A theme brush by key, or a fallback when the theme is missing.</summary>
    public static Brush Brush(FrameworkElement scope, string key, Color fallback) =>
        scope.TryFindResource(key) as Brush ?? new SolidColorBrush(fallback);

    /// <summary>Series color n (1-based).</summary>
    public static Brush Series(FrameworkElement scope, int index) => Brush(scope, VeloKeys.Series(index), Colors.SteelBlue);

    internal static Color ColorOf(Brush? brush, Color fallback) => brush is SolidColorBrush solid ? solid.Color : fallback;

    internal static SolidColorBrush Frozen(Color color, double opacity = 1)
    {
        var brush = new SolidColorBrush(color) { Opacity = opacity };
        brush.Freeze();
        return brush;
    }

    /// <summary>Linear mix of two colors (t = 0 → a, 1 → b).</summary>
    internal static Color Mix(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        byte L(byte x, byte y) => (byte)Math.Round(x + (y - x) * t);
        return Color.FromArgb(L(a.A, b.A), L(a.R, b.R), L(a.G, b.G), L(a.B, b.B));
    }

    internal static FontFamily Font(FrameworkElement scope) => scope.TryFindResource(VeloKeys.FontFamily) as FontFamily ?? new FontFamily("Segoe UI");
}
