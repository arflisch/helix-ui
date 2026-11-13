using Avalonia.Data.Converters;

namespace Helix.Avalonia;

/// <summary>Small converters used by the templates (and usable in yours).</summary>
public static class HelixConverters
{
    public static readonly IValueConverter IsSuccess = new FuncValueConverter<HelixSeverity, bool>(s => s == HelixSeverity.Success);
    public static readonly IValueConverter IsWarning = new FuncValueConverter<HelixSeverity, bool>(s => s == HelixSeverity.Warning);
    public static readonly IValueConverter IsDanger = new FuncValueConverter<HelixSeverity, bool>(s => s == HelixSeverity.Danger);

    /// <summary>A width times a share (the parameter, 0 to 1).</summary>
    public static readonly IMultiValueConverter Share = new FuncMultiValueConverter<object?, double>(values =>
    {
        var v = values.ToList();
        return v.Count == 2 && v[0] is double width && v[1] is double share ? Math.Max(0, width * share) : 0;
    });
}
