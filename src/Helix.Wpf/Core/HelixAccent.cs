using System.Windows;
using System.Windows.Media;

namespace Helix.Wpf;

/// <summary>
/// Shared behavior of <c>Tone</c> / <c>Accent</c> / <c>OnAccent</c>.
/// <list type="bullet">
/// <item><c>ToneBrush</c> follows the theme brush of the tone ({DynamicResource Helix.Danger}…).</item>
/// <item>The default style sets <c>Accent</c> to ToneBrush, so a local value or your own style wins (normal WPF precedence).</item>
/// <item><c>AutoOnAccent</c> is the text color on the accent: the theme's for a tone, black or white for contrast on a custom brush.</item>
/// </list>
/// </summary>
internal static class HelixAccent
{
    private static readonly Brush Dark = Frozen(Color.FromRgb(0x18, 0x18, 0x1b));
    private static readonly Brush Light = Frozen(Colors.White);

    public static DependencyProperty Tone(Type owner) =>
        DependencyProperty.Register("Tone", typeof(HelixTone), owner, new PropertyMetadata(HelixTone.Primary, (d, _) => ApplyTone(d)));

    public static DependencyProperty Accent(Type owner) =>
        DependencyProperty.Register("Accent", typeof(Brush), owner,
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => UpdateOnAccent(d)));

    public static DependencyProperty OnAccent(Type owner) =>
        DependencyProperty.Register("OnAccent", typeof(Brush), owner, new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static DependencyProperty ToneBrush(Type owner) =>
        DependencyProperty.Register("ToneBrush", typeof(Brush), owner, new PropertyMetadata(null, (d, _) => UpdateOnAccent(d)));

    public static DependencyProperty OnToneBrush(Type owner) =>
        DependencyProperty.Register("OnToneBrush", typeof(Brush), owner, new PropertyMetadata(null, (d, _) => UpdateOnAccent(d)));

    public static DependencyProperty AutoOnAccent(Type owner) =>
        DependencyProperty.Register("AutoOnAccent", typeof(Brush), owner, new PropertyMetadata(null));

    /// <summary>Call from the constructor.</summary>
    public static void Init(FrameworkElement element) => ApplyTone(element);

    private static void ApplyTone(DependencyObject d)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        var tone = (HelixTone)d.GetValue(Find(d, "Tone"));
        element.SetResourceReference(Find(d, "ToneBrush"), HelixKeys.Of(tone));
        element.SetResourceReference(Find(d, "OnToneBrush"), HelixKeys.OnOf(tone));
    }

    private static void UpdateOnAccent(DependencyObject d)
    {
        var accent = d.GetValue(Find(d, "Accent")) as Brush;
        var toneBrush = d.GetValue(Find(d, "ToneBrush")) as Brush;
        Brush? auto = ReferenceEquals(accent, toneBrush) || accent is null
            ? d.GetValue(Find(d, "OnToneBrush")) as Brush
            : accent is SolidColorBrush solid ? (IsLight(solid.Color) ? Dark : Light) : Light;
        d.SetValue(Find(d, "AutoOnAccent"), auto);
    }

    /// <summary>Relative luminance high enough that dark text reads better on it.</summary>
    public static bool IsLight(Color c)
    {
        static double Lin(byte v)
        {
            var s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Lin(c.R) + 0.7152 * Lin(c.G) + 0.0722 * Lin(c.B) > 0.4;
    }

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static readonly Dictionary<(Type, string), DependencyProperty> Cache = [];

    /// <summary>A control's own XxxProperty field, cached per type.</summary>
    private static DependencyProperty Find(DependencyObject d, string name)
    {
        var type = d.GetType();
        lock (Cache)
        {
            if (Cache.TryGetValue((type, name), out var dp))
            {
                return dp;
            }

            for (var t = type; t is not null; t = t.BaseType)
            {
                var field = t.GetField(name + "Property", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field?.GetValue(null) is DependencyProperty found)
                {
                    Cache[(type, name)] = found;
                    return found;
                }
            }

            throw new InvalidOperationException($"{type.Name} has no {name}Property.");
        }
    }
}
