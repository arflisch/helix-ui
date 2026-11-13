using Avalonia;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;

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

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();

    private static int Screenshots(string folder)
    {
        Directory.CreateDirectory(folder);
        AppBuilder.Configure<App>().UseSkia().WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();

        var window = new MainWindow { Width = 1180, Height = 900 };
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
