using System.Globalization;

namespace Velo.Demo.Components.Shared;

public sealed record ApiRow(string Param, string Type, string? CssVar, string Description);

/// <summary>Helpers to generate the Razor code shown under each playground.</summary>
public static class Snippet
{
    /// <summary>Attribute only when it differs from the default.</summary>
    public static string A(string name, string? value, string? defaultValue = null) =>
        string.IsNullOrEmpty(value) || value == defaultValue ? "" : $" {name}=\"{value}\"";

    public static string A<TEnum>(string name, TEnum value, TEnum defaultValue) where TEnum : struct, Enum =>
        EqualityComparer<TEnum>.Default.Equals(value, defaultValue) ? "" : $" {name}=\"{typeof(TEnum).Name}.{value}\"";

    public static string B(string name, bool value) => value ? $" {name}" : "";

    public static string N(double value) => value.ToString(CultureInfo.InvariantCulture);
}
