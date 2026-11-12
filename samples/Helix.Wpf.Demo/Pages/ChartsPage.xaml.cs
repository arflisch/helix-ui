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

        string[] months = ["janv.", "févr.", "mars", "avr.", "mai", "juin", "juil.", "août", "sept.", "oct.", "nov.", "déc."];
        Y2025.Points = Enumerable.Range(0, 12).Select(m => new HelixPoint(m, 80 + m * 6 + m % 3 * 14)).ToList();
        Y2026.Points = Enumerable.Range(0, 10).Select(m => new HelixPoint(m, 95 + m * 7 + m % 4 * 10)).ToList();
        Monthly.XFormat = x => months[Math.Clamp((int)Math.Round(x), 0, 11)];

        Mix.Items =
        [
            new HelixPart("Nucléaire", 312), new HelixPart("Hydraulique", 64), new HelixPart("Éolien", 48),
            new HelixPart("Solaire", 22), new HelixPart("Gaz", 31), new HelixPart("Bioénergies", 9), new HelixPart("Charbon", 2),
        ];
        Share.Items = [new HelixPart("Renouvelable", 42), new HelixPart("Nucléaire", 46), new HelixPart("Fossile", 12)];
        Load.Thresholds = [new HelixGaugeThreshold(70, "warning", "Élevée"), new HelixGaugeThreshold(90, "danger", "Critique")];

        string[] days = ["Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim"];
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
            new HelixBarItem("/accueil", 12840), new HelixBarItem("/tarifs", 8210), new HelixBarItem("/documentation", 6455),
            new HelixBarItem("/blog/helix-1-2", 3120), new HelixBarItem("/contact", 2290), new HelixBarItem("/carrieres", 1210), new HelixBarItem("/presse", 540),
        ];
    }
}
