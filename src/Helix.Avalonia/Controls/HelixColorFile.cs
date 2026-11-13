using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Helix.Avalonia;

/// <summary>
/// A color field: swatch + hex; the popup has a saturation/brightness area, hue and opacity sliders, a hex box and suggested colors.
/// SelectedColor is two-way (null = no color).
/// </summary>
[TemplatePart("PART_Panel", typeof(Border))]
[TemplatePart("PART_Clear", typeof(Button))]
public class HelixColorPicker : HelixFieldBase
{
    public static readonly StyledProperty<Color?> SelectedColorProperty = AvaloniaProperty.Register<HelixColorPicker, Color?>(nameof(SelectedColor), defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);
    public static readonly StyledProperty<bool> ShowAlphaProperty = AvaloniaProperty.Register<HelixColorPicker, bool>(nameof(ShowAlpha));
    public static readonly StyledProperty<IEnumerable<Color>?> SwatchesProperty = AvaloniaProperty.Register<HelixColorPicker, IEnumerable<Color>?>(nameof(Swatches));
    public static readonly StyledProperty<bool> IsDropDownOpenProperty = AvaloniaProperty.Register<HelixColorPicker, bool>(nameof(IsDropDownOpen), defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);
    public static readonly StyledProperty<bool> IsClearableProperty = AvaloniaProperty.Register<HelixColorPicker, bool>(nameof(IsClearable));
    public static readonly DirectProperty<HelixColorPicker, string?> HexProperty = AvaloniaProperty.RegisterDirect<HelixColorPicker, string?>(nameof(Hex), p => p.Hex);
    public static readonly DirectProperty<HelixColorPicker, IBrush?> SwatchBrushProperty = AvaloniaProperty.RegisterDirect<HelixColorPicker, IBrush?>(nameof(SwatchBrush), p => p.SwatchBrush);
    public static readonly DirectProperty<HelixColorPicker, bool> HasValueProperty = AvaloniaProperty.RegisterDirect<HelixColorPicker, bool>(nameof(HasValue), p => p.HasValue);

    public HelixColorPicker() => SetCurrentValue(PlaceholderProperty, HelixTexts.Current.SelectPlaceholder);

    public Color? SelectedColor { get => GetValue(SelectedColorProperty); set => SetValue(SelectedColorProperty, value); }

    /// <summary>Adds the opacity slider (#RRGGBBAA hex).</summary>
    public bool ShowAlpha { get => GetValue(ShowAlphaProperty); set => SetValue(ShowAlphaProperty, value); }

    /// <summary>Suggested colors under the picker (a default set when null).</summary>
    public IEnumerable<Color>? Swatches { get => GetValue(SwatchesProperty); set => SetValue(SwatchesProperty, value); }
    public bool IsDropDownOpen { get => GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

    /// <summary>An × that empties the field.</summary>
    public bool IsClearable { get => GetValue(IsClearableProperty); set => SetValue(IsClearableProperty, value); }

    private string? _hex;
    private IBrush? _swatchBrush;
    private bool _hasValue;

    /// <summary>"#FF8A00" (with alpha when not opaque), or null.</summary>
    public string? Hex { get => _hex; private set => SetAndRaise(HexProperty, ref _hex, value); }
    public IBrush? SwatchBrush { get => _swatchBrush; private set => SetAndRaise(SwatchBrushProperty, ref _swatchBrush, value); }
    public bool HasValue { get => _hasValue; private set => SetAndRaise(HasValueProperty, ref _hasValue, value); }

    private static readonly Color[] DefaultSwatches =
    [
        Color.Parse("#4F46E5"), Color.Parse("#0EA5E9"), Color.Parse("#10B981"), Color.Parse("#84CC16"), Color.Parse("#F59E0B"), Color.Parse("#F97316"),
        Color.Parse("#E11D48"), Color.Parse("#EC4899"), Color.Parse("#8B5CF6"), Color.Parse("#64748B"), Color.Parse("#18181B"), Color.Parse("#FFFFFF"),
    ];

    private Border? _panel;
    private Button? _clear;
    private double _h, _s = 1, _v = 1, _a = 1;
    private bool _syncing;

    // Popup parts built in code.
    private Panel? _area;
    private Border? _areaThumb, _hueThumb, _alphaThumb, _hueFill, _hueBar, _alphaBar;
    private GradientStop? _alphaEnd;
    private HelixTextField? _hexBox;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_clear is not null) _clear.Click -= OnClear;
        base.OnApplyTemplate(e);
        _panel = e.NameScope.Find<Border>("PART_Panel");
        _clear = e.NameScope.Find<Button>("PART_Clear");
        if (_clear is not null) _clear.Click += OnClear;
        OnColorChanged();
    }

    private void OnClear(object? sender, RoutedEventArgs e) => SetCurrentValue(SelectedColorProperty, null);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedColorProperty) OnColorChanged();
        else if (change.Property == IsDropDownOpenProperty && IsDropDownOpen && _panel is not null)
        {
            _panel.Child = BuildPanel();
            SyncFromColor();
        }

        if (change.Property == HasValueProperty || change.Property == IsClearableProperty) PseudoClasses.Set(":clearable", HasValue && IsClearable);
    }

    private void OnColorChanged()
    {
        var color = SelectedColor;
        Hex = color is { } c ? ToHex(c) : null;
        SwatchBrush = color is { } c2 ? new ImmutableSolidColorBrush(c2) : null;
        HasValue = color is not null;
        if (!_syncing) SyncFromColor();
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

    /// <summary>Sets the color from hue (0–360), saturation and brightness (0–1), as the panel does.</summary>
    public void SetHsv(double hue, double saturation, double brightness)
    {
        (_h, _s, _v) = (Math.Clamp(hue, 0, 359.99), Math.Clamp(saturation, 0, 1), Math.Clamp(brightness, 0, 1));
        Commit();
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

    private Control BuildPanel()
    {
        var root = new StackPanel { Width = 240 };

        // Saturation (x) × brightness (y) on the current hue.
        _area = new Panel { Height = 150, Cursor = new Cursor(StandardCursorType.Cross), Background = Brushes.Transparent };
        _hueFill = new Border { CornerRadius = new CornerRadius(8) };
        _area.Children.Add(_hueFill);
        _area.Children.Add(new Border { CornerRadius = new CornerRadius(8), Background = Gradient(Colors.White, Color.FromArgb(0, 255, 255, 255), horizontal: true) });
        _area.Children.Add(new Border { CornerRadius = new CornerRadius(8), Background = Gradient(Color.FromArgb(0, 0, 0, 0), Colors.Black, horizontal: false) });
        _areaThumb = Thumb(14);
        _area.Children.Add(new Canvas { Children = { _areaThumb } });
        Drag(_area, p =>
        {
            _s = Math.Clamp(p.X / _area.Bounds.Width, 0, 1);
            _v = 1 - Math.Clamp(p.Y / _area.Bounds.Height, 0, 1);
            Commit();
        });
        root.Children.Add(_area);

        // Hue.
        var hue = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative) };
        for (var i = 0; i <= 6; i++) hue.GradientStops.Add(new GradientStop(FromHsv(i * 60 % 360, 1, 1), i / 6.0));
        _hueBar = new Border { Height = 12, CornerRadius = new CornerRadius(6), Background = hue, Margin = new Thickness(0, 12, 0, 0), Cursor = new Cursor(StandardCursorType.Hand) };
        _hueThumb = Thumb(16);
        _hueBar.Child = new Canvas { Children = { _hueThumb } };
        Drag(_hueBar, p =>
        {
            _h = Math.Clamp(p.X / _hueBar.Bounds.Width, 0, 0.9999) * 360;
            Commit();
        });
        root.Children.Add(_hueBar);

        // Opacity.
        if (ShowAlpha)
        {
            var alpha = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative) };
            alpha.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0));
            _alphaEnd = new GradientStop(Colors.Black, 1);
            alpha.GradientStops.Add(_alphaEnd);
            var checker = new Border { Height = 12, CornerRadius = new CornerRadius(6), Margin = new Thickness(0, 10, 0, 0), Background = Checkerboard(), ClipToBounds = true };
            _alphaBar = new Border { CornerRadius = new CornerRadius(6), Background = alpha, Cursor = new Cursor(StandardCursorType.Hand) };
            _alphaThumb = Thumb(16);
            _alphaBar.Child = new Canvas { Children = { _alphaThumb } };
            checker.Child = _alphaBar;
            Drag(_alphaBar, p =>
            {
                _a = Math.Clamp(p.X / _alphaBar.Bounds.Width, 0, 1);
                Commit();
            });
            global::Avalonia.Automation.AutomationProperties.SetName(_alphaBar, HelixTexts.Current.Opacity);
            root.Children.Add(checker);
        }

        // Hex.
        _hexBox = new HelixTextField { Margin = new Thickness(0, 12, 0, 0), Icon = "edit" };
        global::Avalonia.Automation.AutomationProperties.SetName(_hexBox, HelixTexts.Current.Hex);
        _hexBox.LostFocus += (_, _) => ApplyHex();
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
                Cursor = new Cursor(StandardCursorType.Hand),
                Background = new ImmutableSolidColorBrush(color),
                Template = SwatchTemplate,
            };
            ToolTip.SetTip(swatch, ToHex(color));
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

    private static readonly IControlTemplate SwatchTemplate = new FuncControlTemplate<Button>((button, _) => new Border
    {
        CornerRadius = new CornerRadius(6),
        BorderThickness = new Thickness(1),
        BorderBrush = new ImmutableSolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
        [!Border.BackgroundProperty] = button[!BackgroundProperty],
    });

    private void ApplyHex()
    {
        if (_hexBox is null) return;
        if (ParseHex(_hexBox.Text) is { } color)
        {
            SetCurrentValue(SelectedColorProperty, color);
            SyncFromColor();
        }
        else
        {
            UpdateVisuals(); // invalid: back to the current value
        }
    }

    /// <summary>"#FF8A00", "FF8A00" or "#FF8A00CC" (alpha last); null if invalid.</summary>
    public static Color? ParseHex(string? text)
    {
        var hex = text?.Trim().TrimStart('#') ?? "";
        if (hex.Length is not (6 or 8) || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) return null;
        return hex.Length == 6
            ? Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value)
            : Color.FromArgb((byte)value, (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8));
    }

    private void UpdateVisuals()
    {
        if (_area is null) return;
        if (_hueFill is not null) _hueFill.Background = new ImmutableSolidColorBrush(FromHsv(_h, 1, 1));
        Place(_areaThumb, _s * _area.Bounds.Width, (1 - _v) * _area.Bounds.Height);
        if (_hueBar is not null) Place(_hueThumb, _h / 360 * _hueBar.Bounds.Width, _hueBar.Bounds.Height / 2);
        if (_alphaBar is not null) Place(_alphaThumb, _a * _alphaBar.Bounds.Width, _alphaBar.Bounds.Height / 2);
        if (_alphaEnd is not null) _alphaEnd.Color = FromHsv(_h, _s, _v);
        if (_hexBox is not null && !_hexBox.IsKeyboardFocusWithin) _hexBox.Text = Hex ?? "";

        // First layout pass not done yet: place the thumbs once sizes are known.
        if (_area.Bounds.Width == 0) Dispatcher.UIThread.Post(UpdateVisuals, DispatcherPriority.Loaded);
    }

    private static void Place(Control? thumb, double x, double y)
    {
        if (thumb is null) return;
        Canvas.SetLeft(thumb, x - thumb.Width / 2);
        Canvas.SetTop(thumb, y - thumb.Height / 2);
    }

    private static Border Thumb(double size) => new()
    {
        Width = size,
        Height = size,
        CornerRadius = new CornerRadius(size / 2),
        BorderBrush = Brushes.White,
        BorderThickness = new Thickness(2.5),
        BoxShadow = BoxShadows.Parse("0 0 4 0 #80000000"),
        IsHitTestVisible = false,
    };

    private static LinearGradientBrush Gradient(Color from, Color to, bool horizontal) => new()
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = horizontal ? new RelativePoint(1, 0, RelativeUnit.Relative) : new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops = { new GradientStop(from, 0), new GradientStop(to, 1) },
    };

    /// <summary>Pointer drag inside an element (with pointer capture).</summary>
    private static void Drag(Control element, Action<Point> moved)
    {
        element.PointerPressed += (_, e) =>
        {
            e.Pointer.Capture(element);
            moved(e.GetPosition(element));
            e.Handled = true;
        };
        element.PointerMoved += (_, e) =>
        {
            if (ReferenceEquals(e.Pointer.Captured, element)) moved(e.GetPosition(element));
        };
        element.PointerReleased += (_, e) => e.Pointer.Capture(null);
    }

    private static IBrush Checkerboard()
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing { Brush = Brushes.White, Geometry = new RectangleGeometry(new Rect(0, 0, 8, 8)) });
        group.Children.Add(new GeometryDrawing
        {
            Brush = new ImmutableSolidColorBrush(Color.FromRgb(0xdd, 0xdd, 0xdd)),
            Geometry = new GeometryGroup { Children = { new RectangleGeometry(new Rect(0, 0, 4, 4)), new RectangleGeometry(new Rect(4, 4, 4, 4)) } },
        });
        return new DrawingBrush(group) { TileMode = TileMode.Tile, DestinationRect = new RelativeRect(0, 0, 8, 8, RelativeUnit.Absolute) };
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
[TemplatePart("PART_Browse", typeof(Button))]
[PseudoClasses(":dragover")]
public class HelixFileDrop : TemplatedControl
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixFileDrop>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixFileDrop>();
    public static readonly StyledProperty<string?> AcceptProperty = AvaloniaProperty.Register<HelixFileDrop, string?>(nameof(Accept));
    public static readonly StyledProperty<bool> MultipleProperty = AvaloniaProperty.Register<HelixFileDrop, bool>(nameof(Multiple), true);
    public static readonly StyledProperty<long> MaxFileSizeProperty = AvaloniaProperty.Register<HelixFileDrop, long>(nameof(MaxFileSize));
    public static readonly StyledProperty<int> MaxFilesProperty = AvaloniaProperty.Register<HelixFileDrop, int>(nameof(MaxFiles));
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixFileDrop, string?>(nameof(Title));
    public static readonly StyledProperty<string?> HintProperty = AvaloniaProperty.Register<HelixFileDrop, string?>(nameof(Hint));
    public static readonly StyledProperty<string?> BrowseTextProperty = AvaloniaProperty.Register<HelixFileDrop, string?>(nameof(BrowseText));
    public static readonly StyledProperty<bool> ShowListProperty = AvaloniaProperty.Register<HelixFileDrop, bool>(nameof(ShowList), true);
    public static readonly StyledProperty<bool> IsCompactProperty = AvaloniaProperty.Register<HelixFileDrop, bool>(nameof(IsCompact));
    public static readonly DirectProperty<HelixFileDrop, bool> IsDragOverProperty = AvaloniaProperty.RegisterDirect<HelixFileDrop, bool>(nameof(IsDragOver), d => d.IsDragOver);
    public static readonly DirectProperty<HelixFileDrop, bool> HasFilesProperty = AvaloniaProperty.RegisterDirect<HelixFileDrop, bool>(nameof(HasFiles), d => d.HasFiles);
    public static readonly DirectProperty<HelixFileDrop, string?> ErrorProperty = AvaloniaProperty.RegisterDirect<HelixFileDrop, string?>(nameof(Error), d => d.Error);
    public static readonly DirectProperty<HelixFileDrop, string?> AutoHintProperty = AvaloniaProperty.RegisterDirect<HelixFileDrop, string?>(nameof(AutoHint), d => d.AutoHint);
    public static readonly RoutedEvent<RoutedEventArgs> FilesChangedEvent = RoutedEvent.Register<HelixFileDrop, RoutedEventArgs>(nameof(FilesChanged), RoutingStrategies.Bubble);

    public HelixFileDrop()
    {
        SetCurrentValue(TitleProperty, HelixTexts.Current.DropFilesHere);
        SetCurrentValue(BrowseTextProperty, HelixTexts.Current.Browse);
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, (_, _) => IsDragOver = false);
        AddHandler(DragDrop.DropEvent, OnDrop);
        Files.CollectionChanged += (_, _) =>
        {
            HasFiles = Files.Count > 0;
            RaiseEvent(new RoutedEventArgs(FilesChangedEvent, this));
        };
    }

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Allowed types: ".pdf,.xlsx" or "image/*". Null accepts everything.</summary>
    public string? Accept { get => GetValue(AcceptProperty); set => SetValue(AcceptProperty, value); }
    public bool Multiple { get => GetValue(MultipleProperty); set => SetValue(MultipleProperty, value); }

    /// <summary>Bytes; 0 = no limit.</summary>
    public long MaxFileSize { get => GetValue(MaxFileSizeProperty); set => SetValue(MaxFileSizeProperty, value); }

    /// <summary>0 = no limit.</summary>
    public int MaxFiles { get => GetValue(MaxFilesProperty); set => SetValue(MaxFilesProperty, value); }
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Line under the title; by default the accepted types and size limit.</summary>
    public string? Hint { get => GetValue(HintProperty); set => SetValue(HintProperty, value); }
    public string? BrowseText { get => GetValue(BrowseTextProperty); set => SetValue(BrowseTextProperty, value); }
    public bool ShowList { get => GetValue(ShowListProperty); set => SetValue(ShowListProperty, value); }

    /// <summary>A single row instead of the big zone.</summary>
    public bool IsCompact { get => GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }

    private bool _isDragOver, _hasFiles;
    private string? _error, _autoHint;

    public bool IsDragOver
    {
        get => _isDragOver;
        private set
        {
            SetAndRaise(IsDragOverProperty, ref _isDragOver, value);
            PseudoClasses.Set(":dragover", value);
        }
    }

    public bool HasFiles { get => _hasFiles; private set => SetAndRaise(HasFilesProperty, ref _hasFiles, value); }

    /// <summary>Why the last file was refused (type, size, count).</summary>
    public string? Error { get => _error; private set => SetAndRaise(ErrorProperty, ref _error, value); }
    public string? AutoHint { get => _autoHint; private set => SetAndRaise(AutoHintProperty, ref _autoHint, value); }

    public ObservableCollection<HelixFile> Files { get; } = [];

    public event EventHandler<RoutedEventArgs>? FilesChanged { add => AddHandler(FilesChangedEvent, value); remove => RemoveHandler(FilesChangedEvent, value); }

    private Button? _browse;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_browse is not null) _browse.Click -= OnBrowse;
        base.OnApplyTemplate(e);
        _browse = e.NameScope.Find<Button>("PART_Browse");
        if (_browse is not null) _browse.Click += OnBrowse;
        UpdateHint();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HintProperty || change.Property == AcceptProperty || change.Property == MaxFileSizeProperty) UpdateHint();
    }

    internal void Remove(HelixFile file) => Files.Remove(file);

    private void UpdateHint()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Accept)) parts.Add(Accept.Replace(",", ", ", StringComparison.Ordinal));
        if (MaxFileSize > 0) parts.Add($"{FormatSize(MaxFileSize)} max");
        AutoHint = Hint ?? (parts.Count == 0 ? null : string.Join(" · ", parts));
    }

    private async void OnBrowse(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { CanOpen: true } storage) return;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = Multiple, FileTypeFilter = PickerTypes() });
        AddFiles(files.Select(f => f.TryGetLocalPath()).OfType<string>());
    }

    private static bool CarriesFiles(DragEventArgs e) => e.DataTransfer.Contains(DataFormat.File);

    private void OnDragEnter(object? sender, DragEventArgs e) => IsDragOver = CarriesFiles(e) && IsEnabled;

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = CarriesFiles(e) && IsEnabled ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        IsDragOver = false;
        if (!IsEnabled || e.DataTransfer.TryGetFiles() is not { } items) return;
        var paths = items.Select(i => i.TryGetLocalPath()).OfType<string>();
        AddFiles(Multiple ? paths : paths.Take(1));
    }

    /// <summary>Adds files by path (checks type, size and count; the reason of a refusal goes to Error).</summary>
    public void AddFiles(IEnumerable<string> paths)
    {
        string? error = null;
        if (!Multiple) Files.Clear();
        foreach (var path in paths)
        {
            var info = new FileInfo(path);
            if (!info.Exists) continue;
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

            if (Files.All(f => !string.Equals(f.Path, info.FullName, StringComparison.OrdinalIgnoreCase))) Files.Add(new HelixFile(this, info));
        }

        Error = error;
    }

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg", ".tif", ".tiff"];

    private bool Accepts(string name)
    {
        if (string.IsNullOrWhiteSpace(Accept)) return true;
        var extension = System.IO.Path.GetExtension(name).ToLowerInvariant();
        foreach (var rule in Accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var r = rule.ToLowerInvariant();
            if (r.StartsWith('.') && r == extension) return true;
            if (r == "image/*" && ImageExtensions.Contains(extension)) return true;
            if (r is "*" or "*/*") return true;
        }

        return false;
    }

    private List<FilePickerFileType>? PickerTypes()
    {
        if (string.IsNullOrWhiteSpace(Accept)) return null;
        var patterns = Accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(r => r == "image/*" ? ImageExtensions.Select(x => "*" + x) : r.StartsWith('.') ? ["*" + r] : new[] { "*.*" })
            .Distinct().ToList();
        return [new FilePickerFileType(Accept) { Patterns = patterns }];
    }

    public static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        < 1024L * 1024 * 1024 => $"{bytes / 1024.0 / 1024:0.#} MB",
        _ => $"{bytes / 1024.0 / 1024 / 1024:0.#} GB",
    };
}
