using System;
using System.Windows;
using System.Windows.Controls;
using Helix.Wpf.Demo.Pages;

namespace Helix.Wpf.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ThemePicker.SelectedIndex = 0;
        Nav.SelectedIndex = 0;
    }

    private void OnNavigate(object sender, SelectionChangedEventArgs e)
    {
        if (Nav.SelectedItem is not ListBoxItem { Tag: string page })
        {
            return;
        }

        PageHost.Content = page switch
        {
            "Fields" => new FieldsPage(),
            "Choices" => new ChoicesPage(),
            "Feedback" => new FeedbackPage(),
            "Layout" => new LayoutPage(),
            "Customization" => new CustomizationPage(),
            "Navigation" => new NavigationPage(),
            "Data" => new DataPage(),
            "Charts" => new ChartsPage(),
            "Planning" => new PlanningPage(),
            "Board" => new BoardPage(),
            _ => new ButtonsPage(),
        };
        Scroller.ScrollToTop();
    }

    private void OnThemeModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemePicker.SelectedItem is ComboBoxItem { Tag: string mode } && HelixResources.Current is { } theme)
        {
            theme.Mode = Enum.Parse<HelixThemeMode>(mode);
        }
    }
}
