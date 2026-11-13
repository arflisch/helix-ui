using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Helix.Avalonia;

/// <summary>A "loading" veil over its content while <see cref="IsBusy"/>, shown after <see cref="Delay"/> so quick work doesn't flicker.</summary>
public class HelixBusy : ContentControl
{
    public static readonly StyledProperty<bool> IsBusyProperty = AvaloniaProperty.Register<HelixBusy, bool>(nameof(IsBusy));
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<HelixBusy, string?>(nameof(Text));
    public static readonly StyledProperty<TimeSpan> DelayProperty = AvaloniaProperty.Register<HelixBusy, TimeSpan>(nameof(Delay), TimeSpan.FromMilliseconds(150));
    public static readonly DirectProperty<HelixBusy, bool> IsVeilShownProperty = AvaloniaProperty.RegisterDirect<HelixBusy, bool>(nameof(IsVeilShown), b => b.IsVeilShown);

    /// <summary>Shows the veil (after Delay) and blocks the content.</summary>
    public bool IsBusy { get => GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }

    /// <summary>Text under the spinner, e.g. "Saving…".</summary>
    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Wait before showing the veil (150 ms).</summary>
    public TimeSpan Delay { get => GetValue(DelayProperty); set => SetValue(DelayProperty, value); }

    private bool _isVeilShown;

    /// <summary>True while the veil is visible.</summary>
    public bool IsVeilShown { get => _isVeilShown; private set => SetAndRaise(IsVeilShownProperty, ref _isVeilShown, value); }

    private DispatcherTimer? _timer;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // Delay may be set after IsBusy (XAML attribute order): re-arm with the new value.
        if (change.Property != IsBusyProperty && !(change.Property == DelayProperty && IsBusy && !IsVeilShown)) return;
        _timer?.Stop();
        if (!IsBusy)
        {
            IsVeilShown = false;
            return;
        }

        if (Delay <= TimeSpan.Zero)
        {
            IsVeilShown = true;
            return;
        }

        _timer = new DispatcherTimer { Interval = Delay };
        _timer.Tick += (_, _) =>
        {
            _timer?.Stop();
            IsVeilShown = IsBusy;
        };
        _timer.Start();
    }
}

/// <summary>A zone behind a <see cref="HelixTargetBar"/>, e.g. the tolerance around the target.</summary>
public sealed record HelixTargetBand(double From, double To, HelixTone Tone, string? Label = null);

/// <summary>A value against its target: the bar, a mark at the target, optional bands; with Tolerance, green / orange / red.</summary>
[TemplatePart("PART_Track", typeof(HelixTargetTrack))]
[PseudoClasses(":ok", ":warn", ":off")]
public class HelixTargetBar : TemplatedControl
{
    public static readonly StyledProperty<double?> ValueProperty = AvaloniaProperty.Register<HelixTargetBar, double?>(nameof(Value));
    public static readonly StyledProperty<double?> TargetProperty = AvaloniaProperty.Register<HelixTargetBar, double?>(nameof(Target));
    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<HelixTargetBar, double>(nameof(Minimum));
    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<HelixTargetBar, double>(nameof(Maximum), 100);
    public static readonly StyledProperty<double?> ToleranceProperty = AvaloniaProperty.Register<HelixTargetBar, double?>(nameof(Tolerance));
    public static readonly StyledProperty<IList<HelixTargetBand>?> BandsProperty = AvaloniaProperty.Register<HelixTargetBar, IList<HelixTargetBand>?>(nameof(Bands));
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<HelixTargetBar, string?>(nameof(Label));
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<HelixTargetBar, string?>(nameof(Unit));
    public static readonly StyledProperty<string> NumberFormatProperty = AvaloniaProperty.Register<HelixTargetBar, string>(nameof(NumberFormat), "N0");
    public static readonly StyledProperty<bool> ShowValuesProperty = AvaloniaProperty.Register<HelixTargetBar, bool>(nameof(ShowValues), true);
    public static readonly StyledProperty<double> TrackHeightProperty = AvaloniaProperty.Register<HelixTargetBar, double>(nameof(TrackHeight), 14);
    public static readonly DirectProperty<HelixTargetBar, string> ValueTextProperty = AvaloniaProperty.RegisterDirect<HelixTargetBar, string>(nameof(ValueText), b => b.ValueText);
    public static readonly DirectProperty<HelixTargetBar, string?> TargetTextProperty = AvaloniaProperty.RegisterDirect<HelixTargetBar, string?>(nameof(TargetText), b => b.TargetText);
    public static readonly DirectProperty<HelixTargetBar, string?> DeltaTextProperty = AvaloniaProperty.RegisterDirect<HelixTargetBar, string?>(nameof(DeltaText), b => b.DeltaText);
    public static readonly DirectProperty<HelixTargetBar, string> StateProperty = AvaloniaProperty.RegisterDirect<HelixTargetBar, string>(nameof(State), b => b.State);

    public double? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The target, shown as a mark across the bar.</summary>
    public double? Target { get => GetValue(TargetProperty); set => SetValue(TargetProperty, value); }
    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>Allowed gap: green within it, orange beyond, red beyond twice.</summary>
    public double? Tolerance { get => GetValue(ToleranceProperty); set => SetValue(ToleranceProperty, value); }

    /// <summary>Zones behind the bar (From, To, Tone, Label).</summary>
    public IList<HelixTargetBand>? Bands { get => GetValue(BandsProperty); set => SetValue(BandsProperty, value); }
    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string NumberFormat { get => GetValue(NumberFormatProperty); set => SetValue(NumberFormatProperty, value); }
    public bool ShowValues { get => GetValue(ShowValuesProperty); set => SetValue(ShowValuesProperty, value); }
    public double TrackHeight { get => GetValue(TrackHeightProperty); set => SetValue(TrackHeightProperty, value); }

    private string _valueText = "—", _state = "none";
    private string? _targetText, _deltaText;

    public string ValueText { get => _valueText; private set => SetAndRaise(ValueTextProperty, ref _valueText, value); }

    /// <summary>"/ 120 MW".</summary>
    public string? TargetText { get => _targetText; private set => SetAndRaise(TargetTextProperty, ref _targetText, value); }

    /// <summary>"+12 MW" (gap to the target).</summary>
    public string? DeltaText { get => _deltaText; private set => SetAndRaise(DeltaTextProperty, ref _deltaText, value); }

    /// <summary>"ok", "warn", "off" with Tolerance, else "none": drives the bar color (also :ok, :warn, :off).</summary>
    public string State { get => _state; private set => SetAndRaise(StateProperty, ref _state, value); }

    private HelixTargetTrack? _track;

    public HelixTargetBar() => ActualThemeVariantChanged += (_, _) => _track?.InvalidateVisual();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _track = e.NameScope.Find<HelixTargetTrack>("PART_Track");
        if (_track is not null) _track.Owner = this;
        Update();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == TargetProperty || change.Property == MinimumProperty || change.Property == MaximumProperty
            || change.Property == ToleranceProperty || change.Property == BandsProperty || change.Property == UnitProperty || change.Property == NumberFormatProperty
            || change.Property == TrackHeightProperty) Update();
    }

    private string Format(double value) => value.ToString(NumberFormat, CultureInfo.CurrentCulture) + (string.IsNullOrEmpty(Unit) ? "" : " " + Unit);

    private void Update()
    {
        ValueText = Value is { } v ? Format(v) : "—";
        TargetText = Target is { } t ? "/ " + Format(t) : null;
        DeltaText = Value is { } a && Target is { } b ? (a - b >= 0 ? "+" : "") + Format(a - b) : null;
        State = Tolerance is not { } tol || Value is not { } val || Target is not { } tar ? "none"
            : Math.Abs(val - tar) <= tol ? "ok" : Math.Abs(val - tar) <= tol * 2 ? "warn" : "off";
        PseudoClasses.Set(":ok", State == "ok");
        PseudoClasses.Set(":warn", State == "warn");
        PseudoClasses.Set(":off", State == "off");
        _track?.InvalidateMeasure();
        _track?.InvalidateVisual();
    }
}

/// <summary>The drawn track of a <see cref="HelixTargetBar"/>: bands, bar and target mark.</summary>
public class HelixTargetTrack : Control
{
    internal HelixTargetBar? Owner { get; set; }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 200 : availableSize.Width, (Owner?.TrackHeight ?? 14) + 8);

    private IBrush Res(string key, Color fallback) => HelixDraw.Brush(this, key, fallback);

    public override void Render(DrawingContext dc)
    {
        if (Owner is not { } bar) return;
        var width = Bounds.Width;
        var height = bar.TrackHeight;
        const double top = 4;
        double X(double value) => bar.Maximum <= bar.Minimum ? 0 : Math.Clamp((value - bar.Minimum) / (bar.Maximum - bar.Minimum), 0, 1) * width;

        var radius = height / 2;
        var track = new RoundedRect(new Rect(0, top, width, height), radius);
        dc.DrawRectangle(Res(HelixKeys.Surface2, Colors.LightGray), null, track);
        if (bar.Bands is { } bands)
        {
            using (dc.PushClip(track))
            {
                foreach (var band in bands)
                {
                    var color = HelixDraw.ColorOf(this, HelixKeys.Of(band.Tone), Colors.Gray);
                    dc.DrawRectangle(new ImmutableSolidColorBrush(color, 0.22), null, new Rect(X(band.From), top, Math.Max(0, X(band.To) - X(band.From)), height));
                }
            }
        }

        if (bar.Value is { } value)
        {
            var brush = bar.State switch
            {
                "ok" => Res(HelixKeys.Success, Colors.Green),
                "warn" => Res(HelixKeys.Warning, Colors.Orange),
                "off" => Res(HelixKeys.Danger, Colors.Red),
                _ => Res(HelixKeys.Primary, Colors.SteelBlue),
            };
            var barHeight = height / 2;
            dc.DrawRectangle(brush, null, new RoundedRect(new Rect(0, top + height / 4, X(value), barHeight), barHeight / 2));
        }

        if (bar.Target is { } target)
        {
            var x = X(target);
            dc.DrawRectangle(Res(HelixKeys.Text, Colors.Black), null, new RoundedRect(new Rect(Math.Clamp(x - 1.5, 0, Math.Max(0, width - 3)), 0, 3, height + 8), 1.5));
        }
    }
}

/// <summary>Label / value pairs in <see cref="Columns"/> columns (detail screens, summaries).</summary>
public class HelixDescriptionList : ItemsControl
{
    public static readonly StyledProperty<int> ColumnsProperty = AvaloniaProperty.Register<HelixDescriptionList, int>(nameof(Columns), 1);

    /// <summary>Inherited by the items.</summary>
    public static readonly StyledProperty<bool> IsInlineProperty = AvaloniaProperty.Register<HelixDescriptionList, bool>(nameof(IsInline), inherits: true);
    public static readonly StyledProperty<bool> IsBorderedProperty = AvaloniaProperty.Register<HelixDescriptionList, bool>(nameof(IsBordered), inherits: true);
    public static readonly StyledProperty<double> LabelWidthProperty = AvaloniaProperty.Register<HelixDescriptionList, double>(nameof(LabelWidth), 140, inherits: true);

    protected override Type StyleKeyOverride => typeof(HelixDescriptionList);

    public int Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>Label on the left of the value instead of above it.</summary>
    public bool IsInline { get => GetValue(IsInlineProperty); set => SetValue(IsInlineProperty, value); }

    /// <summary>A line under each item.</summary>
    public bool IsBordered { get => GetValue(IsBorderedProperty); set => SetValue(IsBorderedProperty, value); }
    public double LabelWidth { get => GetValue(LabelWidthProperty); set => SetValue(LabelWidthProperty, value); }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) => NeedsContainer<HelixDescription>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new HelixDescription();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ColumnsProperty) ItemsPanelRoot?.InvalidateMeasure();
    }
}

/// <summary>One pair of a <see cref="HelixDescriptionList"/>: Label and any content as value.</summary>
public class HelixDescription : ContentControl
{
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<HelixDescription, string?>(nameof(Label));
    public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<HelixDescription, string?>(nameof(Icon));
    public static readonly StyledProperty<int> SpanProperty = AvaloniaProperty.Register<HelixDescription, int>(nameof(Span), 1);
    public static readonly StyledProperty<bool> IsInlineProperty = HelixDescriptionList.IsInlineProperty.AddOwner<HelixDescription>();
    public static readonly StyledProperty<bool> IsBorderedProperty = HelixDescriptionList.IsBorderedProperty.AddOwner<HelixDescription>();
    public static readonly StyledProperty<double> LabelWidthProperty = HelixDescriptionList.LabelWidthProperty.AddOwner<HelixDescription>();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SpanProperty) (Parent as Control ?? this.GetVisualParent() as Control)?.InvalidateMeasure();
    }

    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Columns taken (e.g. 2 for a long text).</summary>
    public int Span { get => GetValue(SpanProperty); set => SetValue(SpanProperty, value); }
    public bool IsInline { get => GetValue(IsInlineProperty); set => SetValue(IsInlineProperty, value); }
    public bool IsBordered { get => GetValue(IsBorderedProperty); set => SetValue(IsBorderedProperty, value); }
    public double LabelWidth { get => GetValue(LabelWidthProperty); set => SetValue(LabelWidthProperty, value); }
}

/// <summary>Lays children in columns, each taking its span (description lists and auto forms).</summary>
public abstract class HelixSpanPanel : Panel
{
    protected abstract int Columns { get; }
    protected abstract double GapX { get; }
    protected abstract double GapY { get; }
    protected abstract int SpanOf(Control child);

    private IEnumerable<List<(Control Child, int Span)>> Rows()
    {
        var columns = Math.Max(1, Columns);
        var row = new List<(Control, int)>();
        var used = 0;
        foreach (var child in Children.Where(c => c.IsVisible))
        {
            var span = Math.Clamp(SpanOf(child), 1, columns);
            if (used + span > columns && row.Count > 0)
            {
                yield return row;
                row = [];
                used = 0;
            }

            row.Add((child, span));
            used += span;
        }

        if (row.Count > 0) yield return row;
    }

    private double Cell(double width) => Math.Max(0, (width - GapX * (Math.Max(1, Columns) - 1)) / Math.Max(1, Columns));

    protected override Size MeasureOverride(Size available)
    {
        var width = double.IsInfinity(available.Width) ? 640 : available.Width;
        var cell = Cell(width);
        var height = 0.0;
        var first = true;
        foreach (var row in Rows())
        {
            var rowHeight = 0.0;
            foreach (var (child, span) in row)
            {
                child.Measure(new Size(cell * span + GapX * (span - 1), double.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            }

            height += rowHeight + (first ? 0 : GapY);
            first = false;
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size final)
    {
        var cell = Cell(final.Width);
        var y = 0.0;
        foreach (var row in Rows())
        {
            var rowHeight = row.Max(r => r.Child.DesiredSize.Height);
            var x = 0.0;
            foreach (var (child, span) in row)
            {
                var w = cell * span + GapX * (span - 1);
                child.Arrange(new Rect(x, y, w, rowHeight));
                x += w + GapX;
            }

            y += rowHeight + GapY;
        }

        return final;
    }
}

/// <summary>Lays the items of a <see cref="HelixDescriptionList"/> in columns, each taking its Span.</summary>
public class HelixDescriptionPanel : HelixSpanPanel
{
    protected override int Columns => (TemplatedParent as HelixDescriptionList ?? this.FindLogicalAncestorOfType<HelixDescriptionList>())?.Columns ?? 1;
    protected override double GapX => 28;
    protected override double GapY => 16;
    protected override int SpanOf(Control child) => child is HelixDescription d ? d.Span : 1;
}

/// <summary>
/// A form built from an object: one field per property, the right control for its type, labels and rules from the usual
/// attributes ([Display], [Required], [Range], [StringLength]…, plus [HelixField]); two-way bindings to the model.
/// </summary>
[TemplatePart("PART_Fields", typeof(Panel))]
[TemplatePart("PART_Submit", typeof(Button))]
[TemplatePart("PART_Cancel", typeof(Button))]
public class HelixAutoForm : TemplatedControl
{
    public static readonly StyledProperty<object?> ModelProperty = AvaloniaProperty.Register<HelixAutoForm, object?>(nameof(Model));
    public static readonly StyledProperty<int> ColumnsProperty = AvaloniaProperty.Register<HelixAutoForm, int>(nameof(Columns), 2);
    public static readonly StyledProperty<string?> FieldsProperty = AvaloniaProperty.Register<HelixAutoForm, string?>(nameof(Fields));
    public static readonly StyledProperty<string> SubmitTextProperty = AvaloniaProperty.Register<HelixAutoForm, string>(nameof(SubmitText), "");
    public static readonly StyledProperty<string> CancelTextProperty = AvaloniaProperty.Register<HelixAutoForm, string>(nameof(CancelText), "");
    public static readonly StyledProperty<bool> ShowActionsProperty = AvaloniaProperty.Register<HelixAutoForm, bool>(nameof(ShowActions), true);
    public static readonly StyledProperty<bool> ShowCancelProperty = AvaloniaProperty.Register<HelixAutoForm, bool>(nameof(ShowCancel));
    public static readonly RoutedEvent<RoutedEventArgs> SubmittedEvent = RoutedEvent.Register<HelixAutoForm, RoutedEventArgs>(nameof(Submitted), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<RoutedEventArgs> CancelledEvent = RoutedEvent.Register<HelixAutoForm, RoutedEventArgs>(nameof(Cancelled), RoutingStrategies.Bubble);

    /// <summary>Span of a field in the form's columns (set by the form from [HelixField(Span = …)]).</summary>
    public static readonly AttachedProperty<int> SpanProperty = AvaloniaProperty.RegisterAttached<HelixAutoForm, Control, int>("Span", 1);

    public static int GetSpan(Control element) => element.GetValue(SpanProperty);
    public static void SetSpan(Control element, int value) => element.SetValue(SpanProperty, value);

    public HelixAutoForm()
    {
        SetCurrentValue(SubmitTextProperty, HelixTexts.Current.Save);
        SetCurrentValue(CancelTextProperty, HelixTexts.Current.Cancel);
    }

    /// <summary>The object edited in place.</summary>
    public object? Model { get => GetValue(ModelProperty); set => SetValue(ModelProperty, value); }

    /// <summary>Columns of the form (2).</summary>
    public int Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>Only these properties, comma-separated, in this order (null: every editable property, by [Display(Order)]).</summary>
    public string? Fields { get => GetValue(FieldsProperty); set => SetValue(FieldsProperty, value); }
    public string SubmitText { get => GetValue(SubmitTextProperty); set => SetValue(SubmitTextProperty, value); }
    public string CancelText { get => GetValue(CancelTextProperty); set => SetValue(CancelTextProperty, value); }

    /// <summary>The submit (and cancel) buttons (true). Without them, call Validate() yourself.</summary>
    public bool ShowActions { get => GetValue(ShowActionsProperty); set => SetValue(ShowActionsProperty, value); }
    public bool ShowCancel { get => GetValue(ShowCancelProperty); set => SetValue(ShowCancelProperty, value); }

    /// <summary>Raised when the submit button is pressed and every rule passes.</summary>
    public event EventHandler<RoutedEventArgs>? Submitted { add => AddHandler(SubmittedEvent, value); remove => RemoveHandler(SubmittedEvent, value); }
    public event EventHandler<RoutedEventArgs>? Cancelled { add => AddHandler(CancelledEvent, value); remove => RemoveHandler(CancelledEvent, value); }

    private sealed record Entry(PropertyInfo Property, string Label, Control Field, Action<string?> ShowError);

    private readonly List<Entry> _entries = [];
    private Panel? _panel;
    private Button? _submit, _cancel;

    /// <summary>The generated fields, in order (tests, custom layouts).</summary>
    public IReadOnlyList<Control> FieldControls => _entries.Select(e => e.Field).ToList();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_submit is not null) _submit.Click -= OnSubmit;
        if (_cancel is not null) _cancel.Click -= OnCancel;
        base.OnApplyTemplate(e);
        _panel = e.NameScope.Find<Panel>("PART_Fields");
        _submit = e.NameScope.Find<Button>("PART_Submit");
        _cancel = e.NameScope.Find<Button>("PART_Cancel");
        if (_submit is not null) _submit.Click += OnSubmit;
        if (_cancel is not null) _cancel.Click += OnCancel;
        Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ModelProperty || change.Property == FieldsProperty) Build();
        else if (change.Property == ColumnsProperty) _panel?.InvalidateMeasure();
    }

    /// <summary>Validates, then raises Submitted when every rule passes (the submit button does this).</summary>
    public bool Submit()
    {
        if (!Validate()) return false;
        RaiseEvent(new RoutedEventArgs(SubmittedEvent, this));
        return true;
    }

    private void OnSubmit(object? sender, RoutedEventArgs e) => Submit();

    private void OnCancel(object? sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(CancelledEvent, this));

    private static string Humanize(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + char.ToLowerInvariant(c) : c.ToString()));

    private static bool IsNumber(Type t) => t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) || t == typeof(double) || t == typeof(float) || t == typeof(decimal);

    private void Build()
    {
        if (_panel is null) return;
        _panel.Children.Clear();
        _entries.Clear();
        if (Model is not { } model) return;

        var properties = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
            .Where(p => p.GetCustomAttribute<ScaffoldColumnAttribute>()?.Scaffold != false && p.GetCustomAttribute<HelixFieldAttribute>()?.Hidden != true)
            .ToList();
        var ordered = Fields is { Length: > 0 } names
            ? names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(n => properties.FirstOrDefault(p => p.Name == n)).OfType<PropertyInfo>().ToList()
            : properties.OrderBy(p => p.GetCustomAttribute<DisplayAttribute>()?.GetOrder() ?? 0).ToList();

        foreach (var property in ordered)
        {
            if (Create(model, property) is not { } entry) continue;
            SetSpan(entry.Field, Math.Clamp(property.GetCustomAttribute<HelixFieldAttribute>()?.Span ?? 1, 1, Math.Max(1, Columns)));
            _panel.Children.Add(entry.Field);
            _entries.Add(entry);
        }
    }

    private Entry? Create(object model, PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var display = property.GetCustomAttribute<DisplayAttribute>();
        var hints = property.GetCustomAttribute<HelixFieldAttribute>();
        var label = display?.GetName() ?? Humanize(property.Name);
        var hint = display?.GetDescription();
        var required = property.GetCustomAttribute<RequiredAttribute>() is not null;
        var readOnly = property.GetCustomAttribute<EditableAttribute>()?.AllowEdit == false;
        Binding Bind(IValueConverter? converter = null, object? parameter = null) => new(property.Name)
        {
            Source = model, Mode = readOnly ? BindingMode.OneWay : BindingMode.TwoWay, Converter = converter, ConverterParameter = parameter,
        };
        Entry? entry = null;
        void Watch(AvaloniaObject control, AvaloniaProperty watched) => control.PropertyChanged += (_, e) =>
        {
            if (e.Property == watched && entry is not null) ValidateEntry(entry);
        };

        if (type == typeof(bool))
        {
            var box = new HelixSwitch { Content = label, IsEnabled = !readOnly };
            box.Bind(ToggleButton.IsCheckedProperty, Bind());
            var error = new TextBlock { FontSize = 12.5, TextWrapping = TextWrapping.Wrap, IsVisible = false };
            error.Bind(TextBlock.ForegroundProperty, error.GetResourceObservable(HelixKeys.Danger));
            var hintText = new TextBlock { Text = hint, FontSize = 12.5, TextWrapping = TextWrapping.Wrap, IsVisible = !string.IsNullOrEmpty(hint) };
            hintText.Bind(TextBlock.ForegroundProperty, hintText.GetResourceObservable(HelixKeys.TextMuted));
            var field = new StackPanel { Spacing = 6, Margin = new Thickness(0, 26, 0, 0), Children = { box, hintText, error } };
            Watch(box, ToggleButton.IsCheckedProperty);
            return entry = new Entry(property, label, field, message => { error.Text = message; error.IsVisible = !string.IsNullOrEmpty(message); });
        }

        if (IsNumber(type))
        {
            var range = property.GetCustomAttribute<RangeAttribute>();
            var integer = type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte);
            var number = new HelixNumberField
            {
                Label = label, Hint = hint, IsRequired = required, IsReadOnly = readOnly, Unit = hints?.Unit, Placeholder = hints?.Placeholder,
                Step = hints?.Step ?? 1,
                Decimals = integer ? 0 : hints?.Decimals is >= 0 ? hints.Decimals : null,
                Minimum = range?.Minimum is { } min ? Convert.ToDouble(min, CultureInfo.InvariantCulture) : null,
                Maximum = range?.Maximum is { } max ? Convert.ToDouble(max, CultureInfo.InvariantCulture) : null,
            };
            number.Bind(HelixNumberField.ValueProperty, Bind(NumberConverter.Instance, property.PropertyType));
            Watch(number, HelixNumberField.ValueProperty);
            return entry = new Entry(property, label, number, error => number.Error = error);
        }

        if (type.IsEnum)
        {
            var select = new HelixSelect { Label = label, Hint = hint, IsRequired = required, IsEnabled = !readOnly, ItemsSource = Enum.GetValues(type) };
            select.Bind(SelectingItemsControl.SelectedItemProperty, Bind());
            Watch(select, SelectingItemsControl.SelectedItemProperty);
            return entry = new Entry(property, label, select, error => select.Error = error);
        }

        if (type == typeof(DateTime) && hints?.WithTime != false)
        {
            var picker = new HelixDateTimePicker { Label = label, Hint = hint, IsRequired = required, IsEnabled = !readOnly };
            picker.Bind(HelixDateTimePicker.SelectedDateTimeProperty, Bind());
            Watch(picker, HelixDateTimePicker.SelectedDateTimeProperty);
            return entry = new Entry(property, label, picker, error => picker.Error = error);
        }

        if (type == typeof(DateTime) || type == typeof(DateOnly))
        {
            var date = new HelixDatePicker { Label = label, Hint = hint, IsRequired = required, IsEnabled = !readOnly };
            date.Bind(HelixDatePicker.SelectedDateProperty, Bind(type == typeof(DateOnly) ? DateOnlyConverter.Instance : null));
            Watch(date, HelixDatePicker.SelectedDateProperty);
            return entry = new Entry(property, label, date, error => date.Error = error);
        }

        if (type == typeof(string))
        {
            var text = new HelixTextField { Label = label, Hint = hint, IsRequired = required, IsReadOnly = readOnly, Placeholder = hints?.Placeholder };
            text.Bind(HelixTextField.TextProperty, Bind());
            Watch(text, HelixTextField.TextProperty);
            return entry = new Entry(property, label, text, error => text.Error = error);
        }

        return null; // other types are left out
    }

    private bool ValidateEntry(Entry entry)
    {
        if (Model is not { } model) return true;
        var results = new List<ValidationResult>();
        Validator.TryValidateProperty(entry.Property.GetValue(model), new ValidationContext(model) { MemberName = entry.Property.Name, DisplayName = entry.Label }, results);
        entry.ShowError(results.FirstOrDefault()?.ErrorMessage);
        return results.Count == 0;
    }

    /// <summary>Checks every field (and IValidatableObject rules); shows the errors; true when the model is valid.</summary>
    public bool Validate()
    {
        if (Model is not { } model) return false;
        var valid = true;
        foreach (var entry in _entries) valid &= ValidateEntry(entry);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
        {
            foreach (var result in results)
            {
                foreach (var member in result.MemberNames) _entries.FirstOrDefault(e => e.Property.Name == member)?.ShowError(result.ErrorMessage);
            }

            valid = false;
        }

        return valid;
    }

    private sealed class NumberConverter : IValueConverter
    {
        public static readonly NumberConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is null ? null : System.Convert.ToDouble(value, CultureInfo.InvariantCulture);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var type = parameter as Type ?? targetType;
            var target = Nullable.GetUnderlyingType(type) ?? type;
            if (value is not double number) return Nullable.GetUnderlyingType(type) is null ? Activator.CreateInstance(target) : null;
            return System.Convert.ChangeType(target == typeof(decimal) ? (object)(decimal)number : Math.Round(number, target == typeof(double) || target == typeof(float) ? 15 : 0), target, CultureInfo.InvariantCulture);
        }
    }

    private sealed class DateOnlyConverter : IValueConverter
    {
        public static readonly DateOnlyConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is DateOnly day ? day.ToDateTime(TimeOnly.MinValue) : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is DateTime date ? DateOnly.FromDateTime(date) : null;
    }
}

/// <summary>Lays the fields of a <see cref="HelixAutoForm"/> in columns, each taking its Span.</summary>
public class HelixAutoFormPanel : HelixSpanPanel
{
    protected override int Columns => (TemplatedParent as HelixAutoForm ?? this.FindLogicalAncestorOfType<HelixAutoForm>())?.Columns ?? 2;
    protected override double GapX => 20;
    protected override double GapY => 16;
    protected override int SpanOf(Control child) => HelixAutoForm.GetSpan(child);
}
