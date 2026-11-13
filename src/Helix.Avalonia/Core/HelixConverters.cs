using Avalonia.Data.Converters;

namespace Helix.Avalonia;

/// <summary>Small converters used by the templates (and usable in yours).</summary>
public static class HelixConverters
{
    public static readonly IValueConverter IsSuccess = new FuncValueConverter<HelixSeverity, bool>(s => s == HelixSeverity.Success);
    public static readonly IValueConverter IsWarning = new FuncValueConverter<HelixSeverity, bool>(s => s == HelixSeverity.Warning);
    public static readonly IValueConverter IsDanger = new FuncValueConverter<HelixSeverity, bool>(s => s == HelixSeverity.Danger);
}
