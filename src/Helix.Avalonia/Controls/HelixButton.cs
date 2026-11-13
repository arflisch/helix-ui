using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Helix.Avalonia;

/// <summary>A button: Variant (Solid, Soft, Outline, Ghost), Tone or any Accent brush, Size, icons and a loading state.</summary>
public class HelixButton : Button
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixButton>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixButton>();
    public static readonly StyledProperty<IBrush?> OnAccentProperty = HelixAccent.OnAccentProperty.AddOwner<HelixButton>();
    public static readonly StyledProperty<IBrush?> ToneOnBrushProperty = HelixAccent.ToneOnBrushProperty.AddOwner<HelixButton>();
    public static readonly StyledProperty<IBrush?> ResolvedOnAccentProperty = HelixAccent.ResolvedOnAccentProperty.AddOwner<HelixButton>();
    public static readonly StyledProperty<HelixVariant> VariantProperty = AvaloniaProperty.Register<HelixButton, HelixVariant>(nameof(Variant));
    public static readonly StyledProperty<HelixSize> SizeProperty = AvaloniaProperty.Register<HelixButton, HelixSize>(nameof(Size), HelixSize.Md);
    public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<HelixButton, string?>(nameof(Icon));
    public static readonly StyledProperty<string?> IconEndProperty = AvaloniaProperty.Register<HelixButton, string?>(nameof(IconEnd));
    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<HelixButton, bool>(nameof(IsLoading));

    private bool _customAccent;

    protected override Type StyleKeyOverride => typeof(HelixButton);

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public IBrush? OnAccent { get => GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public IBrush? ToneOnBrush { get => GetValue(ToneOnBrushProperty); set => SetValue(ToneOnBrushProperty, value); }
    public IBrush? ResolvedOnAccent => GetValue(ResolvedOnAccentProperty);

    public HelixVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public HelixSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Built-in icon before the text (see HelixIcon.Names).</summary>
    public string? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Built-in icon after the text.</summary>
    public string? IconEnd { get => GetValue(IconEndProperty); set => SetValue(IconEndProperty, value); }

    /// <summary>Shows a spinner and ignores clicks (keeps the button's width).</summary>
    public bool IsLoading { get => GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        HelixAccent.OnChanged(this, change, ref _customAccent);
        if (change.Property == CommandProperty)
        {
            if (change.OldValue is HelixAsyncCommand oldCommand) oldCommand.PropertyChanged -= OnCommandRunning;
            if (change.NewValue is HelixAsyncCommand newCommand) newCommand.PropertyChanged += OnCommandRunning;
        }
    }

    private void OnCommandRunning(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is HelixAsyncCommand command && e.PropertyName == nameof(HelixAsyncCommand.IsRunning))
        {
            SetCurrentValue(IsLoadingProperty, command.IsRunning);
        }
    }

    protected override void OnClick()
    {
        if (!IsLoading) base.OnClick();
    }
}
