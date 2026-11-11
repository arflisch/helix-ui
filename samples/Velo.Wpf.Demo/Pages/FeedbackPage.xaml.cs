using System.Windows;
using System.Windows.Controls;

namespace Velo.Wpf.Demo.Pages;

public partial class FeedbackPage : UserControl
{
    public FeedbackPage() => InitializeComponent();

    private void OnToastSuccess(object sender, RoutedEventArgs e) => VeloToasts.Success("Les modifications sont enregistrées.", "Enregistré");
    private void OnToastWarning(object sender, RoutedEventArgs e) => VeloToasts.Warning("Il reste 8 % de l'espace disponible.");
    private void OnToastError(object sender, RoutedEventArgs e) => VeloToasts.Error("Le serveur ne répond pas. Réessayez dans un instant.", "Erreur réseau");

    private void OnOpenDialog(object sender, RoutedEventArgs e) => Dialog.IsOpen = true;
    private void OnCloseDialog(object sender, RoutedEventArgs e) => Dialog.IsOpen = false;

    private void OnInvite(object sender, RoutedEventArgs e)
    {
        Dialog.IsOpen = false;
        VeloToasts.Success("L'invitation est partie.");
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (await VeloConfirm.AskAsync("Supprimer le rapport ?", "Cette action est définitive.", danger: true, confirmText: "Supprimer"))
        {
            VeloToasts.Success("Rapport supprimé.");
        }
    }
}
