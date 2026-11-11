using Microsoft.Extensions.DependencyInjection;

namespace Velo.Blazor;

public sealed record VeloToast(Guid Id, string Message, string? Title, VeloSeverity Severity, TimeSpan Duration);

/// <summary>Shows notifications from anywhere: inject it and call <c>Success("Saved")</c>.</summary>
public sealed class VeloToastService : IDisposable
{
    private readonly List<VeloToast> _toasts = [];
    private readonly Lock _gate = new();
    private readonly CancellationTokenSource _disposed = new();

    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromSeconds(4);

    public event Action? Changed;

    public IReadOnlyList<VeloToast> Toasts
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
    public Guid Show(string message, VeloSeverity severity = VeloSeverity.Info, string? title = null, TimeSpan? duration = null)
    {
        var toast = new VeloToast(Guid.NewGuid(), message, title, severity, duration ?? DefaultDuration);
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

    public Guid Info(string message, string? title = null) => Show(message, VeloSeverity.Info, title);
    public Guid Success(string message, string? title = null) => Show(message, VeloSeverity.Success, title);
    public Guid Warning(string message, string? title = null) => Show(message, VeloSeverity.Warning, title);
    public Guid Error(string message, string? title = null) => Show(message, VeloSeverity.Danger, title);

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

public static class VeloServiceCollectionExtensions
{
    /// <summary>Registers the Velo services (toasts). One line in Program.cs.</summary>
    public static IServiceCollection AddVelo(this IServiceCollection services, Action<VeloToastService>? configureToasts = null)
    {
        services.AddScoped(_ =>
        {
            var toasts = new VeloToastService();
            configureToasts?.Invoke(toasts);
            return toasts;
        });
        services.AddScoped<VeloConfirmService>();
        return services;
    }
}
