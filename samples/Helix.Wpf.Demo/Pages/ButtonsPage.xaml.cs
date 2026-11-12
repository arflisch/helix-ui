using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Helix.Wpf.Demo.Pages;

public partial class ButtonsPage : UserControl
{
    public ButtonsPage() => InitializeComponent();

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        // Event handlers are the one place where async void is expected in WPF.
        SaveButton.IsLoading = true;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5));
            HelixToasts.Success("Les modifications sont enregistrées.", "Enregistré");
        }
        finally
        {
            SaveButton.IsLoading = false;
        }
    }
}
