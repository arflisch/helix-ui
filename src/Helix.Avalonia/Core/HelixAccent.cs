using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Helix.Avalonia;

/// <summary>
/// The properties shared by every control that has a color: <c>Tone</c> (a theme color), <c>Accent</c> (any brush, replaces the tone)
/// and <c>OnAccent</c> (text on the accent; automatic black or white for a custom accent).
/// The control themes set Accent and ToneOnBrush per tone; a local Accent wins, as usual in Avalonia.
/// </summary>
public sealed class HelixAccent : AvaloniaObject
{
    private HelixAccent()
    {
    }

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public static readonly StyledProperty<HelixTone> ToneProperty = AvaloniaProperty.Register<HelixAccent, HelixTone>("Tone");

    /// <summary>The control's color: the tone's brush, or yours.</summary>
    public static readonly StyledProperty<IBrush?> AccentProperty = AvaloniaProperty.Register<HelixAccent, IBrush?>("Accent");

    /// <summary>Text color on the accent; when null, the tone's or a contrasting black/white for a custom accent.</summary>
    public static readonly StyledProperty<IBrush?> OnAccentProperty = AvaloniaProperty.Register<HelixAccent, IBrush?>("OnAccent");

    /// <summary>Set by the theme: text color on the tone.</summary>
    public static readonly StyledProperty<IBrush?> ToneOnBrushProperty = AvaloniaProperty.Register<HelixAccent, IBrush?>("ToneOnBrush");

    /// <summary>The text color on the accent actually used by the templates.</summary>
    public static readonly StyledProperty<IBrush?> ResolvedOnAccentProperty = AvaloniaProperty.Register<HelixAccent, IBrush?>("ResolvedOnAccent");

    private static readonly IBrush DarkText = new ImmutableSolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1b));
    private static readonly IBrush LightText = new ImmutableSolidColorBrush(Colors.White);

    /// <summary>Call from OnPropertyChanged; <paramref name="customAccent"/> remembers whether Accent was set by the app.</summary>
    internal static void OnChanged(AvaloniaObject control, AvaloniaPropertyChangedEventArgs change, ref bool customAccent)
    {
        if (change.Property == AccentProperty)
        {
            customAccent = change.Priority <= BindingPriority.LocalValue && change.NewValue is not null;
        }
        else if (change.Property != OnAccentProperty && change.Property != ToneOnBrushProperty)
        {
            return;
        }

        var onAccent = control.GetValue(OnAccentProperty);
        var resolved = onAccent
            ?? (customAccent && control.GetValue(AccentProperty) is ISolidColorBrush solid ? (IsLight(solid.Color) ? DarkText : LightText) : null)
            ?? control.GetValue(ToneOnBrushProperty);
        control.SetCurrentValue(ResolvedOnAccentProperty, resolved);
    }

    internal static bool IsLight(Color color) =>
        (0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B)) * color.A / 255.0 + (255 - color.A) / 255.0 > 0.45;

    private static double Channel(byte value)
    {
        var c = value / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
