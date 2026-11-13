using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public sealed record SiteRow(string Name, string Status, HelixTone Tone, double Output, double Delta, double Cost, string Notes);

public partial class ExtrasPage : UserControl
{
    public ExtrasPage()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<HelixDataGrid>("Sites")!.Source = new List<SiteRow>
        {
            new("North plant", "Running", HelixTone.Success, 412, 2.4, -1200, "Maintenance window every first Sunday of the month, 02:00–06:00."),
            new("South plant", "Reduced", HelixTone.Warning, 338, -6.1, 850, "Ramp rate limited to 8 MW/min during the winter."),
            new("East plant", "Running", HelixTone.Success, 510, 0, 0, "New turbine commissioned in March."),
            new("West plant", "Stopped", HelixTone.Danger, 0, -100, 4300, "Grid connection works until Friday."),
            new("Harbor", "Running", HelixTone.Success, 120, 11.8, -300, "Runs on the day-ahead contract only."),
        };

        var zone = FindZone();
        var lines = new List<string>();
        foreach (var day in new[] { new DateOnly(2026, 3, 29), new DateOnly(2026, 6, 15), new DateOnly(2026, 10, 25) })
        {
            var slots = HelixTimeSlots.ForDay(day, TimeSpan.FromMinutes(15), zone);
            lines.Add($"{day:dd/MM/yyyy}  {HelixTimeSlots.HoursIn(day, zone),2} h  {slots.Count,3} quarter hours   first {slots[0].Label()}  last {slots[^1].Label()}");
        }

        this.FindControl<TextBlock>("SlotsText")!.Text = $"Time zone {zone.Id}\n" + string.Join("\n", lines);
    }

    internal static TimeZoneInfo FindZone()
    {
        foreach (var id in new[] { "Europe/Paris", "Romance Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }

        return TimeZoneInfo.Local;
    }
}
