using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public partial class FeedbackPage : UserControl
{
    public FeedbackPage() => AvaloniaXamlLoader.Load(this);

    public FeedbackPage(bool showToasts) : this()
    {
        if (showToasts) HelixToasts.Success("Your changes were saved.", "Saved");
    }

    private void OnSuccess(object? sender, RoutedEventArgs e) => HelixToasts.Success("Your changes were saved.", "Saved");
    private void OnWarning(object? sender, RoutedEventArgs e) => HelixToasts.Warning("Your session expires in 5 minutes.");
    private void OnError(object? sender, RoutedEventArgs e) => HelixToasts.Error("The server does not answer. Try again in a moment.", "Network error");
}
