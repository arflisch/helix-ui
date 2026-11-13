using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public sealed record Employee(string Name, string Department, string Status, decimal Salary, DateTime Start)
{
    public HelixTone Tone => Status switch { "Active" => HelixTone.Success, "Remote" => HelixTone.Info, "Leave" => HelixTone.Warning, _ => HelixTone.Danger };
}

public sealed record Order(string Product, string Region, double Volume, double Price);

public partial class DataPage : UserControl
{
    public DataPage()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<HelixDataGrid>("Employees")!.Source = SampleEmployees();
        this.FindControl<HelixDataGrid>("Orders")!.Source = new List<Order>
        {
            new("Baseload", "East", 1450, 78.5), new("Peak", "East", 540, 95.0), new("Off-peak", "East", 410, 61.8),
            new("Baseload", "North", 1240, 82.4), new("Peak", "North", 610, 97.9), new("Off-peak", "North", 380, 64.2),
            new("Baseload", "South", 980, 80.1), new("Peak", "South", 220, 92.3),
        };
    }

    internal static List<Employee> SampleEmployees()
    {
        string[] first = ["Camille", "Lucas", "Léa", "Hugo", "Chloé", "Louis", "Manon", "Gabriel", "Inès", "Arthur", "Jade", "Nathan"];
        string[] last = ["Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard", "Petit", "Durand", "Leroy", "Moreau"];
        string[] departments = ["Engineering", "Design", "Product", "Sales", "Support"];
        string[] statuses = ["Active", "Active", "Active", "Remote", "Leave", "Offboarding"];
        var random = new Random(42);
        return Enumerable.Range(0, 37).Select(i => new Employee(
            $"{first[random.Next(first.Length)]} {last[random.Next(last.Length)]}",
            departments[random.Next(departments.Length)],
            statuses[random.Next(statuses.Length)],
            38000 + random.Next(0, 42) * 1000,
            new DateTime(2018, 1, 1).AddDays(random.Next(0, 2800)))).ToList();
    }
}
