using System.Windows;
using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class LayoutPage : UserControl
{
    public LayoutPage() => InitializeComponent();

    private void OnOpenRight(object sender, RoutedEventArgs e)
    {
        Filters.Position = HelixDrawerPosition.Right;
        Filters.DrawerSize = 420;
        Filters.IsOpen = true;
    }

    private void OnOpenBottom(object sender, RoutedEventArgs e)
    {
        Filters.Position = HelixDrawerPosition.Bottom;
        Filters.DrawerSize = 360;
        Filters.IsOpen = true;
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        Filters.IsOpen = false;
        HelixToasts.Success("Filtres appliqués.");
    }
}
