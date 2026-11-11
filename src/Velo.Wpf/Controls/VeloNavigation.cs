using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Velo.Wpf;

/// <summary>Tabs: Variant (Underline, Pills, Segmented) and Accent. Use <see cref="VeloTab"/> items.</summary>
public class VeloTabs : TabControl
{
    static VeloTabs() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloTabs), new FrameworkPropertyMetadata(typeof(VeloTabs)));

    public VeloTabs() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloTabs));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloTabs));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloTabs));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloTabs));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloTabs));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloTabs));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty VariantProperty = VeloProperty.Register<VeloTabs, VeloTabsVariant>(nameof(Variant), VeloTabsVariant.Underline);

    public VeloTabsVariant Variant { get => (VeloTabsVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    protected override DependencyObject GetContainerForItemOverride() => new VeloTab();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is TabItem;
}

/// <summary>A tab with an optional icon.</summary>
public class VeloTab : TabItem
{
    static VeloTab() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloTab), new FrameworkPropertyMetadata(typeof(VeloTab)));

    public static readonly DependencyProperty IconProperty = VeloProperty.Register<VeloTab, string?>(nameof(Icon), null);

    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
}

/// <summary>Collapsible sections. One open at a time unless <see cref="AllowMultiple"/>.</summary>
public class VeloAccordion : ItemsControl
{
    static VeloAccordion() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloAccordion), new FrameworkPropertyMetadata(typeof(VeloAccordion)));

    public VeloAccordion() => AddHandler(Expander.ExpandedEvent, new RoutedEventHandler(OnItemExpanded));

    public static readonly DependencyProperty AllowMultipleProperty = VeloProperty.Register<VeloAccordion, bool>(nameof(AllowMultiple), false);
    public static readonly DependencyProperty IsFlushProperty = VeloProperty.Register<VeloAccordion, bool>(nameof(IsFlush), false);

    public bool AllowMultiple { get => (bool)GetValue(AllowMultipleProperty); set => SetValue(AllowMultipleProperty, value); }

    /// <summary>No outer border: sections only separated by lines.</summary>
    public bool IsFlush { get => (bool)GetValue(IsFlushProperty); set => SetValue(IsFlushProperty, value); }

    protected override DependencyObject GetContainerForItemOverride() => new VeloAccordionItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is VeloAccordionItem;

    private void OnItemExpanded(object sender, RoutedEventArgs e)
    {
        if (AllowMultiple || e.OriginalSource is not VeloAccordionItem opened)
        {
            return;
        }

        foreach (var item in Items)
        {
            if (ItemContainerGenerator.ContainerFromItem(item) is VeloAccordionItem other && other != opened)
            {
                other.SetCurrentValue(Expander.IsExpandedProperty, false);
            }
        }
    }
}

/// <summary>A section of a <see cref="VeloAccordion"/>: Header (title), Subtitle, Icon, Extra (right side), content.</summary>
public class VeloAccordionItem : Expander
{
    static VeloAccordionItem() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloAccordionItem), new FrameworkPropertyMetadata(typeof(VeloAccordionItem)));

    public static readonly DependencyProperty SubtitleProperty = VeloProperty.Register<VeloAccordionItem, string?>(nameof(Subtitle), null);
    public static readonly DependencyProperty IconProperty = VeloProperty.Register<VeloAccordionItem, string?>(nameof(Icon), null);
    public static readonly DependencyProperty ExtraProperty = VeloProperty.Register<VeloAccordionItem, object?>(nameof(Extra), null);

    public string? Subtitle { get => (string?)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Content on the right of the header (a badge, a count…).</summary>
    public object? Extra { get => GetValue(ExtraProperty); set => SetValue(ExtraProperty, value); }
}

/// <summary>A path of links: the last item is the current page. Long paths collapse to "…" (MaxItems).</summary>
[TemplatePart(Name = "PART_Panel", Type = typeof(Panel))]
public class VeloBreadcrumb : Control
{
    static VeloBreadcrumb() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloBreadcrumb), new FrameworkPropertyMetadata(typeof(VeloBreadcrumb)));

    public static readonly DependencyProperty ItemsSourceProperty = VeloProperty.Register<VeloBreadcrumb, IEnumerable?>(nameof(ItemsSource), null, (d, _) => ((VeloBreadcrumb)d).Build());
    public static readonly DependencyProperty MaxItemsProperty = VeloProperty.Register<VeloBreadcrumb, int?>(nameof(MaxItems), null, (d, _) => ((VeloBreadcrumb)d).Build());
    public static readonly RoutedEvent ItemClickEvent = EventManager.RegisterRoutedEvent(nameof(ItemClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VeloBreadcrumb));

    /// <summary>VeloBreadcrumbItem objects, or plain strings.</summary>
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>Above this count, the middle items collapse to "…" (click to expand).</summary>
    public int? MaxItems { get => (int?)GetValue(MaxItemsProperty); set => SetValue(MaxItemsProperty, value); }

    /// <summary>Raised with <see cref="VeloEventArgs{T}"/> of VeloBreadcrumbItem.</summary>
    public event RoutedEventHandler ItemClick { add => AddHandler(ItemClickEvent, value); remove => RemoveHandler(ItemClickEvent, value); }

    private Panel? _panel;
    private bool _expanded;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _panel = GetTemplateChild("PART_Panel") as Panel;
        Build();
    }

    private void Build()
    {
        if (_panel is null)
        {
            return;
        }

        _panel.Children.Clear();
        var items = (ItemsSource?.Cast<object>() ?? []).Select(o => o as VeloBreadcrumbItem ?? new VeloBreadcrumbItem(o?.ToString() ?? "")).ToList();
        var collapse = !_expanded && MaxItems is { } max && max >= 2 && items.Count > max;
        for (var i = 0; i < items.Count; i++)
        {
            if (collapse && i == 1)
            {
                var more = Link("…", null, isCurrent: false);
                more.ToolTip = "…";
                more.Click += (_, _) =>
                {
                    _expanded = true;
                    Build();
                };
                _panel.Children.Add(more);
                _panel.Children.Add(Separator());
                i = items.Count - MaxItems!.Value + 1;
            }

            var item = items[i];
            var current = i == items.Count - 1;
            var link = Link(item.Text, item.Icon, current);
            if (!current)
            {
                link.Click += (_, _) => RaiseEvent(new VeloEventArgs<VeloBreadcrumbItem>(ItemClickEvent, item));
            }

            _panel.Children.Add(link);
            if (!current)
            {
                _panel.Children.Add(Separator());
            }
        }
    }

    private static VeloButton Link(string text, string? icon, bool isCurrent) => new()
    {
        Content = text,
        Icon = icon,
        Variant = VeloVariant.Ghost,
        Tone = VeloTone.Neutral,
        Size = VeloSize.Sm,
        Padding = new Thickness(8, 0, 8, 0),
        FontWeight = isCurrent ? FontWeights.SemiBold : FontWeights.Normal,
        IsHitTestVisible = !isCurrent,
        Focusable = !isCurrent,
    };

    private UIElement Separator()
    {
        var icon = new VeloIcon { Icon = "chevron-right", Size = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 2, 0) };
        icon.SetResourceReference(VeloIcon.ForegroundProperty, VeloKeys.TextSubtle);
        return icon;
    }
}

/// <summary>Segmented buttons: pick one option (SelectedItem / SelectedValue / SelectedIndex), like a ListBox.</summary>
public class VeloButtonGroup : ListBox
{
    static VeloButtonGroup() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloButtonGroup), new FrameworkPropertyMetadata(typeof(VeloButtonGroup)));

    public VeloButtonGroup() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloButtonGroup));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloButtonGroup));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloButtonGroup));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloButtonGroup));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloButtonGroup));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloButtonGroup));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty OrientationProperty = VeloProperty.Register<VeloButtonGroup, Orientation>(nameof(Orientation), Orientation.Horizontal);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloButtonGroup, CornerRadius>(nameof(CornerRadius), new CornerRadius(10));

    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
}

/// <summary>
/// Page numbers with previous / next. Give Count (pages) or TotalItems + PageSize. Page is 1-based and two-way.
/// </summary>
[TemplatePart(Name = "PART_Pages", Type = typeof(Panel))]
public class VeloPagination : Control
{
    static VeloPagination() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloPagination), new FrameworkPropertyMetadata(typeof(VeloPagination)));

    public static readonly DependencyProperty PageProperty = DependencyProperty.Register(nameof(Page), typeof(int), typeof(VeloPagination),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((VeloPagination)d).Build()));
    public static readonly DependencyProperty CountProperty = VeloProperty.Register<VeloPagination, int?>(nameof(Count), null, (d, _) => ((VeloPagination)d).Build());
    public static readonly DependencyProperty TotalItemsProperty = VeloProperty.Register<VeloPagination, int?>(nameof(TotalItems), null, (d, _) => ((VeloPagination)d).Build());
    public static readonly DependencyProperty PageSizeProperty = VeloProperty.Register<VeloPagination, int>(nameof(PageSize), 10, (d, _) => ((VeloPagination)d).Build());
    public static readonly DependencyProperty ShowInfoProperty = VeloProperty.Register<VeloPagination, bool>(nameof(ShowInfo), false, (d, _) => ((VeloPagination)d).Build());
    public static readonly DependencyProperty ShowLabelsProperty = VeloProperty.Register<VeloPagination, bool>(nameof(ShowLabels), false, (d, _) => ((VeloPagination)d).Build());
    public static readonly DependencyProperty IsCompactProperty = VeloProperty.Register<VeloPagination, bool>(nameof(IsCompact), false, (d, _) => ((VeloPagination)d).Build());

    public int Page { get => (int)GetValue(PageProperty); set => SetValue(PageProperty, value); }

    /// <summary>Number of pages (or use TotalItems + PageSize).</summary>
    public int? Count { get => (int?)GetValue(CountProperty); set => SetValue(CountProperty, value); }
    public int? TotalItems { get => (int?)GetValue(TotalItemsProperty); set => SetValue(TotalItemsProperty, value); }
    public int PageSize { get => (int)GetValue(PageSizeProperty); set => SetValue(PageSizeProperty, value); }

    /// <summary>"11–20 of 87" on the left (needs TotalItems).</summary>
    public bool ShowInfo { get => (bool)GetValue(ShowInfoProperty); set => SetValue(ShowInfoProperty, value); }

    /// <summary>"Previous" / "Next" next to the arrows.</summary>
    public bool ShowLabels { get => (bool)GetValue(ShowLabelsProperty); set => SetValue(ShowLabelsProperty, value); }

    /// <summary>"3 / 9" instead of the page numbers.</summary>
    public bool IsCompact { get => (bool)GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }

    private int PageCount => Math.Max(1, Count ?? (TotalItems is { } total ? (int)Math.Ceiling(total / (double)Math.Max(1, PageSize)) : 1));

    private Panel? _pages;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _pages = GetTemplateChild("PART_Pages") as Panel;
        Build();
    }

    private void Go(int page) => SetCurrentValue(PageProperty, Math.Clamp(page, 1, PageCount));

    private void Build()
    {
        if (_pages is null)
        {
            return;
        }

        _pages.Children.Clear();
        var count = PageCount;
        var page = Math.Clamp(Page, 1, count);
        var texts = VeloTexts.Current;

        if (ShowInfo && TotalItems is { } total)
        {
            var first = total == 0 ? 0 : (page - 1) * PageSize + 1;
            var last = Math.Min(total, page * PageSize);
            var info = new TextBlock { Text = VeloTexts.Format(texts.RangeOf, first, last, total), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0), FontSize = 13 };
            info.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.TextMuted);
            _pages.Children.Add(info);
        }

        _pages.Children.Add(Nav("chevron-left", ShowLabels ? "‹" : null, page > 1, () => Go(page - 1), isNext: false));
        if (IsCompact)
        {
            var label = new TextBlock { Text = $"{page} / {count}", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0), FontSize = 13 };
            label.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.Text);
            _pages.Children.Add(label);
        }
        else
        {
            foreach (var number in Numbers(page, count))
            {
                if (number is null)
                {
                    var gap = new TextBlock { Text = "…", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
                    gap.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.TextSubtle);
                    _pages.Children.Add(gap);
                    continue;
                }

                var target = number.Value;
                var button = new VeloButton
                {
                    Content = target.ToString(CultureInfo.CurrentCulture),
                    Size = VeloSize.Sm,
                    MinWidth = 32,
                    Padding = new Thickness(8, 0, 8, 0),
                    Margin = new Thickness(2, 0, 2, 0),
                    Variant = target == page ? VeloVariant.Soft : VeloVariant.Ghost,
                    Tone = target == page ? VeloTone.Primary : VeloTone.Neutral,
                };
                AutomationLabel(button, $"{target}");
                button.Click += (_, _) => Go(target);
                _pages.Children.Add(button);
            }
        }

        _pages.Children.Add(Nav("chevron-right", null, page < count, () => Go(page + 1), isNext: true));

        VeloButton Nav(string icon, string? _, bool enabled, Action go, bool isNext)
        {
            var button = new VeloButton
            {
                Variant = VeloVariant.Ghost,
                Tone = VeloTone.Neutral,
                Size = VeloSize.Sm,
                IsEnabled = enabled,
                Padding = new Thickness(ShowLabels ? 10 : 0, 0, ShowLabels ? 10 : 0, 0),
                MinWidth = 32,
                Margin = new Thickness(2, 0, 2, 0),
                ToolTip = isNext ? texts.Next : texts.Previous,
            };
            if (ShowLabels)
            {
                button.Content = isNext ? texts.Next : texts.Previous;
                if (isNext) button.IconEnd = icon; else button.Icon = icon;
            }
            else
            {
                button.Icon = icon;
            }

            button.Click += (_, _) => go();
            return button;
        }
    }

    private static void AutomationLabel(FrameworkElement element, string name) =>
        System.Windows.Automation.AutomationProperties.SetName(element, name);

    /// <summary>1 … 4 5 6 … 20 (null = gap).</summary>
    private static IEnumerable<int?> Numbers(int page, int count)
    {
        if (count <= 7)
        {
            return Enumerable.Range(1, count).Select(n => (int?)n);
        }

        var list = new List<int?> { 1 };
        var from = Math.Max(2, page - 1);
        var to = Math.Min(count - 1, page + 1);
        if (page <= 3) (from, to) = (2, 4);
        if (page >= count - 2) (from, to) = (count - 3, count - 1);
        if (from > 2) list.Add(null);
        for (var n = from; n <= to; n++) list.Add(n);
        if (to < count - 1) list.Add(null);
        list.Add(count);
        return list;
    }
}

/// <summary>
/// Steps of a process: done, current, upcoming or in error (icon + text, not color alone).
/// ActiveIndex is two-way; IsClickable lets the user go back (or anywhere with AllowSkip).
/// </summary>
[TemplatePart(Name = "PART_Steps", Type = typeof(Grid))]
public class VeloStepper : Control, IVeloThemeAware
{
    static VeloStepper() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloStepper), new FrameworkPropertyMetadata(typeof(VeloStepper)));

    public VeloStepper()
    {
        VeloAccent.Init(this);
        VeloTheme.Watch(this);
    }

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloStepper));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloStepper));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloStepper));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloStepper));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloStepper));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloStepper));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);

    public static readonly DependencyProperty StepsProperty = VeloProperty.Register<VeloStepper, IEnumerable?>(nameof(Steps), null, (d, _) => ((VeloStepper)d).Build());
    public static readonly DependencyProperty ActiveIndexProperty = DependencyProperty.Register(nameof(ActiveIndex), typeof(int), typeof(VeloStepper),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((VeloStepper)d).Build()));
    public static readonly DependencyProperty ErrorStepsProperty = VeloProperty.Register<VeloStepper, IEnumerable<int>?>(nameof(ErrorSteps), null, (d, _) => ((VeloStepper)d).Build());
    public static readonly DependencyProperty IsClickableProperty = VeloProperty.Register<VeloStepper, bool>(nameof(IsClickable), false, (d, _) => ((VeloStepper)d).Build());
    public static readonly DependencyProperty AllowSkipProperty = VeloProperty.Register<VeloStepper, bool>(nameof(AllowSkip), false, (d, _) => ((VeloStepper)d).Build());
    public static readonly DependencyProperty OrientationProperty = VeloProperty.Register<VeloStepper, Orientation>(nameof(Orientation), Orientation.Horizontal, (d, _) => ((VeloStepper)d).Build());

    /// <summary>VeloStep objects (Title, Description) or strings.</summary>
    public IEnumerable? Steps { get => (IEnumerable?)GetValue(StepsProperty); set => SetValue(StepsProperty, value); }
    public int ActiveIndex { get => (int)GetValue(ActiveIndexProperty); set => SetValue(ActiveIndexProperty, value); }

    /// <summary>Indexes of steps shown in error.</summary>
    public IEnumerable<int>? ErrorSteps { get => (IEnumerable<int>?)GetValue(ErrorStepsProperty); set => SetValue(ErrorStepsProperty, value); }

    /// <summary>Clicking a done step goes back to it.</summary>
    public bool IsClickable { get => (bool)GetValue(IsClickableProperty); set => SetValue(IsClickableProperty, value); }

    /// <summary>With IsClickable: any step can be clicked, also ahead.</summary>
    public bool AllowSkip { get => (bool)GetValue(AllowSkipProperty); set => SetValue(AllowSkipProperty, value); }
    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    private Grid? _grid;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _grid = GetTemplateChild("PART_Steps") as Grid;
        Build();
    }

    void IVeloThemeAware.OnThemeChanged() => Build();

    private void Build()
    {
        if (_grid is null)
        {
            return;
        }

        _grid.Children.Clear();
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();
        var steps = (Steps?.Cast<object>() ?? []).Select(o => o as VeloStep ?? new VeloStep(o?.ToString() ?? "")).ToList();
        var errors = ErrorSteps?.ToHashSet() ?? [];
        var vertical = Orientation == Orientation.Vertical;
        var accent = Accent ?? Brushes.SteelBlue;
        var onAccent = OnAccent ?? Brushes.White;
        var danger = VeloTheme.Brush(this, VeloKeys.Danger, Colors.Crimson);

        for (var i = 0; i < steps.Count; i++)
        {
            if (vertical) _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            else _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var state = errors.Contains(i) ? "error" : i < ActiveIndex ? "done" : i == ActiveIndex ? "current" : "todo";
            var circle = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(1.5),
                VerticalAlignment = VerticalAlignment.Top,
            };
            UIElement mark;
            switch (state)
            {
                case "done":
                    circle.Background = accent;
                    circle.BorderBrush = accent;
                    mark = new VeloIcon { Icon = "check", Size = 14, StrokeWidth = 3, Foreground = onAccent };
                    break;
                case "error":
                    circle.Background = danger;
                    circle.BorderBrush = danger;
                    mark = new VeloIcon { Icon = "x", Size = 14, StrokeWidth = 3, Foreground = Brushes.White };
                    break;
                default:
                    circle.SetResourceReference(Border.BackgroundProperty, VeloKeys.Surface);
                    if (state == "current") circle.BorderBrush = accent;
                    else circle.SetResourceReference(Border.BorderBrushProperty, VeloKeys.BorderStrong);
                    var number = new TextBlock { Text = (i + 1).ToString(CultureInfo.CurrentCulture), FontSize = 13, FontWeight = FontWeights.SemiBold };
                    if (state == "current") number.Foreground = accent;
                    else number.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.TextMuted);
                    mark = number;
                    break;
            }

            if (mark is FrameworkElement fe)
            {
                fe.HorizontalAlignment = HorizontalAlignment.Center;
                fe.VerticalAlignment = VerticalAlignment.Center;
            }

            circle.Child = mark;

            var title = new TextBlock { Text = steps[i].Title, FontWeight = state == "current" ? FontWeights.SemiBold : FontWeights.Medium, TextWrapping = TextWrapping.Wrap };
            title.SetResourceReference(TextBlock.ForegroundProperty, state == "todo" ? VeloKeys.TextMuted : VeloKeys.Text);
            var text = new StackPanel { Margin = vertical ? new Thickness(12, 4, 0, 18) : new Thickness(0, 8, 8, 0) };
            text.Children.Add(title);
            var description = state == "error" ? VeloTexts.Current.StepError : steps[i].Description;
            if (!string.IsNullOrEmpty(description))
            {
                var d = new TextBlock { Text = description, FontSize = 12.5, Margin = new Thickness(0, 2, 0, 0), TextWrapping = TextWrapping.Wrap };
                d.SetResourceReference(TextBlock.ForegroundProperty, state == "error" ? VeloKeys.Danger : VeloKeys.TextMuted);
                text.Children.Add(d);
            }

            // Connector to the next step (accent when done).
            var cell = new Grid();
            if (i < steps.Count - 1)
            {
                var line = new Border { Background = i < ActiveIndex ? accent : null };
                if (i >= ActiveIndex) line.SetResourceReference(Border.BackgroundProperty, VeloKeys.Border);
                if (vertical)
                {
                    line.Width = 2;
                    line.HorizontalAlignment = HorizontalAlignment.Left;
                    line.Margin = new Thickness(13, 32, 0, 4);
                }
                else
                {
                    line.Height = 2;
                    line.VerticalAlignment = VerticalAlignment.Top;
                    line.Margin = new Thickness(36, 13, 8, 0);
                }

                cell.Children.Add(line);
            }

            var row = new DockPanel { Background = Brushes.Transparent };
            if (vertical)
            {
                DockPanel.SetDock(circle, Dock.Left);
                row.Children.Add(circle);
                row.Children.Add(text);
            }
            else
            {
                var stack = new StackPanel();
                circle.HorizontalAlignment = HorizontalAlignment.Left;
                stack.Children.Add(circle);
                stack.Children.Add(text);
                row.Children.Add(stack);
            }

            var index = i;
            var clickable = IsClickable && i != ActiveIndex && (AllowSkip || i < ActiveIndex);
            if (clickable)
            {
                row.Cursor = Cursors.Hand;
                row.MouseLeftButtonUp += (_, _) => SetCurrentValue(ActiveIndexProperty, index);
            }

            cell.Children.Add(row);
            if (vertical) Grid.SetRow(cell, i); else Grid.SetColumn(cell, i);
            _grid.Children.Add(cell);
        }
    }
}

/// <summary>A round picture, or initials on a color picked from the name; optional status dot.</summary>
public class VeloAvatar : Control
{
    static VeloAvatar() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloAvatar), new FrameworkPropertyMetadata(typeof(VeloAvatar)));

    public VeloAvatar() => UpdateLook();

    public static readonly DependencyProperty DisplayNameProperty = VeloProperty.Register<VeloAvatar, string?>(nameof(DisplayName), null, (d, _) => ((VeloAvatar)d).UpdateLook());
    public static readonly DependencyProperty SourceProperty = VeloProperty.Register<VeloAvatar, ImageSource?>(nameof(Source), null);
    public static readonly DependencyProperty SizeProperty = VeloProperty.Register<VeloAvatar, VeloSize>(nameof(Size), VeloSize.Md);
    public static readonly DependencyProperty StatusProperty = VeloProperty.Register<VeloAvatar, VeloAvatarStatus>(nameof(Status), VeloAvatarStatus.None);
    public static readonly DependencyProperty AccentProperty = VeloProperty.Register<VeloAvatar, Brush?>(nameof(Accent), null);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloAvatar, CornerRadius>(nameof(CornerRadius), new CornerRadius(999));

    private static readonly DependencyPropertyKey InitialsKey = DependencyProperty.RegisterReadOnly(nameof(Initials), typeof(string), typeof(VeloAvatar), new PropertyMetadata(""));
    public static readonly DependencyProperty InitialsProperty = InitialsKey.DependencyProperty;

    /// <summary>Full name: gives the initials, the color and the tooltip.</summary>
    public string? DisplayName { get => (string?)GetValue(DisplayNameProperty); set => SetValue(DisplayNameProperty, value); }
    public ImageSource? Source { get => (ImageSource?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public VeloSize Size { get => (VeloSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public VeloAvatarStatus Status { get => (VeloAvatarStatus)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    /// <summary>Background of the initials (a series color picked from the name by default).</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>999 for a circle, e.g. 8 for a rounded square.</summary>
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public string Initials => (string)GetValue(InitialsProperty);

    private void UpdateLook()
    {
        var name = DisplayName?.Trim() ?? "";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = parts.Length switch
        {
            0 => "?",
            1 => parts[0][..Math.Min(2, parts[0].Length)],
            _ => $"{parts[0][0]}{parts[^1][0]}",
        };
        SetValue(InitialsKey, initials.ToUpperInvariant());
        ToolTip = string.IsNullOrEmpty(name) ? null : name;

        if (ReadLocalValue(AccentProperty) == DependencyProperty.UnsetValue || _autoAccent)
        {
            // Stable color per name: the same person always gets the same color.
            var hash = name.Aggregate(17, (h, c) => unchecked(h * 31 + c));
            _autoAccent = true;
            SetResourceReference(AccentProperty, VeloKeys.Series(Math.Abs(hash % 8) + 1));
        }
    }

    private bool _autoAccent;

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == AccentProperty && ReadLocalValue(AccentProperty) is Brush)
        {
            _autoAccent = false; // the user chose a color
        }
    }
}

/// <summary>Overlapping avatars; above Max, a "+N" bubble.</summary>
public class VeloAvatarGroup : ItemsControl
{
    static VeloAvatarGroup() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloAvatarGroup), new FrameworkPropertyMetadata(typeof(VeloAvatarGroup)));

    public static readonly DependencyProperty MaxProperty = VeloProperty.Register<VeloAvatarGroup, int?>(nameof(Max), null, (d, _) => ((VeloAvatarGroup)d).UpdateOverflow());

    private static readonly DependencyPropertyKey OverflowTextKey = DependencyProperty.RegisterReadOnly(nameof(OverflowText), typeof(string), typeof(VeloAvatarGroup), new PropertyMetadata(null));
    public static readonly DependencyProperty OverflowTextProperty = OverflowTextKey.DependencyProperty;

    public int? Max { get => (int?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>"+3" when more avatars than Max.</summary>
    public string? OverflowText => (string?)GetValue(OverflowTextProperty);

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        UpdateOverflow();
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        UpdateOverflow();
    }

    private void UpdateOverflow()
    {
        var count = Items.Count;
        var max = Max ?? int.MaxValue;
        for (var i = 0; i < count; i++)
        {
            if (ItemContainerGenerator.ContainerFromIndex(i) is UIElement container)
            {
                container.Visibility = i < max ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        SetValue(OverflowTextKey, count > max ? $"+{count - max}" : null);
    }
}

/// <summary>A loading placeholder that pulses: Lines of text, a circle, or any block (Width, Height, CornerRadius).</summary>
public class VeloSkeleton : Control
{
    static VeloSkeleton() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloSkeleton), new FrameworkPropertyMetadata(typeof(VeloSkeleton)));

    public static readonly DependencyProperty LinesProperty = VeloProperty.Register<VeloSkeleton, int>(nameof(Lines), 1);
    public static readonly DependencyProperty IsCircleProperty = VeloProperty.Register<VeloSkeleton, bool>(nameof(IsCircle), false);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloSkeleton, CornerRadius>(nameof(CornerRadius), new CornerRadius(6));

    private static readonly DependencyPropertyKey LineItemsKey = DependencyProperty.RegisterReadOnly(nameof(LineItems), typeof(IReadOnlyList<VeloSkeletonLine>), typeof(VeloSkeleton), new PropertyMetadata(Array.Empty<VeloSkeletonLine>()));
    public static readonly DependencyProperty LineItemsProperty = LineItemsKey.DependencyProperty;
    private static readonly DependencyPropertyKey IsMultilineKey = DependencyProperty.RegisterReadOnly(nameof(IsMultiline), typeof(bool), typeof(VeloSkeleton), new PropertyMetadata(false));
    public static readonly DependencyProperty IsMultilineProperty = IsMultilineKey.DependencyProperty;

    /// <summary>Number of text lines (the last one is shorter).</summary>
    public int Lines { get => (int)GetValue(LinesProperty); set => SetValue(LinesProperty, value); }
    public bool IsCircle { get => (bool)GetValue(IsCircleProperty); set => SetValue(IsCircleProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>The text lines (when Lines &gt; 1).</summary>
    public IReadOnlyList<VeloSkeletonLine> LineItems => (IReadOnlyList<VeloSkeletonLine>)GetValue(LineItemsProperty);
    public bool IsMultiline => (bool)GetValue(IsMultilineProperty);

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == LinesProperty)
        {
            var count = Math.Max(1, Lines);
            SetValue(IsMultilineKey, count > 1);
            SetValue(LineItemsKey, Enumerable.Range(0, count)
                .Select(i => i == count - 1 ? new VeloSkeletonLine(new GridLength(3, GridUnitType.Star), new GridLength(2, GridUnitType.Star))
                                            : new VeloSkeletonLine(new GridLength(1, GridUnitType.Star), new GridLength(0)))
                .ToArray());
        }
    }
}

/// <summary>One line of a <see cref="VeloSkeleton"/>: filled part and empty rest.</summary>
public sealed record VeloSkeletonLine(GridLength Filled, GridLength Empty);

/// <summary>A grid splitter with a visible grip; Accent on hover and drag. Place it like a GridSplitter.</summary>
public class VeloSplitter : GridSplitter
{
    static VeloSplitter() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloSplitter), new FrameworkPropertyMetadata(typeof(VeloSplitter)));

    public VeloSplitter() => VeloAccent.Init(this);

    public static readonly DependencyProperty ToneProperty = VeloAccent.Tone(typeof(VeloSplitter));
    public static readonly DependencyProperty AccentProperty = VeloAccent.Accent(typeof(VeloSplitter));
    public static readonly DependencyProperty OnAccentProperty = VeloAccent.OnAccent(typeof(VeloSplitter));
    public static readonly DependencyProperty ToneBrushProperty = VeloAccent.ToneBrush(typeof(VeloSplitter));
    public static readonly DependencyProperty OnToneBrushProperty = VeloAccent.OnToneBrush(typeof(VeloSplitter));
    public static readonly DependencyProperty AutoOnAccentProperty = VeloAccent.AutoOnAccent(typeof(VeloSplitter));

    /// <summary>Theme color: Primary, Neutral, Success, Warning, Danger, Info.</summary>
    public VeloTone Tone { get => (VeloTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Any brush; replaces the tone (e.g. Accent="#FF8A00").</summary>
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    /// <summary>Text color on a solid button; automatic (black or white) for a custom Accent.</summary>
    public Brush? OnAccent { get => (Brush?)GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public Brush? ToneBrush => (Brush?)GetValue(ToneBrushProperty);
    public Brush? AutoOnAccent => (Brush?)GetValue(AutoOnAccentProperty);
}

/// <summary>A panel sliding from a side of the window (needs a <see cref="VeloHost"/>): Position, DrawerSize, Title, content, Footer.</summary>
public class VeloDrawer : VeloDialog
{
    public static readonly DependencyProperty PositionProperty = VeloProperty.Register<VeloDrawer, VeloDrawerPosition>(nameof(Position), VeloDrawerPosition.Right);
    public static readonly DependencyProperty DrawerSizeProperty = VeloProperty.Register<VeloDrawer, double>(nameof(DrawerSize), 420d);

    public VeloDrawerPosition Position { get => (VeloDrawerPosition)GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    /// <summary>Width (left/right) or height (bottom).</summary>
    public double DrawerSize { get => (double)GetValue(DrawerSizeProperty); set => SetValue(DrawerSizeProperty, value); }

    internal override object CreateSlot() => new VeloDrawerSlot(this);
}

/// <summary>An open drawer in the host's overlay layer.</summary>
public sealed class VeloDrawerSlot(VeloDrawer drawer)
{
    public VeloDrawer Dialog { get; } = drawer;

    public HorizontalAlignment HorizontalAlignment => Dialog.Position switch
    {
        VeloDrawerPosition.Left => HorizontalAlignment.Left,
        VeloDrawerPosition.Bottom => HorizontalAlignment.Stretch,
        _ => HorizontalAlignment.Right,
    };
    public VerticalAlignment VerticalAlignment => Dialog.Position == VeloDrawerPosition.Bottom ? VerticalAlignment.Bottom : VerticalAlignment.Stretch;
    public double Width => Dialog.Position == VeloDrawerPosition.Bottom ? double.NaN : Dialog.DrawerSize;
    public double Height => Dialog.Position == VeloDrawerPosition.Bottom ? Dialog.DrawerSize : double.NaN;
    public CornerRadius CornerRadius => Dialog.Position switch
    {
        VeloDrawerPosition.Left => new CornerRadius(0, 16, 16, 0),
        VeloDrawerPosition.Bottom => new CornerRadius(16, 16, 0, 0),
        _ => new CornerRadius(16, 0, 0, 16),
    };
}

/// <summary>A context menu with the Velo look. Fill it with <see cref="VeloMenuItem"/> and <see cref="VeloMenuSeparator"/>.</summary>
public class VeloContextMenu : ContextMenu
{
    static VeloContextMenu() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloContextMenu), new FrameworkPropertyMetadata(typeof(VeloContextMenu)));

    protected override DependencyObject GetContainerForItemOverride() => new VeloMenuItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MenuItem or Separator;
}

/// <summary>A menu entry: IconName, Description (second line), Shortcut, IsDanger.</summary>
public class VeloMenuItem : MenuItem
{
    static VeloMenuItem() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloMenuItem), new FrameworkPropertyMetadata(typeof(VeloMenuItem)));

    public static readonly DependencyProperty IconNameProperty = VeloProperty.Register<VeloMenuItem, string?>(nameof(IconName), null);
    public static readonly DependencyProperty DescriptionProperty = VeloProperty.Register<VeloMenuItem, string?>(nameof(Description), null);
    public static readonly DependencyProperty IsDangerProperty = VeloProperty.Register<VeloMenuItem, bool>(nameof(IsDanger), false);

    /// <summary>Built-in icon on the left (MenuItem.Icon still works for your own content).</summary>
    public string? IconName { get => (string?)GetValue(IconNameProperty); set => SetValue(IconNameProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Red text and icon, for destructive actions.</summary>
    public bool IsDanger { get => (bool)GetValue(IsDangerProperty); set => SetValue(IsDangerProperty, value); }

    /// <summary>Text on the right, e.g. "Ctrl+S" (same as InputGestureText).</summary>
    public string? Shortcut { get => InputGestureText; set => InputGestureText = value ?? ""; }

    protected override DependencyObject GetContainerForItemOverride() => new VeloMenuItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MenuItem or Separator;
}

public class VeloMenuSeparator : Separator
{
    static VeloMenuSeparator() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloMenuSeparator), new FrameworkPropertyMetadata(typeof(VeloMenuSeparator)));
}

/// <summary>A small heading that groups menu items ("Compte", "Danger zone"…); not selectable.</summary>
public class VeloMenuLabel : Separator
{
    static VeloMenuLabel() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloMenuLabel), new FrameworkPropertyMetadata(typeof(VeloMenuLabel)));

    public static readonly DependencyProperty TextProperty = VeloProperty.Register<VeloMenuLabel, string?>(nameof(Text), null);

    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }
}

/// <summary>A button that opens its <see cref="Menu"/> below it.</summary>
public class VeloMenuButton : VeloButton
{
    public static readonly DependencyProperty MenuProperty = VeloProperty.Register<VeloMenuButton, ContextMenu?>(nameof(Menu), null);
    public static readonly DependencyProperty MenuAlignmentProperty = VeloProperty.Register<VeloMenuButton, HorizontalAlignment>(nameof(MenuAlignment), HorizontalAlignment.Left);

    public ContextMenu? Menu { get => (ContextMenu?)GetValue(MenuProperty); set => SetValue(MenuProperty, value); }

    /// <summary>Left: menu aligned on the button's left edge; Right: on its right edge.</summary>
    public HorizontalAlignment MenuAlignment { get => (HorizontalAlignment)GetValue(MenuAlignmentProperty); set => SetValue(MenuAlignmentProperty, value); }

    protected override void OnClick()
    {
        base.OnClick();
        if (Menu is not { } menu)
        {
            return;
        }

        menu.PlacementTarget = this;
        menu.Placement = PlacementMode.Bottom;
        menu.DataContext ??= DataContext;
        if (MenuAlignment == HorizontalAlignment.Right)
        {
            menu.Placement = PlacementMode.Custom;
            menu.CustomPopupPlacementCallback = (popup, target, _) =>
                [new CustomPopupPlacement(new Point(target.Width - popup.Width, target.Height + 4), PopupPrimaryAxis.Horizontal)];
        }
        else
        {
            menu.VerticalOffset = 4;
        }

        menu.IsOpen = true;
    }
}
