using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class PlanningPage : UserControl
{
    private List<HelixTimelineItem> _items;

    public PlanningPage()
    {
        InitializeComponent();
        var day = DateTime.Today;
        Schedule.Start = day.AddHours(6);
        Schedule.End = day.AddHours(20);
        Schedule.Statuses =
        [
            new HelixTimelineStatus("done", "Done", "series-3"),
            new HelixTimelineStatus("live", "In progress", "series-1"),
            new HelixTimelineStatus("planned", "Planned", "series-1", HelixTimelineAppearance.Outline),
            new HelixTimelineStatus("draft", "Draft", "subtle", HelixTimelineAppearance.Dashed),
            new HelixTimelineStatus("issue", "Incident", "danger"),
        ];
        _items =
        [
            new("1", day.AddHours(7), day.AddHours(10), "Inspection") { Lane = "Team A", Status = "done" },
            new("2", day.AddHours(10.5), day.AddHours(13), "Turbine overhaul") { Lane = "Team A", Status = "live" },
            new("3", day.AddHours(14), day.AddHours(17), "Tests") { Lane = "Team A", Status = "planned" },
            new("4", day.AddHours(8), day.AddHours(12), "Wiring") { Lane = "Team B", Status = "issue" },
            new("5", day.AddHours(9), day.AddHours(11), "Delivery") { Lane = "Team B", Status = "done" },
            new("6", day.AddHours(13), day.AddHours(18), "Commissioning") { Lane = "Team B", Status = "draft" },
            new("7", day.AddHours(6.5), day.AddHours(9.5), "Training") { Lane = "Team C", Status = "done" },
            new("8", day.AddHours(15), day.AddHours(19), "On call") { Lane = "Team C", Status = "planned" },
        ];
        Schedule.Items = _items;
        Schedule.ItemChanged += (_, e) =>
        {
            var change = ((HelixEventArgs<HelixTimelineChange>)e).Value;
            _items = _items.Select(i => i.Id == change.Item.Id ? change.Applied : i).ToList();
            Schedule.Items = _items;
        };
        Schedule.ItemClick += (_, e) => HelixToasts.Info(((HelixEventArgs<HelixTimelineItem>)e).Value.Label);

        var quarter = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute / 15 * 15, 0);
        Demand.Start = quarter.AddMinutes(-15);
        Demand.End = quarter.AddMinutes(45);
        Demand.Statuses =
        [
            new HelixTimelineStatus("direct-sent", "Direct demand sent", "#F15A24", HelixTimelineAppearance.Outline),
            new HelixTimelineStatus("direct-ack", "Direct demand acknowledged", "#F15A24"),
            new HelixTimelineStatus("scheduled-saved", "Scheduled demand saved", "series-1", HelixTimelineAppearance.Dashed),
            new HelixTimelineStatus("scheduled-ack", "Scheduled demand acknowledged", "series-1"),
            new HelixTimelineStatus("local", "Local demand", "#EDA100"),
        ];
        Demand.Items =
        [
            new("a", quarter.AddMinutes(-15), quarter, "SA: 0 MW") { Status = "scheduled-ack" },
            new("b", quarter, quarter.AddMinutes(15), "SA: 0 MW") { Status = "scheduled-saved" },
            new("c", quarter.AddMinutes(5), quarter.AddMinutes(30), "SA: -30 MW") { Status = "direct-ack" },
            new("d", quarter.AddMinutes(15), quarter.AddMinutes(30), "SA: 15 MW") { Status = "local" },
        ];
    }
}
