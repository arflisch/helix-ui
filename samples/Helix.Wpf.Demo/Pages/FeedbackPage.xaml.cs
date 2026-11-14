using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class FeedbackPage : UserControl
{
    private static readonly (HelixLogLevel Level, string Source, string Message, string Properties, string? User)[] Samples =
    [
        (HelixLogLevel.Info, "dispatch", "Request DR-0042 sent to the grid operator", "RequestId=DR-0042;Unit=A", "ada.l"),
        (HelixLogLevel.Debug, "forecast", "Forecast recomputed in 412 ms", "DurationMs=412", null),
        (HelixLogLevel.Warning, "telemetry", "Unit B: no measurement for 45 s", "Unit=B;SilenceSeconds=45", null),
        (HelixLogLevel.Error, "telemetry", "Unit C: connection lost, retrying in 5 s", "Unit=C;RetryInSeconds=5", null),
        (HelixLogLevel.Info, "telemetry", "Unit C: connection restored", "Unit=C", null),
        (HelixLogLevel.Info, "auth", "User signed in", "Method=OIDC", "grace.h"),
        (HelixLogLevel.Warning, "api", "Slow request: GET /sites took 1.8 s", "Path=/sites;DurationMs=1800", "grace.h"),
    ];

    private readonly System.Collections.ObjectModel.ObservableCollection<HelixLogEntry> _log = [];
    private int _next;

    public FeedbackPage()
    {
        InitializeComponent();
        SubmissionBar.SlotColor = slot => slot.Offset < 0 ? "success" : slot.Offset == 2 ? "danger" : null;
        SubmissionBar.SlotText = slot => slot.Offset switch { < 0 => "Sent", 0 => "In progress", 2 => "Unavailable", _ => "To send" };
        for (var i = 0; i < 200; i++) AddLine(DateTime.Now.AddSeconds(-(200 - i) * 6));
        Log.ItemsSource = _log;
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
        timer.Tick += (_, _) => AddLine(DateTime.Now);
        Loaded += (_, _) => timer.Start();
        Unloaded += (_, _) => timer.Stop();
    }

    private void AddLine(DateTime time)
    {
        var s = Samples[_next++ % Samples.Length];
        _log.Add(new HelixLogEntry(time, s.Level, s.Message, s.Source)
        {
            Properties = s.Properties.Split(';').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => p[1]),
            User = s.User,
            TraceId = $"4bf92f3577b34da6a3ce929d0e0e{_next:x4}",
            Exception = s.Level == HelixLogLevel.Error
                ? "System.IO.IOException: Unable to read data from the transport connection.\n   at Telemetry.Client.ReadAsync() in Client.cs:line 88"
                : null,
        });
    }

    private async void OnPrompt(object sender, RoutedEventArgs e)
    {
        var name = await HelixConfirm.PromptAsync("Save the view", "Give it a name to find it again.", placeholder: "e.g. Pending in Lyon",
            confirmText: "Enregistrer", validate: text => text.Trim().Length < 3 ? "At least 3 characters." : null);
        if (name is not null) DialogResult.Text = $"View “{name}” saved";
    }

    private async void OnShowView(object sender, RoutedEventArgs e)
    {
        var name = await HelixDialogs.ShowAsync<string>(dialog =>
        {
            var field = new HelixTextField { Label = "Name", Text = "Lyon plant" };
            var save = new HelixButton { Content = "Enregistrer", Icon = "check", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            save.Click += (_, _) => dialog.Close(field.Text);
            var panel = new StackPanel();
            panel.Children.Add(field);
            panel.Children.Add(save);
            return panel;
        }, "Edit the site", width: 420);
        if (name is not null) DialogResult.Text = $"Site renamed: {name}";
    }

    private async void OnBusy(object sender, RoutedEventArgs e)
    {
        Busy.IsBusy = true;
        BusyButton.IsLoading = true;
        await System.Threading.Tasks.Task.Delay(1800);
        Busy.IsBusy = false;
        BusyButton.IsLoading = false;
    }

    private void OnToastSuccess(object sender, RoutedEventArgs e) => HelixToasts.Success("Your changes were saved.", "Saved");
    private void OnToastWarning(object sender, RoutedEventArgs e) => HelixToasts.Warning("8% of the space is left.");
    private void OnToastError(object sender, RoutedEventArgs e) => HelixToasts.Error("The server does not answer. Try again in a moment.", "Network error");

    private int _notified;

    private void OnNotify(object sender, RoutedEventArgs e) => _ = (++_notified % 3) switch
    {
        1 => HelixNotifications.Warning("Deadline in 5 minutes", "Submission for the next quarter hour", actionText: "View"),
        2 => HelixNotifications.Error("Submission rejected", "Slot 14:15–14:30: value above the limit."),
        _ => HelixNotifications.Success("Report exported", "team-report.docx"),
    };
    private void OnOpenDialog(object sender, RoutedEventArgs e) => Dialog.IsOpen = true;
    private void OnCloseDialog(object sender, RoutedEventArgs e) => Dialog.IsOpen = false;

    private void OnInvite(object sender, RoutedEventArgs e)
    {
        Dialog.IsOpen = false;
        HelixToasts.Success("The invitation was sent.");
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (await HelixConfirm.AskAsync("Delete the report?", "This cannot be undone.", danger: true, confirmText: "Delete"))
        {
            HelixToasts.Success("Report deleted.");
        }
    }
}
