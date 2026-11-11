using System;
using System.Linq;
using System.Windows.Controls;

namespace Velo.Wpf.Demo.Pages;

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
        Y2025.Points = Enumerable.Range(0, 12).Select(m => new VeloPoint(m, 80 + m * 6 + m % 3 * 14)).ToList();
        Y2026.Points = Enumerable.Range(0, 10).Select(m => new VeloPoint(m, 95 + m * 7 + m % 4 * 10)).ToList();
        Monthly.XFormat = x => months[Math.Clamp((int)Math.Round(x), 0, 11)];

        Mix.Items =
        [
            new VeloPart("Nucléaire", 312), new VeloPart("Hydraulique", 64), new VeloPart("Éolien", 48),
            new VeloPart("Solaire", 22), new VeloPart("Gaz", 31), new VeloPart("Bioénergies", 9), new VeloPart("Charbon", 2),
        ];
        Share.Items = [new VeloPart("Renouvelable", 42), new VeloPart("Nucléaire", 46), new VeloPart("Fossile", 12)];
        Load.Thresholds = [new VeloGaugeThreshold(70, "warning", "Élevée"), new VeloGaugeThreshold(90, "danger", "Critique")];

        string[] days = ["Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim"];
        var cols = Enumerable.Range(0, 24).Select(h => $"{h}h").ToList();
        Heat.Rows = days;
        Heat.Columns = cols;
        Heat.Cells = days.SelectMany((d, di) => cols.Select((c, h) =>
            new VeloHeatCell(d, c, di == 6 && h == 3 ? null : Math.Round(40 + 35 * Math.Sin((h - 7) / 24.0 * Math.PI * 2) + (di < 5 ? 20 : 0), 1)))).ToList();

        string[] teams = ["Lyon", "Grenoble", "Marseille", "Lille"];
        string[] quarters = ["T1", "T2", "T3", "T4"];
        Delta.Rows = teams;
        Delta.Columns = quarters;
        Delta.Cells = teams.SelectMany((t, i) => quarters.Select((q, j) => new VeloHeatCell(t, q, Math.Round(Math.Sin(i * 1.7 + j) * 12, 1)))).ToList();

        Pages.Items =
        [
            new VeloBarItem("/accueil", 12840), new VeloBarItem("/tarifs", 8210), new VeloBarItem("/documentation", 6455),
            new VeloBarItem("/blog/velo-1-2", 3120), new VeloBarItem("/contact", 2290), new VeloBarItem("/carrieres", 1210), new VeloBarItem("/presse", 540),
        ];
    }
}
