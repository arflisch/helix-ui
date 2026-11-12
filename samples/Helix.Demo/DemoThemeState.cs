using System.Text.Json;
using Helix.Blazor;

namespace Helix.Demo;

/// <summary>Theme shared by the whole demo, edited from the top bar and the theme pages, saved in the browser.</summary>
public sealed class DemoThemeState
{
    private static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

    public HelixTheme Theme { get; private set; } = new();

    /// <summary>Name of the preset the theme started from (null once edited by hand).</summary>
    public string? Preset { get; set; } = "Indigo";

    public event Action? Changed;

    public void Notify() => Changed?.Invoke();

    public void Apply(HelixTheme theme, string? preset)
    {
        var mode = Theme.Mode;
        Theme = theme;
        Theme.Mode = mode;
        Preset = preset;
        Notify();
    }

    public string Serialize() => JsonSerializer.Serialize(new Saved(Theme, Preset), Json);

    public void Restore(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            if (JsonSerializer.Deserialize<Saved>(json, Json) is { Theme: not null } saved)
            {
                Theme = saved.Theme;
                Preset = saved.Preset;
                Notify();
            }
        }
        catch (JsonException)
        {
            // An older or corrupted value: keep the default theme.
        }
    }

    private sealed record Saved(HelixTheme Theme, string? Preset);
}
