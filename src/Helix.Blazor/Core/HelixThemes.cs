namespace Helix.Blazor;

/// <summary>
/// Ready-made themes, each with a light and a dark palette. Every call returns a new instance you can tweak:
/// var theme = HelixThemes.Ocean(); theme.Radius = "14px";
/// </summary>
public static class HelixThemes
{
    public static IReadOnlyList<(string Name, Func<HelixTheme> Create)> All { get; } =
    [
        ("Indigo", Indigo),
        ("Ocean", Ocean),
        ("Forest", Forest),
        ("Sunset", Sunset),
        ("Graphite", Graphite),
    ];

    /// <summary>Helix's defaults.</summary>
    public static HelixTheme Indigo() => new();

    public static HelixTheme Ocean() => new()
    {
        Light = new HelixPalette
        {
            Primary = "#0369a1", Background = "#f5f9fc", Surface = "#ffffff", Surface2 = "#edf3f8", Surface3 = "#dce7f0",
            Border = "#dfe8ef", BorderStrong = "#c7d5e1", Text = "#0c1a26", TextMuted = "#45596b", TextSubtle = "#8a9bab",
        },
        Dark = new HelixPalette
        {
            Primary = "#38bdf8", OnPrimary = "#04121a", Background = "#06111c", Surface = "#0b1a28", Surface2 = "#10243a",
            Surface3 = "#183049", Border = "#16293d", BorderStrong = "#22405e", Text = "#e6f1fa", TextMuted = "#9fb3c6", TextSubtle = "#6b8299",
        },
    };

    public static HelixTheme Forest() => new()
    {
        Radius = "8px",
        Light = new HelixPalette
        {
            Primary = "#15803d", Background = "#f7faf7", Surface = "#ffffff", Surface2 = "#eef4ef", Surface3 = "#dde8df",
            Border = "#e0e9e2", BorderStrong = "#c9d8cd", Text = "#0f1f14", TextMuted = "#4b5f51", TextSubtle = "#8fa194",
        },
        Dark = new HelixPalette
        {
            Primary = "#4ade80", OnPrimary = "#052e16", Background = "#07110a", Surface = "#0d1a11", Surface2 = "#132418",
            Surface3 = "#1c3324", Border = "#1a2e20", BorderStrong = "#27432f", Text = "#ecf7ef", TextMuted = "#a3b8a9", TextSubtle = "#6d8574",
        },
    };

    public static HelixTheme Sunset() => new()
    {
        Radius = "14px",
        Light = new HelixPalette
        {
            Primary = "#c2410c", Background = "#fffaf5", Surface = "#ffffff", Surface2 = "#fbf1e8", Surface3 = "#f1e1d2",
            Border = "#f1e4d8", BorderStrong = "#e3cdb9", Text = "#23140a", TextMuted = "#6b5444", TextSubtle = "#a8917f",
        },
        Dark = new HelixPalette
        {
            Primary = "#fb923c", OnPrimary = "#1f0d02", Background = "#140b06", Surface = "#1c120b", Surface2 = "#261810",
            Surface3 = "#352318", Border = "#2e1f15", BorderStrong = "#47301f", Text = "#fbf1e8", TextMuted = "#c4a995", TextSubtle = "#8a7160",
        },
    };

    /// <summary>Monochrome: black accents in light mode, white accents in dark mode.</summary>
    public static HelixTheme Graphite() => new()
    {
        Radius = "6px",
        Light = new HelixPalette { Primary = "#18181b", OnPrimary = "#ffffff" },
        Dark = new HelixPalette { Primary = "#fafafa", OnPrimary = "#18181b" },
    };
}
