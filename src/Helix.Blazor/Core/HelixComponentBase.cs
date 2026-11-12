using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Helix.Blazor;

/// <summary>Common parameters shared by every Helix component.</summary>
public abstract class HelixComponentBase : ComponentBase
{
    /// <summary>Extra CSS classes added to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Extra inline style added to the root element (e.g. "--helix-switch-thumb: red").</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Any other HTML attribute (id, aria-*, data-*, title...) is forwarded.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? Attributes { get; set; }

    /// <summary>Set by <see cref="HelixProvider"/>.</summary>
    [CascadingParameter] private HelixOptions? CascadedOptions { get; set; }

    /// <summary>App-wide settings (texts and defaults).</summary>
    protected HelixOptions HelixOptions => CascadedOptions ?? HelixOptions.Fallback;

    /// <summary>Default values set once for the whole app.</summary>
    protected HelixDefaults Defaults => HelixOptions.Defaults;

    /// <summary>The built-in texts, in the language of the UI culture unless the app chose them.</summary>
    protected HelixTexts Texts => HelixOptions.TextsFor(CultureInfo.CurrentUICulture);
}
