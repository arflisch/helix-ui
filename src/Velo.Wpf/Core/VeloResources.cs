using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Velo.Wpf;

/// <summary>
/// The Velo theme. Add it once to App.xaml; every control reads its brushes with DynamicResource,
/// so changing <see cref="Mode"/> or a palette at runtime restyles the whole app instantly.
/// </summary>
/// <example>
/// &lt;Application.Resources&gt;
///     &lt;velo:VeloResources Mode="System"&gt;
///         &lt;velo:VeloResources.Light&gt;&lt;velo:VeloPalette Primary="#c2410c" /&gt;&lt;/velo:VeloResources.Light&gt;
///     &lt;/velo:VeloResources&gt;
/// &lt;/Application.Resources&gt;
/// </example>
public class VeloResources : ResourceDictionary
{
    private VeloThemeMode _mode = VeloThemeMode.Light;
    private VeloPalette? _light, _dark;
    private CornerRadius _radius = new(10);
    private double _controlHeight = 38;
    private FontFamily _fontFamily = new("Inter, Segoe UI Variable Text, Segoe UI");
    private double _fontSize = 14;
    private bool _listening;

    public VeloResources()
    {
        Current = this;
        Refresh();
    }

    /// <summary>The theme added last (normally the one in App.xaml), to change it from code.</summary>
    public static VeloResources? Current { get; private set; }

    /// <summary>Raised after the mode or a palette changed (e.g. to update a theme toggle).</summary>
    public event EventHandler? ThemeChanged;

    public VeloThemeMode Mode
    {
        get => _mode;
        set { _mode = value; ListenToWindows(value == VeloThemeMode.System); Refresh(); }
    }

    /// <summary>Changes to the light colors (null values keep Velo's).</summary>
    public VeloPalette? Light { get => _light; set { _light = value; Refresh(); } }

    /// <summary>Changes to the dark colors (null values keep Velo's).</summary>
    public VeloPalette? Dark { get => _dark; set { _dark = value; Refresh(); } }

    /// <summary>Corner radius of controls and cards (10 by default).</summary>
    public CornerRadius Radius { get => _radius; set { _radius = value; Refresh(); } }

    /// <summary>Height of buttons and fields (38 by default; 32 for compact apps).</summary>
    public double ControlHeight { get => _controlHeight; set { _controlHeight = value; Refresh(); } }

    public FontFamily FontFamily { get => _fontFamily; set { _fontFamily = value; Refresh(); } }
    public double FontSize { get => _fontSize; set { _fontSize = value; Refresh(); } }

    /// <summary>True when the dark colors are in use (Dark, or System with Windows in dark mode).</summary>
    public bool IsDark => Mode == VeloThemeMode.Dark || (Mode == VeloThemeMode.System && WindowsUsesDarkMode());

    /// <summary>Writes every token into this dictionary; DynamicResource consumers update at once.</summary>
    public void Refresh()
    {
        var palette = IsDark ? VeloPalette.DefaultDark.With(_dark) : VeloPalette.DefaultLight.With(_light);
        foreach (var (key, color) in palette.Tokens())
        {
            var brush = new SolidColorBrush(color ?? Colors.Transparent);
            brush.Freeze();
            this[key] = brush;
        }

        this[VeloKeys.Radius] = _radius;
        this[VeloKeys.RadiusSmall] = new CornerRadius(Math.Max(0, _radius.TopLeft - 4));
        this[VeloKeys.ControlHeight] = _controlHeight;
        this[VeloKeys.FontFamily] = _fontFamily;
        this[VeloKeys.FontSize] = _fontSize;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ListenToWindows(bool listen)
    {
        if (listen == _listening)
        {
            return;
        }

        _listening = listen;
        if (listen)
        {
            SystemEvents.UserPreferenceChanged += OnWindowsPreferenceChanged;
        }
        else
        {
            SystemEvents.UserPreferenceChanged -= OnWindowsPreferenceChanged;
        }
    }

    private void OnWindowsPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            // Raised on a system thread: refresh on the UI thread.
            Application.Current?.Dispatcher.BeginInvoke(Refresh);
        }
    }

    /// <summary>Windows "app mode" (Settings › Personalization › Colors).</summary>
    public static bool WindowsUsesDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int light && light == 0;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }
}
