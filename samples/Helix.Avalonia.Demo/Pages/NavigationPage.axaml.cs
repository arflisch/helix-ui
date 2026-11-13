using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public partial class NavigationPage : UserControl
{
    public NavigationPage()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<HelixBreadcrumb>("Crumbs")!.ItemsSource = new[]
        {
            new HelixBreadcrumbItem("Home", "home"), new("Projects"), new("Helix UI"), new("Avalonia"),
        };
        this.FindControl<HelixButtonGroup>("Range")!.SelectedIndex = 1;
        var steps = new[] { new HelixStep("Account", "Your details"), new("Company", "Billing info"), new("Plan", "Pick a plan"), new("Done") };
        this.FindControl<HelixStepper>("Stepper")!.Steps = steps;
        this.FindControl<HelixStepper>("Stepper2")!.Steps = new[] { "Upload", "Check", "Import" };
        this.FindControl<HelixAvatarGroup>("Team")!.ItemsSource = new[] { "Ada Lovelace", "Grace Hopper", "Alan Turing", "Linus Torvalds", "Barbara Liskov", "Ken Thompson" }
            .Select(n => new HelixAvatar { DisplayName = n }).ToList();
    }
}
