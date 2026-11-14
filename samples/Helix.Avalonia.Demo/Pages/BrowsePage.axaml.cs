using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public sealed record TreeNode(string Name, string? Icon = null, string? Role = null, params TreeNode[] Children);

public partial class BrowsePage : UserControl
{
    public static readonly ObservableCollection<HelixLogEntry> Lines = [];

    public BrowsePage()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<HelixTreeView>("Files")!.ItemsSource = new[]
        {
            new TreeNode("src", "columns", null,
                new TreeNode("Helix.Avalonia", "columns", null,
                    new TreeNode("Controls", "columns", null, new TreeNode("HelixButton.cs", "edit"), new TreeNode("HelixDataGrid.cs", "edit")),
                    new TreeNode("Themes", "columns", null, new TreeNode("Controls.axaml", "layout")),
                    new TreeNode("README.md", "info")),
                new TreeNode("Helix.Export", "columns", null, new TreeNode("HelixExcel.cs", "edit"))),
            new TreeNode("build", "columns", null, new TreeNode("gen_avalonia_theme.py", "zap")),
            new TreeNode("LICENSE", "lock"),
        };
        this.FindControl<HelixTreeView>("Org")!.ItemsSource = new[]
        {
            new TreeNode("Ada Lovelace", null, "CEO",
                new TreeNode("Grace Hopper", null, "CTO", new TreeNode("Alan Turing", null, "Research"), new TreeNode("Linus Torvalds", null, "Platform")),
                new TreeNode("Margaret Hamilton", null, "Operations"),
                new TreeNode("Barbara Liskov", null, "Product", new TreeNode("Ken Thompson", null, "Design"))),
        };

        if (Lines.Count == 0)
        {
            var start = DateTime.Today.AddHours(9);
            (HelixLogLevel Level, string Source, string Message, string Properties, string? User)[] sample =
            [
                (HelixLogLevel.Info, "api", "Service started on port 5080", "Port=5080", null),
                (HelixLogLevel.Debug, "db", "Connection pool warmed up (8 connections)", "Connections=8", null),
                (HelixLogLevel.Info, "scheduler", "Next submission at 09:10", "Slot=09:10", null),
                (HelixLogLevel.Warning, "forecast", "Forecast for North plant is 12 minutes old", "Site=North plant;AgeMinutes=12", null),
                (HelixLogLevel.Info, "export", "employees.xlsx written (37 rows)", "File=employees.xlsx;Rows=37", "ada.l"),
                (HelixLogLevel.Error, "gateway", "Submission rejected: slot 09:15–09:30 above the limit", "Slot=09:15–09:30;RequestId=DR-0042", "ada.l"),
                (HelixLogLevel.Info, "scheduler", "Retrying with the corrected value", "RequestId=DR-0042", null),
                (HelixLogLevel.Info, "gateway", "Submission accepted", "RequestId=DR-0042", "ada.l"),
                (HelixLogLevel.Debug, "cache", "Evicted 120 entries", "Evicted=120", null),
                (HelixLogLevel.Warning, "api", "Slow request: GET /sites took 1.8 s", "Path=/sites;DurationMs=1800", "grace.h"),
                (HelixLogLevel.Info, "auth", "User signed in", "Method=OIDC", "grace.h"),
            ];
            for (var i = 0; i < sample.Length; i++)
            {
                var s = sample[i];
                Lines.Add(new HelixLogEntry(start.AddSeconds(i * 37), s.Level, s.Message, s.Source)
                {
                    Properties = s.Properties.Split(';').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => p[1]),
                    User = s.User,
                    TraceId = $"4bf92f3577b34da6a3ce929d0e0e47{i:x2}",
                    Exception = s.Level == HelixLogLevel.Error
                        ? "System.InvalidOperationException: Value 412 MW is above the limit of 400 MW.\n   at Gateway.Submit(Request request) in Gateway.cs:line 57"
                        : null,
                });
            }
        }

        this.FindControl<HelixLogViewer>("Log")!.ItemsSource = Lines;
    }
}
