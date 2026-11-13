using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        AvaloniaXamlLoader.Load(this);
        var day = DateTime.Today;
        this.FindControl<HelixChart>("Load")!.Series[0].Points = Enumerable.Range(0, 48)
            .ToPoints(i => day.AddMinutes(i * 30), i => (double?)Math.Round(380 + 120 * Math.Sin((i - 15) / 48.0 * Math.PI * 2), 1));
        this.FindControl<HelixDonut>("Mix")!.Items = [new("Nuclear", 44), new("Wind", 20), new("Hydro", 15), new("Solar", 11), new("Gas", 10)];
        this.FindControl<HelixBarList>("Sites")!.Items = [new("North plant", 1840), new("East plant", 1420), new("South park", 960), new("West hydro", 720)];
    }
}
