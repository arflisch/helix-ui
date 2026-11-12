using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Helix.Wpf;

/// <summary>
/// Stars (or hearts) to rate. Value is two-way; hover previews; arrows change it with the keyboard.
/// Read-only ratings show fractions (3.7 ★).
/// </summary>
[TemplatePart(Name = "PART_Items", Type = typeof(Panel))]
public class HelixRating : Control, IHelixThemeAware
{
    static HelixRating() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixRating), new FrameworkPropertyMetadata(typeof(HelixRating)));

    public HelixRating()
    {
        HelixAccent.Init(this);
        HelixTheme.Watch(this);
    }

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixRating));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixRating));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixRating));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixRating));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixRating));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixRating));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(HelixRating),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HelixRating)d).Build()));
    public static readonly DependencyProperty MaxProperty = HelixProperty.Register<HelixRating, int>(nameof(Max), 5, (d, _) => ((HelixRating)d).Build());
    public static readonly DependencyProperty IsReadOnlyProperty = HelixProperty.Register<HelixRating, bool>(nameof(IsReadOnly), false, (d, _) => ((HelixRating)d).Build());
    public static readonly DependencyProperty AllowClearProperty = HelixProperty.Register<HelixRating, bool>(nameof(AllowClear), true);
    public static readonly DependencyProperty ShapeProperty = HelixProperty.Register<HelixRating, string>(nameof(Shape), "star", (d, _) => ((HelixRating)d).Build());
    public static readonly DependencyProperty SizeProperty = HelixProperty.Register<HelixRating, HelixSize>(nameof(Size), HelixSize.Md, (d, _) => ((HelixRating)d).Build());
    public static readonly DependencyProperty LabelsProperty = HelixProperty.Register<HelixRating, IList<string>?>(nameof(Labels), null, (d, _) => ((HelixRating)d).Build());
    public static readonly DependencyProperty ShowValueProperty = HelixProperty.Register<HelixRating, bool>(nameof(ShowValue), false, (d, _) => ((HelixRating)d).Build());
    public static readonly DependencyProperty CountProperty = HelixProperty.Register<HelixRating, int?>(nameof(Count), null, (d, _) => ((HelixRating)d).Build());

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public int Max { get => (int)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>Clicking the current value again resets to 0.</summary>
    public bool AllowClear { get => (bool)GetValue(AllowClearProperty); set => SetValue(AllowClearProperty, value); }

    /// <summary>"star" or "heart" (any built-in icon works).</summary>
    public string Shape { get => (string)GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }
    public HelixSize Size { get => (HelixSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>A word per value, shown on hover and after the stars ("Bad", "OK", "Great"…).</summary>
    public IList<string>? Labels { get => (IList<string>?)GetValue(LabelsProperty); set => SetValue(LabelsProperty, value); }

    /// <summary>Shows "4.5" after the stars.</summary>
    public bool ShowValue { get => (bool)GetValue(ShowValueProperty); set => SetValue(ShowValueProperty, value); }

    /// <summary>Number of reviews, shown as "(128)".</summary>
    public int? Count { get => (int?)GetValue(CountProperty); set => SetValue(CountProperty, value); }

    private Panel? _items;
    private int? _hover;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _items = GetTemplateChild("PART_Items") as Panel;
        Build();
    }

    void IHelixThemeAware.OnThemeChanged() => Build();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (IsReadOnly)
        {
            return;
        }

        var step = e.Key switch { Key.Right or Key.Up => 1, Key.Left or Key.Down => -1, _ => 0 };
        if (step != 0)
        {
            SetCurrentValue(ValueProperty, Math.Clamp(Math.Round(Value) + step, 0, Max));
            e.Handled = true;
        }
    }

    private void Build()
    {
        if (_items is null)
        {
            return;
        }

        _items.Children.Clear();
        var size = Size switch { HelixSize.Sm => 16.0, HelixSize.Lg => 28.0, _ => 22.0 };
        var shown = _hover ?? Value;
        var geometry = HelixIcon.Get(Shape) ?? HelixIcon.Get("star");
        var accent = Accent ?? Brushes.Gold;
        var empty = HelixTheme.Brush(this, HelixKeys.BorderStrong, Colors.LightGray);

        for (var i = 1; i <= Math.Max(1, Max); i++)
        {
            var index = i;
            var fill = Math.Clamp(shown - (i - 1), 0, 1);
            var cell = new Grid { Width = size, Height = size, Margin = new Thickness(0, 0, 3, 0), Background = Brushes.Transparent };
            cell.Children.Add(Glyph(geometry, null, empty, size));
            if (fill > 0)
            {
                var full = Glyph(geometry, accent, accent, size);
                if (fill < 1)
                {
                    full.Clip = new RectangleGeometry(new Rect(0, 0, size * fill, size));
                }

                cell.Children.Add(full);
            }

            if (!IsReadOnly && IsEnabled)
            {
                cell.Cursor = Cursors.Hand;
                cell.MouseEnter += (_, _) => { _hover = index; Build(); };
                cell.MouseLeftButtonUp += (_, _) =>
                {
                    var next = AllowClear && Math.Abs(Value - index) < 0.01 ? 0 : index;
                    _hover = null;
                    SetCurrentValue(ValueProperty, (double)next);
                };
            }

            _items.Children.Add(cell);
        }

        var label = Labels is { Count: > 0 } labels && Math.Round(shown) is var r && r >= 1 && r <= labels.Count ? labels[(int)r - 1] : null;
        var text = string.Join("  ", new[]
        {
            ShowValue ? Value.ToString("0.#", CultureInfo.CurrentCulture) : null,
            label,
            Count is { } count ? $"({count.ToString("N0", CultureInfo.CurrentCulture)})" : null,
        }.Where(t => !string.IsNullOrEmpty(t)));
        if (text.Length > 0)
        {
            var block = new TextBlock { Text = text, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = Size == HelixSize.Lg ? 15 : 13 };
            block.SetResourceReference(TextBlock.ForegroundProperty, HelixKeys.TextMuted);
            _items.Children.Add(block);
        }

        System.Windows.Automation.AutomationProperties.SetName(this, $"{Value.ToString("0.#", CultureInfo.CurrentCulture)} / {Max}");
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hover is not null)
        {
            _hover = null;
            Build();
        }
    }

    private static System.Windows.Shapes.Path Glyph(Geometry? geometry, Brush? fill, Brush stroke, double size) => new()
    {
        Data = geometry,
        Fill = fill,
        Stroke = stroke,
        StrokeThickness = 1.8,
        StrokeLineJoin = PenLineJoin.Round,
        Stretch = Stretch.Uniform,
        Width = size,
        Height = size,
    };
}

/// <summary>
/// A color field: swatch + hex; the popup has a saturation/brightness area, hue and opacity sliders, a hex box and suggested colors.
/// SelectedColor is two-way (null = no color).
/// </summary>
[TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
[TemplatePart(Name = "PART_Panel", Type = typeof(Border))]
public class HelixColorPicker : Control
{
    static HelixColorPicker() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixColorPicker), new FrameworkPropertyMetadata(typeof(HelixColorPicker)));

    public HelixColorPicker() => SetCurrentValue(PlaceholderProperty, HelixTexts.Current.SelectPlaceholder);

    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(nameof(SelectedColor), typeof(Color?), typeof(HelixColorPicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HelixColorPicker)d).OnColorChanged()));
    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixColorPicker, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = HelixProperty.Register<HelixColorPicker, string?>(nameof(Hint), null);
    public static readonly DependencyProperty PlaceholderProperty = HelixProperty.Register<HelixColorPicker, string?>(nameof(Placeholder), null);
    public static readonly DependencyProperty ShowAlphaProperty = HelixProperty.Register<HelixColorPicker, bool>(nameof(ShowAlpha), false);
    public static readonly DependencyProperty SwatchesProperty = HelixProperty.Register<HelixColorPicker, IEnumerable<Color>?>(nameof(Swatches), null);
    public static readonly DependencyProperty IsOpenProperty = HelixProperty.Register<HelixColorPicker, bool>(nameof(IsOpen), false, (d, _) => ((HelixColorPicker)d).OnOpenChanged());
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixColorPicker, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

    private static readonly DependencyPropertyKey HexKey = DependencyProperty.RegisterReadOnly(nameof(Hex), typeof(string), typeof(HelixColorPicker), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey SwatchBrushKey = DependencyProperty.RegisterReadOnly(nameof(SwatchBrush), typeof(Brush), typeof(HelixColorPicker), new PropertyMetadata(null));
    public static readonly DependencyProperty HexProperty = HexKey.DependencyProperty;
    public static readonly DependencyProperty SwatchBrushProperty = SwatchBrushKey.DependencyProperty;

    public Color? SelectedColor { get => (Color?)GetValue(SelectedColorProperty); set => SetValue(SelectedColorProperty, value); }
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Adds the opacity slider (#RRGGBBAA hex).</summary>
    public bool ShowAlpha { get => (bool)GetValue(ShowAlphaProperty); set => SetValue(ShowAlphaProperty, value); }

    /// <summary>Suggested colors under the picker (a default set when null).</summary>
    public IEnumerable<Color>? Swatches { get => (IEnumerable<Color>?)GetValue(SwatchesProperty); set => SetValue(SwatchesProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>"#FF8A00" (with alpha when not opaque), or null.</summary>
    public string? Hex => (string?)GetValue(HexProperty);
    public Brush? SwatchBrush => (Brush?)GetValue(SwatchBrushProperty);

    private static readonly Color[] DefaultSwatches =
    [
        C("#4F46E5"), C("#0EA5E9"), C("#10B981"), C("#84CC16"), C("#F59E0B"), C("#F97316"),
        C("#E11D48"), C("#EC4899"), C("#8B5CF6"), C("#64748B"), C("#18181B"), C("#FFFFFF"),
    ];

    private Border? _panel;
    private double _h, _s = 1, _v = 1, _a = 1;
    private bool _syncing;

    // Popup parts built in code.
    private Grid? _area;
    private Ellipse? _areaThumb, _hueThumb, _alphaThumb;
    private Border? _hueBar, _alphaBar;
    private Rectangle? _hueFill;
    private GradientStop? _alphaEnd;
    private TextBox? _hexBox;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _panel = GetTemplateChild("PART_Panel") as Border;
        OnColorChanged();
    }

    private void OnOpenChanged()
    {
        if (IsOpen && _panel is not null)
        {
            _panel.Child = BuildPanel();
            SyncFromColor();
        }
    }

    private void OnColorChanged()
    {
        var color = SelectedColor;
        SetValue(HexKey, color is { } c ? ToHex(c) : null);
        SetValue(SwatchBrushKey, color is { } c2 ? HelixTheme.Frozen(c2) : null);
        if (!_syncing)
        {
            SyncFromColor();
        }
    }

    private void SyncFromColor()
    {
        if (SelectedColor is { } c)
        {
            (_h, _s, _v) = ToHsv(c);
            _a = c.A / 255.0;
        }

        UpdateVisuals();
    }

    private void Commit()
    {
        _syncing = true;
        try
        {
            var c = FromHsv(_h, _s, _v);
            SetCurrentValue(SelectedColorProperty, Color.FromArgb((byte)Math.Round(_a * 255), c.R, c.G, c.B));
        }
        finally
        {
            _syncing = false;
        }

        UpdateVisuals();
    }

    private UIElement BuildPanel()
    {
        var root = new StackPanel { Width = 240 };

        // Saturation (x) × brightness (y) on the current hue.
        _area = new Grid { Height = 150, Cursor = Cursors.Cross, ClipToBounds = true };
        _hueFill = new Rectangle { RadiusX = 8, RadiusY = 8 };
        _area.Children.Add(_hueFill);
        _area.Children.Add(new Rectangle { RadiusX = 8, RadiusY = 8, Fill = new LinearGradientBrush(Colors.White, Color.FromArgb(0, 255, 255, 255), 0) });
        _area.Children.Add(new Rectangle { RadiusX = 8, RadiusY = 8, Fill = new LinearGradientBrush(Color.FromArgb(0, 0, 0, 0), Colors.Black, 90) });
        _areaThumb = Thumb(14);
        _area.Children.Add(new Canvas { Children = { _areaThumb } });
        Drag(_area, p =>
        {
            _s = Math.Clamp(p.X / _area.ActualWidth, 0, 1);
            _v = 1 - Math.Clamp(p.Y / _area.ActualHeight, 0, 1);
            Commit();
        });
        root.Children.Add(_area);

        // Hue.
        var hue = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        for (var i = 0; i <= 6; i++)
        {
            hue.GradientStops.Add(new GradientStop(FromHsv(i * 60 % 360, 1, 1), i / 6.0));
        }

        hue.Freeze();
        _hueBar = new Border { Height = 12, CornerRadius = new CornerRadius(6), Background = hue, Margin = new Thickness(0, 12, 0, 0), Cursor = Cursors.Hand };
        _hueThumb = Thumb(16);
        _hueBar.Child = new Canvas { Children = { _hueThumb } };
        Drag(_hueBar, p =>
        {
            _h = Math.Clamp(p.X / _hueBar.ActualWidth, 0, 0.9999) * 360;
            Commit();
        });
        root.Children.Add(_hueBar);

        // Opacity.
        if (ShowAlpha)
        {
            var alpha = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
            alpha.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0));
            _alphaEnd = new GradientStop(Colors.Black, 1);
            alpha.GradientStops.Add(_alphaEnd);
            var checker = new Border { Height = 12, CornerRadius = new CornerRadius(6), Margin = new Thickness(0, 10, 0, 0), Background = Checkerboard() };
            _alphaBar = new Border { CornerRadius = new CornerRadius(6), Background = alpha, Cursor = Cursors.Hand };
            _alphaThumb = Thumb(16);
            _alphaBar.Child = new Canvas { Children = { _alphaThumb } };
            checker.Child = _alphaBar;
            Drag(_alphaBar, p =>
            {
                _a = Math.Clamp(p.X / _alphaBar.ActualWidth, 0, 1);
                Commit();
            });
            System.Windows.Automation.AutomationProperties.SetName(_alphaBar, HelixTexts.Current.Opacity);
            root.Children.Add(checker);
        }

        // Hex.
        _hexBox = new HelixTextField { Margin = new Thickness(0, 12, 0, 0), Prefix = new TextBlock { Text = "#", Foreground = Brushes.Gray } };
        System.Windows.Automation.AutomationProperties.SetName(_hexBox, HelixTexts.Current.Hex);
        _hexBox.LostKeyboardFocus += (_, _) => ApplyHex();
        _hexBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) ApplyHex();
        };
        root.Children.Add(_hexBox);

        // Suggested colors.
        var swatches = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        foreach (var color in Swatches ?? DefaultSwatches)
        {
            var swatch = new Button
            {
                Width = 22,
                Height = 22,
                Margin = new Thickness(0, 0, 6, 6),
                Cursor = Cursors.Hand,
                ToolTip = ToHex(color),
                Template = SwatchTemplate(),
                Background = HelixTheme.Frozen(color),
            };
            swatch.Click += (_, _) =>
            {
                SetCurrentValue(SelectedColorProperty, color);
                SyncFromColor();
            };
            swatches.Children.Add(swatch);
        }

        root.Children.Add(swatches);
        return root;
    }

    private void ApplyHex()
    {
        if (_hexBox is null)
        {
            return;
        }

        var text = _hexBox.Text.Trim().TrimStart('#');
        if (text.Length is 6 or 8 && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            var color = text.Length == 6
                ? Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value)
                : Color.FromArgb((byte)value, (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8));
            SetCurrentValue(SelectedColorProperty, color);
            SyncFromColor();
        }
        else
        {
            UpdateVisuals(); // invalid: back to the current value
        }
    }

    private void UpdateVisuals()
    {
        if (_area is null)
        {
            return;
        }

        if (_hueFill is not null) _hueFill.Fill = HelixTheme.Frozen(FromHsv(_h, 1, 1));
        Place(_areaThumb, _s * _area.ActualWidth, (1 - _v) * _area.ActualHeight);
        if (_hueBar is not null) Place(_hueThumb, _h / 360 * _hueBar.ActualWidth, _hueBar.ActualHeight / 2);
        if (_alphaBar is not null) Place(_alphaThumb, _a * _alphaBar.ActualWidth, _alphaBar.ActualHeight / 2);
        if (_alphaEnd is not null) _alphaEnd.Color = FromHsv(_h, _s, _v);
        if (_hexBox is not null && !_hexBox.IsKeyboardFocusWithin)
        {
            _hexBox.Text = Hex?.TrimStart('#') ?? "";
        }

        if (_area.ActualWidth == 0)
        {
            // First layout pass not done yet: place the thumbs once sizes are known.
            _area.Dispatcher.BeginInvoke(UpdateVisuals, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private static void Place(FrameworkElement? thumb, double x, double y)
    {
        if (thumb is null) return;
        Canvas.SetLeft(thumb, x - thumb.Width / 2);
        Canvas.SetTop(thumb, y - thumb.Height / 2);
    }

    private static Ellipse Thumb(double size) => new()
    {
        Width = size,
        Height = size,
        Stroke = Brushes.White,
        StrokeThickness = 2.5,
        IsHitTestVisible = false,
        Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 4, ShadowDepth = 0, Opacity = 0.5 },
    };

    /// <summary>Pointer drag inside an element (with mouse capture).</summary>
    private static void Drag(FrameworkElement element, Action<Point> moved)
    {
        // Hit-testable everywhere, not only on painted pixels.
        if (element is Panel { Background: null } panel) panel.Background = Brushes.Transparent;
        if (element is Border { Background: null } border) border.Background = Brushes.Transparent;
        element.MouseLeftButtonDown += (_, e) =>
        {
            element.CaptureMouse();
            moved(e.GetPosition(element));
            e.Handled = true;
        };
        element.MouseMove += (_, e) =>
        {
            if (element.IsMouseCaptured) moved(e.GetPosition(element));
        };
        element.MouseLeftButtonUp += (_, _) => element.ReleaseMouseCapture();
    }

    private static ControlTemplate SwatchTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        border.SetValue(Border.BorderBrushProperty, HelixTheme.Frozen(Color.FromArgb(40, 0, 0, 0)));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        return new ControlTemplate(typeof(Button)) { VisualTree = border };
    }

    private static DrawingBrush Checkerboard()
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, 8, 8))));
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(0xdd, 0xdd, 0xdd)), null,
            new GeometryGroup { Children = { new RectangleGeometry(new Rect(0, 0, 4, 4)), new RectangleGeometry(new Rect(4, 4, 4, 4)) } }));
        var brush = new DrawingBrush(group) { TileMode = TileMode.Tile, Viewport = new Rect(0, 0, 8, 8), ViewportUnits = BrushMappingMode.Absolute };
        brush.Freeze();
        return brush;
    }

    public static string ToHex(Color c) => c.A == 255
        ? $"#{c.R:X2}{c.G:X2}{c.B:X2}"
        : $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";

    private static (double H, double S, double V) ToHsv(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        var h = delta == 0 ? 0 : max == r ? 60 * ((g - b) / delta % 6) : max == g ? 60 * ((b - r) / delta + 2) : 60 * ((r - g) / delta + 4);
        return (h < 0 ? h + 360 : h, max == 0 ? 0 : delta / max, max);
    }

    private static Color FromHsv(double h, double s, double v)
    {
        var c = v * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = v - c;
        var (r, g, b) = (h % 360) switch
        {
            < 60 => (c, x, 0d),
            < 120 => (x, c, 0d),
            < 180 => (0d, c, x),
            < 240 => (0d, x, c),
            < 300 => (x, 0d, c),
            _ => (c, 0d, x),
        };
        byte B(double t) => (byte)Math.Round((t + m) * 255);
        return Color.FromRgb(B(r), B(g), B(b));
    }

    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}

/// <summary>A file picked or dropped on a <see cref="HelixFileDrop"/>.</summary>
public sealed class HelixFile
{
    internal HelixFile(HelixFileDrop owner, FileInfo info)
    {
        Name = info.Name;
        Path = info.FullName;
        Size = info.Length;
        RemoveCommand = new HelixCommand(() => owner.Remove(this));
    }

    public string Name { get; }
    public string Path { get; }
    public long Size { get; }
    public string SizeText => HelixFileDrop.FormatSize(Size);
    public string Extension => System.IO.Path.GetExtension(Name).TrimStart('.').ToUpperInvariant();
    public ICommand RemoveCommand { get; }
}

/// <summary>
/// Drop files here or browse: Accept (".pdf,.png" or "image/*"), Multiple, MaxFileSize, MaxFiles.
/// Files is the current list; FilesChanged is raised on every change.
/// </summary>
[TemplatePart(Name = "PART_Browse", Type = typeof(ButtonBase))]
public class HelixFileDrop : Control
{
    static HelixFileDrop() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixFileDrop), new FrameworkPropertyMetadata(typeof(HelixFileDrop)));

    public HelixFileDrop()
    {
        HelixAccent.Init(this);
        AllowDrop = true;
        SetCurrentValue(TitleProperty, HelixTexts.Current.DropFilesHere);
        SetCurrentValue(BrowseTextProperty, HelixTexts.Current.Browse);
        Files.CollectionChanged += (_, _) =>
        {
            SetValue(HasFilesKey, Files.Count > 0);
            RaiseEvent(new RoutedEventArgs(FilesChangedEvent, this));
        };
    }

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixFileDrop));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixFileDrop));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixFileDrop));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixFileDrop));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixFileDrop));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixFileDrop));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty AcceptProperty = HelixProperty.Register<HelixFileDrop, string?>(nameof(Accept), null, (d, _) => ((HelixFileDrop)d).UpdateHint());
    public static readonly DependencyProperty MultipleProperty = HelixProperty.Register<HelixFileDrop, bool>(nameof(Multiple), true, (d, _) => ((HelixFileDrop)d).UpdateHint());
    public static readonly DependencyProperty MaxFileSizeProperty = HelixProperty.Register<HelixFileDrop, long>(nameof(MaxFileSize), 0L, (d, _) => ((HelixFileDrop)d).UpdateHint());
    public static readonly DependencyProperty MaxFilesProperty = HelixProperty.Register<HelixFileDrop, int>(nameof(MaxFiles), 0);
    public static readonly DependencyProperty TitleProperty = HelixProperty.Register<HelixFileDrop, string?>(nameof(Title), null);
    public static readonly DependencyProperty HintProperty = HelixProperty.Register<HelixFileDrop, string?>(nameof(Hint), null);
    public static readonly DependencyProperty BrowseTextProperty = HelixProperty.Register<HelixFileDrop, string?>(nameof(BrowseText), null);
    public static readonly DependencyProperty ShowListProperty = HelixProperty.Register<HelixFileDrop, bool>(nameof(ShowList), true);
    public static readonly DependencyProperty IsCompactProperty = HelixProperty.Register<HelixFileDrop, bool>(nameof(IsCompact), false);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixFileDrop, CornerRadius>(nameof(CornerRadius), new CornerRadius(14));
    public static readonly RoutedEvent FilesChangedEvent = EventManager.RegisterRoutedEvent(nameof(FilesChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixFileDrop));

    private static readonly DependencyPropertyKey IsDragOverKey = DependencyProperty.RegisterReadOnly(nameof(IsDragOver), typeof(bool), typeof(HelixFileDrop), new PropertyMetadata(false));
    private static readonly DependencyPropertyKey HasFilesKey = DependencyProperty.RegisterReadOnly(nameof(HasFiles), typeof(bool), typeof(HelixFileDrop), new PropertyMetadata(false));
    private static readonly DependencyPropertyKey ErrorKey = DependencyProperty.RegisterReadOnly(nameof(Error), typeof(string), typeof(HelixFileDrop), new PropertyMetadata(null));
    private static readonly DependencyPropertyKey AutoHintKey = DependencyProperty.RegisterReadOnly(nameof(AutoHint), typeof(string), typeof(HelixFileDrop), new PropertyMetadata(null));
    public static readonly DependencyProperty IsDragOverProperty = IsDragOverKey.DependencyProperty;
    public static readonly DependencyProperty HasFilesProperty = HasFilesKey.DependencyProperty;
    public static readonly DependencyProperty ErrorProperty = ErrorKey.DependencyProperty;
    public static readonly DependencyProperty AutoHintProperty = AutoHintKey.DependencyProperty;

    /// <summary>Allowed types: ".pdf,.xlsx" or "image/*". Null accepts everything.</summary>
    public string? Accept { get => (string?)GetValue(AcceptProperty); set => SetValue(AcceptProperty, value); }
    public bool Multiple { get => (bool)GetValue(MultipleProperty); set => SetValue(MultipleProperty, value); }

    /// <summary>Bytes; 0 = no limit.</summary>
    public long MaxFileSize { get => (long)GetValue(MaxFileSizeProperty); set => SetValue(MaxFileSizeProperty, value); }

    /// <summary>0 = no limit.</summary>
    public int MaxFiles { get => (int)GetValue(MaxFilesProperty); set => SetValue(MaxFilesProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Line under the title; by default the accepted types and size limit.</summary>
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }
    public string? BrowseText { get => (string?)GetValue(BrowseTextProperty); set => SetValue(BrowseTextProperty, value); }
    public bool ShowList { get => (bool)GetValue(ShowListProperty); set => SetValue(ShowListProperty, value); }

    /// <summary>A single row instead of the big zone.</summary>
    public bool IsCompact { get => (bool)GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public bool IsDragOver => (bool)GetValue(IsDragOverProperty);
    public bool HasFiles => (bool)GetValue(HasFilesProperty);

    /// <summary>Why the last file was refused (type, size, count).</summary>
    public string? Error => (string?)GetValue(ErrorProperty);
    public string? AutoHint => (string?)GetValue(AutoHintProperty);

    public ObservableCollection<HelixFile> Files { get; } = [];

    public event RoutedEventHandler FilesChanged { add => AddHandler(FilesChangedEvent, value); remove => RemoveHandler(FilesChangedEvent, value); }

    private ButtonBase? _browse;

    public override void OnApplyTemplate()
    {
        if (_browse is not null) _browse.Click -= OnBrowse;
        base.OnApplyTemplate();
        _browse = GetTemplateChild("PART_Browse") as ButtonBase;
        if (_browse is not null) _browse.Click += OnBrowse;
        UpdateHint();
    }

    internal void Remove(HelixFile file) => Files.Remove(file);

    private void UpdateHint()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Accept)) parts.Add(Accept.Replace(",", ", ", StringComparison.Ordinal));
        if (MaxFileSize > 0) parts.Add($"{FormatSize(MaxFileSize)} max");
        SetValue(AutoHintKey, Hint ?? (parts.Count == 0 ? null : string.Join(" · ", parts)));
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == HintProperty) UpdateHint();
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Multiselect = Multiple, Filter = DialogFilter() };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            AddFiles(dialog.FileNames);
        }
    }

    protected override void OnDragEnter(DragEventArgs e)
    {
        base.OnDragEnter(e);
        SetValue(IsDragOverKey, e.Data.GetDataPresent(DataFormats.FileDrop) && IsEnabled);
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) && IsEnabled ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    protected override void OnDragLeave(DragEventArgs e)
    {
        base.OnDragLeave(e);
        SetValue(IsDragOverKey, false);
    }

    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);
        SetValue(IsDragOverKey, false);
        if (IsEnabled && e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            AddFiles(Multiple ? paths : paths.Take(1));
        }
    }

    /// <summary>Adds files by path (checks type, size and count; the reason of a refusal goes to Error).</summary>
    public void AddFiles(IEnumerable<string> paths)
    {
        string? error = null;
        if (!Multiple)
        {
            Files.Clear();
        }

        foreach (var path in paths)
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                continue;
            }

            if (!Accepts(info.Name))
            {
                error = $"{info.Name}: {Accept}";
                continue;
            }

            if (MaxFileSize > 0 && info.Length > MaxFileSize)
            {
                error = HelixTexts.Format(HelixTexts.Current.FileTooLarge, info.Name, FormatSize(MaxFileSize));
                continue;
            }

            if (MaxFiles > 0 && Files.Count >= MaxFiles)
            {
                error = $"{MaxFiles} max";
                break;
            }

            if (Files.All(f => !string.Equals(f.Path, info.FullName, StringComparison.OrdinalIgnoreCase)))
            {
                Files.Add(new HelixFile(this, info));
            }
        }

        SetValue(ErrorKey, error);
    }

    private bool Accepts(string name)
    {
        if (string.IsNullOrWhiteSpace(Accept))
        {
            return true;
        }

        var extension = System.IO.Path.GetExtension(name).ToLowerInvariant();
        foreach (var rule in Accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var r = rule.ToLowerInvariant();
            if (r.StartsWith('.') && r == extension) return true;
            if (r == "image/*" && extension is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" or ".svg" or ".tif" or ".tiff") return true;
            if (r is "*" or "*/*") return true;
        }

        return false;
    }

    private string DialogFilter()
    {
        if (string.IsNullOrWhiteSpace(Accept))
        {
            return "*.*|*.*";
        }

        var patterns = Accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(r => r == "image/*" ? new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp", "*.svg" } : r.StartsWith('.') ? new[] { "*" + r } : new[] { "*.*" })
            .Distinct();
        var joined = string.Join(";", patterns);
        return $"{Accept}|{joined}";
    }

    public static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        < 1024L * 1024 * 1024 => $"{bytes / 1024.0 / 1024:0.#} MB",
        _ => $"{bytes / 1024.0 / 1024 / 1024:0.#} GB",
    };
}
