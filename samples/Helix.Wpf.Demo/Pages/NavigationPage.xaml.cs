using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class NavigationPage : UserControl
{
    public NavigationPage()
    {
        InitializeComponent();
        Crumbs.ItemsSource = new[]
        {
            new HelixBreadcrumbItem("Accueil", "home"),
            new HelixBreadcrumbItem("Projets"),
            new HelixBreadcrumbItem("Énergie"),
            new HelixBreadcrumbItem("Lyon"),
            new HelixBreadcrumbItem("Rapport de mars"),
        };
        Crumbs.ItemClick += (_, e) => HelixToasts.Info(((HelixEventArgs<HelixBreadcrumbItem>)e).Value.Text);

        var steps = new[]
        {
            new HelixStep("Compte", "E-mail et mot de passe"),
            new HelixStep("Profil", "Nom et photo"),
            new HelixStep("Équipe", "Inviter des membres"),
            new HelixStep("Terminé"),
        };
        Steps.Steps = steps;
        StepsVertical.Steps = steps;
        StepsVertical.ErrorSteps = [1];
    }
}
