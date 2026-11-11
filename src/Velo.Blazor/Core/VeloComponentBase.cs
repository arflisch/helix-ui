using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Velo.Blazor;

/// <summary>Common parameters shared by every Velo component.</summary>
public abstract class VeloComponentBase : ComponentBase
{
    /// <summary>Extra CSS classes added to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Extra inline style added to the root element (e.g. "--velo-switch-thumb: red").</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Any other HTML attribute (id, aria-*, data-*, title...) is forwarded.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? Attributes { get; set; }

    /// <summary>Set by <see cref="VeloProvider"/>.</summary>
    [CascadingParameter] private VeloOptions? CascadedOptions { get; set; }

    /// <summary>App-wide settings (texts and defaults).</summary>
    protected VeloOptions VeloOptions => CascadedOptions ?? VeloOptions.Fallback;

    /// <summary>Default values set once for the whole app.</summary>
    protected VeloDefaults Defaults => VeloOptions.Defaults;

    /// <summary>The built-in texts, in the language of the UI culture unless the app chose them.</summary>
    protected VeloTexts Texts => VeloOptions.TextsFor(CultureInfo.CurrentUICulture);
}
