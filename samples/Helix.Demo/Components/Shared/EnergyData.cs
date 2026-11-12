using Helix.Blazor;

namespace Helix.Demo.Components.Shared;

/// <summary>Deterministic fake grid-balancing data for today, relative to the current time.</summary>
public static class EnergyData
{
    public static DateTime Today => DateTime.Today;

    private static double Noise(int i, int seed) => Math.Sin(i * 12.9898 + seed * 78.233) * 43758.5453 % 1;

    /// <summary>Measured imbalance (MW), every 15 min, until now.</summary>
    public static IReadOnlyList<HelixPoint> Imbalance(DateTime now) =>
        Enumerable.Range(0, 96).Select(i =>
        {
            var t = Today.AddMinutes(15 * i);
            double? value = t <= now ? Math.Round(Shape(i) + Noise(i, 1) * 60) : null;
            return HelixPoint.At(t, value);
        }).ToList();

    /// <summary>Forecast imbalance (MW) from one hour ago to the end of the day.</summary>
    public static IReadOnlyList<HelixPoint> Forecast(DateTime now) =>
        Enumerable.Range(0, 96).Select(i =>
        {
            var t = Today.AddMinutes(15 * i);
            double? value = t >= now.AddHours(-1) ? Math.Round(Shape(i) * 0.9 + 20) : null;
            return HelixPoint.At(t, value);
        }).ToList();

    /// <summary>Reserve activated by the operator (MW), held for an hour at a time.</summary>
    public static IReadOnlyList<HelixPoint> Activated(DateTime now) =>
        Enumerable.Range(0, 96).Select(i =>
        {
            var t = Today.AddMinutes(15 * i);
            var hourStart = i / 4 * 4;
            double? value = t <= now ? Math.Round(-Shape(hourStart) * 0.6 / 50) * 50 : null;
            return HelixPoint.At(t, value);
        }).ToList();

    /// <summary>Imbalance price (€/MWh), hourly.</summary>
    public static IReadOnlyList<HelixPoint> Prices() =>
        Enumerable.Range(0, 24).Select(h =>
            HelixPoint.At(Today.AddHours(h).AddMinutes(30), Math.Round(85 + 55 * Math.Sin((h - 7) / 24.0 * Math.PI * 2) + Noise(h, 3) * 25 - (h is 3 or 4 ? 140 : 0))))
        .ToList();

    private static double Shape(int quarter)
    {
        var hour = quarter / 4.0;
        return 260 * Math.Sin((hour - 6) / 24 * Math.PI * 2) + 140 * Math.Sin(hour / 3.1) - 60;
    }

    public static readonly IReadOnlyList<HelixTimelineStatus> Statuses =
    [
        new("direct-saved", "Direct request saved", "var(--helix-series-2)", HelixTimelineAppearance.Dashed),
        new("direct-sent", "Direct request sent", "var(--helix-series-2)", HelixTimelineAppearance.Outline),
        new("direct-ack", "Direct request confirmed", "var(--helix-series-2)", HelixTimelineAppearance.Filled),
        new("scheduled-saved", "Scheduled, saved", "var(--helix-series-1)", HelixTimelineAppearance.Dashed),
        new("scheduled-sent", "Scheduled, sent", "var(--helix-series-1)", HelixTimelineAppearance.Outline),
        new("scheduled-ack", "Scheduled, confirmed", "var(--helix-series-1)", HelixTimelineAppearance.Filled),
        new("rejected", "Rejected", "danger", HelixTimelineAppearance.Filled),
        new("overwritten", "Overwritten", "var(--helix-text-subtle)", HelixTimelineAppearance.Filled),
        new("local", "Local request", "var(--helix-series-4)", HelixTimelineAppearance.Filled),
    ];

    /// <summary>Dispatch requests over [start, start + hours], each one quarter-hour, status driven by time.</summary>
    public static IReadOnlyList<HelixTimelineItem> Demands(DateTime start, int hours, DateTime now)
    {
        var items = new List<HelixTimelineItem>();
        for (var q = 0; q < hours * 4; q++)
        {
            if (Noise(q, 7) < 0.25 && q % 3 != 0)
            {
                continue; // some quarters have no request
            }

            var from = start.AddMinutes(15 * q);
            var to = from.AddMinutes(q % 5 == 0 ? 30 : 15);
            var direct = q % 4 == 1;
            var phase = to <= now ? "ack" : from <= now ? "sent" : "saved";
            var status = q % 11 == 7 ? "rejected" : q % 13 == 5 ? "overwritten" : q % 9 == 4 ? "local" : $"{(direct ? "direct" : "scheduled")}-{phase}";
            var mw = Math.Round(Math.Abs(Shape(q + 40)) / 10, 1);
            items.Add(new HelixTimelineItem($"d{q}", from, to, $"SA: {mw:0.0} MW") { Status = status, Data = mw });
        }

        return items;
    }

    public static IReadOnlyList<HelixTimelineItem> UnitPlan(DateTime day)
    {
        (string Unit, int From, int To, string Status, string Label)[] plan =
        [
            ("Plant A", 0, 6, "scheduled-ack", "Base 120 MW"),
            ("Plant A", 6, 10, "scheduled-ack", "Ramp-up 180 MW"),
            ("Plant A", 17, 22, "scheduled-sent", "Peak 210 MW"),
            ("Plant B", 5, 9, "direct-ack", "Reserve 40 MW"),
            ("Plant B", 8, 14, "scheduled-saved", "Maintenance"),
            ("Plant B", 18, 21, "direct-sent", "Reserve 60 MW"),
            ("Battery", 2, 5, "local", "Charge 20 MW"),
            ("Battery", 11, 13, "direct-ack", "Discharge 25 MW"),
            ("Battery", 12, 15, "rejected", "Discharge 30 MW"),
            ("Battery", 19, 21, "scheduled-saved", "Discharge 25 MW"),
        ];

        return plan.Select((p, i) => new HelixTimelineItem($"u{i}", day.AddHours(p.From), day.AddHours(p.To), p.Label) { Lane = p.Unit, Status = p.Status }).ToList();
    }
}
