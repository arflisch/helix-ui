using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Velo.Wpf;

/// <summary>
/// An on/off switch. Every part is a property: Accent (track on), TrackBrush, ThumbBrush, ThumbOnBrush,
/// an icon or a short text inside, a description under the label, and the side of the label.
/// Content is the label.
/// </summary>
[TemplatePart(Name = "PART_Track", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PART_Thumb", Type = typeof(FrameworkElement))]
public class VeloSwitch : ToggleButton
{
    static VeloSwitch() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloSwitch), new FrameworkPropertyMetadata(typeof(VeloSwitch)));

    public VeloSwitch() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloSwitch));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloSwitch));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloSwitch));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloSwitch));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloSwitch));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloSwitch));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty TrackBrushProperty = VeloProperty.Register<VeloSwitch, Brush?>(nameof(TrackBrush), null);
    public static readonly DependencyProperty ThumbBrushProperty = VeloProperty.Register<VeloSwitch, Brush?>(nameof(ThumbBrush), null);
    public static readonly DependencyProperty ThumbOnBrushProperty = VeloProperty.Register<VeloSwitch, Brush?>(nameof(ThumbOnBrush), null);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloSwitch, CornerRadius>(nameof(CornerRadius), new CornerRadius(999));
    public static readonly DependencyProperty SizeProperty = VeloProperty.Register<VeloSwitch, VeloSize>(nameof(Size), VeloSize.Md);
    public static readonly DependencyProperty OnTextProperty = VeloProperty.Register<VeloSwitch, string?>(nameof(OnText), null);
    public static readonly DependencyProperty OffTextProperty = VeloProperty.Register<VeloSwitch, string?>(nameof(OffText), null);
    public static readonly DependencyProperty OnIconProperty = VeloProperty.Register<VeloSwitch, string?>(nameof(OnIcon), null);
    public static readonly DependencyProperty OffIconProperty = VeloProperty.Register<VeloSwitch, string?>(nameof(OffIcon), null);
    public static readonly DependencyProperty DescriptionProperty = VeloProperty.Register<VeloSwitch, string?>(nameof(Description), null);
    public static readonly DependencyProperty LabelPositionProperty = VeloProperty.Register<VeloSwitch, VeloLabelPosition>(nameof(LabelPosition), VeloLabelPosition.End);
    public static readonly DependencyProperty TrackMinWidthProperty = VeloProperty.Register<VeloSwitch, double>(nameof(TrackMinWidth), 0d);

    /// <summary>Track color when OFF.</summary>
    public Brush? TrackBrush { get => (Brush?)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }

    /// <summary>Thumb (knob) color.</summary>
    public Brush? ThumbBrush { get => (Brush?)GetValue(ThumbBrushProperty); set => SetValue(ThumbBrushProperty, value); }

    /// <summary>Thumb color when ON (ThumbBrush when not set).</summary>
    public Brush? ThumbOnBrush { get => (Brush?)GetValue(ThumbOnBrushProperty); set => SetValue(ThumbOnBrushProperty, value); }

    /// <summary>Corner radius of track and thumb; e.g. 4 for a square switch.</summary>
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public VeloSize Size { get => (VeloSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Short text in the track when ON (e.g. "ON", "Auto"); the track grows to fit.</summary>
    public string? OnText { get => (string?)GetValue(OnTextProperty); set => SetValue(OnTextProperty, value); }

    /// <summary>Short text in the track when OFF.</summary>
    public string? OffText { get => (string?)GetValue(OffTextProperty); set => SetValue(OffTextProperty, value); }

    /// <summary>Built-in icon in the thumb when ON (e.g. "check", "moon").</summary>
    public string? OnIcon { get => (string?)GetValue(OnIconProperty); set => SetValue(OnIconProperty, value); }

    /// <summary>Built-in icon in the thumb when OFF (e.g. "x", "sun").</summary>
    public string? OffIcon { get => (string?)GetValue(OffIconProperty); set => SetValue(OffIconProperty, value); }

    /// <summary>Second, muted line under the label.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Label after the switch (End) or before it (Start).</summary>
    public VeloLabelPosition LabelPosition { get => (VeloLabelPosition)GetValue(LabelPositionProperty); set => SetValue(LabelPositionProperty, value); }

    /// <summary>Minimum track width when it shows a text, so several switches line up.</summary>
    public double TrackMinWidth { get => (double)GetValue(TrackMinWidthProperty); set => SetValue(TrackMinWidthProperty, value); }

    private FrameworkElement? _track, _thumb;
    private readonly TranslateTransform _offset = new();

    public override void OnApplyTemplate()
    {
        if (_track is not null)
        {
            _track.SizeChanged -= OnTrackSizeChanged;
        }

        base.OnApplyTemplate();
        _track = GetTemplateChild("PART_Track") as FrameworkElement;
        _thumb = GetTemplateChild("PART_Thumb") as FrameworkElement;
        if (_thumb is not null)
        {
            _thumb.RenderTransform = _offset;
        }

        if (_track is not null)
        {
            _track.SizeChanged += OnTrackSizeChanged;
        }

        MoveThumb(animate: false);
    }

    private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e) => MoveThumb(animate: false);

    protected override void OnChecked(RoutedEventArgs e)
    {
        base.OnChecked(e);
        MoveThumb(animate: true);
    }

    protected override void OnUnchecked(RoutedEventArgs e)
    {
        base.OnUnchecked(e);
        MoveThumb(animate: true);
    }

    /// <summary>The thumb slides to the end of the track, whatever its width (text, size).</summary>
    private void MoveThumb(bool animate)
    {
        if (_track is null || _thumb is null)
        {
            return;
        }

        var travel = Math.Max(0, _track.ActualWidth - _thumb.ActualWidth - _thumb.Margin.Left - _thumb.Margin.Right);
        var target = IsChecked == true ? travel : 0;
        if (animate && SystemParameters.ClientAreaAnimation)
        {
            var animation = new DoubleAnimation(target, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };
            _offset.BeginAnimation(TranslateTransform.XProperty, animation);
        }
        else
        {
            _offset.BeginAnimation(TranslateTransform.XProperty, null);
            _offset.X = target;
        }
    }
}

/// <summary>A check box with Accent, CheckBrush (the tick), CornerRadius and a Description line.</summary>
public class VeloCheckBox : CheckBox
{
    static VeloCheckBox() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloCheckBox), new FrameworkPropertyMetadata(typeof(VeloCheckBox)));

    public VeloCheckBox() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloCheckBox));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloCheckBox));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloCheckBox));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloCheckBox));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloCheckBox));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloCheckBox));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty CheckBrushProperty = VeloProperty.Register<VeloCheckBox, Brush?>(nameof(CheckBrush), null);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloCheckBox, CornerRadius>(nameof(CornerRadius), new CornerRadius(5));
    public static readonly DependencyProperty DescriptionProperty = VeloProperty.Register<VeloCheckBox, string?>(nameof(Description), null);

    /// <summary>Color of the tick (OnAccent when not set).</summary>
    public Brush? CheckBrush { get => (Brush?)GetValue(CheckBrushProperty); set => SetValue(CheckBrushProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
}

/// <summary>A radio button with Accent and a Description line.</summary>
public class VeloRadioButton : RadioButton
{
    static VeloRadioButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloRadioButton), new FrameworkPropertyMetadata(typeof(VeloRadioButton)));

    public VeloRadioButton() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloRadioButton));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloRadioButton));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloRadioButton));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloRadioButton));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloRadioButton));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloRadioButton));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty DescriptionProperty = VeloProperty.Register<VeloRadioButton, string?>(nameof(Description), null);

    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
}

/// <summary>A slider with Accent (filled part) and TrackBrush (rest).</summary>
public class VeloSlider : Slider
{
    static VeloSlider() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloSlider), new FrameworkPropertyMetadata(typeof(VeloSlider)));

    public VeloSlider() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloSlider));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloSlider));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloSlider));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloSlider));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloSlider));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloSlider));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty TrackBrushProperty = VeloProperty.Register<VeloSlider, Brush?>(nameof(TrackBrush), null);

    public Brush? TrackBrush { get => (Brush?)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
}
