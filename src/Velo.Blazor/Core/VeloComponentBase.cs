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
}
