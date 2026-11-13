using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Helix.Avalonia.Demo;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // dotnet run -- --screenshots <folder>: renders every page, light and dark, to PNG (no window).
        if (args.Length >= 1 && args[0] == "--screenshots")
        {
            return Screenshots(args.Length > 1 ? args[1] : "screenshots");
        }

        if (args.Length >= 1 && args[0] == "--selftest")
        {
            return SelfTest();
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();

    /// <summary>Checks the grid behavior headlessly: search, paging, export.</summary>
    private static int SelfTest()
    {
        AppBuilder.Configure<App>().UseSkia().WithInterFont().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
        var window = new MainWindow { Width = 1180, Height = 1500 };
        window.Show();
        window.ShowPage(MainWindow.IndexOf("Data"));
        void Pump(int ms = 0)
        {
            var end = DateTime.Now.AddMilliseconds(ms);
            do { Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Thread.Sleep(20); } while (DateTime.Now < end);
        }

        Pump(200);
        var grid = window.GetVisualDescendants().OfType<HelixDataGrid>().First();
        var failures = 0;
        void Check(string what, bool ok) { Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {what}"); if (!ok) failures++; }

        Check($"37 rows, 5 pages ({grid.TotalCount}, {grid.PageText})", grid.TotalCount == 37 && grid.PageText == "1 / 5");
        grid.Page = 5;
        Pump(100);
        Check($"last page shows 33–37 ({grid.InfoText})", grid.InfoText?.Contains("33") == true && grid.InfoText.Contains("37"));
        grid.SearchDelay = TimeSpan.Zero; // the headless test has no timers
        grid.SearchText = "Design";
        Pump(600);
        Check($"search 'Design' filters and goes back to page 1 ({grid.TotalCount} rows, page {grid.Page})", grid.TotalCount is > 0 and < 37 && grid.Page == 1);
        grid.SearchText = "zzz";
        Pump(600);
        Check($"no result shows the empty state ({grid.IsEmpty}, '{grid.EmptyMessage}')", grid.IsEmpty && grid.EmptyMessage?.Contains("zzz") == true);
        grid.SearchText = "";
        Pump(600);
        var xlsx = grid.ToFile(Helix.Export.HelixExportFormat.Excel);
        Check($"Excel export is a zip ({xlsx.Length} bytes)", xlsx.Length > 1000 && xlsx[0] == (byte)'P' && xlsx[1] == (byte)'K');
        var csv = grid.ToCsv();
        Check($"CSV has a header and 37 rows + total ({csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length} lines)", csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length == 39);

        // ----- Fields and pickers -----
        window.ShowPage(MainWindow.IndexOf("Pickers"));
        Pump(200);
        var page = window.GetVisualDescendants().OfType<Helix.Avalonia.Demo.Pages.PickersPage>().First();
        T Find<T>(string name) where T : Control => page.FindControl<T>(name)!;
        IEnumerable<Visual> PopupContent(Control owner) =>
            owner.GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.Popup>().First().Child!.GetVisualDescendants();
        Button Day(Control owner, DateTime day) => PopupContent(owner).OfType<Button>().First(b => b.Tag is DateTime d && d == day.Date);
        void Click(Button b) => b.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        var number = Find<HelixNumberField>("Capacity");
        number.Text = "120*0.8";
        number.Commit();
        Check($"number field computes 120*0.8 ({number.Value})", number.Value == 96);
        number.Text = "900";
        number.Commit();
        Check($"number field clamps to its maximum ({number.Value})", number.Value == 500);

        var date = Find<HelixDatePicker>("Delivery");
        date.IsDropDownOpen = true;
        Pump(100);
        var monday = DateTime.Today.AddDays(((int)DayOfWeek.Monday - (int)DateTime.Today.DayOfWeek + 7) % 7 + 7);
        var saturday = monday.AddDays(5);
        Check("weekend days are disabled", !Day(date, saturday).IsEnabled);
        Click(Day(date, monday));
        Pump(50);
        Check($"date picker takes the clicked day and closes ({date.DisplayText}, open={date.IsDropDownOpen})", date.SelectedDate == monday && !date.IsDropDownOpen);

        var range = Find<HelixDateRangePicker>("Period");
        range.IsDropDownOpen = true;
        Pump(100);
        Click(Day(range, monday));
        Pump(50);
        Check("range picker asks for the last day after the first click", range.IsPickingEnd && range.IsDropDownOpen);
        Click(Day(range, monday.AddDays(3)));
        Pump(50);
        Check($"range picker keeps 4 days ({range.DisplayText})", range.SelectedRange?.Days == 4 && !range.IsDropDownOpen);

        var start = Find<HelixDateTimePicker>("Start");
        start.IsDropDownOpen = true;
        Pump(100);
        Click(Day(start, monday));
        Pump(50);
        var times = PopupContent(start).OfType<ListBox>().First();
        times.SelectedItem = times.ItemsSource!.Cast<HelixTimeSlot>().First(t => t.Time == TimeSpan.FromHours(9.5));
        Pump(50);
        Check($"date-time picker keeps the day and the time ({start.DisplayText})", start.SelectedDateTime == monday.AddHours(9.5) && !start.IsDropDownOpen);

        var teams = Find<HelixMultiSelect>("Teams");
        teams.IsDropDownOpen = true;
        Pump(100);
        var product = PopupContent(teams).OfType<HelixCheckBox>().First(b => (string?)b.Content == "Product");
        product.IsChecked = true;
        Pump(50);
        Check($"multiselect adds a choice ({teams.SelectedItems?.Count} chosen, chips {teams.Chips.Count} {teams.MoreText})", teams.SelectedItems?.Count == 4 && teams.MoreText == "+2");
        teams.SearchText = "fin";
        Pump(50);
        Check("multiselect search hides the other options", PopupContent(teams).OfType<HelixCheckBox>().Count(b => b.IsVisible) == 1);
        teams.IsDropDownOpen = false;

        var region = Find<HelixSelect>("Region");
        region.IsDropDownOpen = true;
        Pump(100);
        region.SearchText = "th";
        Pump(50);
        var visible = Enumerable.Range(0, region.ItemCount).Count(i => region.ContainerFromIndex(i)?.IsVisible == true);
        Check($"select search keeps North and South ({visible} visible)", visible == 2);
        region.IsDropDownOpen = false;
        Pump(50);
        Check("select search is emptied when the list closes", string.IsNullOrEmpty(region.SearchText));
        return failures;
    }

    private static int Screenshots(string folder)
    {
        Directory.CreateDirectory(folder);
        AppBuilder.Configure<App>().UseSkia().WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();

        var window = new MainWindow { Width = 1180, Height = 1500 };
        window.Show();
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            Application.Current!.RequestedThemeVariant = variant;
            for (var i = 0; i < MainWindow.PageCount; i++)
            {
                window.ShowPage(i);
                // Let layout and the short transitions (fills, thumbs) finish before the capture.
                for (var tick = 0; tick < 12; tick++)
                {
                    Dispatcher.UIThread.RunJobs();
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                    Thread.Sleep(40);
                }

                var path = Path.Combine(folder, $"{i + 1:00}-{MainWindow.PageName(i).ToLowerInvariant()}-{variant.Key.ToString()!.ToLowerInvariant()}.png");
                window.CaptureRenderedFrame()?.Save(path);
                Console.WriteLine(path);
            }
        }

        return 0;
    }
}
