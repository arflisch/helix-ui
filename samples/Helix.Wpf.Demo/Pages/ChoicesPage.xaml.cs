using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class ChoicesPage : UserControl
{
    public ChoicesPage()
    {
        InitializeComponent();
        Plans.ItemsSource = new[]
        {
            new HelixRadioOption("Découverte", "Jusqu'à 3 sites, sans engagement.", "star"),
            new HelixRadioOption("Pro", "Sites illimités et exports.", "zap"),
            new HelixRadioOption("Entreprise", "SSO, support dédié, SLA.", "lock"),
        };
    }
}
