using System;
using System.Windows;
using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class FeedbackPage : UserControl
{
    private static readonly (HelixLogLevel Level, string Source, string Message)[] Samples =
    [
        (HelixLogLevel.Info, "dispatch", "Demande DR-0042 envoyée au gestionnaire de réseau"),
        (HelixLogLevel.Debug, "forecast", "Prévision recalculée en 412 ms"),
        (HelixLogLevel.Warning, "telemetry", "Unité B : pas de mesure depuis 45 s"),
        (HelixLogLevel.Error, "telemetry", "Unité C : connexion perdue, nouvel essai dans 5 s"),
        (HelixLogLevel.Info, "telemetry", "Unité C : connexion rétablie"),
    ];

    private readonly System.Collections.ObjectModel.ObservableCollection<HelixLogEntry> _log = [];
    private int _next;

    public FeedbackPage()
    {
        InitializeComponent();
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
        _log.Add(new HelixLogEntry(time, s.Level, s.Message, s.Source));
    }

    private async void OnBusy(object sender, RoutedEventArgs e)
    {
        Busy.IsBusy = true;
        BusyButton.IsLoading = true;
        await System.Threading.Tasks.Task.Delay(1800);
        Busy.IsBusy = false;
        BusyButton.IsLoading = false;
    }

    private void OnToastSuccess(object sender, RoutedEventArgs e) => HelixToasts.Success("Les modifications sont enregistrées.", "Enregistré");
    private void OnToastWarning(object sender, RoutedEventArgs e) => HelixToasts.Warning("Il reste 8 % de l'espace disponible.");
    private void OnToastError(object sender, RoutedEventArgs e) => HelixToasts.Error("Le serveur ne répond pas. Réessayez dans un instant.", "Erreur réseau");

    private void OnOpenDialog(object sender, RoutedEventArgs e) => Dialog.IsOpen = true;
    private void OnCloseDialog(object sender, RoutedEventArgs e) => Dialog.IsOpen = false;

    private void OnInvite(object sender, RoutedEventArgs e)
    {
        Dialog.IsOpen = false;
        HelixToasts.Success("L'invitation est partie.");
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (await HelixConfirm.AskAsync("Supprimer le rapport ?", "Cette action est définitive.", danger: true, confirmText: "Supprimer"))
        {
            HelixToasts.Success("Rapport supprimé.");
        }
    }
}
