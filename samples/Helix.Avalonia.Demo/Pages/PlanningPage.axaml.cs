using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public sealed class TaskCard(string title, string status, string tag, HelixTone tone, string owner)
{
    public string Title { get; } = title;
    public string Status { get; set; } = status;
    public string Tag { get; } = tag;
    public HelixTone Tone { get; } = tone;
    public string Owner { get; } = owner;
}

public partial class PlanningPage : UserControl
{
    public PlanningPage()
    {
        AvaloniaXamlLoader.Load(this);
        var day = DateTime.Today;
        var schedule = this.FindControl<HelixTimeline>("Schedule")!;
        schedule.Start = day.AddHours(7);
        schedule.End = day.AddHours(15);
        schedule.Statuses =
        [
            new HelixTimelineStatus("confirmed", "Confirmed", "series-1"),
            new HelixTimelineStatus("sent", "Sent", "series-3", HelixTimelineAppearance.Outline),
            new HelixTimelineStatus("draft", "Draft", "series-2", HelixTimelineAppearance.Dashed),
        ];
        schedule.Items =
        [
            new HelixTimelineItem("1", day.AddHours(7.5), day.AddHours(10), "Baseload 120 MW") { Lane = "Unit A", Status = "confirmed" },
            new HelixTimelineItem("2", day.AddHours(10.5), day.AddHours(13), "Peak 160 MW") { Lane = "Unit A", Status = "sent" },
            new HelixTimelineItem("3", day.AddHours(8), day.AddHours(9.5), "Start-up") { Lane = "Unit B", Status = "confirmed" },
            new HelixTimelineItem("4", day.AddHours(9), day.AddHours(12), "Reserve 40 MW") { Lane = "Unit B", Status = "draft" },
            new HelixTimelineItem("5", day.AddHours(11), day.AddHours(14.5), "Maintenance") { Lane = "Unit C", Status = "sent", Color = "warning" },
        ];
        schedule.ItemChanged += (_, e) =>
        {
            var change = e.Item;
            schedule.Items = schedule.Items!.Select(i => i.Id == change.Item.Id ? change.Applied : i).ToList();
        };

        var editable = this.FindControl<HelixKanbanBoard>("EditableBoard")!;
        editable.Columns =
        [
            new HelixKanbanColumn("todo", "To do", "series-1"),
            new HelixKanbanColumn("doing", "In progress", "warning", limit: 3),
            new HelixKanbanColumn("done", "Done", "success"),
        ];
        var today = DateTime.Today;
        editable.ItemsSource = new ObservableCollection<HelixKanbanCard>
        {
            new("Connect the billing API", "todo") { Description = "Webhook for paid invoices, retries on 5xx.", Assignee = "Camille Martin", Tags = ["Backend"], Color = "info", DueDate = today.AddDays(5) },
            new("Fix the time zone", "todo") { Description = "Dates shift by one hour on DST days.", Assignee = "Hugo Robert", Tags = ["Bug"], Color = "danger", DueDate = today.AddDays(-1) },
            new("Sign-in screen", "doing") { Assignee = "Léo Bernard", Tags = ["Front", "Auth"], Color = "primary", DueDate = today.AddDays(2) },
            new("Dark mode", "done") { Assignee = "Chloé Richard", Tags = ["Front"], Color = "success" },
        };
        editable.CardAdded += (_, e) => HelixToasts.Success($"“{e.Item.Title}” added");
        editable.CardChanged += (_, e) => HelixToasts.Info($"“{e.Item.Title}” saved");
        editable.CardDeleted += (_, e) => HelixToasts.Info($"“{e.Item.Title}” deleted");

        var board = this.FindControl<HelixKanban>("Board")!;
        board.Columns =
        [
            new HelixKanbanColumn("todo", "To do", "series-1"),
            new HelixKanbanColumn("doing", "In progress", "warning", limit: 2),
            new HelixKanbanColumn("review", "Review", "series-7"),
            new HelixKanbanColumn("done", "Done", "success"),
        ];
        board.ItemsSource = new ObservableCollection<TaskCard>
        {
            new("Import yesterday's meter data", "todo", "Data", HelixTone.Info, "Camille"),
            new("Check the forecast deviation", "todo", "Forecast", HelixTone.Primary, "Lucas"),
            new("Send the day-ahead offer", "doing", "Market", HelixTone.Warning, "Léa"),
            new("Fix the unit B ramp", "doing", "Bug", HelixTone.Danger, "Hugo"),
            new("Update the limits table", "doing", "Config", HelixTone.Neutral, "Chloé"),
            new("Weekly report", "review", "Report", HelixTone.Success, "Louis"),
        };

        this.FindControl<HelixSortableList>("Priorities")!.ItemsSource =
            new ObservableCollection<string> { "Safety checks", "Day-ahead offer", "Forecast review", "Maintenance planning", "Monthly report" };
    }
}
