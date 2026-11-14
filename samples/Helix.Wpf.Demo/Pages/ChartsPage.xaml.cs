using System;
using System.Linq;
using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class ChartsPage : UserControl
{
    public ChartsPage()
    {
        InitializeComponent();
        var today = DateTime.Today;
        var hours = Enumerable.Range(0, 25).Select(h => today.AddHours(h)).ToList();
        Actual.Points = hours.Where(h => h <= DateTime.Now).ToPoints(h => h, h => 260 + 140 * Math.Sin((h.Hour - 6) / 24.0 * Math.PI * 2) + h.Hour % 3 * 12);
        Forecast.Points = hours.ToPoints(h => h, h => 250 + 150 * Math.Sin((h.Hour - 6) / 24.0 * Math.PI * 2));
        Power.From = today;
        Power.To = today.AddDays(1);

        string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        Y2025.Points = Enumerable.Range(0, 12).Select(m => new HelixPoint(m, 80 + m * 6 + m % 3 * 14)).ToList();
        Y2026.Points = Enumerable.Range(0, 10).Select(m => new HelixPoint(m, 95 + m * 7 + m % 4 * 10)).ToList();
        Monthly.XFormat = x => months[Math.Clamp((int)Math.Round(x), 0, 11)];

        Mix.Items =
        [
            new HelixPart("Nuclear", 312), new HelixPart("Hydro", 64), new HelixPart("Wind", 48),
            new HelixPart("Solar", 22), new HelixPart("Gas", 31), new HelixPart("Bioenergy", 9), new HelixPart("Coal", 2),
        ];
        Share.Items = [new HelixPart("Renewable", 42), new HelixPart("Nuclear", 46), new HelixPart("Fossil", 12)];
        Load.Thresholds = [new HelixGaugeThreshold(70, "warning", "High"), new HelixGaugeThreshold(90, "danger", "Critical")];

        string[] days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
        var cols = Enumerable.Range(0, 24).Select(h => $"{h}h").ToList();
        Heat.Rows = days;
        Heat.Columns = cols;
        Heat.Cells = days.SelectMany((d, di) => cols.Select((c, h) =>
            new HelixHeatCell(d, c, di == 6 && h == 3 ? null : Math.Round(40 + 35 * Math.Sin((h - 7) / 24.0 * Math.PI * 2) + (di < 5 ? 20 : 0), 1)))).ToList();

        string[] teams = ["Lyon", "Grenoble", "Marseille", "Lille"];
        string[] quarters = ["T1", "T2", "T3", "T4"];
        Delta.Rows = teams;
        Delta.Columns = quarters;
        Delta.Cells = teams.SelectMany((t, i) => quarters.Select((q, j) => new HelixHeatCell(t, q, Math.Round(Math.Sin(i * 1.7 + j) * 12, 1)))).ToList();

        Pages.Items =
        [
            new HelixBarItem("/home", 12840), new HelixBarItem("/pricing", 8210), new HelixBarItem("/documentation", 6455),
            new HelixBarItem("/blog/helix-1-2", 3120), new HelixBarItem("/contact", 2290), new HelixBarItem("/careers", 1210), new HelixBarItem("/press", 540),
        ];
    }
}
