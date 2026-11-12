using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Helix.Wpf;

/// <summary>
/// A button: Variant (Solid, Soft, Outline, Ghost), Tone or any Accent brush, Size, CornerRadius, icons and a loading state.
/// </summary>
public class HelixButton : Button
{
    static HelixButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixButton), new FrameworkPropertyMetadata(typeof(HelixButton)));

    public HelixButton() => HelixAccent.Init(this);

    // ----- Color -----
    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixButton));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixButton));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixButton));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixButton));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixButton));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixButton));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    // ----- Look -----
    public static readonly DependencyProperty VariantProperty = HelixProperty.Register<HelixButton, HelixVariant>(nameof(Variant), HelixVariant.Solid);
    public static readonly DependencyProperty SizeProperty = HelixProperty.Register<HelixButton, HelixSize>(nameof(Size), HelixSize.Md);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixButton, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));
    public static readonly DependencyProperty IconProperty = HelixProperty.Register<HelixButton, string?>(nameof(Icon), null);
    public static readonly DependencyProperty IconEndProperty = HelixProperty.Register<HelixButton, string?>(nameof(IconEnd), null);
    public static readonly DependencyProperty IsLoadingProperty = HelixProperty.Register<HelixButton, bool>(nameof(IsLoading), false);

    public HelixVariant Variant { get => (HelixVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public HelixSize Size { get => (HelixSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>Built-in icon before the text (see HelixIcon.Names).</summary>
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Built-in icon after the text.</summary>
    public string? IconEnd { get => (string?)GetValue(IconEndProperty); set => SetValue(IconEndProperty, value); }

    /// <summary>Shows a spinner and ignores clicks (keeps the button's width).</summary>
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }

    protected override void OnClick()
    {
        if (!IsLoading)
        {
            base.OnClick();
        }
    }
}
