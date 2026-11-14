using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Helix.Avalonia.Demo.Pages;

namespace Helix.Avalonia.Demo;

public partial class MainWindow : Window
{
    private static readonly (string Name, Func<Control> Create)[] Pages =
    [
        ("Buttons", () => new ButtonsPage()),
        ("Forms", () => new FormsPage()),
        ("Pickers", () => new PickersPage()),
        ("Display", () => new DisplayPage()),
        ("Data", () => new DataPage()),
        ("Charts", () => new ChartsPage()),
        ("Planning", () => new PlanningPage()),
        ("Dashboard", () => new DashboardPage()),
        ("Navigation", () => new Pages.NavigationPage()),
        ("Dialogs", () => new DialogsPage()),
        ("Live", () => new LivePage()),
        ("Filters & files", () => new ToolsPage()),
        ("Trees & logs", () => new BrowsePage()),
        ("Auto form", () => new FormsMorePage()),
        ("Grid extras", () => new ExtrasPage()),
        ("Feedback", () => new FeedbackPage(showToasts: true)),
    ];

    /// <summary>The components of each page, under its name in the menu.</summary>
    private static readonly Dictionary<string, string> Captions = new()
    {
        ["Buttons"] = "Button, async command",
        ["Forms"] = "Text field, switch, check box",
        ["Pickers"] = "Number, select, dates, radio, slider, rating",
        ["Display"] = "Badge, alert, card, progress, stat, empty state",
        ["Data"] = "Data grid, totals, export",
        ["Charts"] = "Chart, donut, gauge, heatmap, bar list",
        ["Planning"] = "Timeline, kanban, sortable list",
        ["Dashboard"] = "Dashboard with tiles",
        ["Navigation"] = "Tabs, accordion, breadcrumb, stepper, menus",
        ["Dialogs"] = "Dialog, drawer, confirm, prompt",
        ["Live"] = "Time bar, notification center",
        ["Filters & files"] = "Filter bar, color picker, file drop",
        ["Trees & logs"] = "Tree view, carousel, log viewer",
        ["Auto form"] = "Auto form, description list, target bar, busy",
        ["Grid extras"] = "Grid columns, field, settings, time slots",
        ["Feedback"] = "Toasts, spinners",
    };

    public sealed record NavEntry(string Name, string Caption);

    public static int PageCount => Pages.Length;
    public static string PageName(int index) => Pages[index].Name;
    public static int IndexOf(string name) => Array.FindIndex(Pages, p => p.Name == name);

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        var nav = this.FindControl<ListBox>("Nav")!;
        nav.ItemsSource = Pages.Select(p => new NavEntry(p.Name, Captions.GetValueOrDefault(p.Name, ""))).ToList();
        nav.SelectedIndex = 0;
        Application.Current!.ActualThemeVariantChanged += (_, _) => UpdateThemeButton();
        UpdateThemeButton();
    }

    public void ShowPage(int index) => this.FindControl<ListBox>("Nav")!.SelectedIndex = index;

    private void OnNavigate(object? sender, SelectionChangedEventArgs e)
    {
        var index = ((ListBox)sender!).SelectedIndex;
        if (index >= 0) this.FindControl<ContentControl>("Page")!.Content = Pages[index].Create();
    }

    private void OnToggleTheme(object? sender, RoutedEventArgs e) => HelixTheme.Current?.Toggle();

    private void UpdateThemeButton()
    {
        var button = this.FindControl<HelixButton>("ThemeButton")!;
        var dark = Application.Current!.ActualThemeVariant == ThemeVariant.Dark;
        button.Content = dark ? "Light mode" : "Dark mode";
        button.Icon = dark ? "sun" : "moon";
    }
}
