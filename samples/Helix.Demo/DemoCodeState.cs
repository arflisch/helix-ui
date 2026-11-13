using System.Text.RegularExpressions;

namespace Helix.Demo;

/// <summary>The version of the code samples shown across the site.</summary>
public enum DemoFlavor { Blazor, Wpf, Avalonia }

/// <summary>Which version of the code samples is shown (Blazor, WPF or Avalonia). One choice for the whole site.</summary>
public sealed class DemoCodeState
{
    public DemoFlavor Flavor { get; private set; }

    public bool ShowWpf => Flavor == DemoFlavor.Wpf;

    public event Action? Changed;

    public void Set(DemoFlavor flavor)
    {
        if (Flavor == flavor) return;
        Flavor = flavor;
        Changed?.Invoke();
    }
}

/// <summary>
/// Turns a WPF sample into its Avalonia version. The XAML of both libraries is the same apart from a few names,
/// so most pages only write the WPF sample; a page passes AvaloniaCode where the two really differ.
/// </summary>
public static partial class DemoAvalonia
{
    public static string FromWpf(string wpf)
    {
        var code = wpf
            .Replace("https://helix-ui.dev/wpf", "using:Helix.Avalonia")
            .Replace("Helix.Wpf", "Helix.Avalonia")
            .Replace("helix-wpf", "helix-avalonia")
            .Replace("HelixResources", "HelixTheme")
            .Replace("HierarchicalDataTemplate", "TreeDataTemplate")
            .Replace(", UpdateSourceTrigger=PropertyChanged", "")
            .Replace("helix:HelixChartSeries Name=", "helix:HelixChartSeries Label=")
            .Replace(" AutoGenerateColumns=\"False\"", "")
            .Replace("SelectedItemChanged=", "SelectionChanged=")
            .Replace("DisplayDateStart=", "Minimum=")
            .Replace("DisplayDateEnd=", "Maximum=")
            .Replace("ToolTipService.InitialShowDelay=", "ToolTip.ShowDelay=")
            .Replace("ToolTipService.Placement=", "ToolTip.Placement=")
            .Replace("WPF ·", "Avalonia ·");
        code = ToolTip().Replace(code, "ToolTip.Tip=");
        code = ToolTipElement().Replace(code, "<$1ToolTip.Tip>");
        code = ElementName().Replace(code, "{Binding #$1");
        code = Visibility().Replace(code, m => m.Groups[1].Value == "Visible" ? "IsVisible=\"True\"" : "IsVisible=\"False\"");
        return code;
    }

    [GeneratedRegex(@"(?<=\s)ToolTip=(?="")")]
    private static partial Regex ToolTip();

    [GeneratedRegex(@"<(/?)helix:\w+\.ToolTip>")]
    private static partial Regex ToolTipElement();

    [GeneratedRegex(@"\{Binding ElementName=(\w+)")]
    private static partial Regex ElementName();

    [GeneratedRegex(@"Visibility=""(Visible|Collapsed|Hidden)""")]
    private static partial Regex Visibility();
}
