using System.Windows.Media;

namespace Helix.Wpf;

/// <summary>
/// The colors of one mode (light or dark). Every property is optional: null keeps the built-in Helix color.
/// Same tokens as the Blazor version, so both apps can share a brand.
/// </summary>
public sealed class HelixPalette
{
    public Color? Primary { get; set; }
    public Color? OnPrimary { get; set; }
    public Color? Neutral { get; set; }
    public Color? OnNeutral { get; set; }
    public Color? Success { get; set; }
    public Color? OnSuccess { get; set; }
    public Color? Warning { get; set; }
    public Color? OnWarning { get; set; }
    public Color? Danger { get; set; }
    public Color? OnDanger { get; set; }
    public Color? Info { get; set; }
    public Color? OnInfo { get; set; }

    public Color? Background { get; set; }
    public Color? Surface { get; set; }
    public Color? Surface2 { get; set; }
    public Color? Surface3 { get; set; }
    public Color? Border { get; set; }
    public Color? BorderStrong { get; set; }
    public Color? Text { get; set; }
    public Color? TextMuted { get; set; }
    public Color? TextSubtle { get; set; }
    public Color? Overlay { get; set; }

    /// <summary>Data series, in this fixed order (validated for color-vision deficiency on adjacent pairs).</summary>
    public Color? Series1 { get; set; }
    public Color? Series2 { get; set; }
    public Color? Series3 { get; set; }
    public Color? Series4 { get; set; }
    public Color? Series5 { get; set; }
    public Color? Series6 { get; set; }
    public Color? Series7 { get; set; }
    public Color? Series8 { get; set; }

    /// <summary>Heatmap: sequential low → high, diverging negative ← mid → positive.</summary>
    public Color? HeatLow { get; set; }
    public Color? HeatHigh { get; set; }
    public Color? HeatNegative { get; set; }
    public Color? HeatMid { get; set; }
    public Color? HeatPositive { get; set; }

    /// <summary>Built-in light colors.</summary>
    public static HelixPalette DefaultLight => new()
    {
        Primary = C("#4f46e5"), OnPrimary = C("#ffffff"),
        Neutral = C("#27272a"), OnNeutral = C("#ffffff"),
        Success = C("#16a34a"), OnSuccess = C("#ffffff"),
        Warning = C("#d97706"), OnWarning = C("#ffffff"),
        Danger = C("#e11d48"), OnDanger = C("#ffffff"),
        Info = C("#0284c7"), OnInfo = C("#ffffff"),
        Background = C("#fafafa"), Surface = C("#ffffff"), Surface2 = C("#f4f4f5"), Surface3 = C("#e4e4e7"),
        Border = C("#e7e7ea"), BorderStrong = C("#d4d4d8"),
        Text = C("#18181b"), TextMuted = C("#52525b"), TextSubtle = C("#a1a1aa"),
        Overlay = C("#6109090b"),
        Series1 = C("#2a78d6"), Series2 = C("#eb6834"), Series3 = C("#1baf7a"), Series4 = C("#eda100"),
        Series5 = C("#e87ba4"), Series6 = C("#008300"), Series7 = C("#6250d6"), Series8 = C("#e34948"),
        HeatLow = C("#e3eefc"), HeatHigh = C("#104281"), HeatNegative = C("#2a78d6"), HeatMid = C("#f0efec"), HeatPositive = C("#e34948"),
    };

    /// <summary>Built-in dark colors.</summary>
    public static HelixPalette DefaultDark => new()
    {
        Primary = C("#5b5ef4"), OnPrimary = C("#ffffff"),
        Neutral = C("#f4f4f5"), OnNeutral = C("#18181b"),
        Success = C("#22c55e"), OnSuccess = C("#052e16"),
        Warning = C("#f59e0b"), OnWarning = C("#1c1003"),
        Danger = C("#f43f5e"), OnDanger = C("#ffffff"),
        Info = C("#38bdf8"), OnInfo = C("#04121a"),
        Background = C("#09090b"), Surface = C("#111114"), Surface2 = C("#19191d"), Surface3 = C("#27272d"),
        Border = C("#232328"), BorderStrong = C("#34343b"),
        Text = C("#fafafa"), TextMuted = C("#a1a1aa"), TextSubtle = C("#71717a"),
        Overlay = C("#99000000"),
        Series1 = C("#3987e5"), Series2 = C("#d95926"), Series3 = C("#199e70"), Series4 = C("#c98500"),
        Series5 = C("#d55181"), Series6 = C("#008300"), Series7 = C("#9085e9"), Series8 = C("#e66767"),
        HeatLow = C("#172235"), HeatHigh = C("#9ec5f4"), HeatNegative = C("#3987e5"), HeatMid = C("#383835"), HeatPositive = C("#e66767"),
    };

    /// <summary>The values of <paramref name="overrides"/> on top of this palette.</summary>
    internal HelixPalette With(HelixPalette? overrides)
    {
        if (overrides is null)
        {
            return this;
        }

        var result = new HelixPalette();
        foreach (var property in typeof(HelixPalette).GetProperties())
        {
            property.SetValue(result, property.GetValue(overrides) ?? property.GetValue(this));
        }

        return result;
    }

    /// <summary>Resource key → color, for <see cref="HelixResources"/>.</summary>
    internal IEnumerable<(string Key, Color? Color)> Tokens()
    {
        yield return (HelixKeys.Primary, Primary);
        yield return (HelixKeys.OnPrimary, OnPrimary);
        yield return (HelixKeys.Neutral, Neutral);
        yield return (HelixKeys.OnNeutral, OnNeutral);
        yield return (HelixKeys.Success, Success);
        yield return (HelixKeys.OnSuccess, OnSuccess);
        yield return (HelixKeys.Warning, Warning);
        yield return (HelixKeys.OnWarning, OnWarning);
        yield return (HelixKeys.Danger, Danger);
        yield return (HelixKeys.OnDanger, OnDanger);
        yield return (HelixKeys.Info, Info);
        yield return (HelixKeys.OnInfo, OnInfo);
        yield return (HelixKeys.Background, Background);
        yield return (HelixKeys.Surface, Surface);
        yield return (HelixKeys.Surface2, Surface2);
        yield return (HelixKeys.Surface3, Surface3);
        yield return (HelixKeys.Border, Border);
        yield return (HelixKeys.BorderStrong, BorderStrong);
        yield return (HelixKeys.Text, Text);
        yield return (HelixKeys.TextMuted, TextMuted);
        yield return (HelixKeys.TextSubtle, TextSubtle);
        yield return (HelixKeys.Overlay, Overlay);
        yield return ("Helix.Series1", Series1);
        yield return ("Helix.Series2", Series2);
        yield return ("Helix.Series3", Series3);
        yield return ("Helix.Series4", Series4);
        yield return ("Helix.Series5", Series5);
        yield return ("Helix.Series6", Series6);
        yield return ("Helix.Series7", Series7);
        yield return ("Helix.Series8", Series8);
        yield return (HelixKeys.HeatLow, HeatLow);
        yield return (HelixKeys.HeatHigh, HeatHigh);
        yield return (HelixKeys.HeatNegative, HeatNegative);
        yield return (HelixKeys.HeatMid, HeatMid);
        yield return (HelixKeys.HeatPositive, HeatPositive);
    }

    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}

/// <summary>Resource keys of the Helix brushes, usable as {DynamicResource Helix.Primary} in your own XAML.</summary>
public static class HelixKeys
{
    public const string Primary = "Helix.Primary";
    public const string OnPrimary = "Helix.OnPrimary";
    public const string Neutral = "Helix.Neutral";
    public const string OnNeutral = "Helix.OnNeutral";
    public const string Success = "Helix.Success";
    public const string OnSuccess = "Helix.OnSuccess";
    public const string Warning = "Helix.Warning";
    public const string OnWarning = "Helix.OnWarning";
    public const string Danger = "Helix.Danger";
    public const string OnDanger = "Helix.OnDanger";
    public const string Info = "Helix.Info";
    public const string OnInfo = "Helix.OnInfo";
    public const string Background = "Helix.Background";
    public const string Surface = "Helix.Surface";
    public const string Surface2 = "Helix.Surface2";
    public const string Surface3 = "Helix.Surface3";
    public const string Border = "Helix.Border";
    public const string BorderStrong = "Helix.BorderStrong";
    public const string Text = "Helix.Text";
    public const string TextMuted = "Helix.TextMuted";
    public const string TextSubtle = "Helix.TextSubtle";
    public const string Overlay = "Helix.Overlay";
    public const string PrimarySoft = "Helix.PrimarySoft";
    public const string HeatLow = "Helix.HeatLow";
    public const string HeatHigh = "Helix.HeatHigh";
    public const string HeatNegative = "Helix.HeatNegative";
    public const string HeatMid = "Helix.HeatMid";
    public const string HeatPositive = "Helix.HeatPositive";

    /// <summary>Brush key of data series n (1-based, wraps after 8): "Helix.Series1"…</summary>
    public static string Series(int index) => "Helix.Series" + ((Math.Max(1, index) - 1) % 8 + 1);

    public const string Radius = "Helix.Radius";
    public const string RadiusSmall = "Helix.RadiusSmall";
    public const string ControlHeight = "Helix.ControlHeight";
    public const string FontFamily = "Helix.FontFamily";
    public const string FontSize = "Helix.FontSize";

    /// <summary>Brush key of a tone, e.g. Danger → "Helix.Danger".</summary>
    public static string Of(HelixTone tone) => "Helix." + tone;

    /// <summary>Text-on-color brush key of a tone, e.g. Danger → "Helix.OnDanger".</summary>
    public static string OnOf(HelixTone tone) => "Helix.On" + tone;

    public static HelixTone ToneOf(HelixSeverity severity) => severity switch
    {
        HelixSeverity.Success => HelixTone.Success,
        HelixSeverity.Warning => HelixTone.Warning,
        HelixSeverity.Danger => HelixTone.Danger,
        _ => HelixTone.Info,
    };
}
