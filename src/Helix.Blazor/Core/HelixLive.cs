namespace Helix.Blazor;

/// <summary>
/// Collects items coming from any thread (SignalR, timers, queues) and publishes them in batches, every <see cref="Interval"/>:
/// grids, charts and logs re-render a few times per second instead of once per item. Optionally keeps only the last <see cref="MaxCount"/> items.
/// <code>live.Changed += () => InvokeAsync(StateHasChanged);   … live.Add(entry);   … &lt;HelixLogViewer Entries="live.Items" /&gt;</code>
/// </summary>
public sealed class HelixLiveCollection<T> : IDisposable
{
    private readonly object _gate = new();
    private readonly List<T> _pending = [];
    private readonly Timer _timer;
    private IReadOnlyList<T> _items = [];
    private bool _dirty;

    public HelixLiveCollection(TimeSpan? interval = null, int? maxCount = null)
    {
        Interval = interval ?? TimeSpan.FromMilliseconds(250);
        MaxCount = maxCount;
        _timer = new Timer(_ => Flush(), null, Interval, Interval);
    }

    /// <summary>How often new items are published (250 ms).</summary>
    public TimeSpan Interval { get; }

    /// <summary>Keeps only the newest items (null: all).</summary>
    public int? MaxCount { get; }

    /// <summary>The published items, oldest first: a new list after each batch (safe to bind and to enumerate).</summary>
    public IReadOnlyList<T> Items => _items;

    /// <summary>Raised after a batch was published (from a timer thread: use InvokeAsync).</summary>
    public event Action? Changed;

    public void Add(T item)
    {
        lock (_gate) _pending.Add(item);
    }

    public void AddRange(IEnumerable<T> items)
    {
        lock (_gate) _pending.AddRange(items);
    }

    /// <summary>Replaces everything (e.g. after a reload).</summary>
    public void Reset(IEnumerable<T> items)
    {
        lock (_gate)
        {
            _pending.Clear();
            _items = Trim(items.ToList());
            _dirty = true;
        }
    }

    /// <summary>Publishes the pending items now.</summary>
    public void Flush()
    {
        lock (_gate)
        {
            if (_pending.Count == 0 && !_dirty) return;
            if (_pending.Count > 0)
            {
                var next = new List<T>(_items.Count + _pending.Count);
                next.AddRange(_items);
                next.AddRange(_pending);
                _pending.Clear();
                _items = Trim(next);
            }

            _dirty = false;
        }

        Changed?.Invoke();
    }

    private IReadOnlyList<T> Trim(List<T> items) =>
        MaxCount is { } max && items.Count > max ? items.GetRange(items.Count - max, max) : items;

    public void Dispose() => _timer.Dispose();
}

/// <summary>Time slots of a day (quarter hours, hours…), correct on daylight saving days (23 or 25 hours).</summary>
public static class HelixTimeSlots
{
    /// <summary>
    /// The slots of a local day in a time zone, at the given step: 96 quarter hours on a normal day, 92 or 100 on DST days.
    /// Each slot has its local start and end (an autumn repeated hour appears twice, with different UTC instants).
    /// </summary>
    public static IReadOnlyList<HelixDaySlot> ForDay(DateOnly day, TimeSpan step, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Local;
        if (step <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(step));
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
        var slots = new List<HelixDaySlot>();
        for (var t = startUtc; t < endUtc; t += step)
        {
            var next = t + step < endUtc ? t + step : endUtc;
            slots.Add(new HelixDaySlot(slots.Count, t, next, TimeZoneInfo.ConvertTimeFromUtc(t, zone), TimeZoneInfo.ConvertTimeFromUtc(next, zone)));
        }

        return slots;
    }

    /// <summary>Number of hours of a local day: 24, or 23 / 25 on DST days.</summary>
    public static double HoursIn(DateOnly day, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Local;
        var start = TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone);
        var end = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue), zone);
        return (end - start).TotalHours;
    }

    /// <summary>Start of the slot containing a time, e.g. 14:37 → 14:30 with a 15-minute step.</summary>
    public static DateTime Floor(DateTime time, TimeSpan step) => new(time.Ticks - time.Ticks % step.Ticks, time.Kind);
}

/// <summary>A time slot: its index in the day, UTC instants and local times.</summary>
public sealed record HelixDaySlot(int Index, DateTime StartUtc, DateTime EndUtc, DateTime Start, DateTime End)
{
    /// <summary>"14:00–14:15".</summary>
    public string Label(string format = "HH:mm") => $"{Start.ToString(format)}–{End.ToString(format)}";
}
