using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Helix.Avalonia.Demo.Pages;

public partial class ToolsPage : UserControl
{
    public ToolsPage()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<HelixDataGrid>("Staff")!.Source = DataPage.SampleEmployees();
        this.FindControl<HelixToggleFilter>("HighPay")!.Rule = row => row is Employee { Salary: >= 60000 };
        var bar = this.FindControl<HelixFilterBar>("Filters")!;
        bar.Presets = new System.Collections.ObjectModel.ObservableCollection<HelixFilterPreset>
        {
            new("Active engineers", new Dictionary<string, string> { ["q"] = "Engineering", ["status"] = "[\"Active\"]" }),
        };
        var state = this.FindControl<TextBlock>("StateText")!;
        bar.FiltersChanged += (_, _) => state.Text = "State: " + string.Join(", ", (bar.State ?? new Dictionary<string, string>()).Select(kv => $"{kv.Key}={kv.Value}"));
        bar.Apply(bar.Presets[0]);

        // Two sample files, so the list shows.
        var folder = Path.Combine(Path.GetTempPath(), "helix-demo");
        Directory.CreateDirectory(folder);
        var report = Path.Combine(folder, "Q3 report.xlsx");
        var notes = Path.Combine(folder, "notes.pdf");
        if (!File.Exists(report)) File.WriteAllBytes(report, new byte[184_320]);
        if (!File.Exists(notes)) File.WriteAllBytes(notes, new byte[52_100]);
        this.FindControl<HelixFileDrop>("Drop")!.AddFiles([report, notes]);
    }
}
