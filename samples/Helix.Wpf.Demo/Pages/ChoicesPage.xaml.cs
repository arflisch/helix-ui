using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class ChoicesPage : UserControl
{
    public ChoicesPage()
    {
        InitializeComponent();
        Plans.ItemsSource = new[]
        {
            new HelixRadioOption("Starter", "Up to 3 sites, no commitment.", "star"),
            new HelixRadioOption("Pro", "Unlimited sites and exports.", "zap"),
            new HelixRadioOption("Enterprise", "SSO, dedicated support, SLA.", "lock"),
        };
    }
}
