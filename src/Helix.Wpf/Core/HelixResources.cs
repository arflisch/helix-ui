using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Helix.Wpf;

/// <summary>
/// The Helix theme. Add it once to App.xaml; every control reads its brushes with DynamicResource,
/// so changing <see cref="Mode"/> or a palette at runtime restyles the whole app instantly.
/// </summary>
/// <example>
/// &lt;Application.Resources&gt;
///     &lt;helix:HelixResources Mode="System"&gt;
///         &lt;helix:HelixResources.Light&gt;&lt;helix:HelixPalette Primary="#c2410c" /&gt;&lt;/helix:HelixResources.Light&gt;
///     &lt;/helix:HelixResources&gt;
/// &lt;/Application.Resources&gt;
/// </example>
public class HelixResources : ResourceDictionary
{
    private HelixThemeMode _mode = HelixThemeMode.Light;
    private HelixPalette? _light, _dark;
    private CornerRadius _radius = new(10);
    private double _controlHeight = 38;
    private FontFamily _fontFamily = new("Inter, Segoe UI Variable Text, Segoe UI");
    private double _fontSize = 14;
    private bool _listening;
    private bool _styleNative = true;
    private readonly ResourceDictionary _native = new() { Source = new Uri("pack://application:,,,/Helix.Wpf;component/Themes/Native.xaml", UriKind.Absolute) };

    public HelixResources()
    {
        Current = this;
        MergedDictionaries.Add(_native);
        Refresh();
    }

    /// <summary>Gives scroll bars and tooltips of the whole app the Helix look (true by default).</summary>
    public bool StyleNativeControls
    {
        get => _styleNative;
        set
        {
            _styleNative = value;
            if (value && !MergedDictionaries.Contains(_native)) MergedDictionaries.Add(_native);
            if (!value) MergedDictionaries.Remove(_native);
        }
    }

    /// <summary>The theme added last (normally the one in App.xaml), to change it from code.</summary>
    public static HelixResources? Current { get; private set; }

    /// <summary>Raised after the mode or a palette changed (e.g. to update a theme toggle).</summary>
    public event EventHandler? ThemeChanged;

    public HelixThemeMode Mode
    {
        get => _mode;
        set { _mode = value; ListenToWindows(value == HelixThemeMode.System); Refresh(); }
    }

    /// <summary>Changes to the light colors (null values keep Helix's).</summary>
    public HelixPalette? Light { get => _light; set { _light = value; Refresh(); } }

    /// <summary>Changes to the dark colors (null values keep Helix's).</summary>
    public HelixPalette? Dark { get => _dark; set { _dark = value; Refresh(); } }

    /// <summary>Corner radius of controls and cards (10 by default).</summary>
    public CornerRadius Radius { get => _radius; set { _radius = value; Refresh(); } }

    /// <summary>Height of buttons and fields (38 by default; 32 for compact apps).</summary>
    public double ControlHeight { get => _controlHeight; set { _controlHeight = value; Refresh(); } }

    public FontFamily FontFamily { get => _fontFamily; set { _fontFamily = value; Refresh(); } }
    public double FontSize { get => _fontSize; set { _fontSize = value; Refresh(); } }

    private string? _follow;

    /// <summary>
    /// Takes the colors of the app's own theme, so Helix screens blend in: "MaterialDesign", or your keys,
    /// e.g. "Primary=AccentBrush;Surface=CardBrush;Text=TextBrush" (Helix token = app brush key; "a|b" tries a then b).
    /// Call Refresh() after the app switches theme.
    /// </summary>
    public string? Follow { get => _follow; set { _follow = value; Refresh(); } }

    /// <summary>MaterialDesign In XAML keys (v4 and v5 names) for each Helix token.</summary>
    public const string MaterialDesignMap =
        "Primary=PrimaryHueMidBrush|MaterialDesign.Brush.Primary;" +
        "OnPrimary=PrimaryHueMidForegroundBrush|MaterialDesign.Brush.Primary.Foreground;" +
        "Background=MaterialDesignPaper|MaterialDesign.Brush.Background;" +
        "Surface=MaterialDesignCardBackground|MaterialDesign.Brush.Card.Background;" +
        "Text=MaterialDesignBody|MaterialDesign.Brush.Foreground;" +
        "TextMuted=MaterialDesignBodyLight|MaterialDesign.Brush.ForegroundLight;" +
        "Border=MaterialDesignDivider|MaterialDesign.Brush.Separator;" +
        "Danger=MaterialDesignValidationErrorBrush|MaterialDesign.Brush.ValidationError";

    /// <summary>Overrides tokens with the app's brushes (Follow).</summary>
    private void ApplyFollow()
    {
        if (string.IsNullOrWhiteSpace(_follow) || Application.Current is not { } app) return;
        var map = _follow.Trim().Equals("MaterialDesign", StringComparison.OrdinalIgnoreCase) ? MaterialDesignMap : _follow;
        foreach (var pair in map.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2) continue;
            var helixKey = parts[0].StartsWith("Helix.", StringComparison.Ordinal) ? parts[0] : "Helix." + parts[0];
            var brush = parts[1].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(app.TryFindResource).OfType<SolidColorBrush>().FirstOrDefault();
            if (brush is null) continue;
            var copy = new SolidColorBrush(brush.Color);
            copy.Freeze();
            this[helixKey] = copy;
            if (helixKey == HelixKeys.Primary)
            {
                var soft = new SolidColorBrush(Color.FromArgb(IsDark ? (byte)0x33 : (byte)0x1F, brush.Color.R, brush.Color.G, brush.Color.B));
                soft.Freeze();
                this[HelixKeys.PrimarySoft] = soft;
            }
        }
    }

    /// <summary>True when the dark colors are in use (Dark, or System with Windows in dark mode).</summary>
    public bool IsDark => Mode == HelixThemeMode.Dark || (Mode == HelixThemeMode.System && WindowsUsesDarkMode());

    /// <summary>Writes every token into this dictionary; DynamicResource consumers update at once.</summary>
    public void Refresh()
    {
        var palette = IsDark ? HelixPalette.DefaultDark.With(_dark) : HelixPalette.DefaultLight.With(_light);
        foreach (var (key, color) in palette.Tokens())
        {
            var brush = new SolidColorBrush(color ?? Colors.Transparent);
            brush.Freeze();
            this[key] = brush;
        }

        // Soft primary for selected rows and highlighted items.
        var primary = palette.Primary ?? Colors.SteelBlue;
        var soft = new SolidColorBrush(Color.FromArgb(IsDark ? (byte)0x33 : (byte)0x1F, primary.R, primary.G, primary.B));
        soft.Freeze();
        this[HelixKeys.PrimarySoft] = soft;

        ApplyFollow();

        this[HelixKeys.Radius] = _radius;
        this[HelixKeys.RadiusSmall] = new CornerRadius(Math.Max(0, _radius.TopLeft - 4));
        this[HelixKeys.ControlHeight] = _controlHeight;
        this[HelixKeys.FontFamily] = _fontFamily;
        this[HelixKeys.FontSize] = _fontSize;
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
