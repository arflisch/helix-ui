using Avalonia;
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
        window.ShowPage(3);
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
