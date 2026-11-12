using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Helix.Wpf;

/// <summary>
/// A form built from an object: one field per property, the right control for its type, labels and rules from the usual
/// attributes ([Display], [Required], [Range], [StringLength]…, plus [HelixField]); two-way bindings to the model.
/// </summary>
[TemplatePart(Name = "PART_Fields", Type = typeof(Panel))]
[TemplatePart(Name = "PART_Submit", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_Cancel", Type = typeof(ButtonBase))]
public class HelixAutoForm : Control
{
    static HelixAutoForm() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixAutoForm), new FrameworkPropertyMetadata(typeof(HelixAutoForm)));

    public HelixAutoForm()
    {
        SetCurrentValue(SubmitTextProperty, HelixTexts.Current.Save);
        SetCurrentValue(CancelTextProperty, HelixTexts.Current.Cancel);
    }

    public static readonly DependencyProperty ModelProperty = HelixProperty.Register<HelixAutoForm, object?>(nameof(Model), null, (d, _) => ((HelixAutoForm)d).Build());
    public static readonly DependencyProperty ColumnsProperty = HelixProperty.Register<HelixAutoForm, int>(nameof(Columns), 2);
    public static readonly DependencyProperty FieldsProperty = HelixProperty.Register<HelixAutoForm, string?>(nameof(Fields), null, (d, _) => ((HelixAutoForm)d).Build());
    public static readonly DependencyProperty SubmitTextProperty = HelixProperty.Register<HelixAutoForm, string>(nameof(SubmitText), "");
    public static readonly DependencyProperty CancelTextProperty = HelixProperty.Register<HelixAutoForm, string>(nameof(CancelText), "");
    public static readonly DependencyProperty ShowActionsProperty = HelixProperty.Register<HelixAutoForm, bool>(nameof(ShowActions), true);
    public static readonly DependencyProperty ShowCancelProperty = HelixProperty.Register<HelixAutoForm, bool>(nameof(ShowCancel), false);

    public static readonly RoutedEvent SubmittedEvent = EventManager.RegisterRoutedEvent(nameof(Submitted), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixAutoForm));
    public static readonly RoutedEvent CancelledEvent = EventManager.RegisterRoutedEvent(nameof(Cancelled), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixAutoForm));

    /// <summary>The object edited in place.</summary>
    public object? Model { get => GetValue(ModelProperty); set => SetValue(ModelProperty, value); }

    /// <summary>Columns of the form (2).</summary>
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>Only these properties, comma-separated, in this order (null: every editable property, by [Display(Order)]).</summary>
    public string? Fields { get => (string?)GetValue(FieldsProperty); set => SetValue(FieldsProperty, value); }
    public string SubmitText { get => (string)GetValue(SubmitTextProperty); set => SetValue(SubmitTextProperty, value); }
    public string CancelText { get => (string)GetValue(CancelTextProperty); set => SetValue(CancelTextProperty, value); }

    /// <summary>The submit (and cancel) buttons (true). Without them, call Validate() yourself.</summary>
    public bool ShowActions { get => (bool)GetValue(ShowActionsProperty); set => SetValue(ShowActionsProperty, value); }
    public bool ShowCancel { get => (bool)GetValue(ShowCancelProperty); set => SetValue(ShowCancelProperty, value); }

    /// <summary>Raised when the submit button is pressed and every rule passes.</summary>
    public event RoutedEventHandler Submitted { add => AddHandler(SubmittedEvent, value); remove => RemoveHandler(SubmittedEvent, value); }
    public event RoutedEventHandler Cancelled { add => AddHandler(CancelledEvent, value); remove => RemoveHandler(CancelledEvent, value); }

    private sealed record Entry(PropertyInfo Property, string Label, FrameworkElement Field, Action<string?> ShowError);

    private readonly List<Entry> _entries = [];
    private Panel? _panel;
    private ButtonBase? _submit, _cancel;

    public override void OnApplyTemplate()
    {
        if (_submit is not null) _submit.Click -= OnSubmit;
        if (_cancel is not null) _cancel.Click -= OnCancel;
        base.OnApplyTemplate();
        _panel = GetTemplateChild("PART_Fields") as Panel;
        _submit = GetTemplateChild("PART_Submit") as ButtonBase;
        _cancel = GetTemplateChild("PART_Cancel") as ButtonBase;
        if (_submit is not null) _submit.Click += OnSubmit;
        if (_cancel is not null) _cancel.Click += OnCancel;
        Build();
    }

    private void OnSubmit(object sender, RoutedEventArgs e)
    {
        if (Validate()) RaiseEvent(new RoutedEventArgs(SubmittedEvent, this));
    }

    private void OnCancel(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(CancelledEvent, this));

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
            if (Create(model, property) is { } entry)
            {
                var span = Math.Clamp(property.GetCustomAttribute<HelixFieldAttribute>()?.Span ?? 1, 1, Math.Max(1, Columns));
                HelixAutoFormPanel.SetSpan(entry.Field, span);
                _panel.Children.Add(entry.Field);
                _entries.Add(entry);
            }
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
        var binding = new Binding(property.Name) { Source = model, Mode = readOnly ? BindingMode.OneWay : BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
        Entry? entry = null;
        void Changed() { if (entry is not null) ValidateEntry(entry); }

        if (type == typeof(bool))
        {
            var box = new HelixSwitch { Content = label, IsEnabled = !readOnly, Margin = new Thickness(0, 26, 0, 0) };
            BindingOperations.SetBinding(box, ToggleButton.IsCheckedProperty, binding);
            var field = new HelixField { Hint = hint, Content = box };
            box.Click += (_, _) => Changed();
            return entry = new Entry(property, label, field, error => field.Error = error);
        }

        if (IsNumber(type))
        {
            var range = property.GetCustomAttribute<RangeAttribute>();
            var number = new HelixNumberField
            {
                Label = label, Hint = hint, IsRequired = required, IsReadOnly = readOnly, Unit = hints?.Unit, Placeholder = hints?.Placeholder,
                Step = hints?.Step ?? 1,
                Decimals = type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ? 0 : hints?.Decimals is >= 0 ? hints.Decimals : null,
                Minimum = range?.Minimum is { } min ? Convert.ToDouble(min, CultureInfo.InvariantCulture) : null,
                Maximum = range?.Maximum is { } max ? Convert.ToDouble(max, CultureInfo.InvariantCulture) : null,
            };
            binding.Converter = NumberConverter.Instance;
            binding.ConverterParameter = property.PropertyType;
            BindingOperations.SetBinding(number, HelixNumberField.ValueProperty, binding);
            System.ComponentModel.DependencyPropertyDescriptor.FromProperty(HelixNumberField.ValueProperty, typeof(HelixNumberField)).AddValueChanged(number, (_, _) => Changed());
            return entry = new Entry(property, label, number, error => number.Error = error);
        }

        if (type.IsEnum)
        {
            var select = new HelixSelect { Label = label, Hint = hint, IsRequired = required, IsEnabled = !readOnly, ItemsSource = Enum.GetValues(type) };
            BindingOperations.SetBinding(select, Selector.SelectedItemProperty, binding);
            select.SelectionChanged += (_, _) => Changed();
            return entry = new Entry(property, label, select, error => select.Error = error);
        }

        if (type == typeof(DateTime) && hints?.WithTime != false)
        {
            var picker = new HelixDateTimePicker { Label = label, Hint = hint, IsRequired = required, IsEnabled = !readOnly };
            BindingOperations.SetBinding(picker, HelixDateTimePicker.SelectedDateTimeProperty, binding);
            System.ComponentModel.DependencyPropertyDescriptor.FromProperty(HelixDateTimePicker.SelectedDateTimeProperty, typeof(HelixDateTimePicker)).AddValueChanged(picker, (_, _) => Changed());
            return entry = new Entry(property, label, picker, error => picker.Error = error);
        }

        if (type == typeof(DateTime) || type == typeof(DateOnly))
        {
            var date = new HelixDatePicker { Label = label, Hint = hint, IsRequired = required, IsEnabled = !readOnly };
            if (type == typeof(DateOnly)) binding.Converter = DateOnlyConverter.Instance;
            BindingOperations.SetBinding(date, DatePicker.SelectedDateProperty, binding);
            date.SelectedDateChanged += (_, _) => Changed();
            return entry = new Entry(property, label, date, error => date.Error = error);
        }

        if (type == typeof(string))
        {
            var text = new HelixTextField { Label = label, Hint = hint, IsRequired = required, IsReadOnly = readOnly, Placeholder = hints?.Placeholder };
            BindingOperations.SetBinding(text, TextBox.TextProperty, binding);
            text.TextChanged += (_, _) => Changed();
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
                foreach (var member in result.MemberNames)
                {
                    _entries.FirstOrDefault(e => e.Property.Name == member)?.ShowError(result.ErrorMessage);
                }
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
public class HelixAutoFormPanel : Panel
{
    public static readonly DependencyProperty SpanProperty = DependencyProperty.RegisterAttached("Span", typeof(int), typeof(HelixAutoFormPanel),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

    public static int GetSpan(DependencyObject element) => (int)element.GetValue(SpanProperty);
    public static void SetSpan(DependencyObject element, int value) => element.SetValue(SpanProperty, value);

    private int Columns => Math.Max(1, (TemplatedParent as HelixAutoForm ?? FindForm())?.Columns ?? 2);

    private HelixAutoForm? FindForm()
    {
        DependencyObject? current = this;
        while (current is not null and not HelixAutoForm) current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        return current as HelixAutoForm;
    }

    private const double GapX = 20, GapY = 16;

    private IEnumerable<List<(UIElement Child, int Span)>> Rows()
    {
        var row = new List<(UIElement, int)>();
        var used = 0;
        foreach (UIElement child in InternalChildren)
        {
            var span = Math.Clamp(GetSpan(child), 1, Columns);
            if (used + span > Columns && row.Count > 0)
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

    private double Cell(double width) => Math.Max(0, (width - GapX * (Columns - 1)) / Columns);

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
