namespace Velo.Blazor;

public enum VeloRadioAppearance { Default, Cards, Segmented }

public enum VeloDrawerPosition { Right, Left, Bottom }

/// <summary>A row of a <see cref="VeloBarList"/>.</summary>
public sealed record VeloBarItem(string Label, double Value)
{
    public string? Href { get; init; }
    public string? Icon { get; init; }
}

/// <summary>From this value on, the gauge takes this color and label (e.g. 80 → "Critique").</summary>
public sealed record VeloGaugeThreshold(double Value, string Color, string Label);

public sealed record VeloBreadcrumbItem(string Text, string? Href = null, string? Icon = null);

public sealed record VeloStep(string Title, string? Description = null);

/// <summary>What <see cref="VeloConfirmService.AskAsync(VeloConfirmOptions, CancellationToken)"/> shows.</summary>
public sealed record VeloConfirmOptions(string Title)
{
    public string? Message { get; init; }
    public string ConfirmText { get; init; } = "Confirmer";
    public string CancelText { get; init; } = "Annuler";

    /// <summary>Red confirm button, for destructive actions.</summary>
    public bool Danger { get; init; }
}

internal static class VeloPaging
{
    /// <summary>0-based page indexes to show; -1 is an ellipsis. First, last and neighbours of the current page.</summary>
    public static List<int> Window(int count, int current)
    {
        if (count <= 7)
        {
            return Enumerable.Range(0, count).ToList();
        }

        var start = Math.Clamp(current - 1, 1, count - 4);
        var end = Math.Clamp(current + 1, 3, count - 2);
        var pages = new List<int> { 0 };
        if (start > 1)
        {
            pages.Add(-1);
        }

        for (var p = start; p <= end; p++)
        {
            pages.Add(p);
        }

        if (end < count - 2)
        {
            pages.Add(-1);
        }

        pages.Add(count - 1);
        return pages;
    }

    public static string FileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} o",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} Ko",
        < 1024L * 1024 * 1024 => $"{bytes / 1024.0 / 1024:0.#} Mo",
        _ => $"{bytes / 1024.0 / 1024 / 1024:0.#} Go",
    };
}

/// <summary>Promise-style confirmation: if (await Confirm.AskAsync("Supprimer ?")) …</summary>
public sealed class VeloConfirmService : IDisposable
{
    private TaskCompletionSource<bool>? _pending;
    private CancellationTokenRegistration _registration;

    internal VeloConfirmOptions? Current { get; private set; }

    internal event Action? Changed;

    public Task<bool> AskAsync(string title, string? message = null, bool danger = false, string? confirmText = null) =>
        AskAsync(new VeloConfirmOptions(title) { Message = message, Danger = danger, ConfirmText = confirmText ?? "Confirmer" });

    /// <summary>Shows the dialog and completes with true (confirmed) or false (cancelled, closed, or token cancelled).</summary>
    public Task<bool> AskAsync(VeloConfirmOptions options, CancellationToken cancellationToken = default)
    {
        Resolve(false); // only one question at a time: a new one cancels the previous
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = pending;
        Current = options;
        _registration = cancellationToken.Register(() => Resolve(false));
        Changed?.Invoke();
        return pending.Task;
    }

    internal void Resolve(bool confirmed)
    {
        var pending = _pending;
        if (pending is null)
        {
            return;
        }

        _pending = null;
        Current = null;
        _registration.Dispose();
        Changed?.Invoke();
        pending.TrySetResult(confirmed);
    }

    public void Dispose() => Resolve(false);
}
