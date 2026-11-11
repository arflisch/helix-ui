using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Velo.Wpf;

/// <summary>
/// A list reordered by dragging (or Alt + ↑/↓ on the selected item). Works on ItemsSource lists (IList, e.g. ObservableCollection)
/// and on items declared in XAML. Reordered gives (From, To).
/// </summary>
public class VeloSortableList : ListBox
{
    static VeloSortableList() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloSortableList), new FrameworkPropertyMetadata(typeof(VeloSortableList)));

    public VeloSortableList() => AllowDrop = true;

    public static readonly DependencyProperty ShowHandleProperty = VeloProperty.Register<VeloSortableList, bool>(nameof(ShowHandle), true);
    public static readonly RoutedEvent ReorderedEvent = EventManager.RegisterRoutedEvent(nameof(Reordered), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VeloSortableList));

    /// <summary>A grip on the left of each row.</summary>
    public bool ShowHandle { get => (bool)GetValue(ShowHandleProperty); set => SetValue(ShowHandleProperty, value); }

    /// <summary>Raised with <see cref="VeloEventArgs{T}"/> of (int From, int To) after a move.</summary>
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
        DragDrop.DoDragDrop(this, new DataObject(typeof(VeloSortableList), item), DragDropEffects.Move);
        var to = Items.IndexOf(item);
        if (_dragFrom >= 0 && to >= 0 && to != _dragFrom)
        {
            RaiseEvent(new VeloEventArgs<(int From, int To)>(ReorderedEvent, (_dragFrom, to)));
        }

        _dragFrom = -1;
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        if (e.Data.GetData(typeof(VeloSortableList)) is not { } item || !Items.Contains(item))
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
                RaiseEvent(new VeloEventArgs<(int From, int To)>(ReorderedEvent, (from, to)));
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
public class VeloKanban : Control, IVeloThemeAware
{
    static VeloKanban() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloKanban), new FrameworkPropertyMetadata(typeof(VeloKanban)));

    public VeloKanban() => VeloTheme.Watch(this);

    private static void Rebuild(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((VeloKanban)d).Build();

    public static readonly DependencyProperty ColumnsProperty = VeloProperty.Register<VeloKanban, IEnumerable<VeloKanbanColumn>?>(nameof(Columns), null, Rebuild);
    public static readonly DependencyProperty ItemsSourceProperty = VeloProperty.Register<VeloKanban, IEnumerable?>(nameof(ItemsSource), null, (d, e) => ((VeloKanban)d).OnItemsSourceChanged(e));
    public static readonly DependencyProperty ColumnPathProperty = VeloProperty.Register<VeloKanban, string?>(nameof(ColumnPath), null, Rebuild);
    public static readonly DependencyProperty ColumnOfProperty = VeloProperty.Register<VeloKanban, Func<object, string>?>(nameof(ColumnOf), null, Rebuild);
    public static readonly DependencyProperty CardTemplateProperty = VeloProperty.Register<VeloKanban, DataTemplate?>(nameof(CardTemplate), null, Rebuild);
    public static readonly DependencyProperty CanMoveProperty = VeloProperty.Register<VeloKanban, Func<object, string, bool>?>(nameof(CanMove), null);
    public static readonly DependencyProperty ColumnWidthProperty = VeloProperty.Register<VeloKanban, double>(nameof(ColumnWidth), 280d, Rebuild);
    public static readonly DependencyProperty EmptyTextProperty = VeloProperty.Register<VeloKanban, string?>(nameof(EmptyText), null, Rebuild);
    public static readonly DependencyProperty ShowAddButtonProperty = VeloProperty.Register<VeloKanban, bool>(nameof(ShowAddButton), false, Rebuild);
    public static readonly RoutedEvent CardMovedEvent = EventManager.RegisterRoutedEvent(nameof(CardMoved), RoutingStrategy.Bubble, typeof(EventHandler<VeloKanbanMoveEventArgs>), typeof(VeloKanban));
    public static readonly RoutedEvent CardClickEvent = EventManager.RegisterRoutedEvent(nameof(CardClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VeloKanban));
    public static readonly RoutedEvent AddRequestedEvent = EventManager.RegisterRoutedEvent(nameof(AddRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VeloKanban));

    public IEnumerable<VeloKanbanColumn>? Columns { get => (IEnumerable<VeloKanbanColumn>?)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
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

    public event EventHandler<VeloKanbanMoveEventArgs> CardMoved { add => AddHandler(CardMovedEvent, value); remove => RemoveHandler(CardMovedEvent, value); }

    /// <summary>Raised with <see cref="VeloEventArgs{T}"/> of the card.</summary>
    public event RoutedEventHandler CardClick { add => AddHandler(CardClickEvent, value); remove => RemoveHandler(CardClickEvent, value); }

    /// <summary>Raised with <see cref="VeloEventArgs{T}"/> of the column key.</summary>
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

    void IVeloThemeAware.OnThemeChanged() => Build();

    private void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newList) newList.CollectionChanged += OnCollectionChanged;
        Build();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Build();

    private string KeyOf(object item) =>
        ColumnOf?.Invoke(item) ?? (ColumnPath is { } path ? VeloDataGrid.Value(item, path)?.ToString() ?? "" : "");

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

    private FrameworkElement BuildColumn(VeloKanbanColumn column, List<object> cards)
    {
        var root = new Border { Width = ColumnWidth, Margin = new Thickness(0, 0, 14, 0), CornerRadius = new CornerRadius(14), Padding = new Thickness(10) };
        root.SetResourceReference(Border.BackgroundProperty, VeloKeys.Surface2);

        var header = new DockPanel { Margin = new Thickness(4, 2, 2, 10) };
        if (ShowAddButton)
        {
            var add = new VeloButton { Icon = "plus", Variant = VeloVariant.Ghost, Tone = VeloTone.Neutral, Size = VeloSize.Sm, Width = 28, Height = 28, Padding = new Thickness(0), ToolTip = VeloTexts.Current.Add };
            add.Click += (_, _) => RaiseEvent(new VeloEventArgs<string>(AddRequestedEvent, column.Key));
            DockPanel.SetDock(add, Dock.Right);
            header.Children.Add(add);
        }

        var count = new Border { CornerRadius = new CornerRadius(999), Padding = new Thickness(7, 1, 7, 1), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        count.SetResourceReference(Border.BackgroundProperty, VeloKeys.Surface3);
        var over = column.Limit is { } limit && cards.Count > limit;
        var countText = new TextBlock { Text = column.Limit is { } l ? $"{cards.Count} / {l}" : cards.Count.ToString(CultureInfo.CurrentCulture), FontSize = 12, FontWeight = FontWeights.SemiBold };
        countText.SetResourceReference(TextBlock.ForegroundProperty, over ? VeloKeys.Danger : VeloKeys.TextMuted);
        count.Child = countText;
        var title = new StackPanel { Orientation = Orientation.Horizontal };
        var dot = new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, Fill = VeloTheme.Resolve(this, column.Color) ?? VeloTheme.Brush(this, VeloKeys.TextSubtle, Colors.Gray) };
        var name = new TextBlock { Text = column.Title, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        name.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.Text);
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
            empty.SetResourceReference(Border.BorderBrushProperty, VeloKeys.BorderStrong);
            var emptyText = new TextBlock { Text = EmptyText ?? VeloTexts.Current.DropCardHere, FontSize = 12.5, HorizontalAlignment = HorizontalAlignment.Center };
            emptyText.SetResourceReference(TextBlock.ForegroundProperty, VeloKeys.TextSubtle);
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
        card.SetResourceReference(Border.BackgroundProperty, VeloKeys.Surface);
        card.SetResourceReference(Border.BorderBrushProperty, VeloKeys.Border);
        card.SetResourceReference(TextElement.ForegroundProperty, VeloKeys.Text);
        card.MouseEnter += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, VeloKeys.BorderStrong);
        card.MouseLeave += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, VeloKeys.Border);
        card.GotKeyboardFocus += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, VeloKeys.Primary);
        card.LostKeyboardFocus += (_, _) => card.SetResourceReference(Border.BorderBrushProperty, VeloKeys.Border);
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
            DragDrop.DoDragDrop(card, new DataObject(typeof(VeloKanban), item), DragDropEffects.Move);
            card.Opacity = 1;
            RemovePlaceholder();
        };
        card.MouseLeftButtonUp += (_, _) =>
        {
            if (_pressed == item) RaiseEvent(new VeloEventArgs<object>(CardClickEvent, item));
            _pressed = null;
        };
        card.KeyDown += (_, e) =>
        {
            if (e.Key is Key.Enter or Key.Space) RaiseEvent(new VeloEventArgs<object>(CardClickEvent, item));
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
        var item = e.Data.GetData(typeof(VeloKanban));
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
        _placeholder.SetResourceReference(Border.BorderBrushProperty, VeloKeys.Primary);
        _placeholder.SetResourceReference(Border.BackgroundProperty, VeloKeys.PrimarySoft);
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

    private void OnColumnDrop(Panel list, VeloKanbanColumn column, DragEventArgs e)
    {
        var item = e.Data.GetData(typeof(VeloKanban));
        RemovePlaceholder();
        if (item is null || CanMove?.Invoke(item, column.Key) == false) return;
        var index = DropIndex(list, e);
        var args = new VeloKanbanMoveEventArgs(CardMovedEvent, item, KeyOf(item), column.Key, index);
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

/// <summary>
/// A dashboard of tiles on a grid of Columns. In edit mode, tiles move, resize (span) and hide;
/// Layout holds the user's arrangement (bind it two-way to save it).
/// </summary>
public class VeloDashboard : ItemsControl
{
    static VeloDashboard() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloDashboard), new FrameworkPropertyMetadata(typeof(VeloDashboard)));

    public VeloDashboard() => Loaded += (_, _) => ApplyLayout();

    public static readonly DependencyProperty ColumnCountProperty = VeloProperty.Register<VeloDashboard, int>(nameof(ColumnCount), 3);
    public static readonly DependencyProperty GapProperty = VeloProperty.Register<VeloDashboard, double>(nameof(Gap), 16d);
    public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(VeloDashboard),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty LayoutProperty = DependencyProperty.Register(nameof(Layout), typeof(IReadOnlyList<VeloTileState>), typeof(VeloDashboard),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((VeloDashboard)d).ApplyLayout()));
    public static readonly DependencyProperty TitleProperty = VeloProperty.Register<VeloDashboard, string?>(nameof(Title), null);
    public static readonly DependencyProperty ToolbarProperty = VeloProperty.Register<VeloDashboard, object?>(nameof(Toolbar), null);
    public static readonly DependencyProperty ShowToolbarProperty = VeloProperty.Register<VeloDashboard, bool>(nameof(ShowToolbar), true);

    private static readonly DependencyPropertyKey HiddenTilesKey = DependencyProperty.RegisterReadOnly(nameof(HiddenTiles), typeof(IReadOnlyList<VeloDashboardTile>), typeof(VeloDashboard), new PropertyMetadata(Array.Empty<VeloDashboardTile>()));
    public static readonly DependencyProperty HiddenTilesProperty = HiddenTilesKey.DependencyProperty;

    /// <summary>Grid columns (3 by default); a tile spans 1 to ColumnCount.</summary>
    public int ColumnCount { get => (int)GetValue(ColumnCountProperty); set => SetValue(ColumnCountProperty, value); }
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }
    public bool IsEditing { get => (bool)GetValue(IsEditingProperty); set => SetValue(IsEditingProperty, value); }

    /// <summary>Order, span and visibility of the tiles; updated after each user change.</summary>
    public IReadOnlyList<VeloTileState>? Layout { get => (IReadOnlyList<VeloTileState>?)GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? Toolbar { get => GetValue(ToolbarProperty); set => SetValue(ToolbarProperty, value); }

    /// <summary>The title row with "Edit layout" (and your Toolbar).</summary>
    public bool ShowToolbar { get => (bool)GetValue(ShowToolbarProperty); set => SetValue(ShowToolbarProperty, value); }
    public IReadOnlyList<VeloDashboardTile> HiddenTiles => (IReadOnlyList<VeloDashboardTile>)GetValue(HiddenTilesProperty);

    public static string EditText => VeloTexts.Current.EditLayout;
    public static string DoneText => VeloTexts.Current.Done;
    public static string HiddenText => VeloTexts.Current.HiddenTiles;

    /// <summary>Toggles edit mode (the toolbar button).</summary>
    public ICommand ToggleEditCommand => new VeloCommand(() => SetCurrentValue(IsEditingProperty, !IsEditing));

    protected override bool IsItemItsOwnContainerOverride(object item) => item is VeloDashboardTile;

    protected override DependencyObject GetContainerForItemOverride() => new VeloDashboardTile();

    private IEnumerable<VeloDashboardTile> Tiles => Items.Cast<object>().Select(i => i as VeloDashboardTile ?? ItemContainerGenerator.ContainerFromItem(i) as VeloDashboardTile).OfType<VeloDashboardTile>();

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
            var ordered = layout.Select(s => tiles.FirstOrDefault(t => t.TileId == s.Id)).OfType<VeloDashboardTile>()
                .Concat(tiles.Where(t => layout.All(s => s.Id != t.TileId))).ToList();
            foreach (var state in layout)
            {
                if (tiles.FirstOrDefault(t => t.TileId == state.Id) is { } tile)
                {
                    tile.Span = state.Span;
                    tile.IsHidden = state.Hidden;
                }
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
        SetCurrentValue(LayoutProperty, Tiles.Select(t => new VeloTileState(t.TileId ?? "", t.Span, t.IsHidden)).ToList());
        UpdateHidden();
    }

    private void UpdateHidden() => SetValue(HiddenTilesKey, Tiles.Where(t => t.IsHidden).ToList());

    internal void Shift(VeloDashboardTile tile, int direction)
    {
        if (ItemsSource is not null) return;
        var index = Items.IndexOf(tile);
        var target = Math.Clamp(index + direction, 0, Items.Count - 1);
        if (index < 0 || target == index) return;
        Items.RemoveAt(index);
        Items.Insert(target, tile);
        SaveLayout();
        tile.Focus();
    }

    internal void Resize(VeloDashboardTile tile, int delta)
    {
        tile.Span = Math.Clamp(tile.Span + delta, Math.Max(1, tile.MinSpan), Math.Min(ColumnCount, tile.MaxSpan));
        SaveLayout();
    }

    internal void SetHidden(VeloDashboardTile tile, bool hidden)
    {
        tile.IsHidden = hidden;
        SaveLayout();
    }

    /// <summary>Shows a hidden tile again (the hidden-tiles chips).</summary>
    public ICommand RestoreCommand => new VeloCommand<VeloDashboardTile>(t => SetHidden(t, false));
}

/// <summary>A dashboard tile: TileId (for the saved layout), Title, Actions, DefaultSpan / MinSpan / MaxSpan, CanHide, IsPlain.</summary>
public class VeloDashboardTile : ContentControl
{
    static VeloDashboardTile() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloDashboardTile), new FrameworkPropertyMetadata(typeof(VeloDashboardTile)));

    public VeloDashboardTile() => Loaded += (_, _) =>
    {
        if (ReadLocalValue(SpanProperty) == DependencyProperty.UnsetValue) SetCurrentValue(SpanProperty, DefaultSpan);
    };

    public static readonly DependencyProperty TileIdProperty = VeloProperty.Register<VeloDashboardTile, string?>(nameof(TileId), null);
    public static readonly DependencyProperty TitleProperty = VeloProperty.Register<VeloDashboardTile, string?>(nameof(Title), null);
    public static readonly DependencyProperty ActionsProperty = VeloProperty.Register<VeloDashboardTile, object?>(nameof(Actions), null);
    public static readonly DependencyProperty DefaultSpanProperty = VeloProperty.Register<VeloDashboardTile, int>(nameof(DefaultSpan), 1);
    public static readonly DependencyProperty MinSpanProperty = VeloProperty.Register<VeloDashboardTile, int>(nameof(MinSpan), 1);
    public static readonly DependencyProperty MaxSpanProperty = VeloProperty.Register<VeloDashboardTile, int>(nameof(MaxSpan), 12);
    public static readonly DependencyProperty CanHideProperty = VeloProperty.Register<VeloDashboardTile, bool>(nameof(CanHide), true);
    public static readonly DependencyProperty IsPlainProperty = VeloProperty.Register<VeloDashboardTile, bool>(nameof(IsPlain), false);
    public static readonly DependencyProperty SpanProperty = DependencyProperty.Register(nameof(Span), typeof(int), typeof(VeloDashboardTile),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange, (d, _) => ((VeloDashboardTile)d).UpdateSpanText()));
    public static readonly DependencyProperty IsHiddenProperty = DependencyProperty.Register(nameof(IsHidden), typeof(bool), typeof(VeloDashboardTile),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsParentMeasure, (d, e) => ((VeloDashboardTile)d).Visibility = (bool)e.NewValue ? Visibility.Collapsed : Visibility.Visible));

    private static readonly DependencyPropertyKey SpanTextKey = DependencyProperty.RegisterReadOnly(nameof(SpanText), typeof(string), typeof(VeloDashboardTile), new PropertyMetadata("1"));
    public static readonly DependencyProperty SpanTextProperty = SpanTextKey.DependencyProperty;

    public string? TileId { get => (string?)GetValue(TileIdProperty); set => SetValue(TileIdProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public int DefaultSpan { get => (int)GetValue(DefaultSpanProperty); set => SetValue(DefaultSpanProperty, value); }
    public int MinSpan { get => (int)GetValue(MinSpanProperty); set => SetValue(MinSpanProperty, value); }
    public int MaxSpan { get => (int)GetValue(MaxSpanProperty); set => SetValue(MaxSpanProperty, value); }
    public bool CanHide { get => (bool)GetValue(CanHideProperty); set => SetValue(CanHideProperty, value); }

    /// <summary>No card around the content (for a KPI row, a banner…).</summary>
    public bool IsPlain { get => (bool)GetValue(IsPlainProperty); set => SetValue(IsPlainProperty, value); }

    /// <summary>Columns taken (1 … ColumnCount).</summary>
    public int Span { get => (int)GetValue(SpanProperty); set => SetValue(SpanProperty, value); }
    public bool IsHidden { get => (bool)GetValue(IsHiddenProperty); set => SetValue(IsHiddenProperty, value); }
    public string SpanText => (string)GetValue(SpanTextProperty);

    private VeloDashboard? Dashboard => ItemsControl.ItemsControlFromItemContainer(this) as VeloDashboard;

    private void UpdateSpanText() => SetValue(SpanTextKey, $"{Span} / {Dashboard?.ColumnCount ?? 3}");

    public ICommand MoveBeforeCommand => new VeloCommand(() => Dashboard?.Shift(this, -1));
    public ICommand MoveAfterCommand => new VeloCommand(() => Dashboard?.Shift(this, 1));
    public ICommand NarrowerCommand => new VeloCommand(() => Dashboard?.Resize(this, -1));
    public ICommand WiderCommand => new VeloCommand(() => Dashboard?.Resize(this, 1));
    public ICommand HideCommand => new VeloCommand(() => Dashboard?.SetHidden(this, true));

    public static string MoveBeforeText => VeloTexts.Current.MoveBefore;
    public static string MoveAfterText => VeloTexts.Current.MoveAfter;
    public static string NarrowerText => VeloTexts.Current.Narrower;
    public static string WiderText => VeloTexts.Current.Wider;
    public static string HideText => VeloTexts.Current.Hide;
}

/// <summary>Lays tiles out in rows of ColumnCount columns; each tile takes its Span.</summary>
public class VeloDashboardPanel : Panel
{
    private VeloDashboard? Owner => ItemsControl.GetItemsOwner(this) as VeloDashboard;

    private int Columns => Math.Max(1, Owner?.ColumnCount ?? 3);
    private double Gap => Owner?.Gap ?? 16;

    private IEnumerable<List<(UIElement Child, int Span)>> Rows()
    {
        var row = new List<(UIElement, int)>();
        var used = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) continue;
            var span = Math.Clamp(child is VeloDashboardTile t ? t.Span : 1, 1, Columns);
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

    private double CellWidth(double width) => Math.Max(0, (width - Gap * (Columns - 1)) / Columns);

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 1000 : availableSize.Width;
        var cell = CellWidth(width);
        var height = 0.0;
        var first = true;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) child.Measure(new Size(0, 0));
        }

        foreach (var row in Rows())
        {
            var rowHeight = 0.0;
            foreach (var (child, span) in row)
            {
                child.Measure(new Size(cell * span + Gap * (span - 1), double.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            }

            height += rowHeight + (first ? 0 : Gap);
            first = false;
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var cell = CellWidth(finalSize.Width);
        var y = 0.0;
        foreach (var row in Rows())
        {
            var rowHeight = row.Max(r => r.Child.DesiredSize.Height);
            var x = 0.0;
            foreach (var (child, span) in row)
            {
                var w = cell * span + Gap * (span - 1);
                child.Arrange(new Rect(x, y, w, rowHeight));
                x += w + Gap;
            }

            y += rowHeight + Gap;
        }

        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) child.Arrange(new Rect());
        }

        return finalSize;
    }
}

/// <summary>ICommand with a typed parameter.</summary>
internal sealed class VeloCommand<T>(Action<T> execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true; // the parameter may be bound after the command
    public void Execute(object? parameter)
    {
        if (parameter is T value) execute(value);
    }
}
