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

    // The gallery is in English whatever the machine's language: numbers, dates and the controls' own texts.
    protected override void OnStartup(StartupEventArgs e)
    {
        var english = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = english;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = english;
        System.Threading.Thread.CurrentThread.CurrentCulture = english;
        System.Threading.Thread.CurrentThread.CurrentUICulture = english;
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(english.IetfLanguageTag)));
        base.OnStartup(e);
    }

    private static void Report(Exception? ex)
    {
        var text = ex?.ToString() ?? "Unknown error";
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.log"), text);
        MessageBox.Show(text, "Helix demo crashed");
    }
}
