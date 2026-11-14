using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class NavigationPage : UserControl
{
    public NavigationPage()
    {
        InitializeComponent();
        Crumbs.ItemsSource = new[]
        {
            new HelixBreadcrumbItem("Home", "home"),
            new HelixBreadcrumbItem("Projects"),
            new HelixBreadcrumbItem("Energy"),
            new HelixBreadcrumbItem("Lyon"),
            new HelixBreadcrumbItem("March report"),
        };
        Crumbs.ItemClick += (_, e) => HelixToasts.Info(((HelixEventArgs<HelixBreadcrumbItem>)e).Value.Text);

        var steps = new[]
        {
            new HelixStep("Account", "Email and password"),
            new HelixStep("Profile", "Name and photo"),
            new HelixStep("Team", "Invite members"),
            new HelixStep("Done"),
        };
        Steps.Steps = steps;
        StepsVertical.Steps = steps;
        StepsVertical.ErrorSteps = [1];
    }
}
