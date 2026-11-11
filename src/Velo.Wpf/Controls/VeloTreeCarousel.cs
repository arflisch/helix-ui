using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Velo.Wpf;

/// <summary>
/// A tree as an indented list or as a diagram (org chart). Give it a HierarchicalDataTemplate as usual,
/// or just ChildrenPath + TextPath (and IconPath, DescriptionPath) and it builds one.
/// </summary>
public class VeloTreeView : TreeView
{
    static VeloTreeView() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloTreeView), new FrameworkPropertyMetadata(typeof(VeloTreeView)));

    public VeloTreeView() => Loaded += (_, _) => ExpandTo(this, ExpandDepth, 0);

    private static void TemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((VeloTreeView)d).UpdateTemplate();

    public static readonly DependencyProperty LayoutProperty = VeloProperty.Register<VeloTreeView, VeloTreeLayout>(nameof(Layout), VeloTreeLayout.List);
    public static readonly DependencyProperty ChildrenPathProperty = VeloProperty.Register<VeloTreeView, string?>(nameof(ChildrenPath), null, TemplateChanged);
    public static readonly DependencyProperty TextPathProperty = VeloProperty.Register<VeloTreeView, string?>(nameof(TextPath), null, TemplateChanged);
    public static readonly DependencyProperty IconPathProperty = VeloProperty.Register<VeloTreeView, string?>(nameof(IconPath), null, TemplateChanged);
    public static readonly DependencyProperty DescriptionPathProperty = VeloProperty.Register<VeloTreeView, string?>(nameof(DescriptionPath), null, TemplateChanged);
    public static readonly DependencyProperty ExpandDepthProperty = VeloProperty.Register<VeloTreeView, int>(nameof(ExpandDepth), 1);

    /// <summary>List (indented, like files) or Diagram (org chart, boxes linked top-down).</summary>
    public VeloTreeLayout Layout { get => (VeloTreeLayout)GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>Property with the children of an item, e.g. "Children".</summary>
    public string? ChildrenPath { get => (string?)GetValue(ChildrenPathProperty); set => SetValue(ChildrenPathProperty, value); }
    public string? TextPath { get => (string?)GetValue(TextPathProperty); set => SetValue(TextPathProperty, value); }

    /// <summary>Property with a built-in icon name.</summary>
    public string? IconPath { get => (string?)GetValue(IconPathProperty); set => SetValue(IconPathProperty, value); }
    public string? DescriptionPath { get => (string?)GetValue(DescriptionPathProperty); set => SetValue(DescriptionPathProperty, value); }

    /// <summary>Levels opened at start (1 = roots open).</summary>
    public int ExpandDepth { get => (int)GetValue(ExpandDepthProperty); set => SetValue(ExpandDepthProperty, value); }

    protected override DependencyObject GetContainerForItemOverride() => new VeloTreeViewItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is VeloTreeViewItem;

    private void UpdateTemplate()
    {
        if (ChildrenPath is null || ReadLocalValue(ItemTemplateProperty) is DataTemplate { } existing && existing is not VeloGeneratedTemplate)
        {
            return;
        }

        var template = new VeloGeneratedTemplate { ItemsSource = new Binding(ChildrenPath) };
        var root = new FrameworkElementFactory(typeof(DockPanel));
        if (IconPath is not null)
        {
            var icon = new FrameworkElementFactory(typeof(VeloIcon));
            icon.SetBinding(VeloIcon.IconProperty, new Binding(IconPath));
            icon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 1, 8, 0));
            icon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);
            icon.SetValue(DockPanel.DockProperty, Dock.Left);
            icon.SetResourceReference(VeloIcon.ForegroundProperty, VeloKeys.TextMuted);
            root.AppendChild(icon);
        }

        var stack = new FrameworkElementFactory(typeof(StackPanel));
        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, new Binding(TextPath ?? ""));
        stack.AppendChild(text);
        if (DescriptionPath is not null)
        {
            var description = new FrameworkElementFactory(typeof(TextBlock));
            description.SetBinding(TextBlock.TextProperty, new Binding(DescriptionPath));
            description.SetValue(TextBlock.FontSizeProperty, 12.0);
            description.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.TextMuted);
            stack.AppendChild(description);
        }

        root.AppendChild(stack);
        template.VisualTree = root;
        SetCurrentValue(ItemTemplateProperty, template);
    }

    private sealed class VeloGeneratedTemplate : HierarchicalDataTemplate;

    private static void ExpandTo(ItemsControl parent, int depth, int level)
    {
        if (level >= depth) return;
        parent.UpdateLayout();
        foreach (var item in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem container)
            {
                container.IsExpanded = true;
                ExpandTo(container, depth, level + 1);
            }
        }
    }
}

/// <summary>A tree node; knows its level (indent) and its position among siblings (diagram connectors).</summary>
public class VeloTreeViewItem : TreeViewItem
{
    static VeloTreeViewItem() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloTreeViewItem), new FrameworkPropertyMetadata(typeof(VeloTreeViewItem)));

    public VeloTreeViewItem() => Loaded += (_, _) => UpdatePosition();

    private static readonly DependencyPropertyKey LevelKey = DependencyProperty.RegisterReadOnly(nameof(Level), typeof(int), typeof(VeloTreeViewItem), new PropertyMetadata(0));
    private static readonly DependencyPropertyKey IndentKey = DependencyProperty.RegisterReadOnly(nameof(Indent), typeof(Thickness), typeof(VeloTreeViewItem), new PropertyMetadata(default(Thickness)));
    private static readonly DependencyPropertyKey SiblingPositionKey = DependencyProperty.RegisterReadOnly(nameof(SiblingPosition), typeof(string), typeof(VeloTreeViewItem), new PropertyMetadata("only"));
    public static readonly DependencyProperty LevelProperty = LevelKey.DependencyProperty;
    public static readonly DependencyProperty IndentProperty = IndentKey.DependencyProperty;
    public static readonly DependencyProperty SiblingPositionProperty = SiblingPositionKey.DependencyProperty;

    public int Level => (int)GetValue(LevelProperty);
    public Thickness Indent => (Thickness)GetValue(IndentProperty);

    /// <summary>"only", "first", "middle" or "last" among its siblings.</summary>
    public string SiblingPosition => (string)GetValue(SiblingPositionProperty);

    protected override DependencyObject GetContainerForItemOverride() => new VeloTreeViewItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is VeloTreeViewItem;

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        foreach (var item in Items)
        {
            (ItemContainerGenerator.ContainerFromItem(item) as VeloTreeViewItem)?.UpdatePosition();
        }
    }

    internal void UpdatePosition()
    {
        var level = 0;
        for (var parent = ItemsControlFromItemContainer(this); parent is TreeViewItem t; parent = ItemsControlFromItemContainer(t)) level++;
        SetValue(LevelKey, level);
        SetValue(IndentKey, new Thickness(level * 18, 0, 0, 0));

        var owner = ItemsControlFromItemContainer(this);
        if (owner is null) return;
        var index = owner.ItemContainerGenerator.IndexFromContainer(this);
        var count = owner.Items.Count;
        SetValue(SiblingPositionKey, count <= 1 ? "only" : index == 0 ? "first" : index == count - 1 ? "last" : "middle");
    }
}

/// <summary>
/// Slides one or more at a time: arrows, dots, keyboard, swipe, optional autoplay (pauses on hover and focus).
/// Put any content as items. SelectedIndex is two-way.
/// </summary>
[TemplatePart(Name = "PART_Prev", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_Next", Type = typeof(ButtonBase))]
[TemplatePart(Name = "PART_Dots", Type = typeof(Panel))]
[TemplatePart(Name = "PART_Play", Type = typeof(ButtonBase))]
public class VeloCarousel : ItemsControl
{
    static VeloCarousel() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloCarousel), new FrameworkPropertyMetadata(typeof(VeloCarousel)));

    public VeloCarousel()
    {
        _timer = new DispatcherTimer();
        _timer.Tick += (_, _) =>
        {
            if (!IsMouseOver && !IsKeyboardFocusWithin && !_paused) Go(SelectedIndex + 1, wrap: true);
        };
        Loaded += (_, _) => UpdateTimer();
        Unloaded += (_, _) => _timer.Stop();
    }

    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(VeloCarousel),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((VeloCarousel)d).OnIndexChanged()));
    public static readonly DependencyProperty SlidesPerViewProperty = VeloProperty.Register<VeloCarousel, int>(nameof(SlidesPerView), 1, (d, _) => ((VeloCarousel)d).OnIndexChanged());
    public static readonly DependencyProperty GapProperty = VeloProperty.Register<VeloCarousel, double>(nameof(Gap), 16d);
    public static readonly DependencyProperty AutoplayProperty = VeloProperty.Register<VeloCarousel, bool>(nameof(Autoplay), false, (d, _) => ((VeloCarousel)d).UpdateTimer());
    public static readonly DependencyProperty IntervalProperty = VeloProperty.Register<VeloCarousel, TimeSpan>(nameof(Interval), TimeSpan.FromSeconds(5), (d, _) => ((VeloCarousel)d).UpdateTimer());
    public static readonly DependencyProperty LoopProperty = VeloProperty.Register<VeloCarousel, bool>(nameof(Loop), true, (d, _) => ((VeloCarousel)d).OnIndexChanged());
    public static readonly DependencyProperty ShowArrowsProperty = VeloProperty.Register<VeloCarousel, bool>(nameof(ShowArrows), true);
    public static readonly DependencyProperty ShowDotsProperty = VeloProperty.Register<VeloCarousel, bool>(nameof(ShowDots), true);
    public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(nameof(Offset), typeof(double), typeof(VeloCarousel),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsArrange));

    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
    public int SlidesPerView { get => (int)GetValue(SlidesPerViewProperty); set => SetValue(SlidesPerViewProperty, value); }
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }
    public bool Autoplay { get => (bool)GetValue(AutoplayProperty); set => SetValue(AutoplayProperty, value); }
    public TimeSpan Interval { get => (TimeSpan)GetValue(IntervalProperty); set => SetValue(IntervalProperty, value); }

    /// <summary>The arrows wrap from the last slide to the first.</summary>
    public bool Loop { get => (bool)GetValue(LoopProperty); set => SetValue(LoopProperty, value); }
    public bool ShowArrows { get => (bool)GetValue(ShowArrowsProperty); set => SetValue(ShowArrowsProperty, value); }
    public bool ShowDots { get => (bool)GetValue(ShowDotsProperty); set => SetValue(ShowDotsProperty, value); }

    /// <summary>Animated position (in slides) read by the panel.</summary>
    public double Offset { get => (double)GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    private readonly DispatcherTimer _timer;
    private ButtonBase? _prev, _next, _play;
    private Panel? _dots;
    private bool _paused;
    private Point? _swipeStart;

    private int LastStart => Math.Max(0, Items.Count - Math.Max(1, SlidesPerView));

    public override void OnApplyTemplate()
    {
        if (_prev is not null) _prev.Click -= OnPrev;
        if (_next is not null) _next.Click -= OnNext;
        if (_play is not null) _play.Click -= OnPlay;
        base.OnApplyTemplate();
        _prev = GetTemplateChild("PART_Prev") as ButtonBase;
        _next = GetTemplateChild("PART_Next") as ButtonBase;
        _play = GetTemplateChild("PART_Play") as ButtonBase;
        _dots = GetTemplateChild("PART_Dots") as Panel;
        if (_prev is not null) _prev.Click += OnPrev;
        if (_next is not null) _next.Click += OnNext;
        if (_play is not null) _play.Click += OnPlay;
        OnIndexChanged();
    }

    private void OnPrev(object sender, RoutedEventArgs e) => Go(SelectedIndex - 1, Loop);
    private void OnNext(object sender, RoutedEventArgs e) => Go(SelectedIndex + 1, Loop);

    private void OnPlay(object sender, RoutedEventArgs e)
    {
        _paused = !_paused;
        UpdatePlayIcon();
    }

    private void UpdatePlayIcon()
    {
        if (_play is not null)
        {
            _play.Content = new VeloIcon { Icon = _paused ? "arrow-right" : "minus", Size = 14 };
            _play.ToolTip = _paused ? "▶" : "❚❚";
        }
    }

    private void UpdateTimer()
    {
        _timer.Interval = Interval;
        if (Autoplay && IsLoaded) _timer.Start(); else _timer.Stop();
        if (_play is not null) _play.Visibility = Autoplay ? Visibility.Visible : Visibility.Collapsed;
        UpdatePlayIcon();
    }

    /// <summary>Goes to a slide (clamped, or wrapped when <paramref name="wrap"/>).</summary>
    public void Go(int index, bool wrap = false)
    {
        var last = LastStart;
        if (wrap) index = index > last ? 0 : index < 0 ? last : index;
        SetCurrentValue(SelectedIndexProperty, Math.Clamp(index, 0, last));
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        OnIndexChanged();
    }

    private void OnIndexChanged()
    {
        var index = Math.Clamp(SelectedIndex, 0, LastStart);
        var animation = new DoubleAnimation(index, SystemParameters.ClientAreaAnimation ? TimeSpan.FromMilliseconds(380) : TimeSpan.Zero)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
        };
        BeginAnimation(OffsetProperty, animation);
        if (_prev is not null) _prev.IsEnabled = Loop || index > 0;
        if (_next is not null) _next.IsEnabled = Loop || index < LastStart;
        BuildDots(index);
    }

    private void BuildDots(int active)
    {
        if (_dots is null) return;
        _dots.Children.Clear();
        _dots.Visibility = ShowDots && Items.Count > SlidesPerView ? Visibility.Visible : Visibility.Collapsed;
        for (var i = 0; i <= LastStart; i++)
        {
            var target = i;
            var dot = new Button
            {
                Width = target == active ? 22 : 8,
                Height = 8,
                Margin = new Thickness(3, 0, 3, 0),
                Cursor = Cursors.Hand,
                Template = DotTemplate,
                ToolTip = $"{target + 1} / {LastStart + 1}",
            };
            dot.SetResourceReference(Control.BackgroundProperty, target == active ? VeloKeys.Primary : VeloKeys.BorderStrong);
            dot.Click += (_, _) => Go(target);
            _dots.Children.Add(dot);
        }
    }

    private static readonly ControlTemplate DotTemplate = CreateDotTemplate();

    private static ControlTemplate CreateDotTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        border.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
        var template = new ControlTemplate(typeof(Button)) { VisualTree = border };
        template.Seal();
        return template;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Left) { Go(SelectedIndex - 1, Loop); e.Handled = true; }
        if (e.Key == Key.Right) { Go(SelectedIndex + 1, Loop); e.Handled = true; }
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        _swipeStart = e.GetPosition(this);
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        if (_swipeStart is { } start)
        {
            var dx = e.GetPosition(this).X - start.X;
            if (Math.Abs(dx) > 60) Go(SelectedIndex + (dx < 0 ? 1 : -1), Loop);
        }

        _swipeStart = null;
    }
}

/// <summary>Places carousel slides side by side, SlidesPerView at a time, shifted by the carousel's animated Offset.</summary>
public class VeloCarouselPanel : Panel
{
    private VeloCarousel? Owner => ItemsControl.GetItemsOwner(this) as VeloCarousel;

    public VeloCarouselPanel() => ClipToBounds = true;

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
        foreach (UIElement child in InternalChildren)
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
        for (var i = 0; i < InternalChildren.Count; i++)
        {
            InternalChildren[i].Arrange(new Rect((i - offset) * (slide + gap), 0, slide, finalSize.Height));
        }

        return finalSize;
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == VeloCarousel.OffsetProperty) InvalidateArrange();
    }

    static VeloCarouselPanel()
    {
        // Receive the carousel's inherited Offset so its animation re-arranges the slides.
        VeloCarousel.OffsetProperty.AddOwner(typeof(VeloCarouselPanel),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsArrange));
    }
}
