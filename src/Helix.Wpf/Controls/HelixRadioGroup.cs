using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Helix.Wpf;

/// <summary>How a <see cref="HelixRadioGroup"/> draws its options.</summary>
public enum HelixRadioAppearance { Default, Cards, Segmented }

/// <summary>An option with a title, a description and an icon (for cards).</summary>
public sealed class HelixRadioOption
{
    public HelixRadioOption() { }

    public HelixRadioOption(string title, string? description = null, string? icon = null, object? value = null) =>
        (Title, Description, Icon, Value) = (title, description, icon, value);

    public string Title { get; set; } = "";
    public string? Description { get; set; }

    /// <summary>Built-in icon name, shown on cards.</summary>
    public string? Icon { get; set; }

    /// <summary>Your value, given back by SelectedValue (SelectedValuePath="Value").</summary>
    public object? Value { get; set; }
    public bool IsEnabled { get; set; } = true;
    public override string ToString() => Title;
}

/// <summary>
/// Pick one option: radio list (Default), selectable cards (Cards) or segmented buttons (Segmented).
/// Works like a ListBox: Items or ItemsSource, SelectedItem / SelectedValue / SelectedIndex, arrows to move.
/// Options can be <see cref="HelixRadioOption"/> (title, description, icon) or any object
/// (DisplayMemberPath, DescriptionPath, IconPath, or your ItemTemplate).
/// </summary>
public class HelixRadioGroup : ListBox
{
    static HelixRadioGroup() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixRadioGroup), new FrameworkPropertyMetadata(typeof(HelixRadioGroup)));

    public HelixRadioGroup()
    {
        HelixAccent.Init(this);
        SelectionMode = SelectionMode.Single;
    }

    public static readonly DependencyProperty ToneProperty = HelixAccent.Tone(typeof(HelixRadioGroup));
    public static readonly DependencyProperty AccentProperty = HelixAccent.Accent(typeof(HelixRadioGroup));
    public static readonly DependencyProperty OnAccentProperty = HelixAccent.OnAccent(typeof(HelixRadioGroup));
    public static readonly DependencyProperty ToneBrushProperty = HelixAccent.ToneBrush(typeof(HelixRadioGroup));
    public static readonly DependencyProperty OnToneBrushProperty = HelixAccent.OnToneBrush(typeof(HelixRadioGroup));
    public static readonly DependencyProperty AutoOnAccentProperty = HelixAccent.AutoOnAccent(typeof(HelixRadioGroup));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public HelixTone Tone { get => (HelixTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty AppearanceProperty = HelixProperty.Register<HelixRadioGroup, HelixRadioAppearance>(nameof(Appearance), HelixRadioAppearance.Default);
    public static readonly DependencyProperty LabelProperty = HelixProperty.Register<HelixRadioGroup, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = HelixProperty.Register<HelixRadioGroup, string?>(nameof(Hint), null);
    public static readonly DependencyProperty OrientationProperty = HelixProperty.Register<HelixRadioGroup, Orientation>(nameof(Orientation), Orientation.Vertical);
    public static readonly DependencyProperty ColumnsProperty = HelixProperty.Register<HelixRadioGroup, int>(nameof(Columns), 3);
    public static readonly DependencyProperty DescriptionPathProperty = HelixProperty.Register<HelixRadioGroup, string?>(nameof(DescriptionPath), null);
    public static readonly DependencyProperty IconPathProperty = HelixProperty.Register<HelixRadioGroup, string?>(nameof(IconPath), null);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixRadioGroup, CornerRadius>(nameof(CornerRadius), new CornerRadius(12));

    /// <summary>Default (radio list), Cards (selectable cards) or Segmented.</summary>
    public HelixRadioAppearance Appearance { get => (HelixRadioAppearance)GetValue(AppearanceProperty); set => SetValue(AppearanceProperty, value); }
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Direction of the Default list.</summary>
    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Cards per row (Cards appearance).</summary>
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>Property of your items with the second line (when not using HelixRadioOption).</summary>
    public string? DescriptionPath { get => (string?)GetValue(DescriptionPathProperty); set => SetValue(DescriptionPathProperty, value); }

    /// <summary>Property of your items with a built-in icon name.</summary>
    public string? IconPath { get => (string?)GetValue(IconPathProperty); set => SetValue(IconPathProperty, value); }

    /// <summary>Corner radius of the cards.</summary>
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    protected override DependencyObject GetContainerForItemOverride() => new HelixRadioItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is HelixRadioItem;

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is not HelixRadioItem container || item is HelixRadioItem)
        {
            return;
        }

        if (item is HelixRadioOption option)
        {
            container.Content = option.Title;
            container.Description = option.Description;
            container.Icon = option.Icon;
            container.IsEnabled = option.IsEnabled;
        }
        else
        {
            if (DescriptionPath is { } d) container.Description = HelixDataGrid.Value(item, d)?.ToString();
            if (IconPath is { } i) container.Icon = HelixDataGrid.Value(item, i)?.ToString();
        }
    }
}

/// <summary>One option of a <see cref="HelixRadioGroup"/> (also usable directly in XAML).</summary>
public class HelixRadioItem : ListBoxItem
{
    static HelixRadioItem() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixRadioItem), new FrameworkPropertyMetadata(typeof(HelixRadioItem)));

    public static readonly DependencyProperty DescriptionProperty = HelixProperty.Register<HelixRadioItem, string?>(nameof(Description), null);
    public static readonly DependencyProperty IconProperty = HelixProperty.Register<HelixRadioItem, string?>(nameof(Icon), null);

    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
}
