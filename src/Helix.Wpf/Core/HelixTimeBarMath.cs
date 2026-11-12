namespace Helix.Wpf;

/// <summary>Where "now" stands in a countdown: the deadline counted to, the time left and the elapsed share.</summary>
/// <param name="Now">The time used for the computation.</param>
/// <param name="From">Start of the counted period (the previous deadline, or Start).</param>
/// <param name="Until">The deadline counted to.</param>
/// <param name="Remaining">Time left until <paramref name="Until"/> (zero once passed).</param>
/// <param name="Progress">Elapsed share of the period, 0 to 1.</param>
/// <param name="Passed">A fixed deadline is over (never true for a repeating one).</param>
public sealed record HelixTimeBarState(DateTime Now, DateTime From, DateTime Until, TimeSpan Remaining, double Progress, bool Passed);

/// <summary>The time math behind <see cref="HelixTimeBar"/>, usable on its own (view models, tests).</summary>
public static class HelixTimeBarMath
{
    /// <summary>Start of the slot that contains <paramref name="time"/>: slots of <paramref name="step"/> from midnight + <paramref name="offset"/>.</summary>
    public static DateTime SlotStart(DateTime time, TimeSpan step, TimeSpan offset = default)
    {
        if (step <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(step));
        var origin = time.Date + offset;
        if (origin > time) origin -= TimeSpan.FromDays(1);
        var slots = (time - origin).Ticks / step.Ticks;
        return origin + TimeSpan.FromTicks(slots * step.Ticks);
    }

    /// <summary>
    /// A deadline that repeats every <paramref name="step"/>: the end of each slot, minus <paramref name="lead"/>
    /// (e.g. step 15 min, lead 5 min: deadlines at :10, :25, :40, :55).
    /// </summary>
    public static HelixTimeBarState Repeating(DateTime now, TimeSpan step, TimeSpan lead = default, TimeSpan offset = default)
    {
        var until = SlotStart(now, step, offset) + step - lead;
        while (until <= now) until += step;
        while (until - step > now) until -= step;
        var remaining = until - now;
        return new HelixTimeBarState(now, until - step, until, remaining, Math.Clamp(1 - remaining / step, 0, 1), false);
    }

    /// <summary>A single deadline: progress from <paramref name="start"/> to <paramref name="deadline"/>.</summary>
    public static HelixTimeBarState Fixed(DateTime now, DateTime start, DateTime deadline)
    {
        var span = deadline - start;
        var remaining = deadline > now ? deadline - now : TimeSpan.Zero;
        var progress = span <= TimeSpan.Zero ? 1 : Math.Clamp((now - start) / span, 0, 1);
        return new HelixTimeBarState(now, start, deadline, remaining, progress, now >= deadline);
    }

    /// <summary>"04:32", or "1:04:32" from one hour, or "2d 03:04" from one day.</summary>
    public static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
        // Round up so the countdown shows 00:00 only once the deadline is reached.
        var seconds = (long)Math.Ceiling(remaining.TotalSeconds - 0.0005);
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalDays >= 1 ? $"{(int)t.TotalDays}d {t.Hours:00}:{t.Minutes:00}"
            : t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes:00}:{t.Seconds:00}";
    }
}

/// <summary>A slot shown by <see cref="HelixTimeBar"/>: <paramref name="Offset"/> is 0 for the current slot, -1 before, +1 after…</summary>
public sealed record HelixTimeBarSlot(DateTime Start, DateTime End, int Offset)
{
    public bool IsCurrent => Offset == 0;
    public string Label(string format = "HH:mm") => $"{Start.ToString(format)}–{End.ToString(format)}";
}
