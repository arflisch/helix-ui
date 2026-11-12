using System.Text.Json;
using Microsoft.JSInterop;

namespace Helix.Blazor;

/// <summary>Where the components remember their settings (RememberAs). Text values; implement it for a database or an API, per user.</summary>
public interface IHelixSettingsStore
{
    ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Saves a value; null removes it.</summary>
    ValueTask SetAsync(string key, string? value, CancellationToken cancellationToken = default);
}

/// <summary>The default store: the browser's localStorage, keys prefixed with "helix:".</summary>
public sealed class HelixLocalStorageSettingsStore(IJSRuntime js) : IHelixSettingsStore
{
    private const string Prefix = "helix:";

    public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", cancellationToken, Prefix + key);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
            return null; // prerendering, private mode or a closed circuit: nothing remembered
        }
    }

    public async ValueTask SetAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        try
        {
            if (value is null) await js.InvokeVoidAsync("localStorage.removeItem", cancellationToken, Prefix + key);
            else await js.InvokeVoidAsync("localStorage.setItem", cancellationToken, Prefix + key, value);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
            // Storage unavailable: the setting simply isn't kept.
        }
    }
}

/// <summary>
/// Reads and writes the remembered settings of the components (RememberAs="…") as JSON, through the app's store.
/// Also usable from your own code: await settings.SaveAsync("my-page", state).
/// </summary>
public sealed class HelixSettingsService(IHelixSettingsStore store)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public IHelixSettingsStore Store { get; } = store;

    public async Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await Store.GetAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, Json);
        }
        catch (JsonException)
        {
            return default; // an older or corrupted value: start from the defaults
        }
    }

    public Task SaveAsync<T>(string key, T? value, CancellationToken cancellationToken = default) =>
        Store.SetAsync(key, value is null ? null : JsonSerializer.Serialize(value, Json), cancellationToken).AsTask();
}
