using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Velo.Wpf.Demo.Pages;

public partial class CustomizationPage : UserControl
{
    private static readonly string[] Primaries = ["#4F46E5", "#0EA5E9", "#10B981", "#F59E0B", "#E11D48", "#8B5CF6", "#18181B"];

    public CustomizationPage()
    {
        InitializeComponent();
        foreach (var hex in Primaries)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var swatch = new VeloButton
            {
                Width = 30,
                Height = 30,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(999),
                Accent = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, 8, 8),
                ToolTip = hex,
            };
            swatch.Click += (_, _) => SetPrimary(color);
            Swatches.Children.Add(swatch);
        }

        if (VeloResources.Current is { } theme)
        {
            RadiusSlider.Value = theme.Radius.TopLeft;
            HeightSlider.Value = theme.ControlHeight;
        }
    }

    private static void SetPrimary(Color color)
    {
        if (VeloResources.Current is not { } theme)
        {
            return;
        }

        // Same primary in both modes; a real app would choose a lighter one for dark mode.
        theme.Light = new VeloPalette { Primary = color };
        theme.Dark = new VeloPalette { Primary = color };
    }

    private void OnRadiusChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VeloResources.Current is { } theme && IsLoaded)
        {
            theme.Radius = new CornerRadius(e.NewValue);
        }
    }

    private void OnHeightChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VeloResources.Current is { } theme && IsLoaded)
        {
            theme.ControlHeight = e.NewValue;
        }
    }

    private void OnReset(object sender, RoutedEventArgs e)
    {
        if (VeloResources.Current is not { } theme)
        {
            return;
        }

        theme.Light = null;
        theme.Dark = null;
        theme.Radius = new CornerRadius(10);
        theme.ControlHeight = 38;
        RadiusSlider.Value = 10;
        HeightSlider.Value = 38;
    }
}
