using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Helix.Avalonia;

/// <summary>
/// A narrow column with a chevron that opens the row's RowDetailsTemplate.
/// With it, row details no longer open on selection: only with the chevron.
/// </summary>
public class HelixDetailsColumn : DataGridColumn
{
    public HelixDetailsColumn()
    {
        Width = new DataGridLength(40);
        CanUserSort = false;
        CanUserResize = false;
        CanUserReorder = false;
        IsReadOnly = true;
    }

    protected override Control GenerateElement(DataGridCell cell, object dataItem) => new HelixDetailsToggle();

    protected override Control GenerateEditingElement(DataGridCell cell, object dataItem, out BindingExpressionBase? binding)
    {
        binding = null;
        return GenerateElement(cell, dataItem);
    }

    protected override object? PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs) => null;
}

/// <summary>The chevron of <see cref="HelixDetailsColumn"/>: shows or hides the details of its row.</summary>
public class HelixDetailsToggle : ToggleButton
{
    protected override Type StyleKeyOverride => typeof(HelixDetailsToggle);

    private DataGridRow? Row => this.FindAncestorOfType<DataGridRow>();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // Rows are recycled: follow the row this cell now belongs to.
        if (Row is { } row) SetCurrentValue(IsCheckedProperty, row.AreDetailsVisible);
        UpdateTip();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != IsCheckedProperty) return;
        if (Row is { } row) row.AreDetailsVisible = IsChecked == true;
        UpdateTip();
    }

    private void UpdateTip()
    {
        var text = IsChecked == true ? HelixTexts.Current.HideDetails : HelixTexts.Current.ShowDetails;
        ToolTip.SetTip(this, text);
        global::Avalonia.Automation.AutomationProperties.SetName(this, text);
    }
}

/// <summary>A read-only column that shows its <see cref="Binding"/> with a Helix control (base of the badge and signed columns).</summary>
public abstract class HelixValueColumn : DataGridColumn
{
    private BindingBase? _binding;

    protected HelixValueColumn() => IsReadOnly = true;

    /// <summary>The value shown; its path is also the sort key unless SortMemberPath is set.</summary>
    [AssignBinding]
    public BindingBase? Binding
    {
        get => _binding;
        set
        {
            _binding = value;
            if (value is Binding { Path: { Length: > 0 } path } && string.IsNullOrEmpty(SortMemberPath)) SortMemberPath = path;
        }
    }
}

/// <summary>Shows the bound value as a badge. ToneBinding (HelixTone or its name, e.g. "Success") or AccentBinding (a brush) gives its color.</summary>
public class HelixBadgeColumn : HelixValueColumn
{
    /// <summary>Binding to the badge tone: a HelixTone, or its name ("Success", "Warning"…).</summary>
    [AssignBinding]
    public BindingBase? ToneBinding { get; set; }

    /// <summary>Binding to any brush; replaces the tone.</summary>
    [AssignBinding]
    public BindingBase? AccentBinding { get; set; }

    /// <summary>A colored dot before the text (true).</summary>
    public bool ShowDot { get; set; } = true;

    public HelixBadgeVariant Variant { get; set; } = HelixBadgeVariant.Soft;

    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        var badge = new HelixBadge { ShowDot = ShowDot, Variant = Variant, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0) };
        if (Binding is not null)
        {
            badge.Bind(ContentControl.ContentProperty, Binding);
            badge.PropertyChanged += (_, e) =>
            {
                if (e.Property == ContentControl.ContentProperty) badge.IsVisible = e.NewValue is not (null or "");
            };
        }

        if (ToneBinding is not null) badge.Bind(HelixBadge.ToneProperty, ToneBinding);
        if (AccentBinding is not null) badge.Bind(HelixBadge.AccentProperty, AccentBinding);
        return badge;
    }

    protected override Control GenerateEditingElement(DataGridCell cell, object dataItem, out BindingExpressionBase? binding)
    {
        binding = null;
        return GenerateElement(cell, dataItem);
    }

    protected override object? PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs) => null;
}

/// <summary>A number with its sign, an arrow and a color: up and green when positive, down and red when negative (HigherIsBetter="False" swaps the colors).</summary>
public class HelixSignedColumn : HelixValueColumn
{
    /// <summary>.NET format of the value, e.g. "N1" or "0.0'%'".</summary>
    public string? Format { get; set; }

    /// <summary>Unit after the value, e.g. "MW".</summary>
    public string? Unit { get; set; }

    /// <summary>False when a decrease is the good news (costs, delays…).</summary>
    public bool HigherIsBetter { get; set; } = true;

    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        var value = new HelixSignedValue { Format = Format, Unit = Unit, HigherIsBetter = HigherIsBetter, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(12, 0) };
        if (Binding is not null) value.Bind(HelixSignedValue.ValueProperty, Binding);
        return value;
    }

    protected override Control GenerateEditingElement(DataGridCell cell, object dataItem, out BindingExpressionBase? binding)
    {
        binding = null;
        return GenerateElement(cell, dataItem);
    }

    protected override object? PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs) => null;
}

/// <summary>A signed change: "+2.4 %" with an up arrow in green, "-1.1 %" with a down arrow in red, "0" in gray.</summary>
[PseudoClasses(":good", ":bad", ":up", ":down")]
public class HelixSignedValue : TemplatedControl
{
    public static readonly StyledProperty<object?> ValueProperty = AvaloniaProperty.Register<HelixSignedValue, object?>(nameof(Value));
    public static readonly StyledProperty<string?> FormatProperty = AvaloniaProperty.Register<HelixSignedValue, string?>(nameof(Format));
    public static readonly StyledProperty<string?> UnitProperty = AvaloniaProperty.Register<HelixSignedValue, string?>(nameof(Unit));
    public static readonly StyledProperty<bool> HigherIsBetterProperty = AvaloniaProperty.Register<HelixSignedValue, bool>(nameof(HigherIsBetter), true);
    public static readonly DirectProperty<HelixSignedValue, string> TextProperty = AvaloniaProperty.RegisterDirect<HelixSignedValue, string>(nameof(Text), v => v.Text);
    public static readonly DirectProperty<HelixSignedValue, int> DirectionProperty = AvaloniaProperty.RegisterDirect<HelixSignedValue, int>(nameof(Direction), v => v.Direction);
    public static readonly DirectProperty<HelixSignedValue, string> StateProperty = AvaloniaProperty.RegisterDirect<HelixSignedValue, string>(nameof(State), v => v.State);

    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string? Format { get => GetValue(FormatProperty); set => SetValue(FormatProperty, value); }
    public string? Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public bool HigherIsBetter { get => GetValue(HigherIsBetterProperty); set => SetValue(HigherIsBetterProperty, value); }

    private string _text = "", _state = "flat";
    private int _direction;

    /// <summary>The value as displayed, with its sign and unit.</summary>
    public string Text { get => _text; private set => SetAndRaise(TextProperty, ref _text, value); }

    /// <summary>1 positive, -1 negative, 0 zero or empty.</summary>
    public int Direction { get => _direction; private set => SetAndRaise(DirectionProperty, ref _direction, value); }

    /// <summary>"good", "bad" or "flat": drives the color (also :good, :bad, :up, :down).</summary>
    public string State { get => _state; private set => SetAndRaise(StateProperty, ref _state, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == FormatProperty || change.Property == UnitProperty || change.Property == HigherIsBetterProperty) Update();
    }

    private void Update()
    {
        var number = HelixDataGrid.Number(Value);
        var culture = CultureInfo.CurrentCulture;
        string text;
        if (Value is null) text = "";
        else if (number is { } n && !string.IsNullOrEmpty(Format))
        {
            try { text = n.ToString(Format, culture); }
            catch (FormatException) { text = n.ToString(culture); }
        }
        else text = Convert.ToString(Value, culture) ?? "";

        if (number > 0 && !text.StartsWith('+')) text = "+" + text;
        if (text.Length > 0 && !string.IsNullOrEmpty(Unit)) text += " " + Unit;

        Direction = number is null or 0 ? 0 : Math.Sign(number.Value);
        Text = text;
        State = Direction == 0 ? "flat" : Direction > 0 == HigherIsBetter ? "good" : "bad";
        PseudoClasses.Set(":good", State == "good");
        PseudoClasses.Set(":bad", State == "bad");
        PseudoClasses.Set(":up", Direction > 0);
        PseudoClasses.Set(":down", Direction < 0);
    }
}

/// <summary>Label, hint and error around any control (a field look for your own inputs).</summary>
[PseudoClasses(":error")]
public class HelixField : ContentControl
{
    public static readonly StyledProperty<string?> LabelProperty = AvaloniaProperty.Register<HelixField, string?>(nameof(Label));
    public static readonly StyledProperty<string?> HintProperty = AvaloniaProperty.Register<HelixField, string?>(nameof(Hint));
    public static readonly StyledProperty<string?> ErrorProperty = AvaloniaProperty.Register<HelixField, string?>(nameof(Error));
    public static readonly StyledProperty<bool> IsRequiredProperty = AvaloniaProperty.Register<HelixField, bool>(nameof(IsRequired));
    public static readonly DirectProperty<HelixField, string?> MessageProperty = AvaloniaProperty.RegisterDirect<HelixField, string?>(nameof(Message), f => f.Message);
    public static readonly DirectProperty<HelixField, bool> HasErrorProperty = AvaloniaProperty.RegisterDirect<HelixField, bool>(nameof(HasError), f => f.HasError);

    public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Help text under the control.</summary>
    public string? Hint { get => GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Error message; replaces the hint and turns it red.</summary>
    public string? Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }
    public bool IsRequired { get => GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }

    private string? _message;
    private bool _hasError;

    /// <summary>The error, else the hint.</summary>
    public string? Message { get => _message; private set => SetAndRaise(MessageProperty, ref _message, value); }
    public bool HasError { get => _hasError; private set => SetAndRaise(HasErrorProperty, ref _hasError, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != HintProperty && change.Property != ErrorProperty) return;
        HasError = !string.IsNullOrEmpty(Error);
        Message = HasError ? Error : string.IsNullOrEmpty(Hint) ? null : Hint;
        PseudoClasses.Set(":error", HasError);
    }
}
