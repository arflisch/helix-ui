using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace Helix.Blazor;

/// <summary>A filter of a <see cref="HelixFilterBar{TItem}"/>: a key, and how to turn its encoded value into a rule.</summary>
public interface IHelixFilter<TItem>
{
    /// <summary>Stable key of the value in the bar's State (and in saved filters).</summary>
    string Key { get; }

    /// <summary>The rule for an encoded value; null keeps every row.</summary>
    Func<TItem, bool>? CreatePredicate(string value);
}

/// <summary>Base of the built-in filters: registers with the bar and reads / writes its value.</summary>
public abstract class HelixFilter<TItem> : HelixComponentBase, IHelixFilter<TItem>, IDisposable
{
    [CascadingParameter] protected HelixFilterBar<TItem>? Bar { get; set; }

    /// <summary>Stable key of the value (State, saved filters, server requests).</summary>
    [Parameter, EditorRequired] public string Key { get; set; } = "";

    [Parameter] public string? Label { get; set; }

    /// <summary>Width of the filter's field, e.g. "220px".</summary>
    [Parameter] public string? Width { get; set; }

    string IHelixFilter<TItem>.Key => Key;

    protected string? Value => Bar?.ValueOf(Key);

    protected string? FieldStyle => new StyleBuilder().Var("width", Width).Raw(Style).Build();

    protected override void OnInitialized()
    {
        if (Bar is null) throw new InvalidOperationException($"{GetType().Name} must be placed inside a HelixFilterBar.");
        Bar.Register(this);
    }

    protected Task Set(string? value) => Bar?.SetValue(Key, value) ?? Task.CompletedTask;

    public abstract Func<TItem, bool>? CreatePredicate(string value);

    public void Dispose()
    {
        Bar?.Unregister(this);
        GC.SuppressFinalize(this);
    }

    /// <summary>Encodes a list of strings as a JSON array (one value of the State).</summary>
    protected static string EncodeList(IEnumerable<string> values) => JsonSerializer.Serialize(values);

    protected static List<string> DecodeList(string? value)
    {
        if (string.IsNullOrEmpty(value)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(value) ?? [];
        }
        catch (JsonException)
        {
            return [value];
        }
    }

    protected static string EncodeRange(HelixDateRange range) =>
        $"{range.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}/{range.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

    protected static HelixDateRange? DecodeRange(string? value)
    {
        var parts = value?.Split('/');
        return parts is { Length: 2 }
            && DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
            && DateOnly.TryParseExact(parts[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)
            ? new HelixDateRange(start, end) : null;
    }
}
