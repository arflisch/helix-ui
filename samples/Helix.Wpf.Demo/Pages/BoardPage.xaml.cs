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
            new HelixKanbanColumn("todo", "À faire", "subtle"),
            new HelixKanbanColumn("doing", "En cours", "series-1"),
            new HelixKanbanColumn("review", "En revue", "series-4", limit: 2),
            new HelixKanbanColumn("done", "Terminé", "series-3"),
        ];
        Board.ItemsSource = new ObservableCollection<TaskCard>
        {
            new() { Title = "Brancher l'API de facturation", Owner = "Camille Martin", Tag = "Back", Tone = HelixTone.Info, Column = "todo" },
            new() { Title = "Écran de connexion", Owner = "Léo Bernard", Tag = "Front", Tone = HelixTone.Primary, Column = "doing" },
            new() { Title = "Export Excel des rapports", Owner = "Inès Petit", Tag = "Data", Tone = HelixTone.Success, Column = "doing" },
            new() { Title = "Corriger le fuseau horaire", Owner = "Hugo Robert", Tag = "Bug", Tone = HelixTone.Danger, Column = "review" },
            new() { Title = "Mode sombre", Owner = "Chloé Richard", Tag = "Front", Tone = HelixTone.Primary, Column = "review" },
            new() { Title = "Tests de charge", Owner = "Nathan Durand", Tag = "Ops", Tone = HelixTone.Warning, Column = "done" },
        };
        Board.AddRequested += (_, e) => HelixToasts.Info($"Nouvelle carte dans « {((HelixEventArgs<string>)e).Value} »");

        Priorities.ItemsSource = new ObservableCollection<string> { "Sécurité des accès", "Performance des rapports", "Mode hors ligne", "Notifications", "Export PDF" };
        Priorities.Reordered += (_, e) =>
        {
            var (from, to) = ((HelixEventArgs<(int From, int To)>)e).Value;
            HelixToasts.Info($"Déplacé de {from + 1} à {to + 1}");
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
            new Node("Claire Dubois", null, "Directrice",
                new Node("Marc Leroy", null, "Production", new Node("Équipe A", null, "8 personnes"), new Node("Équipe B", null, "6 personnes")),
                new Node("Sofia Moreau", null, "Maintenance", new Node("Astreinte", null, "4 personnes")),
                new Node("Paul Girard", null, "Finance")),
        };
    }
}
