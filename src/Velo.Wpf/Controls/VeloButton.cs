using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Velo.Wpf;

/// <summary>
/// A button: Variant (Solid, Soft, Outline, Ghost), Tone or any Accent brush, Size, CornerRadius, icons and a loading state.
/// </summary>
public class VeloButton : Button
{
    static VeloButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloButton), new FrameworkPropertyMetadata(typeof(VeloButton)));

    public VeloButton() => VeloAccent.Init(this);

    // ----- Color -----
    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloButton));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloButton));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloButton));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloButton));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloButton));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloButton));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    // ----- Look -----
    public static readonly DependencyProperty VariantProperty = VeloProperty.Register<VeloButton, VeloVariant>(nameof(Variant), VeloVariant.Solid);
    public static readonly DependencyProperty SizeProperty = VeloProperty.Register<VeloButton, VeloSize>(nameof(Size), VeloSize.Md);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloButton, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));
    public static readonly DependencyProperty IconProperty = VeloProperty.Register<VeloButton, string?>(nameof(Icon), null);
    public static readonly DependencyProperty IconEndProperty = VeloProperty.Register<VeloButton, string?>(nameof(IconEnd), null);
    public static readonly DependencyProperty IsLoadingProperty = VeloProperty.Register<VeloButton, bool>(nameof(IsLoading), false);

    public VeloVariant Variant { get => (VeloVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public VeloSize Size { get => (VeloSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>Built-in icon before the text (see VeloIcon.Names).</summary>
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
