using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Velo.Wpf.Demo.Pages;

public partial class PlanningPage : UserControl
{
    private List<VeloTimelineItem> _items;

    public PlanningPage()
    {
        InitializeComponent();
        var day = DateTime.Today;
        Schedule.Start = day.AddHours(6);
        Schedule.End = day.AddHours(20);
        Schedule.Statuses =
        [
            new VeloTimelineStatus("done", "Terminé", "series-3"),
            new VeloTimelineStatus("live", "En cours", "series-1"),
            new VeloTimelineStatus("planned", "Planifié", "series-1", VeloTimelineAppearance.Outline),
            new VeloTimelineStatus("draft", "Brouillon", "subtle", VeloTimelineAppearance.Dashed),
            new VeloTimelineStatus("issue", "Incident", "danger"),
        ];
        _items =
        [
            new("1", day.AddHours(7), day.AddHours(10), "Inspection") { Lane = "Équipe A", Status = "done" },
            new("2", day.AddHours(10.5), day.AddHours(13), "Révision turbine") { Lane = "Équipe A", Status = "live" },
            new("3", day.AddHours(14), day.AddHours(17), "Essais") { Lane = "Équipe A", Status = "planned" },
            new("4", day.AddHours(8), day.AddHours(12), "Câblage") { Lane = "Équipe B", Status = "issue" },
            new("5", day.AddHours(9), day.AddHours(11), "Livraison") { Lane = "Équipe B", Status = "done" },
            new("6", day.AddHours(13), day.AddHours(18), "Mise en service") { Lane = "Équipe B", Status = "draft" },
            new("7", day.AddHours(6.5), day.AddHours(9.5), "Formation") { Lane = "Équipe C", Status = "done" },
            new("8", day.AddHours(15), day.AddHours(19), "Astreinte") { Lane = "Équipe C", Status = "planned" },
        ];
        Schedule.Items = _items;
        Schedule.ItemChanged += (_, e) =>
        {
            var change = ((VeloEventArgs<VeloTimelineChange>)e).Value;
            _items = _items.Select(i => i.Id == change.Item.Id ? change.Applied : i).ToList();
            Schedule.Items = _items;
        };
        Schedule.ItemClick += (_, e) => VeloToasts.Info(((VeloEventArgs<VeloTimelineItem>)e).Value.Label);

        // Same statuses as the MARI demand page.
        var quarter = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute / 15 * 15, 0);
        Demand.Start = quarter.AddMinutes(-15);
        Demand.End = quarter.AddMinutes(45);
        Demand.Statuses =
        [
            new VeloTimelineStatus("direct-sent", "Direct demand sent", "#F15A24", VeloTimelineAppearance.Outline),
            new VeloTimelineStatus("direct-ack", "Direct demand acknowledged", "#F15A24"),
            new VeloTimelineStatus("scheduled-saved", "Scheduled demand saved", "series-1", VeloTimelineAppearance.Dashed),
            new VeloTimelineStatus("scheduled-ack", "Scheduled demand acknowledged", "series-1"),
            new VeloTimelineStatus("local", "Local demand", "#EDA100"),
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
