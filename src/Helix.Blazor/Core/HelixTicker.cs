namespace Helix.Blazor;

/// <summary>
/// Re-renders a component on a fixed period (the live "now" line).
/// Owned by the component: started after the first interactive render, cancelled on Dispose.
/// </summary>
internal sealed class HelixTicker : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private bool _started;

    public void Start(TimeSpan period, TimeProvider timeProvider, Func<Task> onTick)
    {
        if (_started)
        {
            return;
        }

        _started = true;
        // Not request work: its lifetime is the component's, and Dispose cancels it.
        _ = RunAsync(period, timeProvider, onTick, _cts.Token);
    }

    private static async Task RunAsync(TimeSpan period, TimeProvider timeProvider, Func<Task> onTick, CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(period, timeProvider);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await onTick();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Component disposed.
        }
        catch (ObjectDisposedException)
        {
            // The renderer went away between the tick and the render (circuit closed).
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
