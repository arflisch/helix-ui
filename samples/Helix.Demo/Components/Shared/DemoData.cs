using Helix.Blazor;

namespace Helix.Demo.Components.Shared;

public sealed record Ticket(int Id, string Title, string Column, string Assignee, string Priority, int Points);

public sealed record Priority(string Id, string Title, string Owner);

/// <summary>Small deterministic datasets for the interaction demos.</summary>
public static class DemoData
{
    public static readonly IReadOnlyList<HelixKanbanColumn> KanbanColumns =
    [
        new("todo", "À faire", "neutral"),
        new("doing", "En cours", "var(--helix-series-1)", Limit: 3),
        new("review", "En revue", "var(--helix-series-7)", Limit: 2),
        new("done", "Terminé", "success"),
    ];

    public static List<Ticket> Tickets() =>
    [
        new(1, "Brancher l'API de prévision", "todo", "Ada Lovelace", "Haute", 5),
        new(2, "Alerte au-delà de 300 MW", "todo", "Alan Turing", "Moyenne", 3),
        new(3, "Export CSV du planning", "todo", "Grace Hopper", "Basse", 2),
        new(4, "Mode sombre des graphiques", "doing", "Linus Torvalds", "Moyenne", 3),
        new(5, "Glisser-déposer du planning", "doing", "Ada Lovelace", "Haute", 8),
        new(6, "Tableau de bord personnalisable", "review", "Margaret Hamilton", "Haute", 5),
        new(7, "Palette validée daltonisme", "done", "Barbara Liskov", "Moyenne", 2),
        new(8, "Recherche serveur du DataGrid", "done", "Ken Thompson", "Haute", 5),
    ];

    public static List<Priority> Priorities() =>
    [
        new("p1", "Stabiliser l'équilibrage du soir", "Exploitation"),
        new("p2", "Réduire les demandes rejetées", "Dispatching"),
        new("p3", "Automatiser les prévisions horaires", "Data"),
        new("p4", "Former l'équipe d'astreinte", "RH"),
        new("p5", "Renouveler le contrat de réserve", "Achats"),
    ];

    public static readonly string[] Days = ["Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim"];

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
