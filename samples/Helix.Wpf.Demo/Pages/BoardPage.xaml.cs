using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class BoardPage : UserControl
{
    public sealed class TaskCard : INotifyPropertyChanged
    {
        private string _column = "";

        public string Title { get; init; } = "";
        public string Owner { get; init; } = "";
        public string Tag { get; init; } = "";
        public HelixTone Tone { get; init; }

        public string Column
        {
            get => _column;
            set { _column = value; Changed(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed record Node(string Name, string? Icon = null, string? Role = null, params Node[] Children);

    public BoardPage()
    {
        InitializeComponent();
        Board.Columns =
        [
            new HelixKanbanColumn("todo", "To do", "subtle"),
            new HelixKanbanColumn("doing", "In progress", "series-1"),
            new HelixKanbanColumn("review", "In review", "series-4", limit: 2),
            new HelixKanbanColumn("done", "Done", "series-3"),
        ];
        Board.ItemsSource = new ObservableCollection<TaskCard>
        {
            new() { Title = "Connect the billing API", Owner = "Camille Martin", Tag = "Back", Tone = HelixTone.Info, Column = "todo" },
            new() { Title = "Sign-in screen", Owner = "Léo Bernard", Tag = "Front", Tone = HelixTone.Primary, Column = "doing" },
            new() { Title = "Excel export of reports", Owner = "Inès Petit", Tag = "Data", Tone = HelixTone.Success, Column = "doing" },
            new() { Title = "Fix the time zone", Owner = "Hugo Robert", Tag = "Bug", Tone = HelixTone.Danger, Column = "review" },
            new() { Title = "Dark mode", Owner = "Chloé Richard", Tag = "Front", Tone = HelixTone.Primary, Column = "review" },
            new() { Title = "Load tests", Owner = "Nathan Durand", Tag = "Ops", Tone = HelixTone.Warning, Column = "done" },
        };
        Board.AddRequested += (_, e) => HelixToasts.Info($"New card in “{((HelixEventArgs<string>)e).Value}”");

        Priorities.ItemsSource = new ObservableCollection<string> { "Access security", "Report performance", "Offline mode", "Notifications", "Export PDF" };
        Priorities.Reordered += (_, e) =>
        {
            var (from, to) = ((HelixEventArgs<(int From, int To)>)e).Value;
            HelixToasts.Info($"Moved from {from + 1} to {to + 1}");
        };

        Files.ItemsSource = new[]
        {
            new Node("src", "columns", null,
                new Node("Controls", "columns", null, new Node("HelixButton.cs", "edit"), new Node("HelixChart.cs", "edit")),
                new Node("Themes", "columns", null, new Node("Generic.xaml", "edit"))),
            new Node("README.md", "edit"),
        };

        Org.ItemsSource = new[]
        {
            new Node("Claire Dubois", null, "Director",
                new Node("Marc Leroy", null, "Production", new Node("Team A", null, "8 people"), new Node("Team B", null, "6 people")),
                new Node("Sofia Moreau", null, "Maintenance", new Node("On call", null, "4 people")),
                new Node("Paul Girard", null, "Finance")),
        };
    }
}
