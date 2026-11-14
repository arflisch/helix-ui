using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class DataPage : UserControl
{
    public sealed record Plant(int Id, string Name, string Site, double Power, string Status, HelixTone Tone, DateTime Updated, double Change = 0);

    private static readonly string[] Sites = ["Lyon", "Grenoble", "Marseille", "Bordeaux", "Lille", "Nantes", "Toulouse"];
    private static readonly (string Status, HelixTone Tone)[] Statuses = [("Running", HelixTone.Success), ("Maintenance", HelixTone.Warning), ("Stopped", HelixTone.Danger), ("Planned", HelixTone.Info)];

    // A "server" with 10 000 rows.
    private static readonly List<Plant> Database = Enumerable.Range(1, 10_000).Select(Make).ToList();

    public DataPage()
    {
        InitializeComponent();
        // A toggle filter with its own rule (Path works too for a bool property).
        HighPower.Rule = row => row is Plant { Power: >= 100 };
        Grid.Source = Enumerable.Range(1, 46).Select(Make).ToList();
        GroupedGrid.Source = Enumerable.Range(1, 18).Select(Make).ToList();
        Filters.Presets = new System.Collections.ObjectModel.ObservableCollection<HelixFilterPreset>
        {
            new("In maintenance", new Dictionary<string, string> { ["status"] = "[\"Maintenance\"]" }),
        };
        ServerGrid.ItemsProvider = QueryAsync;
    }

    private static Plant Make(int i)
    {
        var (status, tone) = Statuses[i * 7 % Statuses.Length];
        return new Plant(i, $"Unit {i:000}", Sites[i * 3 % Sites.Length], 20 + i * 37 % 480, status, tone, DateTime.Today.AddHours(-i * 5 % 96), Math.Round((i * 13 % 21 - 10) * 0.7, 1));
    }

    private static async Task<HelixGridPage> QueryAsync(HelixGridRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(600, cancellationToken); // network
        IEnumerable<Plant> rows = Database;
        if (request.Search is { } search)
        {
            rows = rows.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || r.Site.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        rows = request.SortBy switch
        {
            "Name" => request.Descending ? rows.OrderByDescending(r => r.Name) : rows.OrderBy(r => r.Name),
            "Site" => request.Descending ? rows.OrderByDescending(r => r.Site) : rows.OrderBy(r => r.Site),
            "Power" => request.Descending ? rows.OrderByDescending(r => r.Power) : rows.OrderBy(r => r.Power),
            "Id" => request.Descending ? rows.OrderByDescending(r => r.Id) : rows.OrderBy(r => r.Id),
            _ => rows,
        };

        var list = rows.ToList();
        var page = list.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Cast<object>().ToList();
        return new HelixGridPage(page, list.Count);
    }

    private void OnDensity(object sender, SelectionChangedEventArgs e)
    {
        if (Grid is not null)
        {
            Grid.Density = Density.SelectedIndex switch { 0 => HelixSize.Sm, 2 => HelixSize.Lg, _ => HelixSize.Md };
        }
    }
}
