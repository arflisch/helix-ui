using System.Globalization;

namespace Helix.Blazor;

/// <summary>Hex ↔ HSV conversions for the color picker.</summary>
public static class HelixColorMath
{
    /// <summary>Parses #rgb, #rrggbb or #rrggbbaa. Returns false for anything else.</summary>
    public static bool TryParseHex(string? text, out (double R, double G, double B, double A) rgba)
    {
        rgba = default;
        var hex = text?.Trim().TrimStart('#') ?? "";
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => $"{c}{c}"));
        }

        if ((hex.Length != 6 && hex.Length != 8) || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        if (hex.Length == 6)
        {
            value = (value << 8) | 0xFF;
        }

        rgba = ((value >> 24 & 255) / 255.0, (value >> 16 & 255) / 255.0, (value >> 8 & 255) / 255.0, (value & 255) / 255.0);
        return true;
    }

    public static (double H, double S, double V) ToHsv(double r, double g, double b)
    {
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var d = max - min;
        double h = 0;
        if (d > 1e-9)
        {
            h = max == r ? (g - b) / d % 6 : max == g ? (b - r) / d + 2 : (r - g) / d + 4;
            h *= 60;
            if (h < 0) h += 360;
        }

        return (h, max <= 0 ? 0 : d / max, max);
    }

    public static (double R, double G, double B) FromHsv(double h, double s, double v)
    {
        var c = v * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = v - c;
        var (r, g, b) = (h % 360) switch
        {
            < 60 => (c, x, 0d),
            < 120 => (x, c, 0d),
            < 180 => (0d, c, x),
            < 240 => (0d, x, c),
            < 300 => (x, 0d, c),
            _ => (c, 0d, x),
        };
        return (r + m, g + m, b + m);
    }

    public static string ToHex(double r, double g, double b, double a = 1)
    {
        static int B(double v) => (int)Math.Round(Math.Clamp(v, 0, 1) * 255);
        var hex = $"#{B(r):x2}{B(g):x2}{B(b):x2}";
        return a < 0.999 ? hex + $"{B(a):x2}" : hex;
    }
}
