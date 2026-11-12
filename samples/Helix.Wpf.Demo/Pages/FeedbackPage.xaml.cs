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
        SubmissionBar.SlotColor = slot => slot.Offset < 0 ? "success" : slot.Offset == 2 ? "danger" : null;
        SubmissionBar.SlotText = slot => slot.Offset switch { < 0 => "Envoyé", 0 => "En cours", 2 => "Indisponible", _ => "À envoyer" };
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

    private async void OnPrompt(object sender, RoutedEventArgs e)
    {
        var name = await HelixConfirm.PromptAsync("Enregistrer la vue", "Donnez-lui un nom pour la retrouver.", placeholder: "ex. En attente à Lyon",
            confirmText: "Enregistrer", validate: text => text.Trim().Length < 3 ? "Au moins 3 caractères." : null);
        if (name is not null) DialogResult.Text = $"Vue « {name} » enregistrée";
    }

    private async void OnShowView(object sender, RoutedEventArgs e)
    {
        var name = await HelixDialogs.ShowAsync<string>(dialog =>
        {
            var field = new HelixTextField { Label = "Nom", Text = "Usine de Lyon" };
            var save = new HelixButton { Content = "Enregistrer", Icon = "check", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            save.Click += (_, _) => dialog.Close(field.Text);
            var panel = new StackPanel();
            panel.Children.Add(field);
            panel.Children.Add(save);
            return panel;
        }, "Modifier le site", width: 420);
        if (name is not null) DialogResult.Text = $"Site renommé : {name}";
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
