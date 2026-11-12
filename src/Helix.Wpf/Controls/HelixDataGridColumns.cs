using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Helix.Wpf;

/// <summary>
/// A narrow column with a chevron that opens the row's <see cref="DataGrid.RowDetailsTemplate"/>.
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

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem) => new HelixDetailsToggle();

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem) => GenerateElement(cell, dataItem);
}

/// <summary>The chevron of <see cref="HelixDetailsColumn"/>: shows or hides the details of its row.</summary>
public class HelixDetailsToggle : ToggleButton
{
    static HelixDetailsToggle() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixDetailsToggle), new FrameworkPropertyMetadata(typeof(HelixDetailsToggle)));

    public HelixDetailsToggle()
    {
        Loaded += (_, _) =>
        {
            if (Row is { } row) SetCurrentValue(IsCheckedProperty, row.DetailsVisibility == Visibility.Visible);
            UpdateTip();
        };
    }

    private DataGridRow? Row => DataGridRow.GetRowContainingElement(this);

    protected override void OnChecked(RoutedEventArgs e)
    {
        base.OnChecked(e);
        if (Row is { } row) row.DetailsVisibility = Visibility.Visible;
        UpdateTip();
    }

    protected override void OnUnchecked(RoutedEventArgs e)
    {
        base.OnUnchecked(e);
        if (Row is { } row) row.DetailsVisibility = Visibility.Collapsed;
        UpdateTip();
    }

    private void UpdateTip()
    {
        var text = IsChecked == true ? HelixTexts.Current.HideDetails : HelixTexts.Current.ShowDetails;
        ToolTip = text;
        System.Windows.Automation.AutomationProperties.SetName(this, text);
    }
}

/// <summary>Shows the bound value as a badge. ToneBinding (HelixTone or its name, e.g. "Success") or AccentBinding (a brush or color) gives its color.</summary>
public class HelixBadgeColumn : DataGridBoundColumn
{
    /// <summary>Binding to the badge tone: a HelixTone, or its name ("Success", "Warning"…).</summary>
    public BindingBase? ToneBinding { get; set; }

    /// <summary>Binding to any color (brush or "#RRGGBB"); replaces the tone.</summary>
    public BindingBase? AccentBinding { get; set; }

    /// <summary>A colored dot before the text (true).</summary>
    public bool ShowDot { get; set; } = true;

    public HelixBadgeVariant Variant { get; set; } = HelixBadgeVariant.Soft;

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
    {
        var badge = new HelixBadge { ShowDot = ShowDot, Variant = Variant, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
        if (Binding is not null)
        {
            BindingOperations.SetBinding(badge, ContentControl.ContentProperty, Binding);
            BindingOperations.SetBinding(badge, UIElement.VisibilityProperty, new Binding { Path = new PropertyPath(ContentControl.ContentProperty), Source = badge, Converter = EmptyToCollapsed.Instance });
        }

        if (ToneBinding is not null) BindingOperations.SetBinding(badge, HelixBadge.ToneProperty, ToneBinding);
        if (AccentBinding is not null) BindingOperations.SetBinding(badge, HelixBadge.AccentProperty, AccentBinding);
        return badge;
    }

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem) => GenerateElement(cell, dataItem);

    private sealed class EmptyToCollapsed : IValueConverter
    {
        public static readonly EmptyToCollapsed Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is null || value is string { Length: 0 } ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}

/// <summary>A number with its sign, an arrow and a color: up and green when positive, down and red when negative (HigherIsBetter="False" swaps the colors).</summary>
public class HelixSignedColumn : DataGridBoundColumn
{
    /// <summary>.NET format of the value, e.g. "N1" or "0.0'%'".</summary>
    public string? Format { get; set; }

    /// <summary>Unit after the value, e.g. "MW".</summary>
    public string? Unit { get; set; }

    /// <summary>False when a decrease is the good news (costs, delays…).</summary>
    public bool HigherIsBetter { get; set; } = true;

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
    {
        var value = new HelixSignedValue { Format = Format, Unit = Unit, HigherIsBetter = HigherIsBetter, HorizontalAlignment = HorizontalAlignment.Right };
        if (Binding is not null) BindingOperations.SetBinding(value, HelixSignedValue.ValueProperty, Binding);
        return value;
    }

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem) => GenerateElement(cell, dataItem);
}

/// <summary>A signed change: "+2.4 %" with an up arrow in green, "-1.1 %" with a down arrow in red, "0" in gray.</summary>
public class HelixSignedValue : Control
{
    static HelixSignedValue() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixSignedValue), new FrameworkPropertyMetadata(typeof(HelixSignedValue)));

    public static readonly DependencyProperty ValueProperty = HelixProperty.Register<HelixSignedValue, object?>(nameof(Value), null, (d, _) => ((HelixSignedValue)d).Update());
    public static readonly DependencyProperty FormatProperty = HelixProperty.Register<HelixSignedValue, string?>(nameof(Format), null, (d, _) => ((HelixSignedValue)d).Update());
    public static readonly DependencyProperty UnitProperty = HelixProperty.Register<HelixSignedValue, string?>(nameof(Unit), null, (d, _) => ((HelixSignedValue)d).Update());
    public static readonly DependencyProperty HigherIsBetterProperty = HelixProperty.Register<HelixSignedValue, bool>(nameof(HigherIsBetter), true, (d, _) => ((HelixSignedValue)d).Update());

    private static readonly DependencyPropertyKey TextKey = DependencyProperty.RegisterReadOnly(nameof(Text), typeof(string), typeof(HelixSignedValue), new PropertyMetadata(""));
    private static readonly DependencyPropertyKey DirectionKey = DependencyProperty.RegisterReadOnly(nameof(Direction), typeof(int), typeof(HelixSignedValue), new PropertyMetadata(0));
    private static readonly DependencyPropertyKey StateKey = DependencyProperty.RegisterReadOnly(nameof(State), typeof(string), typeof(HelixSignedValue), new PropertyMetadata("flat"));
    public static readonly DependencyProperty TextProperty = TextKey.DependencyProperty;
    public static readonly DependencyProperty DirectionProperty = DirectionKey.DependencyProperty;
    public static readonly DependencyProperty StateProperty = StateKey.DependencyProperty;

    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string? Format { get => (string?)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }
    public string? Unit { get => (string?)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public bool HigherIsBetter { get => (bool)GetValue(HigherIsBetterProperty); set => SetValue(HigherIsBetterProperty, value); }

    /// <summary>The value as displayed, with its sign and unit.</summary>
    public string Text => (string)GetValue(TextProperty);

    /// <summary>1 positive, -1 negative, 0 zero or empty.</summary>
    public int Direction => (int)GetValue(DirectionProperty);

    /// <summary>"good", "bad" or "flat": drives the color.</summary>
    public string State => (string)GetValue(StateProperty);

    private void Update()
    {
        var number = HelixDataGrid.Number(Value);
        var culture = CultureInfo.CurrentCulture;
        string text;
        if (Value is null)
        {
            text = "";
        }
        else if (number is { } n && !string.IsNullOrEmpty(Format))
        {
            try { text = n.ToString(Format, culture); }
            catch (FormatException) { text = n.ToString(culture); }
        }
        else
        {
            text = Convert.ToString(Value, culture) ?? "";
        }

        if (number > 0 && !text.StartsWith('+')) text = "+" + text;
        if (text.Length > 0 && !string.IsNullOrEmpty(Unit)) text += " " + Unit;

        var direction = number is null or 0 ? 0 : Math.Sign(number.Value);
        SetValue(TextKey, text);
        SetValue(DirectionKey, direction);
        SetValue(StateKey, direction == 0 ? "flat" : direction > 0 == HigherIsBetter ? "good" : "bad");
    }
}
