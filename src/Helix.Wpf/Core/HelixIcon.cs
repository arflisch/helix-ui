using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;

namespace Helix.Wpf;

/// <summary>
/// A line icon on a 24×24 grid, drawn with the current text color (Foreground).
/// Same names as Helix for Blazor: check, x, search, calendar, trash, sun, moon…
/// </summary>
public class HelixIcon : FrameworkElement
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(string), typeof(HelixIcon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Your own path data on a 24×24 grid (replaces <see cref="Icon"/>).</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(Geometry), typeof(HelixIcon), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(double), typeof(HelixIcon), new FrameworkPropertyMetadata(16d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeWidthProperty = DependencyProperty.Register(
        nameof(StrokeWidth), typeof(double), typeof(HelixIcon), new FrameworkPropertyMetadata(2d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Inherited like text color, so an icon in a button takes the button's color.</summary>
    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(
        typeof(HelixIcon), new FrameworkPropertyMetadata(SystemColors.ControlTextBrush, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public Geometry? Data { get => (Geometry?)GetValue(DataProperty); set => SetValue(DataProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public double StrokeWidth { get => (double)GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }
    public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    public HelixIcon()
    {
        SnapsToDevicePixels = true;
        IsHitTestVisible = false;
    }

    /// <summary>The built-in icon names.</summary>
    public static IEnumerable<string> Names => Svg.Keys;

    protected override Size MeasureOverride(Size availableSize) =>
        string.IsNullOrEmpty(Icon) && Data is null ? new Size(0, 0) : new Size(Size, Size);

    protected override void OnRender(DrawingContext dc)
    {
        var geometry = Data ?? Get(Icon);
        if (geometry is null)
        {
            return;
        }

        var scale = Size / 24.0;
        var pen = new Pen(Foreground, StrokeWidth)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };
        dc.PushTransform(new ScaleTransform(scale, scale));
        dc.DrawGeometry(null, pen, geometry);
        dc.Pop();
    }

    private static readonly Dictionary<string, Geometry?> Parsed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The geometry of a built-in icon (null if unknown).</summary>
    public static Geometry? Get(string? name)
    {
        if (string.IsNullOrEmpty(name) || !Svg.TryGetValue(name, out var svg))
        {
            return null;
        }

        lock (Parsed)
        {
            if (!Parsed.TryGetValue(name, out var geometry))
            {
                geometry = Parse(svg);
                Parsed[name] = geometry;
            }

            return geometry;
        }
    }

    /// <summary>Turns the few SVG elements the icons use (path, circle, rect) into one frozen geometry.</summary>
    private static Geometry Parse(string svg)
    {
        var group = new GeometryGroup { FillRule = FillRule.Nonzero };
        var root = XElement.Parse($"<svg>{svg}</svg>");
        double N(XElement e, string name) =>
            double.Parse((string?)e.Attribute(name) ?? "0", CultureInfo.InvariantCulture);

        foreach (var element in root.Elements())
        {
            switch (element.Name.LocalName)
            {
                case "path":
                    group.Children.Add(Geometry.Parse((string?)element.Attribute("d") ?? ""));
                    break;
                case "circle":
                    group.Children.Add(new EllipseGeometry(new Point(N(element, "cx"), N(element, "cy")), N(element, "r"), N(element, "r")));
                    break;
                case "rect":
                    var rx = N(element, "rx");
                    group.Children.Add(new RectangleGeometry(new Rect(N(element, "x"), N(element, "y"), N(element, "width"), N(element, "height")), rx, rx));
                    break;
            }
        }

        group.Freeze();
        return group;
    }

    private static readonly Dictionary<string, string> Svg = new(StringComparer.OrdinalIgnoreCase)
    {
        ["grip"] = "<path d='M9 6h.01M15 6h.01M9 12h.01M15 12h.01M9 18h.01M15 18h.01' stroke-width='3'/>",
        ["layout"] = "<rect x='3' y='3' width='7' height='9' rx='1.5'/><rect x='14' y='3' width='7' height='5' rx='1.5'/><rect x='14' y='12' width='7' height='9' rx='1.5'/><rect x='3' y='16' width='7' height='5' rx='1.5'/>",
        ["eye-off"] = "<path d='M3 3l18 18M10.6 10.6a2 2 0 0 0 2.8 2.8M9.9 5.2A9.8 9.8 0 0 1 12 5c6 0 9.5 7 9.5 7a17 17 0 0 1-3.2 4M6.2 6.2C3.9 7.8 2.5 12 2.5 12S6 19 12 19c1.6 0 3-.4 4.3-1'/>",
        ["columns"] = "<rect x='3' y='4' width='18' height='16' rx='2'/><path d='M9 4v16M15 4v16'/>",
        ["trending-up"] = "<path d='m3 17 6-6 4 4 8-8M15 7h6v6'/>",
        ["trending-down"] = "<path d='m3 7 6 6 4-4 8 8M15 17h6v-6'/>",
        ["zap"] = "<path d='M13 2 4 14h7l-1 8 9-12h-7z'/>",
        ["check"] = "<path d='M20 6 9 17l-5-5'/>",
        ["x"] = "<path d='M18 6 6 18M6 6l12 12'/>",
        ["chevron-down"] = "<path d='m6 9 6 6 6-6'/>",
        ["chevron-right"] = "<path d='m9 6 6 6-6 6'/>",
        ["chevron-left"] = "<path d='m15 6-6 6 6 6'/>",
        ["chevrons-up-down"] = "<path d='m7 15 5 5 5-5M7 9l5-5 5 5'/>",
        ["arrow-up"] = "<path d='M12 19V5M6 11l6-6 6 6'/>",
        ["arrow-down"] = "<path d='M12 5v14M6 13l6 6 6-6'/>",
        ["arrow-right"] = "<path d='M5 12h14M13 6l6 6-6 6'/>",
        ["info"] = "<circle cx='12' cy='12' r='9'/><path d='M12 16v-4M12 8h.01'/>",
        ["success"] = "<circle cx='12' cy='12' r='9'/><path d='m8.5 12 2.5 2.5 4.5-5'/>",
        ["warning"] = "<path d='M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z'/><path d='M12 9v4M12 17h.01'/>",
        ["danger"] = "<circle cx='12' cy='12' r='9'/><path d='M12 8v4M12 16h.01'/>",
        ["search"] = "<circle cx='11' cy='11' r='7'/><path d='m20 20-3.5-3.5'/>",
        ["plus"] = "<path d='M12 5v14M5 12h14'/>",
        ["minus"] = "<path d='M5 12h14'/>",
        ["mail"] = "<rect x='3' y='5' width='18' height='14' rx='2'/><path d='m3 7 9 6 9-6'/>",
        ["lock"] = "<rect x='4' y='11' width='16' height='10' rx='2'/><path d='M8 11V7a4 4 0 0 1 8 0v4'/>",
        ["user"] = "<circle cx='12' cy='8' r='4'/><path d='M4 21a8 8 0 0 1 16 0'/>",
        ["bell"] = "<path d='M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9M10 21h4'/>",
        ["trash"] = "<path d='M3 6h18M8 6V4h8v2M6 6l1 14h10l1-14'/>",
        ["download"] = "<path d='M12 3v12M7 10l5 5 5-5M5 21h14'/>",
        ["copy"] = "<rect x='9' y='9' width='12' height='12' rx='2'/><path d='M5 15V5a2 2 0 0 1 2-2h10'/>",
        ["star"] = "<path d='m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z'/>",
        ["heart"] = "<path d='M12 20s-7-4.4-9-9a5 5 0 0 1 9-3 5 5 0 0 1 9 3c-2 4.6-9 9-9 9z'/>",
        ["settings"] = "<circle cx='12' cy='12' r='3'/><path d='M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1'/>",
        ["sun"] = "<circle cx='12' cy='12' r='4'/><path d='M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4'/>",
        ["moon"] = "<path d='M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z'/>",
        ["sparkles"] = "<path d='M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8zM19 16l.8 2.2L22 19l-2.2.8L19 22l-.8-2.2L16 19l2.2-.8z'/>",
        ["calendar"] = "<rect x='3' y='5' width='18' height='16' rx='2'/><path d='M16 3v4M8 3v4M3 10h18'/>",
        ["upload"] = "<path d='M12 21V9M7 14l5-5 5 5M5 3h14'/>",
        ["external"] = "<path d='M14 4h6v6M20 4l-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5'/>",
        ["refresh"] = "<path d='M21 12a9 9 0 1 1-2.64-6.36'/><path d='M21 3v6h-6'/>",
        ["home"] = "<path d='M3 11 12 4l9 7M5 10v10h14V10'/>",
        ["menu"] = "<path d='M4 6h16M4 12h16M4 18h16'/>",
        ["edit"] = "<path d='M4 20h4L19 9l-4-4L4 16z'/>",
    };
}
