using System.Text;
using System.Text.Json.Serialization;

namespace Helix.Blazor;

/// <summary>
/// The colors of one mode (light or dark). Every property is optional: null keeps Helix's default for that mode.
/// </summary>
public sealed class HelixPalette
{
    public string? Primary { get; set; }

    /// <summary>Text on primary buttons and selected items.</summary>
    public string? OnPrimary { get; set; }
    public string? Success { get; set; }
    public string? Warning { get; set; }
    public string? Danger { get; set; }
    public string? Info { get; set; }

    /// <summary>Page background.</summary>
    public string? Background { get; set; }

    /// <summary>Cards, inputs, menus.</summary>
    public string? Surface { get; set; }

    /// <summary>Subtle fills: hovers, table headers, tracks.</summary>
    public string? Surface2 { get; set; }
    public string? Surface3 { get; set; }
    public string? Border { get; set; }
    public string? BorderStrong { get; set; }
    public string? Text { get; set; }
    public string? TextMuted { get; set; }
    public string? TextSubtle { get; set; }

    /// <summary>Any Helix CSS variable for this mode only, with or without the "--helix-" prefix.</summary>
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, string> Variables { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    public HelixPalette Set(string variable, string value)
    {
        Variables[variable] = value;
        return this;
    }

    public HelixPalette Clone()
    {
        var copy = (HelixPalette)MemberwiseClone();
        copy.Variables = new Dictionary<string, string>(Variables, StringComparer.OrdinalIgnoreCase);
        return copy;
    }

    internal void AppendTo(StringBuilder css)
    {
        Append(css, "--helix-primary", Primary);
        Append(css, "--helix-on-primary", OnPrimary);
        Append(css, "--helix-success", Success);
        Append(css, "--helix-warning", Warning);
        Append(css, "--helix-danger", Danger);
        Append(css, "--helix-info", Info);
        Append(css, "--helix-bg", Background);
        Append(css, "--helix-surface", Surface);
        Append(css, "--helix-surface-2", Surface2);
        Append(css, "--helix-surface-3", Surface3);
        Append(css, "--helix-border", Border);
        Append(css, "--helix-border-strong", BorderStrong);
        Append(css, "--helix-text", Text);
        Append(css, "--helix-text-muted", TextMuted);
        Append(css, "--helix-text-subtle", TextSubtle);
        foreach (var (name, value) in Variables)
        {
            Append(css, HelixTheme.VariableName(name), value);
        }
    }

    internal static void Append(StringBuilder css, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        // The theme is written into a <style> element: keep values to a single declaration.
        var safe = new string(value.Where(c => c is not ('<' or '>' or '{' or '}' or ';' or '\\')).ToArray()).Trim();
        var safeName = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        if (safe.Length > 0 && safeName.Length > 0)
        {
            css.Append(safeName).Append(':').Append(safe).Append(';');
        }
    }
}

/// <summary>
/// Global look of the application: shared settings, plus one palette per mode.
/// </summary>
/// <example>
/// var theme = new HelixTheme
/// {
///     Mode = HelixThemeMode.System,                          // follows the OS
///     Radius = "12px",
///     Light = new HelixPalette { Primary = "#0284c7", Background = "#f6fafd" },
///     Dark = new HelixPalette { Primary = "#38bdf8", Background = "#06111c", Surface = "#0b1a28" },
/// };
/// </example>
public sealed class HelixTheme
{
    /// <summary>Light, Dark, or System (follows the operating system setting, live).</summary>
    public HelixThemeMode Mode { get; set; } = HelixThemeMode.Light;

    /// <summary>Colors used in light mode.</summary>
    public HelixPalette Light { get; set; } = new();

    /// <summary>Colors used in dark mode.</summary>
    public HelixPalette Dark { get; set; } = new();

    /// <summary>Primary color for both modes (a palette's own Primary wins).</summary>
    public string? Primary { get; set; }
    public string? OnPrimary { get; set; }
    public string? Success { get; set; }
    public string? Warning { get; set; }
    public string? Danger { get; set; }
    public string? Info { get; set; }

    /// <summary>Base corner radius, e.g. "6px" (sharp) or "16px" (soft).</summary>
    public string? Radius { get; set; }

    /// <summary>Height of inputs, selects and medium buttons, e.g. "34px" (compact).</summary>
    public string? ControlHeight { get; set; }

    public string? FontFamily { get; set; }
    public string? FontSize { get; set; }

    /// <summary>Any Helix CSS variable for both modes, with or without the "--helix-" prefix.</summary>
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, string> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HelixTheme Set(string variable, string value)
    {
        Variables[variable] = value;
        return this;
    }

    /// <summary>A deep copy, e.g. to preview a variant without touching the live theme.</summary>
    public HelixTheme Clone(HelixThemeMode? mode = null)
    {
        var copy = new HelixTheme
        {
            Mode = mode ?? Mode,
            Light = Light.Clone(),
            Dark = Dark.Clone(),
            Primary = Primary,
            OnPrimary = OnPrimary,
            Success = Success,
            Warning = Warning,
            Danger = Danger,
            Info = Info,
            Radius = Radius,
            ControlHeight = ControlHeight,
            FontFamily = FontFamily,
            FontSize = FontSize,
        };
        foreach (var (name, value) in Variables)
        {
            copy.Variables[name] = value;
        }

        return copy;
    }

    internal static string VariableName(string name) =>
        name.StartsWith("--", StringComparison.Ordinal) ? name : $"--helix-{name}";

    /// <summary>Declarations shared by both modes.</summary>
    internal string SharedDeclarations()
    {
        var css = new StringBuilder();
        HelixPalette.Append(css, "--helix-primary", Primary);
        HelixPalette.Append(css, "--helix-on-primary", OnPrimary);
        HelixPalette.Append(css, "--helix-success", Success);
        HelixPalette.Append(css, "--helix-warning", Warning);
        HelixPalette.Append(css, "--helix-danger", Danger);
        HelixPalette.Append(css, "--helix-info", Info);
        HelixPalette.Append(css, "--helix-radius", Radius);
        HelixPalette.Append(css, "--helix-control-h", ControlHeight);
        HelixPalette.Append(css, "--helix-font", FontFamily);
        HelixPalette.Append(css, "--helix-font-size", FontSize);
        foreach (var (name, value) in Variables)
        {
            HelixPalette.Append(css, VariableName(name), value);
        }

        return css.ToString();
    }

    internal static string Declarations(HelixPalette palette)
    {
        var css = new StringBuilder();
        palette.AppendTo(css);
        return css.ToString();
    }

    /// <summary>
    /// The stylesheet for a provider element: shared settings, then the palette of the active mode.
    /// In System mode both palettes are written, the dark one inside a prefers-color-scheme media query.
    /// </summary>
    internal string ToStyleSheet(string selector)
    {
        var shared = SharedDeclarations();
        var light = Declarations(Light);
        var dark = Declarations(Dark);
        var css = new StringBuilder();

        void Rule(string body)
        {
            if (body.Length > 0)
            {
                css.Append(selector).Append('{').Append(body).Append('}');
            }
        }

        switch (Mode)
        {
            case HelixThemeMode.Dark:
                Rule(shared + dark);
                break;
            case HelixThemeMode.System:
                Rule(shared + light);
                if (dark.Length > 0)
                {
                    css.Append("@media (prefers-color-scheme: dark){");
                    Rule(dark);
                    css.Append('}');
                }

                break;
            default:
                Rule(shared + light);
                break;
        }

        return css.ToString();
    }
}
