using System.Windows.Controls;

namespace Velo.Wpf.Demo.Pages;

public partial class ChoicesPage : UserControl
{
    public ChoicesPage()
    {
        InitializeComponent();
        Plans.ItemsSource = new[]
        {
            new VeloRadioOption("Découverte", "Jusqu'à 3 sites, sans engagement.", "star"),
            new VeloRadioOption("Pro", "Sites illimités et exports.", "zap"),
            new VeloRadioOption("Entreprise", "SSO, support dédié, SLA.", "lock"),
        };
    }
}
