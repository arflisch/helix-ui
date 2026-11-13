using System.Globalization;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public partial class ChartsPage : UserControl
{
    public ChartsPage()
    {
        AvaloniaXamlLoader.Load(this);
        var day = DateTime.Today;
        var load = this.FindControl<HelixChart>("Load")!;
        load.Series[0].Points = Enumerable.Range(0, 96).Where(i => day.AddMinutes(i * 15) <= DateTime.Now.AddHours(2))
            .ToPoints(i => day.AddMinutes(i * 15), i => (double?)Math.Round(380 + 120 * Math.Sin((i - 30) / 96.0 * Math.PI * 2) + 18 * Math.Sin(i * 1.7), 1));
        load.Series[1].Points = Enumerable.Range(0, 97).ToPoints(i => day.AddMinutes(i * 15), i => (double?)Math.Round(390 + 115 * Math.Sin((i - 30) / 96.0 * Math.PI * 2), 1));

        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"];
        var bars = this.FindControl<HelixChart>("Months")!;
        bars.XFormat = x => months[(int)x];
        bars.Series[0].Points = new double[] { 42, 48, 51, 47, 55, 58 }.Select((v, i) => new HelixPoint(i, v)).ToList();
        bars.Series[1].Points = new double[] { 46, 50, 57, 54, 61, 66 }.Select((v, i) => new HelixPoint(i, v)).ToList();

        var steps = this.FindControl<HelixChart>("Steps")!;
        double[] setPoints = [120, 120, 140, 140, 135, 160, 160, 150, 170, 165, 165, 150];
        steps.Series[0].Points = setPoints.Select((v, i) => HelixPoint.At(day.AddHours(8).AddMinutes(i * 15), v)).ToList();

        this.FindControl<HelixDonut>("Mix")!.Items =
        [
            new("Nuclear", 2210), new("Wind", 980), new("Hydro", 760), new("Solar", 540), new("Gas", 310), new("Biomass", 120), new("Other", 60),
        ];
        this.FindControl<HelixGauge>("Gauge")!.Thresholds = [new(70, "warning", "High"), new(90, "danger", "Critical")];

        string[] days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
        var random = new Random(7);
        this.FindControl<HelixHeatmap>("Heat")!.Cells = days.SelectMany(d => Enumerable.Range(0, 24).Select(h =>
            new HelixHeatCell(d, h.ToString("00", CultureInfo.InvariantCulture), Math.Round(30 * Math.Sin((h - 6) / 24.0 * Math.PI * 2) + random.Next(-25, 25), 0)))).ToList();

        this.FindControl<HelixBarList>("Sites")!.Items =
        [
            new("North plant", 1840) { Icon = "zap" }, new("East plant", 1420) { Icon = "zap" }, new("South park", 960) { Icon = "sun" },
            new("West hydro", 720), new("Harbour", 310), new("Hill farm", 180),
        ];
        this.FindControl<HelixDonut>("Segments")!.Items = [new("Open", 18, "info"), new("In progress", 11, "warning"), new("Blocked", 4, "danger"), new("Done", 42, "success")];
    }
}
