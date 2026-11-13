using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace Helix.Avalonia;

/// <summary>
/// The Helix theme. Add it once to App.axaml, after FluentTheme; every control reads its brushes with DynamicResource,
/// so changing <see cref="Mode"/> or a palette at runtime restyles the whole app instantly.
/// </summary>
/// <example>
/// &lt;Application.Styles&gt;
///     &lt;FluentTheme /&gt;
///     &lt;helix:HelixTheme Mode="System"&gt;
///         &lt;helix:HelixTheme.Light&gt;&lt;helix:HelixPalette Primary="#c2410c" /&gt;&lt;/helix:HelixTheme.Light&gt;
///     &lt;/helix:HelixTheme&gt;
/// &lt;/Application.Styles&gt;
/// </example>
public class HelixTheme : Styles
{
    private HelixThemeMode _mode = HelixThemeMode.System;
    private HelixPalette? _light, _dark;
    private CornerRadius _radius = new(10);
    private double _controlHeight = 38;
    private FontFamily _fontFamily = new("Inter, Segoe UI Variable Text, Segoe UI, SF Pro Text, .AppleSystemUIFont, Ubuntu, sans-serif");
    private double _fontSize = 14;

    public HelixTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
        Current = this;
        Refresh();
    }

    /// <summary>The theme added last (normally the one in App.axaml), to change it from code.</summary>
    public static HelixTheme? Current { get; private set; }

    /// <summary>Raised after the mode or a palette changed (e.g. to update a theme toggle).</summary>
    public event EventHandler? ThemeChanged;

    /// <summary>Light, Dark, or System (follows the operating system, live).</summary>
    public HelixThemeMode Mode
    {
        get => _mode;
        set { _mode = value; ApplyMode(); ThemeChanged?.Invoke(this, EventArgs.Empty); }
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

    /// <summary>True when the app currently shows dark colors.</summary>
    public static bool IsDark => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    /// <summary>Switches between light and dark (leaves System mode).</summary>
    public void Toggle() => Mode = IsDark ? HelixThemeMode.Light : HelixThemeMode.Dark;

    private void ApplyMode()
    {
        if (Application.Current is not { } app) return;
        app.RequestedThemeVariant = _mode switch
        {
            HelixThemeMode.Light => ThemeVariant.Light,
            HelixThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }

    /// <summary>Rebuilds every brush and size (called by each setter).</summary>
    public void Refresh()
    {
        Resources.ThemeDictionaries[ThemeVariant.Light] = Brushes(HelixPalette.DefaultLight.With(_light), dark: false);
        Resources.ThemeDictionaries[ThemeVariant.Dark] = Brushes(HelixPalette.DefaultDark.With(_dark), dark: true);
        Resources[HelixKeys.Radius] = _radius;
        Resources[HelixKeys.RadiusSmall] = new CornerRadius(_radius.TopLeft * 0.7, _radius.TopRight * 0.7, _radius.BottomRight * 0.7, _radius.BottomLeft * 0.7);
        Resources[HelixKeys.ControlHeight] = _controlHeight;
        Resources[HelixKeys.FontFamily] = _fontFamily;
        Resources[HelixKeys.FontSize] = _fontSize;
        ApplyMode();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private static ResourceDictionary Brushes(HelixPalette palette, bool dark)
    {
        var dictionary = new ResourceDictionary();
        foreach (var (key, color) in palette.Tokens())
        {
            if (color is { } c) dictionary[key] = new SolidColorBrush(c);
        }

        if (palette.Primary is { } primary)
        {
            dictionary[HelixKeys.PrimarySoft] = new SolidColorBrush(Color.FromArgb(dark ? (byte)0x33 : (byte)0x1F, primary.R, primary.G, primary.B));
        }

        return dictionary;
    }
}
