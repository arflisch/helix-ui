using System.Text;

namespace Helix.Blazor;

/// <summary>Builds a CSS class list, skipping empty or disabled entries.</summary>
public sealed class CssBuilder
{
    private readonly List<string> _classes = [];

    public CssBuilder(string? baseClass = null) => Add(baseClass);

    public CssBuilder Add(string? cssClass, bool when = true)
    {
        if (when && !string.IsNullOrWhiteSpace(cssClass))
        {
            _classes.Add(cssClass.Trim());
        }

        return this;
    }

    public string Build() => string.Join(' ', _classes);
}

/// <summary>
/// Builds an inline style made of CSS custom properties.
/// Every visual parameter of a Helix component ends up here as a <c>--helix-*</c> variable.
/// </summary>
public sealed class StyleBuilder
{
    private readonly StringBuilder _style = new();

    public StyleBuilder Var(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _style.Append(name).Append(':').Append(value.Trim()).Append(';');
        }

        return this;
    }

    /// <summary>
    /// Sets <c>--helix-{component}-color</c> and the matching text color.
    /// Accepts a semantic name ("primary", "danger"...) or any CSS color ("#8b5cf6", "rgb(...)").
    /// </summary>
    public StyleBuilder Color(string component, string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return this;
        }

        var (value, onValue) = HelixColor.Resolve(color);
        return Var($"--helix-{component}-color", value).Var($"--helix-{component}-on-color", onValue);
    }

    public StyleBuilder Raw(string? style)
    {
        if (!string.IsNullOrWhiteSpace(style))
        {
            var trimmed = style.Trim();
            _style.Append(trimmed);
            if (!trimmed.EndsWith(';'))
            {
                _style.Append(';');
            }
        }

        return this;
    }

    public string? Build() => _style.Length == 0 ? null : _style.ToString();
}

/// <summary>Semantic color names understood by every <c>Color</c> parameter.</summary>
public static class HelixColor
{
    public const string Primary = "primary";
    public const string Neutral = "neutral";
    public const string Success = "success";
    public const string Warning = "warning";
    public const string Danger = "danger";
    public const string Info = "info";

    private static readonly HashSet<string> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        Primary, Neutral, Success, Warning, Danger, Info,
    };

    public static (string Value, string OnValue) Resolve(string color)
    {
        if (Named.Contains(color))
        {
            var name = color.ToLowerInvariant();
            return ($"var(--helix-{name})", $"var(--helix-on-{name})");
        }

        return (color, "#fff");
    }

    internal static string FromSeverity(HelixSeverity severity) => severity switch
    {
        HelixSeverity.Success => Success,
        HelixSeverity.Warning => Warning,
        HelixSeverity.Danger => Danger,
        _ => Info,
    };
}

internal static class EnumCss
{
    public static string Css(this Enum value)
    {
        // TopRight -> top-right
        var text = value.ToString();
        var sb = new StringBuilder(text.Length + 4);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsUpper(c) && i > 0)
            {
                sb.Append('-');
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}

internal static class HelixId
{
    public static string Next(string prefix) => $"helix-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";
}
