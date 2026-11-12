using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace Helix.Wpf;

/// <summary>
/// An ObservableCollection fed from any thread (SignalR, timers, queues): items are added on the UI thread in batches,
/// every <see cref="Interval"/>, so a bound grid, chart or log updates a few times per second instead of once per item.
/// Optionally keeps only the last <see cref="MaxCount"/> items.
/// </summary>
public sealed class HelixLiveCollection<T> : ObservableCollection<T>, IDisposable
{
    private readonly object _gate = new();
    private readonly List<T> _pending = [];
    private readonly DispatcherTimer _timer;
    private bool _batching;

    public HelixLiveCollection(TimeSpan? interval = null, int? maxCount = null, Dispatcher? dispatcher = null)
    {
        Interval = interval ?? TimeSpan.FromMilliseconds(250);
        MaxCount = maxCount;
        _timer = new DispatcherTimer(Interval, DispatcherPriority.Background, (_, _) => Flush(), dispatcher ?? Dispatcher.CurrentDispatcher);
        _timer.Start();
    }

    public TimeSpan Interval { get; }

    /// <summary>Keeps only the newest items (null: all).</summary>
    public int? MaxCount { get; }

    /// <summary>Queues an item, from any thread.</summary>
    public void Enqueue(T item)
    {
        lock (_gate) _pending.Add(item);
    }

    /// <summary>Queues items, from any thread.</summary>
    public void EnqueueRange(IEnumerable<T> items)
    {
        lock (_gate) _pending.AddRange(items);
    }

    /// <summary>Adds the queued items now (UI thread): one Reset notification for a large batch, else one per item.</summary>
    public void Flush()
    {
        List<T> batch;
        lock (_gate)
        {
            if (_pending.Count == 0) return;
            batch = [.. _pending];
            _pending.Clear();
        }

        if (batch.Count > 20)
        {
            _batching = true;
            foreach (var item in batch) Items.Add(item);
            Trim();
            _batching = false;
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        else
        {
            foreach (var item in batch) Add(item);
            while (MaxCount is { } max && Count > max) RemoveAt(0);
        }
    }

    private void Trim()
    {
        while (MaxCount is { } max && Items.Count > max) Items.RemoveAt(0);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_batching) base.OnCollectionChanged(e);
    }

    public void Dispose() => _timer.Stop();
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
