using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Helix.Export;
using Avalonia.Threading;

namespace Helix.Avalonia;

public enum HelixTreeLayout { List, Diagram }

/// <summary>
/// A tree as an indented list or as a diagram (org chart). Give it a TreeDataTemplate as usual,
/// or just ChildrenPath + TextPath (and IconPath, DescriptionPath) and it builds one.
/// </summary>
public class HelixTreeView : TreeView
{
    /// <summary>Inherited by the items, so each node picks its look.</summary>
    public static readonly StyledProperty<HelixTreeLayout> LayoutProperty = AvaloniaProperty.Register<HelixTreeView, HelixTreeLayout>(nameof(Layout), inherits: true);
    public static readonly StyledProperty<string?> ChildrenPathProperty = AvaloniaProperty.Register<HelixTreeView, string?>(nameof(ChildrenPath));
    public static readonly StyledProperty<string?> TextPathProperty = AvaloniaProperty.Register<HelixTreeView, string?>(nameof(TextPath));
    public static readonly StyledProperty<string?> IconPathProperty = AvaloniaProperty.Register<HelixTreeView, string?>(nameof(IconPath));
    public static readonly StyledProperty<string?> DescriptionPathProperty = AvaloniaProperty.Register<HelixTreeView, string?>(nameof(DescriptionPath));
    public static readonly StyledProperty<int> ExpandDepthProperty = AvaloniaProperty.Register<HelixTreeView, int>(nameof(ExpandDepth), 1);

    protected override Type StyleKeyOverride => typeof(HelixTreeView);

    /// <summary>List (indented, like files) or Diagram (org chart, boxes linked top-down).</summary>
    public HelixTreeLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>Property with the children of an item, e.g. "Children".</summary>
    public string? ChildrenPath { get => GetValue(ChildrenPathProperty); set => SetValue(ChildrenPathProperty, value); }
    public string? TextPath { get => GetValue(TextPathProperty); set => SetValue(TextPathProperty, value); }

    /// <summary>Property with a built-in icon name.</summary>
    public string? IconPath { get => GetValue(IconPathProperty); set => SetValue(IconPathProperty, value); }
    public string? DescriptionPath { get => GetValue(DescriptionPathProperty); set => SetValue(DescriptionPathProperty, value); }

    /// <summary>Levels opened at start (1 = roots open).</summary>
    public int ExpandDepth { get => GetValue(ExpandDepthProperty); set => SetValue(ExpandDepthProperty, value); }

    private bool _generated;

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) => NeedsContainer<HelixTreeViewItem>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new HelixTreeViewItem();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChildrenPathProperty || change.Property == TextPathProperty || change.Property == IconPathProperty || change.Property == DescriptionPathProperty)
        {
            UpdateTemplate();
        }
    }

    private void UpdateTemplate()
    {
        if (ChildrenPath is null || (ItemTemplate is not null && !_generated)) return;
        var children = ChildrenPath;
        string? text = TextPath, icon = IconPath, description = DescriptionPath;
        _generated = true;
        ItemTemplate = new FuncTreeDataTemplate<object>((item, _) =>
        {
            var root = new DockPanel();
            if (icon is not null)
            {
                var glyph = new HelixIcon { Icon = HelixDataGrid.Value(item, icon)?.ToString(), Margin = new Thickness(0, 1, 8, 0), VerticalAlignment = VerticalAlignment.Top };
                glyph.Bind(HelixIcon.ForegroundProperty, glyph.GetResourceObservable(HelixKeys.TextMuted));
                DockPanel.SetDock(glyph, Dock.Left);
                root.Children.Add(glyph);
            }

            var stack = new StackPanel { Children = { new TextBlock { Text = (text is null ? item : HelixDataGrid.Value(item, text))?.ToString() } } };
            if (description is not null)
            {
                var line = new TextBlock { Text = HelixDataGrid.Value(item, description)?.ToString(), FontSize = 12 };
                line.Bind(TextBlock.ForegroundProperty, line.GetResourceObservable(HelixKeys.TextMuted));
                stack.Children.Add(line);
            }

            root.Children.Add(stack);
            return root;
        }, item => HelixDataGrid.Value(item, children) as IEnumerable ?? Array.Empty<object>());
    }
}

/// <summary>A tree node; knows its level (indent). In a diagram, the connectors use its position among siblings.</summary>
[PseudoClasses(":root-level", ":leaf")]
public class HelixTreeViewItem : TreeViewItem
{
    public static readonly StyledProperty<HelixTreeLayout> LayoutProperty = HelixTreeView.LayoutProperty.AddOwner<HelixTreeViewItem>();
    public static readonly DirectProperty<HelixTreeViewItem, Thickness> IndentProperty = AvaloniaProperty.RegisterDirect<HelixTreeViewItem, Thickness>(nameof(Indent), i => i.Indent);
    public static readonly DirectProperty<HelixTreeViewItem, bool> ShowsChildrenProperty = AvaloniaProperty.RegisterDirect<HelixTreeViewItem, bool>(nameof(ShowsChildren), i => i.ShowsChildren);

    private Thickness _indent;
    private bool _showsChildren;

    /// <summary>Expanded and has children (template).</summary>
    public bool ShowsChildren { get => _showsChildren; private set => SetAndRaise(ShowsChildrenProperty, ref _showsChildren, value); }
    private bool _expandedOnce;

    protected override Type StyleKeyOverride => typeof(HelixTreeViewItem);

    public HelixTreeLayout Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>Left margin of the row in the list layout (18 px per level).</summary>
    public Thickness Indent { get => _indent; private set => SetAndRaise(IndentProperty, ref _indent, value); }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) => NeedsContainer<HelixTreeViewItem>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new HelixTreeViewItem();

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        var level = this.GetLogicalAncestors().OfType<TreeViewItem>().Count();
        Indent = new Thickness(level * 18, 0, 0, 0);
        PseudoClasses.Set(":root-level", level == 0);
        PseudoClasses.Set(":leaf", ItemCount == 0);
        if (!_expandedOnce && this.FindLogicalAncestorOfType<HelixTreeView>() is { } tree && level < tree.ExpandDepth)
        {
            _expandedOnce = true;
            SetCurrentValue(IsExpandedProperty, true);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemCountProperty) PseudoClasses.Set(":leaf", ItemCount == 0);
        if (change.Property == ItemCountProperty || change.Property == IsExpandedProperty) ShowsChildren = IsExpanded && ItemCount > 0;
    }
}

/// <summary>
/// Slides one or more at a time: arrows, dots, keyboard, swipe, optional autoplay (pauses on hover and focus).
/// Put any content as items. SelectedIndex is two-way.
/// </summary>
[TemplatePart("PART_Prev", typeof(Button))]
[TemplatePart("PART_Next", typeof(Button))]
[TemplatePart("PART_Dots", typeof(Panel))]
[TemplatePart("PART_Play", typeof(Button))]
public class HelixCarousel : ItemsControl
{
    public static readonly StyledProperty<int> SelectedIndexProperty = AvaloniaProperty.Register<HelixCarousel, int>(nameof(SelectedIndex), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<int> SlidesPerViewProperty = AvaloniaProperty.Register<HelixCarousel, int>(nameof(SlidesPerView), 1);
    public static readonly StyledProperty<double> GapProperty = AvaloniaProperty.Register<HelixCarousel, double>(nameof(Gap), 16);
    public static readonly StyledProperty<bool> AutoplayProperty = AvaloniaProperty.Register<HelixCarousel, bool>(nameof(Autoplay));
    public static readonly StyledProperty<TimeSpan> IntervalProperty = AvaloniaProperty.Register<HelixCarousel, TimeSpan>(nameof(Interval), TimeSpan.FromSeconds(5));
    public static readonly StyledProperty<bool> LoopProperty = AvaloniaProperty.Register<HelixCarousel, bool>(nameof(Loop), true);
    public static readonly StyledProperty<bool> ShowArrowsProperty = AvaloniaProperty.Register<HelixCarousel, bool>(nameof(ShowArrows), true);
    public static readonly StyledProperty<bool> ShowDotsProperty = AvaloniaProperty.Register<HelixCarousel, bool>(nameof(ShowDots), true);
    public static readonly StyledProperty<double> OffsetProperty = AvaloniaProperty.Register<HelixCarousel, double>(nameof(Offset));

    protected override Type StyleKeyOverride => typeof(HelixCarousel);

    public int SelectedIndex { get => GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
    public int SlidesPerView { get => GetValue(SlidesPerViewProperty); set => SetValue(SlidesPerViewProperty, value); }
    public double Gap { get => GetValue(GapProperty); set => SetValue(GapProperty, value); }
    public bool Autoplay { get => GetValue(AutoplayProperty); set => SetValue(AutoplayProperty, value); }
    public TimeSpan Interval { get => GetValue(IntervalProperty); set => SetValue(IntervalProperty, value); }

    /// <summary>The arrows wrap from the last slide to the first.</summary>
    public bool Loop { get => GetValue(LoopProperty); set => SetValue(LoopProperty, value); }
    public bool ShowArrows { get => GetValue(ShowArrowsProperty); set => SetValue(ShowArrowsProperty, value); }
    public bool ShowDots { get => GetValue(ShowDotsProperty); set => SetValue(ShowDotsProperty, value); }

    /// <summary>Animated position (in slides) read by the panel.</summary>
    public double Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    private readonly DispatcherTimer _timer = new();
    private Button? _prev, _next, _play;
    private Panel? _dots;
    private bool _paused;
    private Point? _swipeStart;

    public HelixCarousel()
    {
        Focusable = true;
        Transitions = [new DoubleTransition { Property = OffsetProperty, Duration = TimeSpan.FromMilliseconds(380), Easing = new CubicEaseInOut() }];
        _timer.Tick += (_, _) =>
        {
            if (!IsPointerOver && !IsKeyboardFocusWithin && !_paused) Go(SelectedIndex + 1, wrap: true);
        };
        AddHandler(PointerPressedEvent, (_, e) => _swipeStart = e.GetPosition(this), RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, (_, e) =>
        {
            if (_swipeStart is { } start)
            {
                var dx = e.GetPosition(this).X - start.X;
                if (Math.Abs(dx) > 60) Go(SelectedIndex + (dx < 0 ? 1 : -1), Loop);
            }

            _swipeStart = null;
        }, RoutingStrategies.Tunnel);
        Items.CollectionChanged += (_, _) => OnIndexChanged();
    }

    private int LastStart => Math.Max(0, ItemCount - Math.Max(1, SlidesPerView));

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_prev is not null) _prev.Click -= OnPrev;
        if (_next is not null) _next.Click -= OnNext;
        if (_play is not null) _play.Click -= OnPlay;
        base.OnApplyTemplate(e);
        _prev = e.NameScope.Find<Button>("PART_Prev");
        _next = e.NameScope.Find<Button>("PART_Next");
        _play = e.NameScope.Find<Button>("PART_Play");
        _dots = e.NameScope.Find<Panel>("PART_Dots");
        if (_prev is not null) _prev.Click += OnPrev;
        if (_next is not null) _next.Click += OnNext;
        if (_play is not null) _play.Click += OnPlay;
        UpdateTimer();
        OnIndexChanged();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateTimer();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty || change.Property == SlidesPerViewProperty || change.Property == LoopProperty
            || change.Property == ItemCountProperty || change.Property == ShowDotsProperty) OnIndexChanged();
        else if (change.Property == AutoplayProperty || change.Property == IntervalProperty) UpdateTimer();
        else if (change.Property == OffsetProperty || change.Property == GapProperty) ItemsPanelRoot?.InvalidateArrange();
    }

    private void OnPrev(object? sender, RoutedEventArgs e) => Go(SelectedIndex - 1, Loop);
    private void OnNext(object? sender, RoutedEventArgs e) => Go(SelectedIndex + 1, Loop);

    private void OnPlay(object? sender, RoutedEventArgs e)
    {
        _paused = !_paused;
        UpdatePlayIcon();
    }

    private void UpdatePlayIcon()
    {
        if (_play is null) return;
        _play.Content = new HelixIcon { Icon = _paused ? "arrow-right" : "minus", Size = 14 };
        ToolTip.SetTip(_play, _paused ? "▶" : "❚❚");
    }

    private void UpdateTimer()
    {
        _timer.Interval = Interval;
        if (Autoplay && IsAttachedToVisualTree()) _timer.Start(); else _timer.Stop();
        if (_play is not null) _play.IsVisible = Autoplay;
        UpdatePlayIcon();
    }

    private bool IsAttachedToVisualTree() => TopLevel.GetTopLevel(this) is not null;

    /// <summary>Goes to a slide (clamped, or wrapped when <paramref name="wrap"/>).</summary>
    public void Go(int index, bool wrap = false)
    {
        var last = LastStart;
        if (wrap) index = index > last ? 0 : index < 0 ? last : index;
        SetCurrentValue(SelectedIndexProperty, Math.Clamp(index, 0, last));
    }

    private void OnIndexChanged()
    {
        var index = Math.Clamp(SelectedIndex, 0, LastStart);
        Offset = index;
        if (_prev is not null) _prev.IsEnabled = Loop || index > 0;
        if (_next is not null) _next.IsEnabled = Loop || index < LastStart;
        BuildDots(index);
    }

    private void BuildDots(int active)
    {
        if (_dots is null) return;
        _dots.Children.Clear();
        _dots.IsVisible = ShowDots && ItemCount > SlidesPerView;
        for (var i = 0; i <= LastStart; i++)
        {
            var target = i;
            var dot = new Button
            {
                Width = target == active ? 22 : 8,
                Height = 8,
                Margin = new Thickness(3, 0),
                Cursor = new Cursor(StandardCursorType.Hand),
                Template = DotTemplate,
            };
            ToolTip.SetTip(dot, $"{target + 1} / {LastStart + 1}");
            dot.Bind(BackgroundProperty, dot.GetResourceObservable(target == active ? HelixKeys.Primary : HelixKeys.BorderStrong));
            dot.Click += (_, _) => Go(target);
            _dots.Children.Add(dot);
        }
    }

    private static readonly IControlTemplate DotTemplate = new FuncControlTemplate<Button>((button, _) => new Border
    {
        CornerRadius = new CornerRadius(4),
        [!Border.BackgroundProperty] = button[!BackgroundProperty],
    });

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Left) { Go(SelectedIndex - 1, Loop); e.Handled = true; }
        if (e.Key == Key.Right) { Go(SelectedIndex + 1, Loop); e.Handled = true; }
    }
}

/// <summary>Places carousel slides side by side, SlidesPerView at a time, shifted by the carousel's animated Offset.</summary>
public class HelixCarouselPanel : Panel
{
    public HelixCarouselPanel() => ClipToBounds = true;

    private HelixCarousel? Owner => this.FindLogicalAncestorOfType<HelixCarousel>() ?? (TemplatedParent as HelixCarousel);

    private double SlideWidth(double width)
    {
        var per = Math.Max(1, Owner?.SlidesPerView ?? 1);
        var gap = Owner?.Gap ?? 16;
        return Math.Max(0, (width - gap * (per - 1)) / per);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 600 : availableSize.Width;
        var slide = SlideWidth(width);
        var height = 0.0;
        foreach (var child in Children)
        {
            child.Measure(new Size(slide, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var slide = SlideWidth(finalSize.Width);
        var gap = Owner?.Gap ?? 16;
        var offset = Owner?.Offset ?? 0;
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Arrange(new Rect((i - offset) * (slide + gap), 0, slide, finalSize.Height));
        }

        return finalSize;
    }
}

public enum HelixLogLevel { Debug, Info, Warning, Error }

/// <summary>
/// A line of a <see cref="HelixLogViewer"/>. Only Time, Level and Message are needed; the rest shows in the line's details
/// (click a line): structured properties (click a value to filter on it), the exception, the trace id and the user.
/// </summary>
public sealed record HelixLogEntry(DateTime Time, HelixLogLevel Level, string Message, string? Source = null)
{
    /// <summary>Structured values of the event: { ["CourseId"] = "…", ["EnrollmentId"] = "…" }.</summary>
    public IReadOnlyDictionary<string, string>? Properties { get; init; }

    /// <summary>The exception, with its stack trace.</summary>
    public string? Exception { get; init; }

    /// <summary>The distributed trace id (W3C).</summary>
    public string? TraceId { get; init; }

    /// <summary>Who was doing it (user id or name).</summary>
    public string? User { get; init; }

    public bool IsInfo => Level == HelixLogLevel.Info;
    public bool IsWarning => Level == HelixLogLevel.Warning;
    public bool IsError => Level == HelixLogLevel.Error;
    public bool HasException => Exception is not null;
    public string LevelText => Level.ToString().ToUpperInvariant();
}

/// <summary>A period of the log viewer's period filter: the last <see cref="Span"/> (null = everything).</summary>
public sealed record HelixLogPeriod(string Label, TimeSpan? Span)
{
    public override string ToString() => Label;
}

/// <summary>A filter on a property value, shown as a chip above the lines.</summary>
public sealed class HelixLogFilter(string key, string label, string value, Action<HelixLogFilter> remove)
{
    public string Key { get; } = key;
    public string Label { get; } = label;
    public string Value { get; } = value;
    public string ShortValue => Value.Length <= 24 ? Value : Value[..10] + "…" + Value[^8..];
    public System.Windows.Input.ICommand RemoveCommand { get; } = new HelixCommand(() => remove(null!));
    internal Action<HelixLogFilter> Remove { get; } = remove;
}

/// <summary>
/// A live log: level toggles with counts, search, source and period filters, filters on property values (chips), the details of
/// a line (properties, exception, trace id, user; copy), export of the shown lines to CSV or Excel, and follow mode with a
/// "new lines" button when you scroll away. Virtualized.
/// </summary>
[TemplatePart("PART_Lines", typeof(ListBox))]
[TemplatePart("PART_Details", typeof(ContentControl))]
public class HelixLogViewer : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable<HelixLogEntry>?> ItemsSourceProperty = AvaloniaProperty.Register<HelixLogViewer, IEnumerable<HelixLogEntry>?>(nameof(ItemsSource));
    public static readonly StyledProperty<bool> IsFollowingProperty = AvaloniaProperty.Register<HelixLogViewer, bool>(nameof(IsFollowing), true, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<string?> SearchTextProperty = AvaloniaProperty.Register<HelixLogViewer, string?>(nameof(SearchText), "", defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<bool> ShowDebugProperty = AvaloniaProperty.Register<HelixLogViewer, bool>(nameof(ShowDebug), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<bool> ShowInfoProperty = AvaloniaProperty.Register<HelixLogViewer, bool>(nameof(ShowInfo), true, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<bool> ShowWarningProperty = AvaloniaProperty.Register<HelixLogViewer, bool>(nameof(ShowWarning), true, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<bool> ShowErrorProperty = AvaloniaProperty.Register<HelixLogViewer, bool>(nameof(ShowError), true, defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<string> TimeFormatProperty = AvaloniaProperty.Register<HelixLogViewer, string>(nameof(TimeFormat), "HH:mm:ss");
    public static readonly StyledProperty<bool> ShowDetailsProperty = AvaloniaProperty.Register<HelixLogViewer, bool>(nameof(ShowDetails), true);
    public static readonly StyledProperty<bool> IsExportableProperty = AvaloniaProperty.Register<HelixLogViewer, bool>(nameof(IsExportable), true);
    public static readonly StyledProperty<string> ExportFileNameProperty = AvaloniaProperty.Register<HelixLogViewer, string>(nameof(ExportFileName), "log");
    public static readonly StyledProperty<string?> SourceChoiceProperty = AvaloniaProperty.Register<HelixLogViewer, string?>(nameof(SourceChoice), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<HelixLogPeriod?> PeriodProperty = AvaloniaProperty.Register<HelixLogViewer, HelixLogPeriod?>(nameof(Period), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<HelixLogEntry?> SelectedEntryProperty = AvaloniaProperty.Register<HelixLogViewer, HelixLogEntry?>(nameof(SelectedEntry), defaultBindingMode: BindingMode.TwoWay);
    public static readonly DirectProperty<HelixLogViewer, IEnumerable?> ViewProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, IEnumerable?>(nameof(View), v => v.View);
    public static readonly DirectProperty<HelixLogViewer, string> DebugLabelProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, string>(nameof(DebugLabel), v => v.DebugLabel);
    public static readonly DirectProperty<HelixLogViewer, string> InfoLabelProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, string>(nameof(InfoLabel), v => v.InfoLabel);
    public static readonly DirectProperty<HelixLogViewer, string> WarningLabelProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, string>(nameof(WarningLabel), v => v.WarningLabel);
    public static readonly DirectProperty<HelixLogViewer, string> ErrorLabelProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, string>(nameof(ErrorLabel), v => v.ErrorLabel);
    public static readonly DirectProperty<HelixLogViewer, IReadOnlyList<string>> SourcesProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, IReadOnlyList<string>>(nameof(Sources), v => v.Sources);
    public static readonly DirectProperty<HelixLogViewer, bool> HasSourcesProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, bool>(nameof(HasSources), v => v.HasSources);
    public static readonly DirectProperty<HelixLogViewer, bool> HasFiltersProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, bool>(nameof(HasFilters), v => v.HasFilters);
    public static readonly DirectProperty<HelixLogViewer, string> StatusTextProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, string>(nameof(StatusText), v => v.StatusText);
    public static readonly DirectProperty<HelixLogViewer, string?> UnseenTextProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, string?>(nameof(UnseenText), v => v.UnseenText);
    public static readonly DirectProperty<HelixLogViewer, bool> HasUnseenProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, bool>(nameof(HasUnseen), v => v.HasUnseen);

    /// <summary>The lines, oldest first; an ObservableCollection is followed live.</summary>
    public IEnumerable<HelixLogEntry>? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>Stays on the newest line (true).</summary>
    public bool IsFollowing { get => GetValue(IsFollowingProperty); set => SetValue(IsFollowingProperty, value); }
    public string? SearchText { get => GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public bool ShowDebug { get => GetValue(ShowDebugProperty); set => SetValue(ShowDebugProperty, value); }
    public bool ShowInfo { get => GetValue(ShowInfoProperty); set => SetValue(ShowInfoProperty, value); }
    public bool ShowWarning { get => GetValue(ShowWarningProperty); set => SetValue(ShowWarningProperty, value); }
    public bool ShowError { get => GetValue(ShowErrorProperty); set => SetValue(ShowErrorProperty, value); }
    public string TimeFormat { get => GetValue(TimeFormatProperty); set => SetValue(TimeFormatProperty, value); }

    /// <summary>A click on a line shows its details (true).</summary>
    public bool ShowDetails { get => GetValue(ShowDetailsProperty); set => SetValue(ShowDetailsProperty, value); }

    /// <summary>The CSV and Excel buttons, which save the lines shown (true).</summary>
    public bool IsExportable { get => GetValue(IsExportableProperty); set => SetValue(IsExportableProperty, value); }
    public string ExportFileName { get => GetValue(ExportFileNameProperty); set => SetValue(ExportFileNameProperty, value); }

    /// <summary>The source shown ("All sources" for every one).</summary>
    public string? SourceChoice { get => GetValue(SourceChoiceProperty); set => SetValue(SourceChoiceProperty, value); }
    public HelixLogPeriod? Period { get => GetValue(PeriodProperty); set => SetValue(PeriodProperty, value); }

    /// <summary>The line whose details are shown.</summary>
    public HelixLogEntry? SelectedEntry { get => GetValue(SelectedEntryProperty); set => SetValue(SelectedEntryProperty, value); }

    /// <summary>A testable clock for the period filter (system clock by default).</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    private DataGridCollectionView? _view;
    private int[] _counts = new int[4];
    private string _debugLabel = "", _infoLabel = "", _warningLabel = "", _errorLabel = "", _statusText = "";
    private IReadOnlyList<string> _sources = [];
    private bool _hasSources, _hasFilters, _hasUnseen;
    private string? _unseenText;
    private int _unseen;

    /// <summary>The filtered lines (template).</summary>
    public IEnumerable? View { get => _view; private set { var old = _view; _view = (DataGridCollectionView?)value; RaisePropertyChanged(ViewProperty, old, _view); } }

    /// <summary>Lines per level: Debug, Info, Warning, Error.</summary>
    public IReadOnlyList<int> Counts => _counts;

    public string DebugLabel { get => _debugLabel; private set => SetAndRaise(DebugLabelProperty, ref _debugLabel, value); }
    public string InfoLabel { get => _infoLabel; private set => SetAndRaise(InfoLabelProperty, ref _infoLabel, value); }
    public string WarningLabel { get => _warningLabel; private set => SetAndRaise(WarningLabelProperty, ref _warningLabel, value); }
    public string ErrorLabel { get => _errorLabel; private set => SetAndRaise(ErrorLabelProperty, ref _errorLabel, value); }

    /// <summary>"All sources" and the sources of the lines (template).</summary>
    public IReadOnlyList<string> Sources { get => _sources; private set => SetAndRaise(SourcesProperty, ref _sources, value); }
    public bool HasSources { get => _hasSources; private set => SetAndRaise(HasSourcesProperty, ref _hasSources, value); }

    /// <summary>The periods offered (template).</summary>
    public IReadOnlyList<HelixLogPeriod> Periods { get; }

    /// <summary>The property filters (chips).</summary>
    public ObservableCollection<HelixLogFilter> Filters { get; } = [];
    public bool HasFilters { get => _hasFilters; private set => SetAndRaise(HasFiltersProperty, ref _hasFilters, value); }

    /// <summary>"102 of 412 lines".</summary>
    public string StatusText { get => _statusText; private set => SetAndRaise(StatusTextProperty, ref _statusText, value); }

    /// <summary>"12 new lines", while not following.</summary>
    public string? UnseenText { get => _unseenText; private set => SetAndRaise(UnseenTextProperty, ref _unseenText, value); }
    public bool HasUnseen { get => _hasUnseen; private set => SetAndRaise(HasUnseenProperty, ref _hasUnseen, value); }

    /// <summary>Number of lines shown after the filters.</summary>
    public int VisibleCount => _view?.Count ?? 0;

    /// <summary>The lines shown, after every filter (what the export writes).</summary>
    public IReadOnlyList<HelixLogEntry> VisibleEntries => _view?.Cast<HelixLogEntry>().ToList() ?? [];

    public System.Windows.Input.ICommand ClearFiltersCommand { get; }
    public System.Windows.Input.ICommand ExportCsvCommand { get; }
    public System.Windows.Input.ICommand ExportExcelCommand { get; }
    public System.Windows.Input.ICommand JumpToNewestCommand { get; }

    public static string FollowText => HelixTexts.Current.FollowLog;
    public static string SearchPlaceholderText => HelixTexts.Current.Search;
    public static string ClearFiltersText => HelixTexts.Current.ClearFilters;

    /// <summary>Formats a line's time with the viewer's TimeFormat (MultiBinding: time, format).</summary>
    public static readonly IMultiValueConverter TimeConverter = new FuncMultiValueConverter<object?, string>(values =>
        values.ToList() is [DateTime time, string format, ..] ? time.ToString(format, CultureInfo.CurrentCulture) : "");

    private const string UserKey = "\u0001user";
    private const string TraceKey = "\u0001trace";
    private readonly DispatcherTimer _refresh;
    private ListBox? _lines;
    private ContentControl? _details;

    public HelixLogViewer()
    {
        var texts = HelixTexts.Current;
        Periods =
        [
            new(texts.LogAllTime, null), new(texts.LogLast15Minutes, TimeSpan.FromMinutes(15)), new(texts.LogLastHour, TimeSpan.FromHours(1)),
            new(texts.LogLast24Hours, TimeSpan.FromDays(1)), new(texts.LogLast7Days, TimeSpan.FromDays(7)),
        ];
        SetCurrentValue(PeriodProperty, Periods[0]);
        SetCurrentValue(SourceChoiceProperty, texts.LogAllSources);
        ClearFiltersCommand = new HelixCommand(ClearFilters);
        ExportCsvCommand = new HelixCommand(() => _ = ExportFileAsync(HelixExportFormat.Csv));
        ExportExcelCommand = new HelixCommand(() => _ = ExportFileAsync(HelixExportFormat.Excel));
        JumpToNewestCommand = new HelixCommand(() => SetCurrentValue(IsFollowingProperty, true));
        Filters.CollectionChanged += (_, _) => HasFilters = Filters.Count > 0;
        _refresh = new DispatcherTimer(TimeSpan.FromMilliseconds(120), DispatcherPriority.Background, (_, _) => Flush());
        _refresh.Stop();
        Recount();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _lines = e.NameScope.Find<ListBox>("PART_Lines");
        _details = e.NameScope.Find<ContentControl>("PART_Details");
        ShowSelected();
        ScrollToEnd();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == ItemsSourceProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= OnLinesChanged;
            if (change.NewValue is INotifyCollectionChanged newList) newList.CollectionChanged += OnLinesChanged;
            View = change.NewValue is IEnumerable<HelixLogEntry> lines ? new DataGridCollectionView(lines as IList ?? lines.ToList()) { Filter = Accept } : null;
            Recount();
            ScrollToEnd();
        }
        else if (p == SearchTextProperty || p == ShowDebugProperty || p == ShowInfoProperty || p == ShowWarningProperty || p == ShowErrorProperty
                 || p == SourceChoiceProperty || p == PeriodProperty)
        {
            Refilter();
        }
        else if (p == IsFollowingProperty)
        {
            if (IsFollowing) SetUnseen(0);
            ScrollToEnd();
        }
        else if (p == SelectedEntryProperty)
        {
            // Reading a line: stop following, so new lines don't scroll it away.
            if (SelectedEntry is not null) SetCurrentValue(IsFollowingProperty, false);
            ShowSelected();
        }
    }

    private void Refilter()
    {
        _view?.Refresh();
        Recount();
        ScrollToEnd();
    }

    /// <summary>Lines may arrive in bursts: counts and scrolling are refreshed at most every 120 ms.</summary>
    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!IsFollowing && e.NewItems is { Count: > 0 } added) SetUnseen(_unseen + added.Count);
        _refresh.Start();
    }

    /// <summary>Applies pending new lines now (the viewer does it every 120 ms); used by tests.</summary>
    public void Flush()
    {
        _refresh.Stop();
        Recount();
        ScrollToEnd();
    }

    private void SetUnseen(int count)
    {
        _unseen = count;
        HasUnseen = count > 0;
        UnseenText = count > 0 ? string.Format(CultureInfo.CurrentCulture, HelixTexts.Current.LogNewLines, count) : null;
    }

    /// <summary>Shows only the lines whose property <paramref name="key"/> equals <paramref name="value"/> (adds to the other filters).</summary>
    public void AddFilter(string key, string value)
    {
        foreach (var same in Filters.Where(f => f.Key == key).ToList()) Filters.Remove(same);
        var label = key switch { UserKey => HelixTexts.Current.LogUser, TraceKey => HelixTexts.Current.LogTraceId, _ => key };
        HelixLogFilter? filter = null;
        filter = new HelixLogFilter(key, label, value, _ => { Filters.Remove(filter!); Refilter(); });
        Filters.Add(filter);
        Refilter();
    }

    /// <summary>Removes the property, source and period filters and the search (levels stay as they are).</summary>
    public void ClearFilters()
    {
        Filters.Clear();
        SetCurrentValue(SearchTextProperty, "");
        SetCurrentValue(SourceChoiceProperty, HelixTexts.Current.LogAllSources);
        SetCurrentValue(PeriodProperty, Periods[0]);
        Refilter();
    }

    private bool Accept(object item)
    {
        if (item is not HelixLogEntry entry) return false;
        var shown = entry.Level switch
        {
            HelixLogLevel.Debug => ShowDebug,
            HelixLogLevel.Info => ShowInfo,
            HelixLogLevel.Warning => ShowWarning,
            _ => ShowError,
        };
        if (!shown) return false;
        if (SourceChoice is { } source && source != HelixTexts.Current.LogAllSources && entry.Source != source) return false;
        if (Period?.Span is { } span && entry.Time < TimeProvider.GetLocalNow().DateTime - span) return false;
        foreach (var filter in Filters)
        {
            var actual = filter.Key switch
            {
                UserKey => entry.User,
                TraceKey => entry.TraceId,
                _ => entry.Properties?.GetValueOrDefault(filter.Key),
            };
            if (actual != filter.Value) return false;
        }

        var term = SearchText?.Trim();
        return string.IsNullOrEmpty(term)
               || entry.Message.Contains(term, StringComparison.CurrentCultureIgnoreCase)
               || entry.Source?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true
               || entry.User?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true
               || entry.TraceId?.Contains(term, StringComparison.OrdinalIgnoreCase) == true
               || entry.Properties?.Values.Any(v => v.Contains(term, StringComparison.CurrentCultureIgnoreCase)) == true;
    }

    private void Recount()
    {
        var counts = new int[4];
        var all = ItemsSource?.ToList() ?? [];
        foreach (var entry in all) counts[(int)entry.Level]++;
        _counts = counts;
        var texts = HelixTexts.Current;
        DebugLabel = $"{texts.LogDebug}  {counts[0]}";
        InfoLabel = $"{texts.LogInfo}  {counts[1]}";
        WarningLabel = $"{texts.LogWarning}  {counts[2]}";
        ErrorLabel = $"{texts.LogError}  {counts[3]}";
        var sources = all.Select(e => e.Source).OfType<string>().Where(s => s.Length > 0).Distinct().Order(StringComparer.CurrentCulture).ToList();
        if (!sources.SequenceEqual(Sources.Skip(1))) Sources = [texts.LogAllSources, .. sources];
        HasSources = sources.Count > 1;
        StatusText = string.Format(CultureInfo.CurrentCulture, texts.LogShown, VisibleCount, all.Count);
        if (SelectedEntry is { } selected && _view is not null && !_view.Contains(selected)) SetCurrentValue(SelectedEntryProperty, null);
    }

    private void ScrollToEnd()
    {
        if (!IsFollowing || _lines is null || _view is null || _view.Count == 0) return;
        var last = _view.Count - 1;
        Dispatcher.UIThread.Post(() => _lines.ScrollIntoView(last), DispatcherPriority.Background);
    }

    private IBrush Res(string key) => HelixDraw.Brush(this, key, Colors.Gray);

    /// <summary>Builds the details panel of the selected line: each value is a button that filters on it.</summary>
    private void ShowSelected()
    {
        if (_details is null) return;
        if (SelectedEntry is not { } e || !ShowDetails)
        {
            _details.Content = null;
            _details.IsVisible = false;
            return;
        }

        var texts = HelixTexts.Current;
        var mono = new FontFamily("Cascadia Mono, Menlo, Consolas, monospace");
        var level = e.Level switch { HelixLogLevel.Info => "Helix.Info", HelixLogLevel.Warning => "Helix.Warning", HelixLogLevel.Error => "Helix.Danger", _ => "Helix.TextSubtle" };
        var copy = new Button { Theme = this.FindResource("HelixIconButton") as ControlTheme, Content = new HelixIcon { Icon = "copy", Size = 14 } };
        ToolTip.SetTip(copy, texts.LogCopy);
        copy.Click += async (_, _) =>
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard) await clipboard.SetTextAsync(DetailsText(e));
        };
        var close = new Button { Theme = this.FindResource("HelixIconButton") as ControlTheme, Content = new HelixIcon { Icon = "x", Size = 14 } };
        ToolTip.SetTip(close, texts.Close);
        close.Click += (_, _) => SetCurrentValue(SelectedEntryProperty, null);
        var head = new DockPanel
        {
            Children =
            {
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, [DockPanel.DockProperty] = Dock.Right, Children = { copy, close } },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = e.LevelText, FontSize = 11, FontWeight = FontWeight.Bold, Foreground = Res(level), VerticalAlignment = VerticalAlignment.Center },
                        new TextBlock { Text = e.Time.ToString("F", CultureInfo.CurrentCulture), FontSize = 12.5, Foreground = Res("Helix.TextMuted"), VerticalAlignment = VerticalAlignment.Center },
                    },
                },
            },
        };

        var facts = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,14,*") };
        void Fact(string label, string value, Action filter)
        {
            var row = facts.RowDefinitions.Count;
            facts.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            facts.Children.Add(new TextBlock { Text = label, FontSize = 12.5, FontWeight = FontWeight.SemiBold, Foreground = Res("Helix.TextMuted"), Margin = new Thickness(0, 2), [Grid.RowProperty] = row });
            var button = new Button
            {
                Content = new TextBlock { Text = value, FontFamily = mono, FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Foreground = Res("Helix.Text") },
                Background = Brushes.Transparent, BorderThickness = default, Padding = new Thickness(4, 1), HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand), [Grid.RowProperty] = row, [Grid.ColumnProperty] = 2,
            };
            ToolTip.SetTip(button, string.Format(CultureInfo.CurrentCulture, texts.LogFilterOn, label, value));
            button.Click += (_, _) => filter();
            facts.Children.Add(button);
        }

        if (!string.IsNullOrEmpty(e.Source)) Fact(texts.LogSource, e.Source, () => SetCurrentValue(SourceChoiceProperty, e.Source));
        if (!string.IsNullOrEmpty(e.User)) Fact(texts.LogUser, e.User, () => AddFilter(UserKey, e.User));
        if (!string.IsNullOrEmpty(e.TraceId)) Fact(texts.LogTraceId, e.TraceId, () => AddFilter(TraceKey, e.TraceId));
        foreach (var (key, value) in e.Properties ?? new Dictionary<string, string>()) Fact(key, value, () => AddFilter(key, value));

        var panel = new StackPanel { Spacing = 8, Children = { head, new TextBlock { Text = e.Message, FontSize = 13.5, TextWrapping = TextWrapping.Wrap, Foreground = Res("Helix.Text") }, facts } };
        if (e.Exception is { } exception)
        {
            panel.Children.Add(new TextBlock { Text = texts.LogException, FontSize = 12.5, FontWeight = FontWeight.SemiBold, Foreground = Res("Helix.Danger"), Margin = new Thickness(0, 4, 0, 0) });
            panel.Children.Add(new Border
            {
                Background = Res("Helix.Surface"), BorderBrush = Res("Helix.Border"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8),
                Child = new SelectableTextBlock { Text = exception, FontFamily = mono, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = Res("Helix.Text") },
            });
        }

        _details.Content = new ScrollViewer { Content = panel, MaxHeight = 200, Padding = new Thickness(14, 10) };
        _details.IsVisible = true;
    }

    private static string DetailsText(HelixLogEntry e)
    {
        var lines = new List<string> { $"{e.Time.ToString("O", CultureInfo.InvariantCulture)} [{e.Level}] {e.Source} {e.Message}" };
        if (e.User is not null) lines.Add($"User: {e.User}");
        if (e.TraceId is not null) lines.Add($"TraceId: {e.TraceId}");
        foreach (var (key, value) in e.Properties ?? new Dictionary<string, string>()) lines.Add($"{key}: {value}");
        if (e.Exception is not null) lines.Add(e.Exception);
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>The lines shown, ready for Helix.Export: time, level, source, message, user, trace id, one column per property, exception.</summary>
    public HelixExportTable ToExportTable()
    {
        var texts = HelixTexts.Current;
        var rows = VisibleEntries;
        var keys = rows.SelectMany(e => e.Properties?.Keys ?? []).Distinct().ToList();
        var columns = new List<HelixExportColumn>
        {
            new() { Header = texts.LogTime, Value = r => ((HelixLogEntry)r).Time, Format = "yyyy-MM-dd HH:mm:ss" },
            new() { Header = texts.LogLevel, Value = r => ((HelixLogEntry)r).Level.ToString() },
            new() { Header = texts.LogSource, Value = r => ((HelixLogEntry)r).Source },
            new() { Header = texts.LogMessage, Value = r => ((HelixLogEntry)r).Message },
            new() { Header = texts.LogUser, Value = r => ((HelixLogEntry)r).User },
            new() { Header = texts.LogTraceId, Value = r => ((HelixLogEntry)r).TraceId },
        };
        columns.AddRange(keys.Select(k => new HelixExportColumn { Header = k, Value = r => ((HelixLogEntry)r).Properties?.GetValueOrDefault(k) }));
        columns.Add(new() { Header = texts.LogException, Value = r => ((HelixLogEntry)r).Exception });
        return new HelixExportTable(columns, rows.Cast<object>());
    }

    /// <summary>The lines shown as a CSV or Excel file (bytes).</summary>
    public byte[] ToFile(HelixExportFormat format) =>
        format == HelixExportFormat.Csv
            ? [.. System.Text.Encoding.UTF8.GetPreamble(), .. System.Text.Encoding.UTF8.GetBytes(HelixCsv.Write(ToExportTable()))]
            : HelixExport.Write(ToExportTable(), format);

    /// <summary>Asks where to save, then writes the lines shown.</summary>
    public async Task ExportFileAsync(HelixExportFormat format)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { CanSave: true } storage) return;
        var extension = HelixExport.Extension(format);
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = ExportFileName + extension,
            DefaultExtension = extension,
            FileTypeChoices = [new FilePickerFileType(HelixExport.Label(format)) { Patterns = ["*" + extension] }],
        });
        if (file is null) return;
        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(ToFile(format));
    }
}
