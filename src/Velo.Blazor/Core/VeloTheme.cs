namespace Velo.Blazor;

/// <summary>
/// Global look of the application. Every property is optional: leave it null to keep the default.
/// </summary>
/// <example>
/// var theme = new VeloTheme { Primary = "#0ea5e9", Radius = "14px" }
///     .Set("switch-thumb", "#fff")      // same as --velo-switch-thumb
///     .Set("btn-radius", "999px");      // all buttons become pills
/// </example>
public sealed class VeloTheme
{
    public VeloThemeMode Mode { get; set; } = VeloThemeMode.Light;

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

    /// <summary>Any Velo CSS variable, with or without the "--velo-" prefix.</summary>
    public Dictionary<string, string> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);

    public VeloTheme Set(string variable, string value)
    {
        Variables[variable] = value;
        return this;
    }

    public string? ToStyle()
    {
        var style = new StyleBuilder()
            .Var("--velo-primary", Primary)
            .Var("--velo-on-primary", OnPrimary)
            .Var("--velo-success", Success)
            .Var("--velo-warning", Warning)
            .Var("--velo-danger", Danger)
            .Var("--velo-info", Info)
            .Var("--velo-radius", Radius)
            .Var("--velo-control-h", ControlHeight)
            .Var("--velo-font", FontFamily)
            .Var("--velo-font-size", FontSize);

        foreach (var (name, value) in Variables)
        {
            style.Var(name.StartsWith("--", StringComparison.Ordinal) ? name : $"--velo-{name}", value);
        }

        return style.Build();
    }
}
