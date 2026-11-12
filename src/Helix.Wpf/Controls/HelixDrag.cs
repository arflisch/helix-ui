using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Helix.Wpf;

/// <summary>
/// A list reordered by dragging (or Alt + ↑/↓ on the selected item). Works on ItemsSource lists (IList, e.g. ObservableCollection)
/// and on items declared in XAML. Reordered gives (From, To).
/// </summary>
public class HelixSortableList : ListBox
{
    static HelixSortableList() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixSortableList), new FrameworkPropertyMetadata(typeof(HelixSortableList)));

    public HelixSortableList() => AllowDrop = true;

    public static readonly DependencyProperty ShowHandleProperty = HelixProperty.Register<HelixSortableList, bool>(nameof(ShowHandle), true);
    public static readonly RoutedEvent ReorderedEvent = EventManager.RegisterRoutedEvent(nameof(Reordered), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixSortableList));

    /// <summary>A grip on the left of each row.</summary>
    public bool ShowHandle { get => (bool)GetValue(ShowHandleProperty); set => SetValue(ShowHandleProperty, value); }

    /// <summary>Raised with <see cref="HelixEventArgs{T}"/> of (int From, int To) after a move.</summary>
    public event RoutedEventHandler Reordered { add => AddHandler(ReorderedEvent, value); remove => RemoveHandler(ReorderedEvent, value); }

    private Point _down;
    private object? _dragged;
    private int _dragFrom = -1;

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        _down = e.GetPosition(this);
        _dragged = ContainerAt(e.OriginalSource as DependencyObject) is { } container ? ItemContainerGenerator.ItemFromContainer(container) : null;
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (_dragged is null || e.LeftButton != MouseButtonState.Pressed || !IsEnabled)
        {
            return;
        }

        var delta = e.GetPosition(this) - _down;
        if (Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance && Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance)
        {
            return;
        }

        _dragFrom = Items.IndexOf(_dragged);
        var item = _dragged;
        _dragged = null;
        DragDrop.DoDragDrop(this, new DataObject(typeof(HelixSortableList), item), DragDropEffects.Move);
        var to = Items.IndexOf(item);
        if (_dragFrom >= 0 && to >= 0 && to != _dragFrom)
        {
            RaiseEvent(new HelixEventArgs<(int From, int To)>(ReorderedEvent, (_dragFrom, to)));
        }

        _dragFrom = -1;
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        if (e.Data.GetData(typeof(HelixSortableList)) is not { } item || !Items.Contains(item))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        // Live reorder: the dragged row takes the place of the row under the pointer.
        if (ContainerAt(e.OriginalSource as DependencyObject) is { } target)
        {
            var to = ItemContainerGenerator.IndexFromContainer(target);
            var from = Items.IndexOf(item);
            if (to >= 0 && from >= 0 && to != from)
            {
                Move(from, to);
            }
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Alt && SelectedItem is { } item && (e.SystemKey is Key.Up or Key.Down))
        {
            var from = Items.IndexOf(item);
            var to = Math.Clamp(from + (e.SystemKey == Key.Up ? -1 : 1), 0, Items.Count - 1);
            if (to != from)
            {
                Move(from, to);
                SelectedItem = item;
                (ItemContainerGenerator.ContainerFromItem(item) as UIElement)?.Focus();
                RaiseEvent(new HelixEventArgs<(int From, int To)>(ReorderedEvent, (from, to)));
            }

            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void Move(int from, int to)
    {
        if (ItemsSource is IList list && !list.IsReadOnly && !list.IsFixedSize)
        {
            if (list is System.Collections.ObjectModel.ObservableCollection<object> oc)
            {
                oc.Move(from, to);
                return;
            }

            var method = list.GetType().GetMethod("Move", [typeof(int), typeof(int)]);
            if (method is not null)
            {
                method.Invoke(list, [from, to]); // ObservableCollection<T>.Move keeps selection and containers
                return;
            }

            var value = list[from];
            list.RemoveAt(from);
            list.Insert(to, value);
        }
        else if (ItemsSource is null)
        {
            var value = Items[from];
            Items.RemoveAt(from);
            Items.Insert(to, value);
        }
    }

    private ListBoxItem? ContainerAt(DependencyObject? source)
    {
        while (source is not null && source is not ListBoxItem)
        {
            source = source is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
        }

        return source as ListBoxItem;
    }
}

/// <summary>
/// A board of columns with cards dragged between them. Columns describe the board; ItemsSource holds the cards;
/// ColumnPath (or ColumnOf) says which column a card is in. CardTemplate draws a card.
/// CardMoved proposes each move; if nobody handles it and ColumnPath is writable, the board moves the card itself.
/// </summary>
[TemplatePart(Name = "PART_Columns", Type = typeof(Panel))]
public class HelixKanban : Control, IHelixThemeAware
{
    static HelixKanban() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixKanban), new FrameworkPropertyMetadata(typeof(HelixKanban)));

    public HelixKanban() => HelixTheme.Watch(this);

    private static void Rebuild(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((HelixKanban)d).Build();

    public static readonly DependencyProperty ColumnsProperty = HelixProperty.Register<HelixKanban, IEnumerable<HelixKanbanColumn>?>(nameof(Columns), null, Rebuild);
    public static readonly DependencyProperty ItemsSourceProperty = HelixProperty.Register<HelixKanban, IEnumerable?>(nameof(ItemsSource), null, (d, e) => ((HelixKanban)d).OnItemsSourceChanged(e));
    public static readonly DependencyProperty ColumnPathProperty = HelixProperty.Register<HelixKanban, string?>(nameof(ColumnPath), null, Rebuild);
    public static readonly DependencyProperty ColumnOfProperty = HelixProperty.Register<HelixKanban, Func<object, string>?>(nameof(ColumnOf), null, Rebuild);
    public static readonly DependencyProperty CardTemplateProperty = HelixProperty.Register<HelixKanban, DataTemplate?>(nameof(CardTemplate), null, Rebuild);
    public static readonly DependencyProperty CanMoveProperty = HelixProperty.Register<HelixKanban, Func<object, string, bool>?>(nameof(CanMove), null);
    public static readonly DependencyProperty ColumnWidthProperty = HelixProperty.Register<HelixKanban, double>(nameof(ColumnWidth), 280d, Rebuild);
    public static readonly DependencyProperty EmptyTextProperty = HelixProperty.Register<HelixKanban, string?>(nameof(EmptyText), null, Rebuild);
    public static readonly DependencyProperty ShowAddButtonProperty = HelixProperty.Register<HelixKanban, bool>(nameof(ShowAddButton), false, Rebuild);
    public static readonly RoutedEvent CardMovedEvent = EventManager.RegisterRoutedEvent(nameof(CardMoved), RoutingStrategy.Bubble, typeof(EventHandler<HelixKanbanMoveEventArgs>), typeof(HelixKanban));
    public static readonly RoutedEvent CardClickEvent = EventManager.RegisterRoutedEvent(nameof(CardClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixKanban));
    public static readonly RoutedEvent AddRequestedEvent = EventManager.RegisterRoutedEvent(nameof(AddRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixKanban));

    public IEnumerable<HelixKanbanColumn>? Columns { get => (IEnumerable<HelixKanbanColumn>?)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>Property holding the column key of a card, e.g. "Status".</summary>
    public string? ColumnPath { get => (string?)GetValue(ColumnPathProperty); set => SetValue(ColumnPathProperty, value); }

    /// <summary>Column key of a card, from code (instead of ColumnPath).</summary>
    public Func<object, string>? ColumnOf { get => (Func<object, string>?)GetValue(ColumnOfProperty); set => SetValue(ColumnOfProperty, value); }
    public DataTemplate? CardTemplate { get => (DataTemplate?)GetValue(CardTemplateProperty); set => SetValue(CardTemplateProperty, value); }

    /// <summary>Whether a card may go to a column (e.g. WIP limits, permissions).</summary>
    public Func<object, string, bool>? CanMove { get => (Func<object, string, bool>?)GetValue(CanMoveProperty); set => SetValue(CanMoveProperty, value); }
    public double ColumnWidth { get => (double)GetValue(ColumnWidthProperty); set => SetValue(ColumnWidthProperty, value); }
    public string? EmptyText { get => (string?)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    /// <summary>A "+" in each column header raising AddRequested with the column key.</summary>
    public bool ShowAddButton { get => (bool)GetValue(ShowAddButtonProperty); set => SetValue(ShowAddButtonProperty, value); }

    public event EventHandler<HelixKanbanMoveEventArgs> CardMoved { add => AddHandler(CardMovedEvent, value); remove => RemoveHandler(CardMovedEvent, value); }

    /// <summary>Raised with <see cref="HelixEventArgs{T}"/> of the card.</summary>
    public event RoutedEventHandler CardClick { add => AddHandler(CardClickEvent, value); remove => RemoveHandler(CardClickEvent, value); }

    /// <summary>Raised with <see cref="HelixEventArgs{T}"/> of the column key.</summary>
    public event RoutedEventHandler AddRequested { add => AddHandler(AddRequestedEvent, value); remove => RemoveHandler(AddRequestedEvent, value); }

    private Panel? _columns;
    private Border? _placeholder;
    private Point _down;
    private object? _pressed;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _columns = GetTemplateChild("PART_Columns") as Panel;
        Build();
    }

    void IHelixThemeAware.OnThemeChanged() => Build();

    private void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newList) newList.CollectionChanged += OnCollectionChanged;
        Build();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Build();

    private string KeyOf(object item) =>
        ColumnOf?.Invoke(item) ?? (ColumnPath is { } path ? HelixDataGrid.Value(item, path)?.ToString() ?? "" : "");

    /// <summary>Draws the board again (e.g. after changing a card's properties).</summary>
    public void Build()
    {
        if (_columns is null) return;
        _columns.Children.Clear();
        var items = ItemsSource?.Cast<object>().ToList() ?? [];
        foreach (var column in Columns ?? [])
        {
            _columns.Children.Add(BuildColumn(column, items.Where(i => KeyOf(i) == column.Key).ToList()));
        }
    }

    private FrameworkElement BuildColumn(HelixKanbanColumn column, List<object> cards)
    {
        var root = new Border { Width = ColumnWidth, Margin = new Thickness(0, 0, 14, 0), CornerRadius = new CornerRadius(14), Padding = new Thickness(10) };
        root.SetResourceReference(Border.BackgroundProperty, HelixKeys.Surface2);

        var header = new DockPanel { Margin = new Thickness(4, 2, 2, 10) };
        if (ShowAddButton)
        {
            var add = new HelixButton { Icon = "plus", Variant = HelixVariant.Ghost, Tone = HelixTone.Neutral, Size = HelixSize.Sm, Width = 28, Height = 28, Padding = new Thickness(0), ToolTip = HelixTexts.Current.Add };
            add.Click += (_, _) => RaiseEvent(new HelixEventArgs<string>(AddRequestedEvent, column.Key));
            DockPanel.SetDock(add, Dock.Right);
            header.Children.Add(add);
        }

        var count = new Border { CornerRadius = new CornerRadius(999), Padding = new Thickness(7, 1, 7, 1), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        count.SetResourceReference(Border.BackgroundProperty, HelixKeys.Surface3);
        var over = column.Limit is { } limit && cards.Count > limit;
        var countText = new TextBlock { Text = column.Limit is { } l ? $"{cards.Count} / {l}" : cards.Count.ToString(CultureInfo.CurrentCulture), FontSize = 12, FontWeight = FontWeights.SemiBold };
        countText.SetResourceReference(TextBlock.ForegroundProperty, over ? HelixKeys.Danger : HelixKeys.TextMuted);
        count.Child = countText;
        var title = new StackPanel { Orientation = Orientation.Horizontal };
        var dot = new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, Fill = HelixTheme.Resolve(this, column.Color) ?? HelixTheme.Brush(this, HelixKeys.TextSubtle, Colors.Gray) };
        var name = new TextBlock { Text = column.Title, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        name.SetResourceReference(TextBlock.ForegroundProperty, HelixKeys.Text);
        title.Children.Add(dot);
        title.Children.Add(name);
        title.Children.Add(count);
        header.Children.Add(title);

        var list = new StackPanel { MinHeight = 60, Background = Brushes.Transparent, AllowDrop = true, Tag = column.Key };
        foreach (var card in cards)
        {
            list.Children.Add(BuildCard(card));
        }

        if (cards.Count == 0)
        {
            var empty = new Border { CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1.5), Padding = new Thickness(10, 18, 10, 18), IsHitTestVisible = false };
            empty.SetResourceReference(Border.BorderBrushProperty, HelixKeys.BorderStrong);
            var emptyText = new TextBlock { Text = EmptyText ?? HelixTexts.Current.DropCardHere, FontSize = 12.5, HorizontalAlignment = HorizontalAlignment.Center };
            emptyText.SetResourceReference(TextBlock.ForegroundProperty, HelixKeys.TextSubtle);
            empty.Child = emptyText;
            list.Children.Add(empty);
        }

        list.DragOver += (_, e) => OnColumnDragOver(list, e);
        list.DragLeave += (_, _) => RemovePlaceholder();
        list.Drop += (_, e) => OnColumnDrop(list, column, e);

        var dock = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);
        dock.Children.Add(new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Focusable = false });
        root.Child = dock;
        return root;
    }

    private FrameworkElement BuildCard(object item)
    {
        var content = new ContentPresenter { Content = item, ContentTemplate = CardTemplate };
        var card = new Border
        {
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = Cursors.Hand,
            Child = content,
            Tag = item,
            Focusable = true,
            FocusVisualStyle = null,
        };
        card.SetResourceReference(Border.BackgroundProperty, HelixKeys.Surface);
        card.SetResourceReference(Border.BorderBrushProperty, HelixKeys.Border);
        card.SetResourceReference(TextElement.ForegroundProperty, HelixKeys.Text);
        card.MouseEnter += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, HelixKeys.BorderStrong);
        card.MouseLeave += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, HelixKeys.Border);
        card.GotKeyboardFocus += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, HelixKeys.Primary);
        card.LostKeyboardFocus += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, HelixKeys.Border);
        card.PreviewMouseLeftButtonDown += (_, e) =>
        {
            _down = e.GetPosition(this);
            _pressed = item;
        };
        card.PreviewMouseMove += (_, e) =>
        {
            if (_pressed != item || e.LeftButton != MouseButtonState.Pressed) return;
            var delta = e.GetPosition(this) - _down;
            if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            _pressed = null;
            card.Opacity = 0.4;
            DragDrop.DoDragDrop(card, new DataObject(typeof(HelixKanban), item), DragDropEffects.Move);
            card.Opacity = 1;
            RemovePlaceholder();
        };
        card.MouseLeftButtonUp += (_, _) =>
        {
            if (_pressed == item) RaiseEvent(new HelixEventArgs<object>(CardClickEvent, item));
            _pressed = null;
        };
        card.KeyDown += (_, e) =>
        {
            if (e.Key is Key.Enter or Key.Space) RaiseEvent(new HelixEventArgs<object>(CardClickEvent, item));
        };
        return card;
    }

    private int DropIndex(Panel list, DragEventArgs e)
    {
        var y = e.GetPosition(list).Y;
        var index = 0;
        foreach (var child in list.Children.OfType<Border>().Where(b => b.Tag is not null && b != _placeholder))
        {
            var top = child.TranslatePoint(new Point(), list).Y;
            if (y > top + child.ActualHeight / 2) index++;
        }

        return index;
    }

    private void OnColumnDragOver(Panel list, DragEventArgs e)
    {
        var item = e.Data.GetData(typeof(HelixKanban));
        var key = (string)list.Tag;
        if (item is null || CanMove?.Invoke(item, key) == false)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var index = DropIndex(list, e);
        RemovePlaceholder();
        _placeholder = new Border { Height = 44, CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1.5), Margin = new Thickness(0, 0, 0, 8), IsHitTestVisible = false };
        _placeholder.SetResourceReference(Border.BorderBrushProperty, HelixKeys.Primary);
        _placeholder.SetResourceReference(Border.BackgroundProperty, HelixKeys.PrimarySoft);
        var cards = list.Children.OfType<Border>().Where(b => b.Tag is not null).ToList();
        var at = index < cards.Count ? list.Children.IndexOf(cards[index]) : list.Children.Count;
        list.Children.Insert(at, _placeholder);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void RemovePlaceholder()
    {
        if (_placeholder?.Parent is Panel parent) parent.Children.Remove(_placeholder);
        _placeholder = null;
    }

    private void OnColumnDrop(Panel list, HelixKanbanColumn column, DragEventArgs e)
    {
        var item = e.Data.GetData(typeof(HelixKanban));
        RemovePlaceholder();
        if (item is null || CanMove?.Invoke(item, column.Key) == false) return;
        var index = DropIndex(list, e);
        var args = new HelixKanbanMoveEventArgs(CardMovedEvent, item, KeyOf(item), column.Key, index);
        RaiseEvent(args);
        if (!args.Handled)
        {
            ApplyMove(item, column.Key, index);
        }

        e.Handled = true;
    }

    /// <summary>Default move: writes ColumnPath and reorders an IList source.</summary>
    private void ApplyMove(object item, string to, int index)
    {
        if (ColumnPath is { } path && !path.Contains('.') && item.GetType().GetProperty(path) is { CanWrite: true } property)
        {
            property.SetValue(item, property.PropertyType == typeof(string) ? to : Convert.ChangeType(to, property.PropertyType, CultureInfo.InvariantCulture));
        }

        if (ItemsSource is IList list && !list.IsReadOnly && !list.IsFixedSize)
        {
            var targetCards = list.Cast<object>().Where(i => i != item && KeyOf(i) == to).ToList();
            list.Remove(item);
            var at = index < targetCards.Count ? list.IndexOf(targetCards[index]) : (targetCards.Count > 0 ? list.IndexOf(targetCards[^1]) + 1 : list.Count);
            list.Insert(Math.Clamp(at, 0, list.Count), item);
        }

        if (ItemsSource is not INotifyCollectionChanged)
        {
            Build();
        }
    }
}

/// <summary>How a <see cref="HelixDashboard"/> places its tiles.</summary>
public enum HelixDashboardLayout
{
    /// <summary>Tiles flow in rows; a row is as tall as its tallest tile.</summary>
    Rows,

    /// <summary>Each tile sits in a column (Column, Span) and stacks under the tiles above it: no gaps between short and tall tiles.</summary>
    Columns,
}

/// <summary>
/// A dashboard of tiles on a grid of ColumnCount columns. In edit mode, tiles are dragged to another place
/// (or moved with the arrow buttons), resized (span) and hidden; Layout holds the user's arrangement
/// (bind it two-way, or handle LayoutChanged, to save it).
/// </summary>
public class HelixDashboard : ItemsControl
{
    static HelixDashboard() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixDashboard), new FrameworkPropertyMetadata(typeof(HelixDashboard)));

    public HelixDashboard()
    {
        Loaded += (_, _) => ApplyLayout();
        AllowDrop = true;
    }

    private static void Relayout(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((HelixDashboard)d).Panel?.InvalidateMeasure();

    public static readonly DependencyProperty ColumnCountProperty = HelixProperty.Register<HelixDashboard, int>(nameof(ColumnCount), 3, Relayout);
    public static readonly DependencyProperty GapProperty = HelixProperty.Register<HelixDashboard, double>(nameof(Gap), 16d, Relayout);
    public static readonly DependencyProperty LayoutModeProperty = HelixProperty.Register<HelixDashboard, HelixDashboardLayout>(nameof(LayoutMode), HelixDashboardLayout.Rows, Relayout);
    public static readonly DependencyProperty ColumnMinWidthProperty = HelixProperty.Register<HelixDashboard, double>(nameof(ColumnMinWidth), 0d, Relayout);
    public static readonly DependencyProperty StretchColumnsProperty = HelixProperty.Register<HelixDashboard, bool>(nameof(StretchColumns), false, Relayout);
    public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(HelixDashboard),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty LayoutProperty = DependencyProperty.Register(nameof(Layout), typeof(IReadOnlyList<HelixTileState>), typeof(HelixDashboard),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HelixDashboard)d).ApplyLayout()));
    public static readonly DependencyProperty TitleProperty = HelixProperty.Register<HelixDashboard, string?>(nameof(Title), null);
    public static readonly DependencyProperty ToolbarProperty = HelixProperty.Register<HelixDashboard, object?>(nameof(Toolbar), null);
    public static readonly DependencyProperty ShowToolbarProperty = HelixProperty.Register<HelixDashboard, bool>(nameof(ShowToolbar), true);
    public static readonly RoutedEvent LayoutChangedEvent = EventManager.RegisterRoutedEvent(nameof(LayoutChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixDashboard));

    private static readonly DependencyPropertyKey HiddenTilesKey = DependencyProperty.RegisterReadOnly(nameof(HiddenTiles), typeof(IReadOnlyList<HelixDashboardTile>), typeof(HelixDashboard), new PropertyMetadata(Array.Empty<HelixDashboardTile>()));
    public static readonly DependencyProperty HiddenTilesProperty = HiddenTilesKey.DependencyProperty;

    /// <summary>Grid columns (3 by default); a tile spans 1 to ColumnCount.</summary>
    public int ColumnCount { get => (int)GetValue(ColumnCountProperty); set => SetValue(ColumnCountProperty, value); }
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }

    /// <summary>Rows (default) or Columns (tiles stack in their column, like cards on a board).</summary>
    public HelixDashboardLayout LayoutMode { get => (HelixDashboardLayout)GetValue(LayoutModeProperty); set => SetValue(LayoutModeProperty, value); }

    /// <summary>Minimum column width: below it the dashboard gets wider than its container (put it in a horizontal ScrollViewer).</summary>
    public double ColumnMinWidth { get => (double)GetValue(ColumnMinWidthProperty); set => SetValue(ColumnMinWidthProperty, value); }

    /// <summary>Columns layout: the last tile of each column grows so that all columns end at the same height.</summary>
    public bool StretchColumns { get => (bool)GetValue(StretchColumnsProperty); set => SetValue(StretchColumnsProperty, value); }
    public bool IsEditing { get => (bool)GetValue(IsEditingProperty); set => SetValue(IsEditingProperty, value); }

    /// <summary>Order, column, span and visibility of the tiles; updated after each user change.</summary>
    public IReadOnlyList<HelixTileState>? Layout { get => (IReadOnlyList<HelixTileState>?)GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? Toolbar { get => GetValue(ToolbarProperty); set => SetValue(ToolbarProperty, value); }

    /// <summary>The title row with "Edit layout" (and your Toolbar).</summary>
    public bool ShowToolbar { get => (bool)GetValue(ShowToolbarProperty); set => SetValue(ShowToolbarProperty, value); }
    public IReadOnlyList<HelixDashboardTile> HiddenTiles => (IReadOnlyList<HelixDashboardTile>)GetValue(HiddenTilesProperty);

    /// <summary>Raised after the user moved, resized, hid or restored a tile; read <see cref="Layout"/> to save it.</summary>
    public event RoutedEventHandler LayoutChanged { add => AddHandler(LayoutChangedEvent, value); remove => RemoveHandler(LayoutChangedEvent, value); }

    public static string EditText => HelixTexts.Current.EditLayout;
    public static string DoneText => HelixTexts.Current.Done;
    public static string HiddenText => HelixTexts.Current.HiddenTiles;

    /// <summary>Toggles edit mode (the toolbar button).</summary>
    public ICommand ToggleEditCommand => new HelixCommand(() => SetCurrentValue(IsEditingProperty, !IsEditing));

    /// <summary>The items panel (set by HelixDashboardPanel when it lays out).</summary>
    internal HelixDashboardPanel? Panel { get; set; }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is HelixDashboardTile;

    protected override DependencyObject GetContainerForItemOverride() => new HelixDashboardTile();

    internal IEnumerable<HelixDashboardTile> Tiles => Items.Cast<object>().Select(i => i as HelixDashboardTile ?? ItemContainerGenerator.ContainerFromItem(i) as HelixDashboardTile).OfType<HelixDashboardTile>();

    private bool _applying;

    private void ApplyLayout()
    {
        if (Layout is not { } layout || ItemsSource is not null)
        {
            UpdateHidden();
            return;
        }

        _applying = true;
        try
        {
            var tiles = Tiles.ToList();
            var ordered = layout.Select(s => tiles.FirstOrDefault(t => t.TileId == s.Id)).OfType<HelixDashboardTile>()
                .Concat(tiles.Where(t => layout.All(s => s.Id != t.TileId))).ToList();
            foreach (var state in layout)
            {
                if (tiles.FirstOrDefault(t => t.TileId == state.Id) is not { } tile) continue;
                tile.Span = state.Span;
                tile.Column = state.Column;

                // Only tiles the user can hide take the saved visibility: the others keep their own (often a binding).
                if (tile.CanHide) tile.IsHidden = state.Hidden;
            }

            if (!ordered.SequenceEqual(tiles))
            {
                Items.Clear();
                foreach (var tile in ordered) Items.Add(tile);
            }
        }
        finally
        {
            _applying = false;
        }

        UpdateHidden();
    }

    private void SaveLayout()
    {
        if (_applying) return;
        SetCurrentValue(LayoutProperty, Tiles.Select(t => new HelixTileState(t.TileId ?? "", t.Span, t.CanHide && t.IsHidden, t.Column)).ToList());
        UpdateHidden();
        RaiseEvent(new RoutedEventArgs(LayoutChangedEvent, this));
    }

    private void UpdateHidden() => SetValue(HiddenTilesKey, Tiles.Where(t => t.IsHidden && t.CanHide).ToList());

    /// <summary>Moves a tile one place earlier (-1) or later (+1) in the order (in its column for the Columns layout).</summary>
    internal void Shift(HelixDashboardTile tile, int direction)
    {
        if (ItemsSource is not null) return;
        var index = Items.IndexOf(tile);
        if (index < 0) return;

        var target = index + direction;
        if (LayoutMode == HelixDashboardLayout.Columns && Panel is { } panel)
        {
            // Swap with the previous / next visible tile of the same column.
            var column = panel.ColumnOf(tile);
            var sameColumn = Items.OfType<HelixDashboardTile>().Where(t => t != tile && !t.IsHidden && panel.ColumnOf(t) == column).ToList();
            var neighbour = direction < 0
                ? sameColumn.LastOrDefault(t => Items.IndexOf(t) < index)
                : sameColumn.FirstOrDefault(t => Items.IndexOf(t) > index);
            if (neighbour is null) return;
            target = Items.IndexOf(neighbour);
        }

        target = Math.Clamp(target, 0, Items.Count - 1);
        if (target == index) return;
        Items.RemoveAt(index);
        Items.Insert(target, tile);
        SaveLayout();
        tile.Focus();
    }

    /// <summary>Columns layout: moves a tile to the column on its left (-1) or right (+1).</summary>
    internal void ShiftColumn(HelixDashboardTile tile, int direction)
    {
        var current = Panel?.ColumnOf(tile) ?? Math.Max(0, tile.Column);
        var target = Math.Clamp(current + direction, 0, Math.Max(0, ColumnCount - Math.Clamp(tile.Span, 1, ColumnCount)));
        if (target == current && tile.Column >= 0) return;
        tile.Column = target;
        SaveLayout();
        tile.Focus();
    }

    internal void Resize(HelixDashboardTile tile, int delta)
    {
        tile.Span = Math.Clamp(tile.Span + delta, Math.Max(1, tile.MinSpan), Math.Min(ColumnCount, tile.MaxSpan));
        if (tile.Column >= 0) tile.Column = Math.Min(tile.Column, ColumnCount - tile.Span);
        SaveLayout();
    }

    internal void SetHidden(HelixDashboardTile tile, bool hidden)
    {
        tile.IsHidden = hidden;
        SaveLayout();
    }

    /// <summary>Shows a hidden tile again (the hidden-tiles chips).</summary>
    public ICommand RestoreCommand => new HelixCommand<HelixDashboardTile>(t => SetHidden(t, false));

    // ----- Drag and drop (edit mode) -----------------------------------------------------------

    private HelixDashboardTile? _dragging;
    private HelixDashboardTile? _dropBefore;
    private bool _dropToEnd;

    internal void BeginDrag(HelixDashboardTile tile)
    {
        if (!IsEditing || ItemsSource is not null) return;
        _dragging = tile;
        _dropBefore = null;
        _dropToEnd = false;
        var startColumn = tile.Column;
        tile.IsDragging = true;
        DragDropEffects result;
        try
        {
            result = DragDrop.DoDragDrop(tile, new DataObject(typeof(HelixDashboardTile), tile), DragDropEffects.Move);
        }
        finally
        {
            tile.IsDragging = false;
            _dragging = null;
        }

        if (result != DragDropEffects.Move)
        {
            tile.Column = startColumn; // cancelled (Escape or dropped outside)
            return;
        }

        // The order changes once the drag is over, so the dragged element is never re-parented during the drag.
        var from = Items.IndexOf(tile);
        if (from >= 0 && (_dropBefore is not null || _dropToEnd) && _dropBefore != tile)
        {
            Items.RemoveAt(from);
            var to = _dropBefore is null ? Items.Count : Items.IndexOf(_dropBefore);
            Items.Insert(Math.Clamp(to, 0, Items.Count), tile);
        }

        SaveLayout();
        tile.Focus();
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        if (_dragging is not { } tile || Panel is not { } panel || e.Data.GetData(typeof(HelixDashboardTile)) != tile)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var (column, before) = panel.DropTarget(tile, e.GetPosition(panel));

        // The column follows the pointer live; the place in the column is applied on drop.
        if (LayoutMode == HelixDashboardLayout.Columns && column != tile.Column)
        {
            tile.Column = column;
        }

        _dropBefore = before;
        _dropToEnd = before is null;
        panel.ShowDropMarker(before, column, tile);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    protected override void OnDragLeave(DragEventArgs e)
    {
        base.OnDragLeave(e);
        Panel?.ShowDropMarker(null, -1, null);
    }

    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);
        Panel?.ShowDropMarker(null, -1, null);
        e.Effects = _dragging is not null ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }
}

/// <summary>
/// A dashboard tile: TileId (for the saved layout), Title, Actions, DefaultSpan / MinSpan / MaxSpan, Column (Columns layout),
/// CanHide, IsPlain. In edit mode, drag it by its header to move it.
/// </summary>
[TemplatePart(Name = "PART_Handle", Type = typeof(FrameworkElement))]
public class HelixDashboardTile : ContentControl
{
    static HelixDashboardTile() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixDashboardTile), new FrameworkPropertyMetadata(typeof(HelixDashboardTile)));

    public HelixDashboardTile() => Loaded += (_, _) =>
    {
        if (ReadLocalValue(SpanProperty) == DependencyProperty.UnsetValue) SetCurrentValue(SpanProperty, DefaultSpan);
        UpdateSpanText();
    };

    public static readonly DependencyProperty TileIdProperty = HelixProperty.Register<HelixDashboardTile, string?>(nameof(TileId), null);
    public static readonly DependencyProperty TitleProperty = HelixProperty.Register<HelixDashboardTile, string?>(nameof(Title), null);
    public static readonly DependencyProperty ActionsProperty = HelixProperty.Register<HelixDashboardTile, object?>(nameof(Actions), null);
    public static readonly DependencyProperty DefaultSpanProperty = HelixProperty.Register<HelixDashboardTile, int>(nameof(DefaultSpan), 1);
    public static readonly DependencyProperty MinSpanProperty = HelixProperty.Register<HelixDashboardTile, int>(nameof(MinSpan), 1);
    public static readonly DependencyProperty MaxSpanProperty = HelixProperty.Register<HelixDashboardTile, int>(nameof(MaxSpan), 12);
    public static readonly DependencyProperty CanHideProperty = HelixProperty.Register<HelixDashboardTile, bool>(nameof(CanHide), true);
    public static readonly DependencyProperty IsPlainProperty = HelixProperty.Register<HelixDashboardTile, bool>(nameof(IsPlain), false);
    public static readonly DependencyProperty SpanProperty = DependencyProperty.Register(nameof(Span), typeof(int), typeof(HelixDashboardTile),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange, (d, _) => ((HelixDashboardTile)d).UpdateSpanText()));
    public static readonly DependencyProperty ColumnProperty = DependencyProperty.Register(nameof(Column), typeof(int), typeof(HelixDashboardTile),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));
    public static readonly DependencyProperty IsHiddenProperty = DependencyProperty.Register(nameof(IsHidden), typeof(bool), typeof(HelixDashboardTile),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsParentMeasure, (d, e) => ((HelixDashboardTile)d).Visibility = (bool)e.NewValue ? Visibility.Collapsed : Visibility.Visible));

    private static readonly DependencyPropertyKey SpanTextKey = DependencyProperty.RegisterReadOnly(nameof(SpanText), typeof(string), typeof(HelixDashboardTile), new PropertyMetadata("1"));
    private static readonly DependencyPropertyKey IsDraggingKey = DependencyProperty.RegisterReadOnly(nameof(IsDragging), typeof(bool), typeof(HelixDashboardTile), new PropertyMetadata(false));
    public static readonly DependencyProperty SpanTextProperty = SpanTextKey.DependencyProperty;
    public static readonly DependencyProperty IsDraggingProperty = IsDraggingKey.DependencyProperty;

    public string? TileId { get => (string?)GetValue(TileIdProperty); set => SetValue(TileIdProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public int DefaultSpan { get => (int)GetValue(DefaultSpanProperty); set => SetValue(DefaultSpanProperty, value); }
    public int MinSpan { get => (int)GetValue(MinSpanProperty); set => SetValue(MinSpanProperty, value); }
    public int MaxSpan { get => (int)GetValue(MaxSpanProperty); set => SetValue(MaxSpanProperty, value); }

    /// <summary>The user can hide it in edit mode. False for tiles whose visibility you drive (e.g. by a filter binding).</summary>
    public bool CanHide { get => (bool)GetValue(CanHideProperty); set => SetValue(CanHideProperty, value); }

    /// <summary>No card around the content (for a KPI row, a banner…).</summary>
    public bool IsPlain { get => (bool)GetValue(IsPlainProperty); set => SetValue(IsPlainProperty, value); }

    /// <summary>Columns taken (1 … ColumnCount).</summary>
    public int Span { get => (int)GetValue(SpanProperty); set => SetValue(SpanProperty, value); }

    /// <summary>Columns layout: first column of the tile (0-based); -1 places it in the shortest column.</summary>
    public int Column { get => (int)GetValue(ColumnProperty); set => SetValue(ColumnProperty, value); }
    public bool IsHidden { get => (bool)GetValue(IsHiddenProperty); set => SetValue(IsHiddenProperty, value); }
    public string SpanText => (string)GetValue(SpanTextProperty);

    /// <summary>True while the user drags this tile.</summary>
    public bool IsDragging { get => (bool)GetValue(IsDraggingProperty); internal set => SetValue(IsDraggingKey, value); }

    private HelixDashboard? Dashboard => ItemsControl.ItemsControlFromItemContainer(this) as HelixDashboard;

    private void UpdateSpanText() => SetValue(SpanTextKey, $"{Span} / {Dashboard?.ColumnCount ?? 3}");

    public ICommand MoveBeforeCommand => new HelixCommand(() => Dashboard?.Shift(this, -1));
    public ICommand MoveAfterCommand => new HelixCommand(() => Dashboard?.Shift(this, 1));
    public ICommand MoveLeftCommand => new HelixCommand(() => Dashboard?.ShiftColumn(this, -1));
    public ICommand MoveRightCommand => new HelixCommand(() => Dashboard?.ShiftColumn(this, 1));
    public ICommand NarrowerCommand => new HelixCommand(() => Dashboard?.Resize(this, -1));
    public ICommand WiderCommand => new HelixCommand(() => Dashboard?.Resize(this, 1));
    public ICommand HideCommand => new HelixCommand(() => Dashboard?.SetHidden(this, true));

    public static string MoveBeforeText => HelixTexts.Current.MoveBefore;
    public static string MoveAfterText => HelixTexts.Current.MoveAfter;
    public static string MoveLeftText => HelixTexts.Current.MoveLeft;
    public static string MoveRightText => HelixTexts.Current.MoveRight;
    public static string NarrowerText => HelixTexts.Current.Narrower;
    public static string WiderText => HelixTexts.Current.Wider;
    public static string HideText => HelixTexts.Current.Hide;
    public static string DragText => HelixTexts.Current.DragToMove;

    private FrameworkElement? _handle;
    private Point? _down;

    public override void OnApplyTemplate()
    {
        if (_handle is not null)
        {
            _handle.PreviewMouseLeftButtonDown -= OnHandleDown;
            _handle.PreviewMouseMove -= OnHandleMove;
        }

        base.OnApplyTemplate();
        _handle = GetTemplateChild("PART_Handle") as FrameworkElement;
        if (_handle is not null)
        {
            _handle.PreviewMouseLeftButtonDown += OnHandleDown;
            _handle.PreviewMouseMove += OnHandleMove;
        }
    }

    private void OnHandleDown(object sender, MouseButtonEventArgs e) =>
        _down = Dashboard?.IsEditing == true && e.OriginalSource is not System.Windows.Controls.Primitives.ButtonBase ? e.GetPosition(this) : null;

    private void OnHandleMove(object sender, MouseEventArgs e)
    {
        if (_down is not { } down || e.LeftButton != MouseButtonState.Pressed) return;
        var delta = e.GetPosition(this) - down;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _down = null;
        Dashboard?.BeginDrag(this);
    }
}

/// <summary>
/// Lays the dashboard tiles out: in rows (each tile takes Span columns), or in columns where tiles stack
/// in their column (Column, Span) without gaps.
/// </summary>
public class HelixDashboardPanel : Panel
{
    private HelixDashboard? Owner => ItemsControl.GetItemsOwner(this) as HelixDashboard;

    private int Columns => Math.Max(1, Owner?.ColumnCount ?? 3);
    private double Gap => Owner?.Gap ?? 16;
    private bool IsColumns => Owner?.LayoutMode == HelixDashboardLayout.Columns;

    // Where each visible tile ended up during the last layout pass (for drag targets and column moves).
    private readonly Dictionary<UIElement, (Rect Rect, int Column)> _placed = [];

    private ScrollViewer? _scroller;

    public HelixDashboardPanel()
    {
        Background = Brushes.Transparent; // the whole area accepts drops
        Loaded += (_, _) =>
        {
            // In a horizontal ScrollViewer the width is unbounded: fill its viewport instead, and follow its size.
            _scroller = FindScroller();
            if (_scroller is not null) _scroller.SizeChanged += OnScrollerSizeChanged;
            InvalidateMeasure();
        };
        Unloaded += (_, _) =>
        {
            if (_scroller is not null) _scroller.SizeChanged -= OnScrollerSizeChanged;
            _scroller = null;
        };
    }

    private void OnScrollerSizeChanged(object sender, SizeChangedEventArgs e) => InvalidateMeasure();

    private ScrollViewer? FindScroller()
    {
        for (DependencyObject? parent = VisualTreeHelper.GetParent(this); parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is ScrollViewer scroller) return scroller;
        }

        return null;
    }

    internal int ColumnOf(HelixDashboardTile tile) => _placed.TryGetValue(tile, out var p) ? p.Column : Math.Max(0, tile.Column);

    private Rect? _marker;

    /// <summary>Shows where the dragged tile will land: above <paramref name="before"/>, or at the bottom of its column.</summary>
    internal void ShowDropMarker(HelixDashboardTile? before, int column, HelixDashboardTile? dragged)
    {
        Rect? marker = null;
        if (dragged is not null)
        {
            var cell = CellWidth(ActualWidth);
            var span = Math.Clamp(dragged.Span, 1, Columns);
            var x = (IsColumns ? column : 0) * (cell + Gap);
            var width = IsColumns ? SpanWidth(cell, span) : ActualWidth;
            if (before is not null && _placed.TryGetValue(before, out var b))
            {
                marker = IsColumns
                    ? new Rect(x, b.Rect.Top - Gap / 2 - 1.5, width, 3)
                    : new Rect(b.Rect.Left - Gap / 2 - 1.5, b.Rect.Top, 3, b.Rect.Height);
            }
            else if (IsColumns)
            {
                var bottom = _placed.Where(p => p.Key != dragged && p.Value.Column < column + span && column < p.Value.Column + Math.Clamp((p.Key as HelixDashboardTile)?.Span ?? 1, 1, Columns))
                    .Select(p => p.Value.Rect.Bottom).DefaultIfEmpty(0).Max();
                marker = new Rect(x, bottom + Gap / 2 - 1.5, width, 3);
            }
        }

        if (marker != _marker)
        {
            _marker = marker;
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (_marker is { } marker)
        {
            var brush = TryFindResource(HelixKeys.Primary) as Brush ?? Brushes.SteelBlue;
            dc.DrawRoundedRectangle(brush, null, marker, 1.5, 1.5);
        }
    }

    private double PanelWidth(double available)
    {
        var min = (Owner?.ColumnMinWidth ?? 0) * Columns + Gap * (Columns - 1);
        double width;
        if (!double.IsInfinity(available))
        {
            width = available;
        }
        else if (_scroller is { ViewportWidth: > 0 } scroller)
        {
            // Viewport minus the space around the dashboard (its left offset, mirrored on the right).
            var left = 0.0;
            try
            {
                left = TransformToAncestor(scroller).Transform(new Point(0, 0)).X + scroller.HorizontalOffset;
            }
            catch (InvalidOperationException)
            {
                // Not connected yet: no offset.
            }

            width = scroller.ViewportWidth - 2 * Math.Max(0, left);
        }
        else
        {
            width = 1000;
        }

        return Math.Max(width, min);
    }

    private double CellWidth(double width) => Math.Max(0, (width - Gap * (Columns - 1)) / Columns);

    private double SpanWidth(double cell, int span) => cell * span + Gap * (span - 1);

    private IEnumerable<List<(UIElement Child, int Span)>> Rows()
    {
        var row = new List<(UIElement, int)>();
        var used = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) continue;
            var span = Math.Clamp(child is HelixDashboardTile t ? t.Span : 1, 1, Columns);
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

    /// <summary>Columns layout: places every visible tile; returns the rectangles and the total height.</summary>
    private (List<(UIElement Child, Rect Rect, int Column)> Items, double Height) PlaceColumns(double width, bool measure)
    {
        var cell = CellWidth(width);
        var bottoms = new double[Columns];
        var result = new List<(UIElement, Rect, int)>();
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) continue;
            var tile = child as HelixDashboardTile;
            var span = Math.Clamp(tile?.Span ?? 1, 1, Columns);
            var column = tile?.Column ?? -1;
            if (column < 0)
            {
                // Auto: the place where the tile can start highest.
                column = Enumerable.Range(0, Columns - span + 1).OrderBy(c => Enumerable.Range(c, span).Max(i => bottoms[i])).First();
            }

            column = Math.Clamp(column, 0, Columns - span);
            var w = SpanWidth(cell, span);
            if (measure) child.Measure(new Size(w, double.PositiveInfinity));
            var top = Enumerable.Range(column, span).Max(i => bottoms[i]);
            var h = child.DesiredSize.Height;
            result.Add((child, new Rect(column * (cell + Gap), top, w, h), column));
            for (var i = column; i < column + span; i++) bottoms[i] = top + h + Gap;
        }

        var height = Math.Max(0, bottoms.DefaultIfEmpty(0).Max() - Gap);
        if (Owner?.StretchColumns == true && result.Count > 0)
        {
            // The last tile of each column grows down to the common bottom.
            for (var c = 0; c < Columns; c++)
            {
                var last = result.Select((r, i) => (r, i)).Where(x => x.r.Item3 <= c && c < x.r.Item3 + Math.Clamp((x.r.Item1 as HelixDashboardTile)?.Span ?? 1, 1, Columns))
                    .OrderByDescending(x => x.r.Item2.Bottom).FirstOrDefault();
                if (last.r.Item1 is null) continue;
                var rect = last.r.Item2;
                if (rect.Bottom < height)
                {
                    result[last.i] = (last.r.Item1, new Rect(rect.X, rect.Y, rect.Width, height - rect.Y), last.r.Item3);
                }
            }
        }

        return (result, height);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Owner is { } owner) owner.Panel = this;
        var width = PanelWidth(availableSize.Width);
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) child.Measure(new Size(0, 0));
        }

        if (IsColumns)
        {
            return new Size(width, PlaceColumns(width, measure: true).Height);
        }

        var cell = CellWidth(width);
        var height = 0.0;
        var first = true;
        foreach (var row in Rows())
        {
            var rowHeight = 0.0;
            foreach (var (child, span) in row)
            {
                child.Measure(new Size(SpanWidth(cell, span), double.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            }

            height += rowHeight + (first ? 0 : Gap);
            first = false;
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _placed.Clear();
        var width = PanelWidth(finalSize.Width);
        if (IsColumns)
        {
            foreach (var (child, rect, column) in PlaceColumns(width, measure: false).Items)
            {
                child.Arrange(rect);
                _placed[child] = (rect, column);
            }
        }
        else
        {
            var cell = CellWidth(width);
            var y = 0.0;
            foreach (var row in Rows())
            {
                var rowHeight = row.Max(r => r.Child.DesiredSize.Height);
                var x = 0.0;
                foreach (var (child, span) in row)
                {
                    var rect = new Rect(x, y, SpanWidth(cell, span), rowHeight);
                    child.Arrange(rect);
                    _placed[child] = (rect, (int)Math.Round(x / Math.Max(1, cell + Gap)));
                    x += rect.Width + Gap;
                }

                y += rowHeight + Gap;
            }
        }

        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) child.Arrange(new Rect());
        }

        return new Size(width, finalSize.Height);
    }

    /// <summary>
    /// Where a dragged tile goes for a pointer position: its column (Columns layout) and the tile it is placed before
    /// (null = at the end of the order).
    /// </summary>
    internal (int Column, HelixDashboardTile? Before) DropTarget(HelixDashboardTile dragged, Point point)
    {
        var cell = CellWidth(ActualWidth);
        var span = Math.Clamp(dragged.Span, 1, Columns);
        var others = _placed.Where(p => p.Key != dragged && p.Key is HelixDashboardTile)
            .Select(p => (Tile: (HelixDashboardTile)p.Key, p.Value.Rect, p.Value.Column))
            .OrderBy(p => InternalChildren.IndexOf(p.Tile))
            .ToList();

        if (IsColumns)
        {
            // Column under the pointer (centered on the tile's width), then the first tile of that column below the pointer.
            var column = (int)Math.Floor((point.X - (SpanWidth(cell, span) - cell) / 2) / Math.Max(1, cell + Gap));
            column = Math.Clamp(column, 0, Columns - span);
            var inColumn = others.Where(o => o.Column < column + span && column < o.Column + Math.Clamp(o.Tile.Span, 1, Columns)).ToList();
            var before = inColumn.FirstOrDefault(o => point.Y < o.Rect.Top + o.Rect.Height / 2).Tile;
            if (before is null && inColumn.Count > 0)
            {
                // Below the last tile of the column: just after it in the order.
                var lastIndex = InternalChildren.IndexOf(inColumn[^1].Tile);
                before = others.Select(o => o.Tile).FirstOrDefault(t => InternalChildren.IndexOf(t) > lastIndex);
            }

            return (column, before);
        }

        // Rows: the tile under the pointer, or the first tile further in the reading order.
        var hit = others.FirstOrDefault(o => o.Rect.Contains(point)).Tile
                  ?? others.FirstOrDefault(o => point.Y < o.Rect.Top || (point.Y < o.Rect.Bottom && point.X < o.Rect.Left)).Tile;
        return (dragged.Column, hit);
    }
}

/// <summary>ICommand with a typed parameter.</summary>
internal sealed class HelixCommand<T>(Action<T> execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true; // the parameter may be bound after the command
    public void Execute(object? parameter)
    {
        if (parameter is T value) execute(value);
    }
}
