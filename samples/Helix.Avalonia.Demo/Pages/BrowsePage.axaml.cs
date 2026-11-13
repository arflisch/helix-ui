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
            (HelixLogLevel Level, string Source, string Message)[] sample =
            [
                (HelixLogLevel.Info, "api", "Service started on port 5080"),
                (HelixLogLevel.Debug, "db", "Connection pool warmed up (8 connections)"),
                (HelixLogLevel.Info, "scheduler", "Next submission at 09:10"),
                (HelixLogLevel.Warning, "forecast", "Forecast for North plant is 12 minutes old"),
                (HelixLogLevel.Info, "export", "employees.xlsx written (37 rows)"),
                (HelixLogLevel.Error, "gateway", "Submission rejected: slot 09:15–09:30 above the limit"),
                (HelixLogLevel.Info, "scheduler", "Retrying with the corrected value"),
                (HelixLogLevel.Info, "gateway", "Submission accepted"),
                (HelixLogLevel.Debug, "cache", "Evicted 120 entries"),
                (HelixLogLevel.Warning, "api", "Slow request: GET /sites took 1.8 s"),
                (HelixLogLevel.Info, "auth", "User signed in"),
            ];
            for (var i = 0; i < sample.Length; i++) Lines.Add(new HelixLogEntry(start.AddSeconds(i * 37), sample[i].Level, sample[i].Message, sample[i].Source));
        }

        this.FindControl<HelixLogViewer>("Log")!.ItemsSource = Lines;
    }
}
