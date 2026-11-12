using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Helix.Wpf;

/// <summary>
/// An on/off switch. Every part is a property: Accent (track on), TrackBrush, ThumbBrush, ThumbOnBrush,
/// an icon or a short text inside, a description under the label, and the side of the label.
/// Content is the label.
/// </summary>
[TemplatePart(Name = "PART_Track", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PART_Thumb", Type = typeof(FrameworkElement))]
public class HelixSwitch : ToggleButton
{
    static HelixSwitch() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixSwitch), new FrameworkPropertyMetadata(typeof(HelixSwitch)));

    public HelixSwitch() => HelixAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixSwitch));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixSwitch));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixSwitch));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixSwitch));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixSwitch));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixSwitch));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty TrackBrushProperty = HelixProperty.Register<HelixSwitch, Brush?>(nameof(TrackBrush), null);
    public static readonly DependencyProperty ThumbBrushProperty = HelixProperty.Register<HelixSwitch, Brush?>(nameof(ThumbBrush), null);
    public static readonly DependencyProperty ThumbOnBrushProperty = HelixProperty.Register<HelixSwitch, Brush?>(nameof(ThumbOnBrush), null);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixSwitch, CornerRadius>(nameof(CornerRadius), new CornerRadius(999));
    public static readonly DependencyProperty SizeProperty = HelixProperty.Register<HelixSwitch, HelixSize>(nameof(Size), HelixSize.Md);
    public static readonly DependencyProperty OnTextProperty = HelixProperty.Register<HelixSwitch, string?>(nameof(OnText), null);
    public static readonly DependencyProperty OffTextProperty = HelixProperty.Register<HelixSwitch, string?>(nameof(OffText), null);
    public static readonly DependencyProperty OnIconProperty = HelixProperty.Register<HelixSwitch, string?>(nameof(OnIcon), null);
    public static readonly DependencyProperty OffIconProperty = HelixProperty.Register<HelixSwitch, string?>(nameof(OffIcon), null);
    public static readonly DependencyProperty DescriptionProperty = HelixProperty.Register<HelixSwitch, string?>(nameof(Description), null);
    public static readonly DependencyProperty LabelPositionProperty = HelixProperty.Register<HelixSwitch, HelixLabelPosition>(nameof(LabelPosition), HelixLabelPosition.End);
    public static readonly DependencyProperty TrackMinWidthProperty = HelixProperty.Register<HelixSwitch, double>(nameof(TrackMinWidth), 0d);

    /// <summary>Track color when OFF.</summary>
    public Brush? TrackBrush { get => (Brush?)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }

    /// <summary>Thumb (knob) color.</summary>
    public Brush? ThumbBrush { get => (Brush?)GetValue(ThumbBrushProperty); set => SetValue(ThumbBrushProperty, value); }

    /// <summary>Thumb color when ON (ThumbBrush when not set).</summary>
    public Brush? ThumbOnBrush { get => (Brush?)GetValue(ThumbOnBrushProperty); set => SetValue(ThumbOnBrushProperty, value); }

    /// <summary>Corner radius of track and thumb; e.g. 4 for a square switch.</summary>
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public HelixSize Size { get => (HelixSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

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
    public HelixLabelPosition LabelPosition { get => (HelixLabelPosition)GetValue(LabelPositionProperty); set => SetValue(LabelPositionProperty, value); }

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
public class HelixCheckBox : CheckBox
{
    static HelixCheckBox() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixCheckBox), new FrameworkPropertyMetadata(typeof(HelixCheckBox)));

    public HelixCheckBox() => HelixAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixCheckBox));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixCheckBox));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixCheckBox));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixCheckBox));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixCheckBox));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixCheckBox));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty CheckBrushProperty = HelixProperty.Register<HelixCheckBox, Brush?>(nameof(CheckBrush), null);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixCheckBox, CornerRadius>(nameof(CornerRadius), new CornerRadius(5));
    public static readonly DependencyProperty DescriptionProperty = HelixProperty.Register<HelixCheckBox, string?>(nameof(Description), null);

    /// <summary>Color of the tick (OnAccent when not set).</summary>
    public Brush? CheckBrush { get => (Brush?)GetValue(CheckBrushProperty); set => SetValue(CheckBrushProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
}

/// <summary>A radio button with Accent and a Description line.</summary>
public class HelixRadioButton : RadioButton
{
    static HelixRadioButton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixRadioButton), new FrameworkPropertyMetadata(typeof(HelixRadioButton)));

    public HelixRadioButton() => HelixAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixRadioButton));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixRadioButton));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixRadioButton));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixRadioButton));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixRadioButton));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixRadioButton));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty DescriptionProperty = HelixProperty.Register<HelixRadioButton, string?>(nameof(Description), null);

    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
}

/// <summary>A slider with Accent (filled part) and TrackBrush (rest).</summary>
public class HelixSlider : Slider
{
    static HelixSlider() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixSlider), new FrameworkPropertyMetadata(typeof(HelixSlider)));

    public HelixSlider() => HelixAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixSlider));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixSlider));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixSlider));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixSlider));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixSlider));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixSlider));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text or mark color on the accent; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty TrackBrushProperty = HelixProperty.Register<HelixSlider, Brush?>(nameof(TrackBrush), null);

    public Brush? TrackBrush { get => (Brush?)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
}
