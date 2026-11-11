using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Velo.Wpf;

/// <summary>How a <see cref="VeloRadioGroup"/> draws its options.</summary>
public enum VeloRadioAppearance { Default, Cards, Segmented }

/// <summary>An option with a title, a description and an icon (for cards).</summary>
public sealed class VeloRadioOption
{
    public VeloRadioOption() { }

    public VeloRadioOption(string title, string? description = null, string? icon = null, object? value = null) =>
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
/// Options can be <see cref="VeloRadioOption"/> (title, description, icon) or any object
/// (DisplayMemberPath, DescriptionPath, IconPath, or your ItemTemplate).
/// </summary>
public class VeloRadioGroup : ListBox
{
    static VeloRadioGroup() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloRadioGroup), new FrameworkPropertyMetadata(typeof(VeloRadioGroup)));

    public VeloRadioGroup()
    {
        VeloAccent.Init(this);
        SelectionMode = SelectionMode.Single;
    }

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloRadioGroup));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloRadioGroup));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloRadioGroup));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloRadioGroup));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloRadioGroup));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloRadioGroup));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty AppearanceProperty = VeloProperty.Register<VeloRadioGroup, VeloRadioAppearance>(nameof(Appearance), VeloRadioAppearance.Default);
    public static readonly DependencyProperty LabelProperty = VeloProperty.Register<VeloRadioGroup, string?>(nameof(Label), null);
    public static readonly DependencyProperty HintProperty = VeloProperty.Register<VeloRadioGroup, string?>(nameof(Hint), null);
    public static readonly DependencyProperty OrientationProperty = VeloProperty.Register<VeloRadioGroup, Orientation>(nameof(Orientation), Orientation.Vertical);
    public static readonly DependencyProperty ColumnsProperty = VeloProperty.Register<VeloRadioGroup, int>(nameof(Columns), 3);
    public static readonly DependencyProperty DescriptionPathProperty = VeloProperty.Register<VeloRadioGroup, string?>(nameof(DescriptionPath), null);
    public static readonly DependencyProperty IconPathProperty = VeloProperty.Register<VeloRadioGroup, string?>(nameof(IconPath), null);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloRadioGroup, CornerRadius>(nameof(CornerRadius), new CornerRadius(12));

    /// <summary>Default (radio list), Cards (selectable cards) or Segmented.</summary>
    public VeloRadioAppearance Appearance { get => (VeloRadioAppearance)GetValue(AppearanceProperty); set => SetValue(AppearanceProperty, value); }
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Direction of the Default list.</summary>
    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Cards per row (Cards appearance).</summary>
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>Property of your items with the second line (when not using VeloRadioOption).</summary>
    public string? DescriptionPath { get => (string?)GetValue(DescriptionPathProperty); set => SetValue(DescriptionPathProperty, value); }

    /// <summary>Property of your items with a built-in icon name.</summary>
    public string? IconPath { get => (string?)GetValue(IconPathProperty); set => SetValue(IconPathProperty, value); }

    /// <summary>Corner radius of the cards.</summary>
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    protected override DependencyObject GetContainerForItemOverride() => new VeloRadioItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is VeloRadioItem;

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is not VeloRadioItem container || item is VeloRadioItem)
        {
            return;
        }

        if (item is VeloRadioOption option)
        {
            container.Content = option.Title;
            container.Description = option.Description;
            container.Icon = option.Icon;
            container.IsEnabled = option.IsEnabled;
        }
        else
        {
            if (DescriptionPath is { } d) container.Description = VeloDataGrid.Value(item, d)?.ToString();
            if (IconPath is { } i) container.Icon = VeloDataGrid.Value(item, i)?.ToString();
        }
    }
}

/// <summary>One option of a <see cref="VeloRadioGroup"/> (also usable directly in XAML).</summary>
public class VeloRadioItem : ListBoxItem
{
    static VeloRadioItem() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloRadioItem), new FrameworkPropertyMetadata(typeof(VeloRadioItem)));

    public static readonly DependencyProperty DescriptionProperty = VeloProperty.Register<VeloRadioItem, string?>(nameof(Description), null);
    public static readonly DependencyProperty IconProperty = VeloProperty.Register<VeloRadioItem, string?>(nameof(Icon), null);

    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
}
