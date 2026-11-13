using System;
using System.IO;
using System.Windows;

namespace Helix.Wpf.Demo;

public partial class App : Application
{
    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Report(e.ExceptionObject as Exception);
        DispatcherUnhandledException += (_, e) =>
        {
            Report(e.Exception);
            e.Handled = true;
        };
    }

    private static void Report(Exception? ex)
    {
        var text = ex?.ToString() ?? "Unknown error";
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.log"), text);
        MessageBox.Show(text, "Helix demo crashed");
    }
}
