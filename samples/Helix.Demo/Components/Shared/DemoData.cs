using Helix.Blazor;

namespace Helix.Demo.Components.Shared;

public sealed record Ticket(int Id, string Title, string Column, string Assignee, string Priority, int Points);

public sealed record Priority(string Id, string Title, string Owner);

/// <summary>Small deterministic datasets for the interaction demos.</summary>
public static class DemoData
{
    public static readonly IReadOnlyList<HelixKanbanColumn> KanbanColumns =
    [
        new("todo", "To do", "neutral"),
        new("doing", "In progress", "var(--helix-series-1)", Limit: 3),
        new("review", "In review", "var(--helix-series-7)", Limit: 2),
        new("done", "Done", "success"),
    ];

    public static List<Ticket> Tickets() =>
    [
        new(1, "Connect the forecast API", "todo", "Ada Lovelace", "High", 5),
        new(2, "Alert above 300 MW", "todo", "Alan Turing", "Medium", 3),
        new(3, "Schedule CSV export", "todo", "Grace Hopper", "Low", 2),
        new(4, "Dark mode for charts", "doing", "Linus Torvalds", "Medium", 3),
        new(5, "Schedule drag and drop", "doing", "Ada Lovelace", "High", 8),
        new(6, "Customizable dashboard", "review", "Margaret Hamilton", "High", 5),
        new(7, "Color-blind safe palette", "done", "Barbara Liskov", "Medium", 2),
        new(8, "DataGrid server-side search", "done", "Ken Thompson", "High", 5),
    ];

    public static List<Priority> Priorities() =>
    [
        new("p1", "Stabilize evening balancing", "Operations"),
        new("p2", "Reduce rejected requests", "Dispatching"),
        new("p3", "Automate hourly forecasts", "Data"),
        new("p4", "Train the on-call team", "HR"),
        new("p5", "Renew the reserve contract", "Purchasing"),
    ];

    public static readonly string[] Days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    /// <summary>Average imbalance (MW) per weekday and hour: a diverging dataset (deficit / surplus).</summary>
    public static IReadOnlyList<HelixHeatCell> WeeklyImbalance()
    {
        var cells = new List<HelixHeatCell>();
        for (var d = 0; d < 7; d++)
        {
            for (var h = 0; h < 24; h++)
            {
                var weekend = d >= 5 ? -60 : 0;
                var value = 220 * Math.Sin((h - 7) / 24.0 * Math.PI * 2) + 80 * Math.Sin(h / 2.3 + d) + weekend;
                cells.Add(new HelixHeatCell(Days[d], $"{h:00}h", d == 6 && h is 3 or 4 ? null : Math.Round(value)));
            }
        }

        return cells;
    }

    /// <summary>Number of activations per weekday and hour: a sequential dataset (only magnitude).</summary>
    public static IReadOnlyList<HelixHeatCell> Activations()
    {
        var cells = new List<HelixHeatCell>();
        for (var d = 0; d < 7; d++)
        {
            for (var h = 6; h < 23; h++)
            {
                var peak = Math.Exp(-Math.Pow(h - 18.5, 2) / 6) * 14 + Math.Exp(-Math.Pow(h - 8, 2) / 3) * 9;
                cells.Add(new HelixHeatCell(Days[d], $"{h:00}h", Math.Round(peak * (d >= 5 ? 0.6 : 1) + (d * 7 + h) % 3)));
            }
        }

        return cells;
    }

    public static IReadOnlyList<double?> Trend(int seed, int count = 14, double start = 100, double drift = 1.5) =>
        Enumerable.Range(0, count).Select(i => (double?)Math.Round(start + i * drift + Math.Sin(i * 1.7 + seed) * 6, 1)).ToList();
}
