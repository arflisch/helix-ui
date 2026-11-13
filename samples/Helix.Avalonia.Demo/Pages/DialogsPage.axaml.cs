using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public partial class DialogsPage : UserControl
{
    public DialogsPage() => AvaloniaXamlLoader.Load(this);

    private void Show(string text) => this.FindControl<TextBlock>("Result")!.Text = text;

    private void OnOpenDialog(object? sender, RoutedEventArgs e) => this.FindControl<HelixDialog>("Dialog")!.IsOpen = true;
    private void OnCloseDialog(object? sender, RoutedEventArgs e) => this.FindControl<HelixDialog>("Dialog")!.IsOpen = false;
    private void OnOpenDrawer(object? sender, RoutedEventArgs e) => this.FindControl<HelixDrawer>("Drawer")!.IsOpen = true;
    private void OnCloseDrawer(object? sender, RoutedEventArgs e) => this.FindControl<HelixDrawer>("Drawer")!.IsOpen = false;
    private void OnOpenSheet(object? sender, RoutedEventArgs e) => this.FindControl<HelixDrawer>("Sheet")!.IsOpen = true;

    private async void OnConfirm(object? sender, RoutedEventArgs e) =>
        Show(await HelixConfirm.AskAsync("Publish the schedule?", "Everyone on the team will see it.") ? "Published." : "Cancelled.");

    private async void OnDelete(object? sender, RoutedEventArgs e) =>
        Show(await HelixConfirm.AskAsync("Delete 3 sites?", "This cannot be undone.", danger: true) ? "Deleted." : "Kept.");

    private async void OnPrompt(object? sender, RoutedEventArgs e)
    {
        var name = await HelixConfirm.PromptAsync("Rename the board", value: "Q3 planning", placeholder: "Board name",
            validate: text => string.IsNullOrWhiteSpace(text) ? "The name cannot be empty." : null);
        Show(name is null ? "Cancelled." : $"Renamed to \"{name}\".");
    }

    private async void OnView(object? sender, RoutedEventArgs e)
    {
        var site = await HelixDialogs.ShowAsync<string>(handle =>
        {
            var field = new HelixTextField { Label = "Site name", Text = "North plant" };
            var save = new HelixButton { Content = "Save", HorizontalAlignment = HorizontalAlignment.Right };
            save.Click += (_, _) => handle.Close(field.Text);
            return new StackPanel { Spacing = 14, Children = { field, save } };
        }, "Edit the site", "Any view can be a dialog and return a value.", 420);
        Show(site is null ? "Cancelled." : $"Saved \"{site}\".");
    }
}
