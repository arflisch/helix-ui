namespace Helix.Blazor;

public enum HelixRadioAppearance { Default, Cards, Segmented }

public enum HelixDrawerPosition { Right, Left, Bottom }

/// <summary>Donut, or a tall segmented bar (easier to read for "how does the whole split").</summary>
public enum HelixPartShape { Donut, SegmentedBar }

public enum HelixTreeLayout { List, Diagram }

/// <summary>One part of a <see cref="HelixDonut"/>.</summary>
public sealed record HelixPart(string Label, double Value, string? Color = null);

/// <summary>A row of a <see cref="HelixBarList"/>.</summary>
public sealed record HelixBarItem(string Label, double Value)
{
    public string? Href { get; init; }
    public string? Icon { get; init; }
}

/// <summary>From this value on, the gauge takes this color and label (e.g. 80 → "Critique").</summary>
public sealed record HelixGaugeThreshold(double Value, string Color, string Label);

public sealed record HelixBreadcrumbItem(string Text, string? Href = null, string? Icon = null);

public sealed record HelixStep(string Title, string? Description = null);

/// <summary>What <see cref="HelixConfirmService.AskAsync(HelixConfirmOptions, CancellationToken)"/> shows.</summary>
public sealed record HelixConfirmOptions(string Title)
{
    public string? Message { get; init; }
    /// <summary>Null: the built-in "Confirm" text, in the app's language.</summary>
    public string? ConfirmText { get; init; }

    /// <summary>Null: the built-in "Cancel" text, in the app's language.</summary>
    public string? CancelText { get; init; }

    /// <summary>Red confirm button, for destructive actions.</summary>
    public bool Danger { get; init; }
}

internal static class HelixPaging
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

/// <summary>Promise-style confirmation: if (await Confirm.AskAsync("Delete?")) …</summary>
public sealed class HelixConfirmService : IDisposable
{
    private TaskCompletionSource<bool>? _pending;
    private CancellationTokenRegistration _registration;

    internal HelixConfirmOptions? Current { get; private set; }

    internal event Action? Changed;

    public Task<bool> AskAsync(string title, string? message = null, bool danger = false, string? confirmText = null) =>
        AskAsync(new HelixConfirmOptions(title) { Message = message, Danger = danger, ConfirmText = confirmText });

    /// <summary>Shows the dialog and completes with true (confirmed) or false (cancelled, closed, or token cancelled).</summary>
    public Task<bool> AskAsync(HelixConfirmOptions options, CancellationToken cancellationToken = default)
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

/// <summary>A period of whole days, both ends included.</summary>
public sealed record HelixDateRange(DateOnly Start, DateOnly End)
{
    /// <summary>Number of days, both ends included.</summary>
    public int Days => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    /// <summary>Start at 00:00 and the day after End at 00:00: handy for "from &lt;= t &lt; to" queries.</summary>
    public (DateTime From, DateTime To) ToDateTimes() => (Start.ToDateTime(TimeOnly.MinValue), End.AddDays(1).ToDateTime(TimeOnly.MinValue));
}

/// <summary>A shortcut of HelixDateRangePicker, e.g. "Last 7 days". Range receives today.</summary>
public sealed record HelixDateRangePreset(string Label, Func<DateOnly, HelixDateRange> Range);

/// <summary>A named set of filter values (key → encoded value), saved by the user in a <see cref="HelixFilterBar{TItem}"/>.</summary>
public sealed record HelixFilterPreset(string Name, IReadOnlyDictionary<string, string> State);

/// <summary>A zone behind a <see cref="HelixTargetBar"/>, e.g. the tolerance around the target.</summary>
public sealed record HelixTargetBand(double From, double To, string Color, string? Label = null);

public enum HelixLogLevel { Debug, Info, Warning, Error }

/// <summary>A line of a <see cref="HelixLogViewer"/>.</summary>
public sealed record HelixLogEntry(DateTime Time, HelixLogLevel Level, string Message, string? Source = null);
