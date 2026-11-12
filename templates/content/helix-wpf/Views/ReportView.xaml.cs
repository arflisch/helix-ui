using System.Windows.Controls;
using Helix.Wpf;

namespace HelixApp.Views;

public partial class ReportView : UserControl
{
    public sealed record Order(DateTime Date, string Customer, string Product, string Status, HelixTone Tone, double Volume);

    private static readonly string[] Customers = ["Acme", "Globex", "Initech", "Umbrella"];
    private static readonly (string Status, HelixTone Tone)[] Statuses = [("Delivered", HelixTone.Success), ("Pending", HelixTone.Warning), ("Cancelled", HelixTone.Danger)];

    public ReportView()
    {
        InitializeComponent();

        // Replace with your data (a repository, an API…), or set Grid.ItemsProvider for server paging.
        Grid.Source = Enumerable.Range(0, 80).Select(i =>
        {
            var (status, tone) = Statuses[i % Statuses.Length];
            return new Order(DateTime.Today.AddDays(-i % 40), Customers[i % Customers.Length], i % 2 == 0 ? "Baseload" : "Peak", status, tone, 100 + i * 13 % 700);
        }).ToList();
    }
}
