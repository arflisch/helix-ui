namespace Helix.Avalonia;

/// <summary>Extra hints for <see cref="HelixAutoForm"/> on a property (all optional).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class HelixFieldAttribute : Attribute
{
    /// <summary>Unit shown after a number, e.g. "MW".</summary>
    public string? Unit { get; set; }

    public string? Placeholder { get; set; }

    /// <summary>Columns taken in the form (e.g. 2 for a long text).</summary>
    public int Span { get; set; } = 1;

    /// <summary>Step of a number field (− / + and arrows).</summary>
    public double Step { get; set; } = 1;

    /// <summary>Decimals kept by a number field (-1: no rounding).</summary>
    public int Decimals { get; set; } = -1;

    /// <summary>Date and time picker for a DateTime (true), or a date only (false).</summary>
    public bool WithTime { get; set; } = true;

    /// <summary>Hides the property from the form.</summary>
    public bool Hidden { get; set; }
}
