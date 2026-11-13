using System.Collections;
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
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
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

/// <summary>A line of a <see cref="HelixLogViewer"/>.</summary>
public sealed record HelixLogEntry(DateTime Time, HelixLogLevel Level, string Message, string? Source = null)
{
    public bool IsInfo => Level == HelixLogLevel.Info;
    public bool IsWarning => Level == HelixLogLevel.Warning;
    public bool IsError => Level == HelixLogLevel.Error;
    public string LevelText => Level.ToString().ToUpperInvariant();
}

/// <summary>A live log: level filters with counts, search, follow mode (stays on the newest line), virtualized.</summary>
[TemplatePart("PART_Lines", typeof(ListBox))]
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
    public static readonly DirectProperty<HelixLogViewer, IEnumerable?> ViewProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, IEnumerable?>(nameof(View), v => v.View);
    public static readonly DirectProperty<HelixLogViewer, string> DebugLabelProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, string>(nameof(DebugLabel), v => v.DebugLabel);
    public static readonly DirectProperty<HelixLogViewer, string> InfoLabelProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, string>(nameof(InfoLabel), v => v.InfoLabel);
    public static readonly DirectProperty<HelixLogViewer, string> WarningLabelProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, string>(nameof(WarningLabel), v => v.WarningLabel);
    public static readonly DirectProperty<HelixLogViewer, string> ErrorLabelProperty = AvaloniaProperty.RegisterDirect<HelixLogViewer, string>(nameof(ErrorLabel), v => v.ErrorLabel);

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

    private DataGridCollectionView? _view;
    private int[] _counts = new int[4];
    private string _debugLabel = "", _infoLabel = "", _warningLabel = "", _errorLabel = "";

    /// <summary>The filtered lines (template).</summary>
    public IEnumerable? View { get => _view; private set { var old = _view; _view = (DataGridCollectionView?)value; RaisePropertyChanged(ViewProperty, old, _view); } }

    /// <summary>Lines per level: Debug, Info, Warning, Error.</summary>
    public IReadOnlyList<int> Counts => _counts;

    /// <summary>"Info 12": the level toggles (template).</summary>
    public string DebugLabel { get => _debugLabel; private set => SetAndRaise(DebugLabelProperty, ref _debugLabel, value); }
    public string InfoLabel { get => _infoLabel; private set => SetAndRaise(InfoLabelProperty, ref _infoLabel, value); }
    public string WarningLabel { get => _warningLabel; private set => SetAndRaise(WarningLabelProperty, ref _warningLabel, value); }
    public string ErrorLabel { get => _errorLabel; private set => SetAndRaise(ErrorLabelProperty, ref _errorLabel, value); }

    /// <summary>Number of lines shown after the level filters and the search.</summary>
    public int VisibleCount => _view?.Count ?? 0;

    public static string FollowText => HelixTexts.Current.FollowLog;
    public static string SearchPlaceholderText => HelixTexts.Current.Search;

    /// <summary>Formats a line's time with the viewer's TimeFormat (MultiBinding: time, format).</summary>
    public static readonly IMultiValueConverter TimeConverter = new FuncMultiValueConverter<object?, string>(values =>
        values.ToList() is [DateTime time, string format, ..] ? time.ToString(format, CultureInfo.CurrentCulture) : "");

    private readonly DispatcherTimer _refresh;
    private ListBox? _lines;

    public HelixLogViewer()
    {
        _refresh = new DispatcherTimer(TimeSpan.FromMilliseconds(120), DispatcherPriority.Background, (_, _) => Flush());
        _refresh.Stop();
        Recount();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _lines = e.NameScope.Find<ListBox>("PART_Lines");
        ScrollToEnd();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= OnLinesChanged;
            if (change.NewValue is INotifyCollectionChanged newList) newList.CollectionChanged += OnLinesChanged;
            var view = change.NewValue is IEnumerable<HelixLogEntry> lines ? new DataGridCollectionView(lines as IList ?? lines.ToList()) { Filter = Accept } : null;
            View = view;
            Recount();
            ScrollToEnd();
        }
        else if (change.Property == SearchTextProperty || change.Property == ShowDebugProperty || change.Property == ShowInfoProperty
                 || change.Property == ShowWarningProperty || change.Property == ShowErrorProperty)
        {
            _view?.Refresh();
            ScrollToEnd();
        }
        else if (change.Property == IsFollowingProperty) ScrollToEnd();
    }

    /// <summary>Lines may arrive in bursts: counts and scrolling are refreshed at most every 120 ms.</summary>
    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e) => _refresh.Start();

    /// <summary>Applies pending new lines now (the viewer does it every 120 ms); used by tests.</summary>
    public void Flush()
    {
        _refresh.Stop();
        Recount();
        ScrollToEnd();
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
        var term = SearchText?.Trim();
        return shown && (string.IsNullOrEmpty(term) || entry.Message.Contains(term, StringComparison.CurrentCultureIgnoreCase)
            || entry.Source?.Contains(term, StringComparison.CurrentCultureIgnoreCase) == true);
    }

    private void Recount()
    {
        var counts = new int[4];
        foreach (var entry in ItemsSource ?? []) counts[(int)entry.Level]++;
        _counts = counts;
        var texts = HelixTexts.Current;
        DebugLabel = $"{texts.LogDebug}  {counts[0]}";
        InfoLabel = $"{texts.LogInfo}  {counts[1]}";
        WarningLabel = $"{texts.LogWarning}  {counts[2]}";
        ErrorLabel = $"{texts.LogError}  {counts[3]}";
    }

    private void ScrollToEnd()
    {
        if (!IsFollowing || _lines is null || _view is null || _view.Count == 0) return;
        var last = _view.Count - 1;
        Dispatcher.UIThread.Post(() => _lines.ScrollIntoView(last), DispatcherPriority.Background);
    }
}
