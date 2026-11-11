using System.Windows.Media;

namespace Velo.Wpf;

/// <summary>
/// The colors of one mode (light or dark). Every property is optional: null keeps the built-in Velo color.
/// Same tokens as the Blazor version, so both apps can share a brand.
/// </summary>
public sealed class VeloPalette
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
    public static VeloPalette DefaultLight => new()
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
    public static VeloPalette DefaultDark => new()
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
    internal VeloPalette With(VeloPalette? overrides)
    {
        if (overrides is null)
        {
            return this;
        }

        var result = new VeloPalette();
        foreach (var property in typeof(VeloPalette).GetProperties())
        {
            property.SetValue(result, property.GetValue(overrides) ?? property.GetValue(this));
        }

        return result;
    }

    /// <summary>Resource key → color, for <see cref="VeloResources"/>.</summary>
    internal IEnumerable<(string Key, Color? Color)> Tokens()
    {
        yield return (VeloKeys.Primary, Primary);
        yield return (VeloKeys.OnPrimary, OnPrimary);
        yield return (VeloKeys.Neutral, Neutral);
        yield return (VeloKeys.OnNeutral, OnNeutral);
        yield return (VeloKeys.Success, Success);
        yield return (VeloKeys.OnSuccess, OnSuccess);
        yield return (VeloKeys.Warning, Warning);
        yield return (VeloKeys.OnWarning, OnWarning);
        yield return (VeloKeys.Danger, Danger);
        yield return (VeloKeys.OnDanger, OnDanger);
        yield return (VeloKeys.Info, Info);
        yield return (VeloKeys.OnInfo, OnInfo);
        yield return (VeloKeys.Background, Background);
        yield return (VeloKeys.Surface, Surface);
        yield return (VeloKeys.Surface2, Surface2);
        yield return (VeloKeys.Surface3, Surface3);
        yield return (VeloKeys.Border, Border);
        yield return (VeloKeys.BorderStrong, BorderStrong);
        yield return (VeloKeys.Text, Text);
        yield return (VeloKeys.TextMuted, TextMuted);
        yield return (VeloKeys.TextSubtle, TextSubtle);
        yield return (VeloKeys.Overlay, Overlay);
        yield return ("Velo.Series1", Series1);
        yield return ("Velo.Series2", Series2);
        yield return ("Velo.Series3", Series3);
        yield return ("Velo.Series4", Series4);
        yield return ("Velo.Series5", Series5);
        yield return ("Velo.Series6", Series6);
        yield return ("Velo.Series7", Series7);
        yield return ("Velo.Series8", Series8);
        yield return (VeloKeys.HeatLow, HeatLow);
        yield return (VeloKeys.HeatHigh, HeatHigh);
        yield return (VeloKeys.HeatNegative, HeatNegative);
        yield return (VeloKeys.HeatMid, HeatMid);
        yield return (VeloKeys.HeatPositive, HeatPositive);
    }

    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}

/// <summary>Resource keys of the Velo brushes, usable as {DynamicResource Velo.Primary} in your own XAML.</summary>
public static class VeloKeys
{
    public const string Primary = "Velo.Primary";
    public const string OnPrimary = "Velo.OnPrimary";
    public const string Neutral = "Velo.Neutral";
    public const string OnNeutral = "Velo.OnNeutral";
    public const string Success = "Velo.Success";
    public const string OnSuccess = "Velo.OnSuccess";
    public const string Warning = "Velo.Warning";
    public const string OnWarning = "Velo.OnWarning";
    public const string Danger = "Velo.Danger";
    public const string OnDanger = "Velo.OnDanger";
    public const string Info = "Velo.Info";
    public const string OnInfo = "Velo.OnInfo";
    public const string Background = "Velo.Background";
    public const string Surface = "Velo.Surface";
    public const string Surface2 = "Velo.Surface2";
    public const string Surface3 = "Velo.Surface3";
    public const string Border = "Velo.Border";
    public const string BorderStrong = "Velo.BorderStrong";
    public const string Text = "Velo.Text";
    public const string TextMuted = "Velo.TextMuted";
    public const string TextSubtle = "Velo.TextSubtle";
    public const string Overlay = "Velo.Overlay";
    public const string PrimarySoft = "Velo.PrimarySoft";
    public const string HeatLow = "Velo.HeatLow";
    public const string HeatHigh = "Velo.HeatHigh";
    public const string HeatNegative = "Velo.HeatNegative";
    public const string HeatMid = "Velo.HeatMid";
    public const string HeatPositive = "Velo.HeatPositive";

    /// <summary>Brush key of data series n (1-based, wraps after 8): "Velo.Series1"…</summary>
    public static string Series(int index) => "Velo.Series" + ((Math.Max(1, index) - 1) % 8 + 1);

    public const string Radius = "Velo.Radius";
    public const string RadiusSmall = "Velo.RadiusSmall";
    public const string ControlHeight = "Velo.ControlHeight";
    public const string FontFamily = "Velo.FontFamily";
    public const string FontSize = "Velo.FontSize";

    /// <summary>Brush key of a tone, e.g. Danger → "Velo.Danger".</summary>
    public static string Of(VeloTone tone) => "Velo." + tone;

    /// <summary>Text-on-color brush key of a tone, e.g. Danger → "Velo.OnDanger".</summary>
    public static string OnOf(VeloTone tone) => "Velo.On" + tone;

    public static VeloTone ToneOf(VeloSeverity severity) => severity switch
    {
        VeloSeverity.Success => VeloTone.Success,
        VeloSeverity.Warning => VeloTone.Warning,
        VeloSeverity.Danger => VeloTone.Danger,
        _ => VeloTone.Info,
    };
}
