using System.Windows;
using System.Windows.Controls;

namespace Velo.Wpf.Demo.Pages;

public partial class LayoutPage : UserControl
{
    public LayoutPage() => InitializeComponent();

    private void OnOpenRight(object sender, RoutedEventArgs e)
    {
        Filters.Position = VeloDrawerPosition.Right;
        Filters.DrawerSize = 420;
        Filters.IsOpen = true;
    }

    private void OnOpenBottom(object sender, RoutedEventArgs e)
    {
        Filters.Position = VeloDrawerPosition.Bottom;
        Filters.DrawerSize = 360;
        Filters.IsOpen = true;
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        Filters.IsOpen = false;
        VeloToasts.Success("Filtres appliqués.");
    }
}
