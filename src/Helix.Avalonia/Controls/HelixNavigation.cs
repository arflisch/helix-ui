using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Helix.Avalonia;

public enum HelixTabsVariant { Underline, Pills, Segmented }

public enum HelixAvatarStatus { None, Online, Away, Busy, Offline }

public sealed class HelixBreadcrumbItem
{
    public HelixBreadcrumbItem() { }

    public HelixBreadcrumbItem(string text, string? icon = null) => (Text, Icon) = (text, icon);

    public string Text { get; set; } = "";
    public string? Icon { get; set; }

    /// <summary>Your own value (a route, a folder…), given back by ItemClick.</summary>
    public object? Tag { get; set; }
}

public sealed class HelixStep
{
    public HelixStep() { }

    public HelixStep(string title, string? description = null) => (Title, Description) = (title, description);

    public string Title { get; set; } = "";
    public string? Description { get; set; }
}

/// <summary>Tabs: Underline, Pills or Segmented. Items are <see cref="HelixTab"/> (Header, Icon, content).</summary>
public class HelixTabs : TabControl
{
    /// <summary>Inherited, so each tab styles itself for the variant.</summary>
    public static readonly StyledProperty<HelixTabsVariant> VariantProperty =
        AvaloniaProperty.Register<HelixTabs, HelixTabsVariant>(nameof(Variant), inherits: true);

    protected override Type StyleKeyOverride => typeof(HelixTabs);

    public HelixTabsVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not HelixTab;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new HelixTab();
}

/// <summary>A tab with an optional icon.</summary>
public class HelixTab : TabItem
{
    public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<HelixTab, string?>(nameof(Icon));
    public static readonly StyledProperty<HelixTabsVariant> VariantProperty = HelixTabs.VariantProperty.AddOwner<HelixTab>();

    protected override Type StyleKeyOverride => typeof(HelixTab);

    public HelixTabsVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    public string? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
}

/// <summary>Collapsible sections. One open at a time unless <see cref="AllowMultiple"/>.</summary>
public class HelixAccordion : ItemsControl
{
    public static readonly StyledProperty<bool> AllowMultipleProperty = AvaloniaProperty.Register<HelixAccordion, bool>(nameof(AllowMultiple));
    public static readonly StyledProperty<bool> IsFlushProperty = AvaloniaProperty.Register<HelixAccordion, bool>(nameof(IsFlush));

    protected override Type StyleKeyOverride => typeof(HelixAccordion);

    public bool AllowMultiple { get => GetValue(AllowMultipleProperty); set => SetValue(AllowMultipleProperty, value); }

    /// <summary>No outer frame: sections separated by lines only.</summary>
    public bool IsFlush { get => GetValue(IsFlushProperty); set => SetValue(IsFlushProperty, value); }

    public HelixAccordion() => AddHandler(Expander.ExpandedEvent, OnExpanded, RoutingStrategies.Bubble);

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not HelixAccordionItem;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new HelixAccordionItem();

    private void OnExpanded(object? sender, RoutedEventArgs e)
    {
        if (AllowMultiple || e.Source is not HelixAccordionItem opened) return;
        foreach (var item in GetRealizedContainers().OfType<HelixAccordionItem>())
        {
            if (item != opened && item.IsExpanded) item.IsExpanded = false;
        }
    }
}

/// <summary>A section of a <see cref="HelixAccordion"/>: Header (title), Subtitle, Icon, Extra (right side), content.</summary>
public class HelixAccordionItem : Expander
{
    public static readonly StyledProperty<string?> SubtitleProperty = AvaloniaProperty.Register<HelixAccordionItem, string?>(nameof(Subtitle));
    public static readonly StyledProperty<string?> IconProperty = AvaloniaProperty.Register<HelixAccordionItem, string?>(nameof(Icon));
    public static readonly StyledProperty<object?> ExtraProperty = AvaloniaProperty.Register<HelixAccordionItem, object?>(nameof(Extra));

    protected override Type StyleKeyOverride => typeof(HelixAccordionItem);

    public string? Subtitle { get => GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Content on the right of the header (a badge, a count…).</summary>
    public object? Extra { get => GetValue(ExtraProperty); set => SetValue(ExtraProperty, value); }
}

/// <summary>A path of links: the last item is the current page. Long paths collapse to "…" (MaxItems).</summary>
[TemplatePart("PART_Panel", typeof(Panel))]
public class HelixBreadcrumb : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty = AvaloniaProperty.Register<HelixBreadcrumb, IEnumerable?>(nameof(ItemsSource));
    public static readonly StyledProperty<int?> MaxItemsProperty = AvaloniaProperty.Register<HelixBreadcrumb, int?>(nameof(MaxItems));
    public static readonly RoutedEvent<HelixEventArgs<HelixBreadcrumbItem>> ItemClickEvent =
        RoutedEvent.Register<HelixBreadcrumb, HelixEventArgs<HelixBreadcrumbItem>>(nameof(ItemClick), RoutingStrategies.Bubble);

    private Panel? _panel;
    private bool _expanded;

    /// <summary>HelixBreadcrumbItem or strings.</summary>
    public IEnumerable? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public int? MaxItems { get => GetValue(MaxItemsProperty); set => SetValue(MaxItemsProperty, value); }
    public event EventHandler<HelixEventArgs<HelixBreadcrumbItem>>? ItemClick { add => AddHandler(ItemClickEvent, value); remove => RemoveHandler(ItemClickEvent, value); }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _panel = e.NameScope.Find<Panel>("PART_Panel");
        Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty || change.Property == MaxItemsProperty) Build();
    }

    private void Build()
    {
        if (_panel is null) return;
        _panel.Children.Clear();
        var items = (ItemsSource?.Cast<object>() ?? []).Select(o => o as HelixBreadcrumbItem ?? new HelixBreadcrumbItem(o?.ToString() ?? "")).ToList();
        var collapse = !_expanded && MaxItems is { } max && max >= 2 && items.Count > max;
        for (var i = 0; i < items.Count; i++)
        {
            if (collapse && i == 1)
            {
                var more = Link("…", null, isCurrent: false);
                more.Click += (_, _) => { _expanded = true; Build(); };
                _panel.Children.Add(more);
                _panel.Children.Add(Separator());
                i = items.Count - MaxItems!.Value + 1;
            }

            var item = items[i];
            var current = i == items.Count - 1;
            var link = Link(item.Text, item.Icon, current);
            if (!current) link.Click += (_, _) => RaiseEvent(new HelixEventArgs<HelixBreadcrumbItem>(ItemClickEvent, item));
            _panel.Children.Add(link);
            if (!current) _panel.Children.Add(Separator());
        }
    }

    private static HelixButton Link(string text, string? icon, bool isCurrent) => new()
    {
        Content = text, Icon = icon, Variant = HelixVariant.Ghost, Tone = HelixTone.Neutral, Size = HelixSize.Sm, Padding = new Thickness(8, 0),
        FontWeight = isCurrent ? FontWeight.SemiBold : FontWeight.Normal, IsHitTestVisible = !isCurrent, Focusable = !isCurrent,
    };

    private HelixIcon Separator()
    {
        var icon = new HelixIcon { Icon = "chevron-right", Size = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0) };
        icon.Bind(HelixIcon.ForegroundProperty, icon.GetResourceObservable(HelixKeys.TextSubtle));
        return icon;
    }
}

/// <summary>Joined buttons: pick one option (SelectedItem / SelectedIndex), like a ListBox.</summary>
public class HelixButtonGroup : ListBox
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixButtonGroup>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixButtonGroup>();

    protected override Type StyleKeyOverride => typeof(HelixButtonGroup);

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
}

/// <summary>Page numbers with previous / next, "…" gaps, an optional "1–10 of 87" and a compact "3 / 9" form.</summary>
[TemplatePart("PART_Pages", typeof(Panel))]
public class HelixPagination : TemplatedControl
{
    public static readonly StyledProperty<int> PageProperty = AvaloniaProperty.Register<HelixPagination, int>(nameof(Page), 1, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<int?> CountProperty = AvaloniaProperty.Register<HelixPagination, int?>(nameof(Count));
    public static readonly StyledProperty<int?> TotalItemsProperty = AvaloniaProperty.Register<HelixPagination, int?>(nameof(TotalItems));
    public static readonly StyledProperty<int> PageSizeProperty = AvaloniaProperty.Register<HelixPagination, int>(nameof(PageSize), 10);
    public static readonly StyledProperty<bool> ShowInfoProperty = AvaloniaProperty.Register<HelixPagination, bool>(nameof(ShowInfo));
    public static readonly StyledProperty<bool> ShowLabelsProperty = AvaloniaProperty.Register<HelixPagination, bool>(nameof(ShowLabels));
    public static readonly StyledProperty<bool> IsCompactProperty = AvaloniaProperty.Register<HelixPagination, bool>(nameof(IsCompact));

    private Panel? _pages;

    /// <summary>Current page, 1-based, two-way.</summary>
    public int Page { get => GetValue(PageProperty); set => SetValue(PageProperty, value); }

    /// <summary>Number of pages (or set TotalItems and PageSize).</summary>
    public int? Count { get => GetValue(CountProperty); set => SetValue(CountProperty, value); }
    public int? TotalItems { get => GetValue(TotalItemsProperty); set => SetValue(TotalItemsProperty, value); }
    public int PageSize { get => GetValue(PageSizeProperty); set => SetValue(PageSizeProperty, value); }

    /// <summary>"11–20 of 87" before the numbers (with TotalItems).</summary>
    public bool ShowInfo { get => GetValue(ShowInfoProperty); set => SetValue(ShowInfoProperty, value); }

    /// <summary>"Previous" / "Next" words on the arrows.</summary>
    public bool ShowLabels { get => GetValue(ShowLabelsProperty); set => SetValue(ShowLabelsProperty, value); }

    /// <summary>"3 / 9" instead of the numbers.</summary>
    public bool IsCompact { get => GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }

    public int PageCount => Math.Max(1, Count ?? (TotalItems is { } total ? (int)Math.Ceiling(total / (double)Math.Max(1, PageSize)) : 1));

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _pages = e.NameScope.Find<Panel>("PART_Pages");
        Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property.OwnerType == typeof(HelixPagination)) Build();
    }

    /// <summary>Goes to a page (clamped).</summary>
    public void Go(int page) => SetCurrentValue(PageProperty, Math.Clamp(page, 1, PageCount));

    private TextBlock Text(string text, string key)
    {
        var block = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, FontSize = 13 };
        block.Bind(TextBlock.ForegroundProperty, block.GetResourceObservable(key));
        return block;
    }

    private void Build()
    {
        if (_pages is null) return;
        _pages.Children.Clear();
        var count = PageCount;
        var page = Math.Clamp(Page, 1, count);
        var texts = HelixTexts.Current;
        if (ShowInfo && TotalItems is { } total)
        {
            var first = total == 0 ? 0 : (page - 1) * PageSize + 1;
            var info = Text(HelixTexts.Format(texts.RangeOf, first, Math.Min(total, page * PageSize), total), HelixKeys.TextMuted);
            info.Margin = new Thickness(0, 0, 16, 0);
            _pages.Children.Add(info);
        }

        _pages.Children.Add(Nav("chevron-left", page > 1, () => Go(page - 1), isNext: false));
        if (IsCompact)
        {
            var label = Text($"{page} / {count}", HelixKeys.Text);
            label.Margin = new Thickness(10, 0);
            _pages.Children.Add(label);
        }
        else
        {
            foreach (var number in Numbers(page, count))
            {
                if (number is null)
                {
                    var gap = Text("…", HelixKeys.TextSubtle);
                    gap.Margin = new Thickness(6, 0);
                    _pages.Children.Add(gap);
                    continue;
                }

                var target = number.Value;
                var button = new HelixButton
                {
                    Content = target.ToString(CultureInfo.CurrentCulture), Size = HelixSize.Sm, MinWidth = 32, Padding = new Thickness(8, 0), Margin = new Thickness(2, 0),
                    Variant = target == page ? HelixVariant.Soft : HelixVariant.Ghost, Tone = target == page ? HelixTone.Primary : HelixTone.Neutral,
                };
                global::Avalonia.Automation.AutomationProperties.SetName(button, target.ToString(CultureInfo.CurrentCulture));
                button.Click += (_, _) => Go(target);
                _pages.Children.Add(button);
            }
        }

        _pages.Children.Add(Nav("chevron-right", page < count, () => Go(page + 1), isNext: true));

        HelixButton Nav(string icon, bool enabled, Action go, bool isNext)
        {
            var button = new HelixButton
            {
                Variant = HelixVariant.Ghost, Tone = HelixTone.Neutral, Size = HelixSize.Sm, IsEnabled = enabled, MinWidth = 32, Margin = new Thickness(2, 0),
                Padding = new Thickness(ShowLabels ? 10 : 0, 0),
            };
            ToolTip.SetTip(button, isNext ? texts.Next : texts.Previous);
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

    /// <summary>1 … 4 5 6 … 20 (null = gap).</summary>
    public static IEnumerable<int?> Numbers(int page, int count)
    {
        if (count <= 7) return Enumerable.Range(1, count).Select(n => (int?)n);
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
/// The steps of a flow: done (check), current, to do, in error. Horizontal or vertical;
/// IsClickable lets the user go back (and forward with AllowSkip).
/// </summary>
[TemplatePart("PART_Steps", typeof(Grid))]
public class HelixStepper : TemplatedControl
{
    public static readonly StyledProperty<HelixTone> ToneProperty = HelixAccent.ToneProperty.AddOwner<HelixStepper>();
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixStepper>();
    public static readonly StyledProperty<IBrush?> OnAccentProperty = HelixAccent.OnAccentProperty.AddOwner<HelixStepper>();
    public static readonly StyledProperty<IBrush?> ToneOnBrushProperty = HelixAccent.ToneOnBrushProperty.AddOwner<HelixStepper>();
    public static readonly StyledProperty<IBrush?> ResolvedOnAccentProperty = HelixAccent.ResolvedOnAccentProperty.AddOwner<HelixStepper>();
    public static readonly StyledProperty<IEnumerable?> StepsProperty = AvaloniaProperty.Register<HelixStepper, IEnumerable?>(nameof(Steps));
    public static readonly StyledProperty<int> ActiveIndexProperty = AvaloniaProperty.Register<HelixStepper, int>(nameof(ActiveIndex), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<IEnumerable<int>?> ErrorStepsProperty = AvaloniaProperty.Register<HelixStepper, IEnumerable<int>?>(nameof(ErrorSteps));
    public static readonly StyledProperty<bool> IsClickableProperty = AvaloniaProperty.Register<HelixStepper, bool>(nameof(IsClickable));
    public static readonly StyledProperty<bool> AllowSkipProperty = AvaloniaProperty.Register<HelixStepper, bool>(nameof(AllowSkip));
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<HelixStepper, Orientation>(nameof(Orientation), Orientation.Horizontal);

    private Grid? _grid;
    private bool _customAccent;

    public HelixTone Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public IBrush? OnAccent { get => GetValue(OnAccentProperty); set => SetValue(OnAccentProperty, value); }
    public IBrush? ToneOnBrush { get => GetValue(ToneOnBrushProperty); set => SetValue(ToneOnBrushProperty, value); }

    /// <summary>HelixStep (Title, Description) or strings.</summary>
    public IEnumerable? Steps { get => GetValue(StepsProperty); set => SetValue(StepsProperty, value); }

    /// <summary>The current step (0-based), two-way.</summary>
    public int ActiveIndex { get => GetValue(ActiveIndexProperty); set => SetValue(ActiveIndexProperty, value); }

    /// <summary>Steps shown in error (red, with a message).</summary>
    public IEnumerable<int>? ErrorSteps { get => GetValue(ErrorStepsProperty); set => SetValue(ErrorStepsProperty, value); }

    /// <summary>Done steps can be clicked to go back.</summary>
    public bool IsClickable { get => GetValue(IsClickableProperty); set => SetValue(IsClickableProperty, value); }

    /// <summary>With IsClickable, later steps can be clicked too.</summary>
    public bool AllowSkip { get => GetValue(AllowSkipProperty); set => SetValue(AllowSkipProperty, value); }
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    public HelixStepper() => ActualThemeVariantChanged += (_, _) => Build();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _grid = e.NameScope.Find<Grid>("PART_Steps");
        Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        HelixAccent.OnChanged(this, change, ref _customAccent);
        if (change.Property.OwnerType == typeof(HelixStepper) || change.Property == AccentProperty || change.Property == ResolvedOnAccentProperty) Build();
    }

    private IBrush Res(string key) => HelixDraw.Brush(this, key, Colors.Gray);

    private void Build()
    {
        if (_grid is null) return;
        _grid.Children.Clear();
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();
        var steps = (Steps?.Cast<object>() ?? []).Select(o => o as HelixStep ?? new HelixStep(o?.ToString() ?? "")).ToList();
        var errors = ErrorSteps?.ToHashSet() ?? [];
        var vertical = Orientation == Orientation.Vertical;
        var accent = Accent ?? Res(HelixKeys.Primary);
        var onAccent = GetValue(ResolvedOnAccentProperty) ?? Brushes.White;
        var danger = Res(HelixKeys.Danger);

        for (var i = 0; i < steps.Count; i++)
        {
            if (vertical) _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            else _grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

            var state = errors.Contains(i) ? "error" : i < ActiveIndex ? "done" : i == ActiveIndex ? "current" : "todo";
            var circle = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(14), BorderThickness = new Thickness(1.5), VerticalAlignment = VerticalAlignment.Top };
            Control mark;
            switch (state)
            {
                case "done":
                    circle.Background = accent;
                    circle.BorderBrush = accent;
                    mark = new HelixIcon { Icon = "check", Size = 14, StrokeWidth = 3, Foreground = onAccent };
                    break;
                case "error":
                    circle.Background = danger;
                    circle.BorderBrush = danger;
                    mark = new HelixIcon { Icon = "x", Size = 14, StrokeWidth = 3, Foreground = Brushes.White };
                    break;
                default:
                    circle.Background = Res(HelixKeys.Surface);
                    circle.BorderBrush = state == "current" ? accent : Res(HelixKeys.BorderStrong);
                    mark = new TextBlock { Text = (i + 1).ToString(CultureInfo.CurrentCulture), FontSize = 13, FontWeight = FontWeight.SemiBold, Foreground = state == "current" ? accent : Res(HelixKeys.TextMuted) };
                    break;
            }

            mark.HorizontalAlignment = HorizontalAlignment.Center;
            mark.VerticalAlignment = VerticalAlignment.Center;
            circle.Child = mark;

            var title = new TextBlock { Text = steps[i].Title, FontWeight = state == "current" ? FontWeight.SemiBold : FontWeight.Medium, TextWrapping = TextWrapping.Wrap, Foreground = Res(state == "todo" ? HelixKeys.TextMuted : HelixKeys.Text) };
            var text = new StackPanel { Margin = vertical ? new Thickness(12, 4, 0, 18) : new Thickness(0, 8, 8, 0), Children = { title } };
            var description = state == "error" ? HelixTexts.Current.StepError : steps[i].Description;
            if (!string.IsNullOrEmpty(description))
            {
                text.Children.Add(new TextBlock { Text = description, FontSize = 12.5, Margin = new Thickness(0, 2, 0, 0), TextWrapping = TextWrapping.Wrap, Foreground = Res(state == "error" ? HelixKeys.Danger : HelixKeys.TextMuted) });
            }

            // Connector to the next step (accent when done).
            var cell = new Grid();
            if (i < steps.Count - 1)
            {
                var line = new Border { Background = i < ActiveIndex ? accent : Res(HelixKeys.Border) };
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
                circle.HorizontalAlignment = HorizontalAlignment.Left;
                row.Children.Add(new StackPanel { Children = { circle, text } });
            }

            var index = i;
            if (IsClickable && i != ActiveIndex && (AllowSkip || i < ActiveIndex))
            {
                row.Cursor = new Cursor(StandardCursorType.Hand);
                row.PointerReleased += (_, _) => SetCurrentValue(ActiveIndexProperty, index);
            }

            cell.Children.Add(row);
            if (vertical) Grid.SetRow(cell, i); else Grid.SetColumn(cell, i);
            _grid.Children.Add(cell);
        }
    }
}

/// <summary>A round picture, or initials on a color picked from the name; optional status dot.</summary>
public class HelixAvatar : TemplatedControl
{
    public static readonly StyledProperty<IBrush?> AccentProperty = HelixAccent.AccentProperty.AddOwner<HelixAvatar>();
    public static readonly StyledProperty<string?> DisplayNameProperty = AvaloniaProperty.Register<HelixAvatar, string?>(nameof(DisplayName));
    public static readonly StyledProperty<IImage?> SourceProperty = AvaloniaProperty.Register<HelixAvatar, IImage?>(nameof(Source));
    public static readonly StyledProperty<HelixSize> SizeProperty = AvaloniaProperty.Register<HelixAvatar, HelixSize>(nameof(Size), HelixSize.Md);
    public static readonly StyledProperty<HelixAvatarStatus> StatusProperty = AvaloniaProperty.Register<HelixAvatar, HelixAvatarStatus>(nameof(Status));
    public static readonly DirectProperty<HelixAvatar, string> InitialsProperty = AvaloniaProperty.RegisterDirect<HelixAvatar, string>(nameof(Initials), a => a.Initials);
    public static readonly DirectProperty<HelixAvatar, IBrush?> FillProperty = AvaloniaProperty.RegisterDirect<HelixAvatar, IBrush?>(nameof(Fill), a => a.Fill);

    private string _initials = "?";
    private IBrush? _fill;

    /// <summary>Background of the initials; picked from the name when not set (the same person always gets the same color).</summary>
    public IBrush? Accent { get => GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public string? DisplayName { get => GetValue(DisplayNameProperty); set => SetValue(DisplayNameProperty, value); }
    public IImage? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public HelixSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public HelixAvatarStatus Status { get => GetValue(StatusProperty); set => SetValue(StatusProperty, value); }
    public string Initials { get => _initials; private set => SetAndRaise(InitialsProperty, ref _initials, value); }
    public IBrush? Fill { get => _fill; private set => SetAndRaise(FillProperty, ref _fill, value); }

    public HelixAvatar() => ActualThemeVariantChanged += (_, _) => UpdateLook();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DisplayNameProperty || change.Property == AccentProperty) UpdateLook();
        if (change.Property == StatusProperty)
        {
            foreach (var s in Enum.GetNames<HelixAvatarStatus>()) PseudoClasses.Set(":" + s.ToLowerInvariant(), Status.ToString() == s);
        }
    }

    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateLook();
    }

    private void UpdateLook()
    {
        var name = DisplayName?.Trim() ?? "";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Initials = (parts.Length switch { 0 => "?", 1 => parts[0][..Math.Min(2, parts[0].Length)], _ => $"{parts[0][0]}{parts[^1][0]}" }).ToUpperInvariant();
        ToolTip.SetTip(this, string.IsNullOrEmpty(name) ? null : name);
        var hash = name.Aggregate(17, (h, c) => unchecked(h * 31 + c));
        Fill = Accent ?? HelixDraw.Series(this, Math.Abs(hash % 8) + 1);
    }
}

/// <summary>Overlapping avatars; above Max, a "+N" bubble.</summary>
public class HelixAvatarGroup : ItemsControl
{
    public static readonly StyledProperty<int?> MaxProperty = AvaloniaProperty.Register<HelixAvatarGroup, int?>(nameof(Max));
    public static readonly DirectProperty<HelixAvatarGroup, string?> OverflowTextProperty = AvaloniaProperty.RegisterDirect<HelixAvatarGroup, string?>(nameof(OverflowText), g => g.OverflowText);

    private string? _overflowText;

    protected override Type StyleKeyOverride => typeof(HelixAvatarGroup);

    /// <summary>Avatars shown before "+N".</summary>
    public int? Max { get => GetValue(MaxProperty); set => SetValue(MaxProperty, value); }
    public string? OverflowText { get => _overflowText; private set => SetAndRaise(OverflowTextProperty, ref _overflowText, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MaxProperty || change.Property == ItemCountProperty) UpdateOverflow();
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        container.IsVisible = Max is not { } max || index < max;
        container.Margin = new Thickness(index == 0 ? 0 : -10, 0, 0, 0);
    }

    private void UpdateOverflow()
    {
        OverflowText = Max is { } max && ItemCount > max ? $"+{ItemCount - max}" : null;
        for (var i = 0; i < ItemCount; i++)
        {
            if (ContainerFromIndex(i) is { } c) c.IsVisible = Max is not { } m || i < m;
        }
    }
}

/// <summary>A loading placeholder that pulses: Lines of text, a circle, or any block (Width, Height, CornerRadius).</summary>
public class HelixSkeleton : TemplatedControl
{
    public static readonly StyledProperty<int> LinesProperty = AvaloniaProperty.Register<HelixSkeleton, int>(nameof(Lines), 1);
    public static readonly StyledProperty<bool> IsCircleProperty = AvaloniaProperty.Register<HelixSkeleton, bool>(nameof(IsCircle));
    public static readonly DirectProperty<HelixSkeleton, IReadOnlyList<double>> LineWidthsProperty =
        AvaloniaProperty.RegisterDirect<HelixSkeleton, IReadOnlyList<double>>(nameof(LineWidths), s => s.LineWidths);

    private IReadOnlyList<double> _lineWidths = [1];

    /// <summary>Lines of text (the last one shorter).</summary>
    public int Lines { get => GetValue(LinesProperty); set => SetValue(LinesProperty, value); }
    public bool IsCircle { get => GetValue(IsCircleProperty); set => SetValue(IsCircleProperty, value); }

    /// <summary>Share of the width taken by each line (for the template).</summary>
    public IReadOnlyList<double> LineWidths { get => _lineWidths; private set => SetAndRaise(LineWidthsProperty, ref _lineWidths, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LinesProperty)
        {
            var lines = Math.Max(1, Lines);
            LineWidths = Enumerable.Range(0, lines).Select(i => lines > 1 && i == lines - 1 ? 0.6 : i % 3 == 1 ? 0.92 : 1).ToList();
        }
    }
}

/// <summary>A resize handle between two Grid cells: a thin line that thickens on hover, with a grip.</summary>
public class HelixSplitter : GridSplitter
{
    protected override Type StyleKeyOverride => typeof(HelixSplitter);
}

/// <summary>A context menu with the Helix look. Fill it with <see cref="HelixMenuItem"/>, <see cref="HelixMenuSeparator"/>, <see cref="HelixMenuLabel"/>.</summary>
public class HelixContextMenu : ContextMenu
{
    protected override Type StyleKeyOverride => typeof(HelixContextMenu);
}

/// <summary>A menu entry: IconName, Description (second line), Shortcut, IsDanger.</summary>
public class HelixMenuItem : MenuItem
{
    public static readonly StyledProperty<string?> IconNameProperty = AvaloniaProperty.Register<HelixMenuItem, string?>(nameof(IconName));
    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<HelixMenuItem, string?>(nameof(Description));
    public static readonly StyledProperty<bool> IsDangerProperty = AvaloniaProperty.Register<HelixMenuItem, bool>(nameof(IsDanger));
    public static readonly StyledProperty<string?> ShortcutProperty = AvaloniaProperty.Register<HelixMenuItem, string?>(nameof(Shortcut));

    protected override Type StyleKeyOverride => typeof(HelixMenuItem);

    public string? IconName { get => GetValue(IconNameProperty); set => SetValue(IconNameProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public bool IsDanger { get => GetValue(IsDangerProperty); set => SetValue(IsDangerProperty, value); }

    /// <summary>Text on the right, e.g. "Ctrl+D" (display only; bind the key with a KeyBinding).</summary>
    public string? Shortcut { get => GetValue(ShortcutProperty); set => SetValue(ShortcutProperty, value); }
}

public class HelixMenuSeparator : Separator
{
    protected override Type StyleKeyOverride => typeof(HelixMenuSeparator);
}

/// <summary>A small heading that groups menu items ("Account", "Danger zone"…); not selectable.</summary>
public class HelixMenuLabel : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<HelixMenuLabel, string?>(nameof(Text));

    public string? Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
}

/// <summary>A button that opens its <see cref="Menu"/> below it.</summary>
public class HelixMenuButton : HelixButton
{
    public static readonly StyledProperty<ContextMenu?> MenuProperty = AvaloniaProperty.Register<HelixMenuButton, ContextMenu?>(nameof(Menu));
    public static readonly StyledProperty<HorizontalAlignment> MenuAlignmentProperty = AvaloniaProperty.Register<HelixMenuButton, HorizontalAlignment>(nameof(MenuAlignment), HorizontalAlignment.Left);

    protected override Type StyleKeyOverride => typeof(HelixButton);

    public ContextMenu? Menu { get => GetValue(MenuProperty); set => SetValue(MenuProperty, value); }

    /// <summary>Left: the menu starts under the button's left edge; Right: it ends under its right edge.</summary>
    public HorizontalAlignment MenuAlignment { get => GetValue(MenuAlignmentProperty); set => SetValue(MenuAlignmentProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MenuProperty)
        {
            if (change.OldValue is ContextMenu old) LogicalChildren.Remove(old);
            if (change.NewValue is ContextMenu menu && menu.Parent is null) LogicalChildren.Add(menu);
        }
    }

    protected override void OnClick()
    {
        base.OnClick();
        if (Menu is not { } menu) return;
        menu.Placement = MenuAlignment == HorizontalAlignment.Right ? PlacementMode.BottomEdgeAlignedRight : PlacementMode.BottomEdgeAlignedLeft;
        menu.Open(this);
    }
}
