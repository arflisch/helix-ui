using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public partial class LivePage : UserControl
{
    private static bool _seeded;

    public LivePage()
    {
        AvaloniaXamlLoader.Load(this);
        var quarter = this.FindControl<HelixTimeBar>("Quarter")!;
        quarter.SlotColor = slot => slot.Offset < 0 ? "success" : slot.Offset == 0 ? "warning" : null;
        quarter.SlotText = slot => slot.Offset < 0 ? "Sent" : slot.Offset == 0 ? "Due" : slot.Offset == 1 ? "Draft ready" : null;
        var fixedBar = this.FindControl<HelixTimeBar>("Fixed")!;
        fixedBar.Start = DateTime.Now.AddMinutes(-40);
        fixedBar.Deadline = DateTime.Now.AddMinutes(20);

        if (_seeded) return;
        _seeded = true;
        var now = DateTime.Now;
        HelixNotifications.Add("Export ready", HelixSeverity.Success, "employees.xlsx (37 rows)", actionText: "Open", action: () => { }, time: now.AddMinutes(-3));
        HelixNotifications.Add("Deadline in 5 minutes", HelixSeverity.Warning, "Submit the next quarter hour before it closes.", time: now.AddMinutes(-12));
        HelixNotifications.Add("New comment on Q3 planning", time: now.AddHours(-2), read: true);
        HelixNotifications.Add("Submission rejected", HelixSeverity.Danger, "Slot 14:15–14:30 is above the limit.", time: now.AddDays(-1).AddHours(-1), read: true);
        HelixNotifications.Add("Welcome to Helix", time: now.AddDays(-4), read: true);
    }

    private void OnInfo(object? sender, RoutedEventArgs e) => HelixNotifications.Info("Forecast updated", "The day-ahead forecast was refreshed.");
    private void OnWarning(object? sender, RoutedEventArgs e) => HelixNotifications.Warning("Capacity at 92 %", "North plant is close to its limit.");
    private void OnError(object? sender, RoutedEventArgs e) =>
        HelixNotifications.Add("Network error", HelixSeverity.Danger, "The server does not answer.", toast: true);
    private void OnMarkAll(object? sender, RoutedEventArgs e) => HelixNotifications.MarkAllRead();
}
