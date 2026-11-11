using System.Windows.Controls;

namespace Velo.Wpf.Demo.Pages;

public partial class NavigationPage : UserControl
{
    public NavigationPage()
    {
        InitializeComponent();
        Crumbs.ItemsSource = new[]
        {
            new VeloBreadcrumbItem("Accueil", "home"),
            new VeloBreadcrumbItem("Projets"),
            new VeloBreadcrumbItem("Énergie"),
            new VeloBreadcrumbItem("Lyon"),
            new VeloBreadcrumbItem("Rapport de mars"),
        };
        Crumbs.ItemClick += (_, e) => VeloToasts.Info(((VeloEventArgs<VeloBreadcrumbItem>)e).Value.Text);

        var steps = new[]
        {
            new VeloStep("Compte", "E-mail et mot de passe"),
            new VeloStep("Profil", "Nom et photo"),
            new VeloStep("Équipe", "Inviter des membres"),
            new VeloStep("Terminé"),
        };
        Steps.Steps = steps;
        StepsVertical.Steps = steps;
        StepsVertical.ErrorSteps = [1];
    }
}
