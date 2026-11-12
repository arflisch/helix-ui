using Microsoft.Extensions.DependencyInjection;

namespace Helix.Blazor;

public sealed record HelixToast(Guid Id, string Message, string? Title, HelixSeverity Severity, TimeSpan Duration);

/// <summary>Shows notifications from anywhere: inject it and call <c>Success("Saved")</c>.</summary>
public sealed class HelixToastService : IDisposable
{
    private readonly List<HelixToast> _toasts = [];
    private readonly Lock _gate = new();
    private readonly CancellationTokenSource _disposed = new();

    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromSeconds(4);

    public event Action? Changed;

    public IReadOnlyList<HelixToast> Toasts
    {
        get
        {
            lock (_gate)
            {
                return _toasts.ToArray();
            }
        }
    }

    /// <param name="duration">TimeSpan.Zero keeps the toast until it is closed.</param>
    public Guid Show(string message, HelixSeverity severity = HelixSeverity.Info, string? title = null, TimeSpan? duration = null)
    {
        var toast = new HelixToast(Guid.NewGuid(), message, title, severity, duration ?? DefaultDuration);
        lock (_gate)
        {
            _toasts.Add(toast);
        }

        Changed?.Invoke();

        if (toast.Duration > TimeSpan.Zero)
        {
            _ = DismissLaterAsync(toast.Id, toast.Duration);
        }

        return toast.Id;
    }

    public Guid Info(string message, string? title = null) => Show(message, HelixSeverity.Info, title);
    public Guid Success(string message, string? title = null) => Show(message, HelixSeverity.Success, title);
    public Guid Warning(string message, string? title = null) => Show(message, HelixSeverity.Warning, title);
    public Guid Error(string message, string? title = null) => Show(message, HelixSeverity.Danger, title);

    public void Dismiss(Guid id)
    {
        bool removed;
        lock (_gate)
        {
            removed = _toasts.RemoveAll(t => t.Id == id) > 0;
        }

        if (removed)
        {
            Changed?.Invoke();
        }
    }

    private async Task DismissLaterAsync(Guid id, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay, _disposed.Token).ConfigureAwait(false);
            Dismiss(id);
        }
        catch (OperationCanceledException)
        {
            // The circuit is gone: nothing left to dismiss.
        }
    }

    public void Dispose()
    {
        _disposed.Cancel();
        _disposed.Dispose();
    }
}

public static class HelixServiceCollectionExtensions
{
    /// <summary>
    /// Registers toasts, confirmations and the app-wide <see cref="HelixOptions"/> (texts and component defaults).
    /// </summary>
    public static IServiceCollection AddHelix(this IServiceCollection services, Action<HelixOptions>? configure = null)
    {
        var options = new HelixOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddScoped(_ => new HelixToastService { DefaultDuration = options.ToastDuration });
        services.AddScoped<HelixConfirmService>();
        services.AddScoped<HelixDialogService>();
        services.AddScoped(sp => new HelixSettingsService(
            options.SettingsStore?.Invoke(sp) ?? new HelixLocalStorageSettingsStore(sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>())));
        return services;
    }
}
