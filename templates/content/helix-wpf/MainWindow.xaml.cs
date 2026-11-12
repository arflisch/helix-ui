using System.Windows;
using Helix.Wpf;

namespace HelixApp;

public partial class MainWindow : Window
{
    // Lines from any thread, shown a few times per second.
    private readonly HelixLiveCollection<HelixLogEntry> _log = new(maxCount: 5000);

    public MainWindow()
    {
        InitializeComponent();
        Log.ItemsSource = _log;
        _log.Enqueue(new HelixLogEntry(DateTime.Now, HelixLogLevel.Info, "Application started", "app"));
    }
}
