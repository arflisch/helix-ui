using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using Avalonia.Controls.Documents;

namespace Helix.Avalonia;

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

/// <summary>A list reordered by dragging its rows (grip on the left) or with Alt + ↑ / ↓.</summary>
public class HelixSortableList : ListBox
{
    public static readonly StyledProperty<bool> ShowHandleProperty = AvaloniaProperty.Register<HelixSortableList, bool>(nameof(ShowHandle), true);
    public static readonly RoutedEvent<HelixEventArgs<(int From, int To)>> ReorderedEvent =
        RoutedEvent.Register<HelixSortableList, HelixEventArgs<(int From, int To)>>(nameof(Reordered), RoutingStrategies.Bubble);

    private Point _down;
    private object? _pressed;
    private int _from = -1;
    private bool _dragging;

    public HelixSortableList()
    {
        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override Type StyleKeyOverride => typeof(HelixSortableList);

    /// <summary>A grip on the left of each row.</summary>
    public bool ShowHandle { get => GetValue(ShowHandleProperty); set => SetValue(ShowHandleProperty, value); }

    /// <summary>Raised with (From, To) after a move.</summary>
    public event EventHandler<HelixEventArgs<(int From, int To)>>? Reordered { add => AddHandler(ReorderedEvent, value); remove => RemoveHandler(ReorderedEvent, value); }

    private ListBoxItem? ContainerAt(object? source) => (source as Visual)?.FindAncestorOfType<ListBoxItem>(includeSelf: true);

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _down = e.GetPosition(this);
        _pressed = ContainerAt(e.Source) is { } container ? ItemFromContainer(container) : null;
        _dragging = false;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_pressed is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || !IsEnabled) return;
        var p = e.GetPosition(this);
        if (!_dragging)
        {
            if (Math.Abs(p.Y - _down.Y) < 4 && Math.Abs(p.X - _down.X) < 4) return;
            _dragging = true;
            _from = IndexOf(_pressed);
            e.Pointer.Capture(this);
        }

        // Live reorder: the dragged row takes the place of the row under the pointer.
        for (var i = 0; i < ItemCount; i++)
        {
            if (ContainerFromIndex(i) is not { } container) continue;
            var top = container.TranslatePoint(new Point(0, 0), this)?.Y ?? 0;
            if (p.Y >= top && p.Y < top + container.Bounds.Height)
            {
                var from = IndexOf(_pressed);
                if (i != from) Move(from, i);
                break;
            }
        }
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragging)
        {
            e.Pointer.Capture(null);
            var to = IndexOf(_pressed);
            if (_from >= 0 && to >= 0 && to != _from) RaiseEvent(new HelixEventArgs<(int From, int To)>(ReorderedEvent, (_from, to)));
            SelectedItem = _pressed;
            e.Handled = true;
        }

        _pressed = null;
        _dragging = false;
    }

    private int IndexOf(object? item) => item is null ? -1 : Items.IndexOf(item);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Alt && SelectedItem is { } item && e.Key is Key.Up or Key.Down)
        {
            var from = IndexOf(item);
            var to = Math.Clamp(from + (e.Key == Key.Up ? -1 : 1), 0, ItemCount - 1);
            if (to != from)
            {
                Move(from, to);
                SelectedItem = item;
                ContainerFromItem(item)?.Focus();
                RaiseEvent(new HelixEventArgs<(int From, int To)>(ReorderedEvent, (from, to)));
            }

            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    /// <summary>Moves a row (in the ItemsSource list when it can be changed, else in Items).</summary>
    public void Move(int from, int to)
    {
        if (from < 0 || to < 0 || from == to) return;
        var selected = SelectedItem;
        if (ItemsSource is IList list && !list.IsReadOnly && !list.IsFixedSize)
        {
            var method = list.GetType().GetMethod("Move", [typeof(int), typeof(int)]);
            if (method is not null)
            {
                method.Invoke(list, [from, to]); // ObservableCollection<T>.Move / AvaloniaList.Move keep the containers
            }
            else
            {
                var value = list[from];
                list.RemoveAt(from);
                list.Insert(to, value);
            }
        }
        else if (ItemsSource is null)
        {
            var value = Items[from];
            Items.RemoveAt(from);
            Items.Insert(to, value);
        }

        SelectedItem = selected;
    }
}

/// <summary>
/// A board of columns with cards dragged between them. Columns describe the board; ItemsSource holds the cards;
/// ColumnPath (or ColumnOf) says which column a card is in. CardTemplate draws a card.
/// CardMoved proposes each move; if nobody handles it and ColumnPath is writable, the board moves the card itself.
/// </summary>
[TemplatePart("PART_Columns", typeof(Panel))]
[TemplatePart("PART_Overlay", typeof(Canvas))]
public class HelixKanban : TemplatedControl
{
    public static readonly StyledProperty<IEnumerable<HelixKanbanColumn>?> ColumnsProperty = AvaloniaProperty.Register<HelixKanban, IEnumerable<HelixKanbanColumn>?>(nameof(Columns));
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty = AvaloniaProperty.Register<HelixKanban, IEnumerable?>(nameof(ItemsSource));
    public static readonly StyledProperty<string?> ColumnPathProperty = AvaloniaProperty.Register<HelixKanban, string?>(nameof(ColumnPath));
    public static readonly StyledProperty<Func<object, string>?> ColumnOfProperty = AvaloniaProperty.Register<HelixKanban, Func<object, string>?>(nameof(ColumnOf));
    public static readonly StyledProperty<IDataTemplate?> CardTemplateProperty = AvaloniaProperty.Register<HelixKanban, IDataTemplate?>(nameof(CardTemplate));
    public static readonly StyledProperty<Func<object, string, bool>?> CanMoveProperty = AvaloniaProperty.Register<HelixKanban, Func<object, string, bool>?>(nameof(CanMove));
    public static readonly StyledProperty<double> ColumnWidthProperty = AvaloniaProperty.Register<HelixKanban, double>(nameof(ColumnWidth), 280);
    public static readonly StyledProperty<string?> EmptyTextProperty = AvaloniaProperty.Register<HelixKanban, string?>(nameof(EmptyText));
    public static readonly StyledProperty<bool> ShowAddButtonProperty = AvaloniaProperty.Register<HelixKanban, bool>(nameof(ShowAddButton));
    public static readonly RoutedEvent<HelixKanbanMoveEventArgs> CardMovedEvent = RoutedEvent.Register<HelixKanban, HelixKanbanMoveEventArgs>(nameof(CardMoved), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<HelixEventArgs<object>> CardClickEvent = RoutedEvent.Register<HelixKanban, HelixEventArgs<object>>(nameof(CardClick), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<HelixEventArgs<string>> AddRequestedEvent = RoutedEvent.Register<HelixKanban, HelixEventArgs<string>>(nameof(AddRequested), RoutingStrategies.Bubble);

    public IEnumerable<HelixKanbanColumn>? Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public IEnumerable? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>Property holding the column key of a card, e.g. "Status".</summary>
    public string? ColumnPath { get => GetValue(ColumnPathProperty); set => SetValue(ColumnPathProperty, value); }

    /// <summary>Column key of a card, from code (instead of ColumnPath).</summary>
    public Func<object, string>? ColumnOf { get => GetValue(ColumnOfProperty); set => SetValue(ColumnOfProperty, value); }
    public IDataTemplate? CardTemplate { get => GetValue(CardTemplateProperty); set => SetValue(CardTemplateProperty, value); }

    /// <summary>Whether a card may go to a column (e.g. WIP limits, permissions).</summary>
    public Func<object, string, bool>? CanMove { get => GetValue(CanMoveProperty); set => SetValue(CanMoveProperty, value); }
    public double ColumnWidth { get => GetValue(ColumnWidthProperty); set => SetValue(ColumnWidthProperty, value); }
    public string? EmptyText { get => GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

    /// <summary>A "+" in each column header raising AddRequested with the column key.</summary>
    public bool ShowAddButton { get => GetValue(ShowAddButtonProperty); set => SetValue(ShowAddButtonProperty, value); }

    public event EventHandler<HelixKanbanMoveEventArgs>? CardMoved { add => AddHandler(CardMovedEvent, value); remove => RemoveHandler(CardMovedEvent, value); }
    public event EventHandler<HelixEventArgs<object>>? CardClick { add => AddHandler(CardClickEvent, value); remove => RemoveHandler(CardClickEvent, value); }
    public event EventHandler<HelixEventArgs<string>>? AddRequested { add => AddHandler(AddRequestedEvent, value); remove => RemoveHandler(AddRequestedEvent, value); }

    private Panel? _columns;
    private Canvas? _overlay;
    private Border? _placeholder, _ghost;
    private Point _down, _grab;
    private object? _pressed, _dragged;
    private Control? _draggedCard;
    private (StackPanel List, HelixKanbanColumn Column, int Index)? _target;
    private readonly List<(StackPanel List, HelixKanbanColumn Column, Border Box)> _lists = [];

    public HelixKanban() => ActualThemeVariantChanged += (_, _) => Build();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _columns = e.NameScope.Find<Panel>("PART_Columns");
        _overlay = e.NameScope.Find<Canvas>("PART_Overlay");
        Build();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldList) oldList.CollectionChanged -= OnCollectionChanged;
            if (change.NewValue is INotifyCollectionChanged newList) newList.CollectionChanged += OnCollectionChanged;
        }

        if (change.Property.OwnerType == typeof(HelixKanban) && change.Property != CanMoveProperty) Build();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Build();

    private string KeyOf(object item) => ColumnOf?.Invoke(item) ?? (ColumnPath is { } path ? HelixDataGrid.Value(item, path)?.ToString() ?? "" : "");

    private IBrush Res(string key) => HelixDraw.Brush(this, key, Colors.Gray);

    /// <summary>Draws the board again (e.g. after changing a card's properties).</summary>
    public void Build()
    {
        if (_columns is null || _dragged is not null) return;
        _columns.Children.Clear();
        _lists.Clear();
        var items = ItemsSource?.Cast<object>().ToList() ?? [];
        foreach (var column in Columns ?? []) _columns.Children.Add(BuildColumn(column, items.Where(i => KeyOf(i) == column.Key).ToList()));
    }

    private Control BuildColumn(HelixKanbanColumn column, List<object> cards)
    {
        var root = new Border { Width = ColumnWidth, Margin = new Thickness(0, 0, 14, 0), CornerRadius = new CornerRadius(14), Padding = new Thickness(10), Background = Res(HelixKeys.Surface2) };
        var header = new DockPanel { Margin = new Thickness(4, 2, 2, 10) };
        if (ShowAddButton)
        {
            var add = new HelixButton { Icon = "plus", Variant = HelixVariant.Ghost, Tone = HelixTone.Neutral, Size = HelixSize.Sm, Width = 28, Height = 28, Padding = new Thickness(0) };
            ToolTip.SetTip(add, HelixTexts.Current.Add);
            add.Click += (_, _) => RaiseEvent(new HelixEventArgs<string>(AddRequestedEvent, column.Key));
            DockPanel.SetDock(add, Dock.Right);
            header.Children.Add(add);
        }

        var over = column.Limit is { } limit && cards.Count > limit;
        var countText = new TextBlock
        {
            Text = column.Limit is { } l ? $"{cards.Count} / {l}" : cards.Count.ToString(CultureInfo.CurrentCulture),
            FontSize = 12, FontWeight = FontWeight.SemiBold, Foreground = Res(over ? HelixKeys.Danger : HelixKeys.TextMuted),
        };
        var count = new Border { CornerRadius = new CornerRadius(999), Padding = new Thickness(7, 1), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Background = Res(HelixKeys.Surface3), Child = countText };
        var dot = new global::Avalonia.Controls.Shapes.Ellipse
        {
            Width = 8, Height = 8, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center,
            Fill = HelixDraw.Resolve(this, column.Color) ?? Res(HelixKeys.TextSubtle),
        };
        var name = new TextBlock { Text = column.Title, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Foreground = Res(HelixKeys.Text) };
        header.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { dot, name, count } });

        var list = new StackPanel { MinHeight = 60, Background = Brushes.Transparent, Tag = column.Key };
        foreach (var card in cards) list.Children.Add(BuildCard(card));
        if (cards.Count == 0)
        {
            var empty = new Border
            {
                CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1.5), Padding = new Thickness(10, 18), IsHitTestVisible = false, BorderBrush = Res(HelixKeys.BorderStrong),
                Child = new TextBlock { Text = EmptyText ?? HelixTexts.Current.DropCardHere, FontSize = 12.5, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Res(HelixKeys.TextSubtle) },
            };
            list.Children.Add(empty);
        }

        _lists.Add((list, column, root));
        DockPanel.SetDock(header, Dock.Top);
        root.Child = new DockPanel { Children = { header, new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } } };
        return root;
    }

    private Control CardContent(object item) => new ContentControl { Content = item, ContentTemplate = CardTemplate };

    private Control BuildCard(object item)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), Padding = new Thickness(12, 10), Margin = new Thickness(0, 0, 0, 8),
            Cursor = new Cursor(StandardCursorType.Hand), Child = CardContent(item), Tag = item, Focusable = true,
            Background = Res(HelixKeys.Surface), BorderBrush = Res(HelixKeys.Border),
        };
        card.Bind(TextElement.ForegroundProperty, card.GetResourceObservable(HelixKeys.Text));
        card.PointerEntered += (_, _) => { if (_dragged is null) card.BorderBrush = Res(HelixKeys.BorderStrong); };
        card.PointerExited += (_, _) => card.BorderBrush = Res(HelixKeys.Border);
        card.GotFocus += (_, _) => card.BorderBrush = Res(HelixKeys.Primary);
        card.LostFocus += (_, _) => card.BorderBrush = Res(HelixKeys.Border);
        card.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(card).Properties.IsLeftButtonPressed) return;
            _down = e.GetPosition(this);
            _grab = e.GetPosition(card);
            _pressed = item;
            e.Pointer.Capture(card);
            e.Handled = true;
        };
        card.PointerMoved += (_, e) =>
        {
            if (_pressed != item) return;
            var p = e.GetPosition(this);
            if (_dragged is null)
            {
                if (Math.Abs(p.X - _down.X) < 4 && Math.Abs(p.Y - _down.Y) < 4) return;
                StartDrag(item, card);
            }

            DragTo(p);
        };
        card.PointerReleased += (_, e) =>
        {
            var dragged = _dragged is not null;
            if (dragged) Drop();
            _pressed = null;
            e.Pointer.Capture(null);
            if (!dragged && e.InitialPressMouseButton == MouseButton.Left) RaiseEvent(new HelixEventArgs<object>(CardClickEvent, item));
        };
        card.PointerCaptureLost += (_, _) => { if (_dragged is not null) CancelDrag(); };
        card.KeyDown += (_, e) =>
        {
            if (e.Key is Key.Enter or Key.Space) RaiseEvent(new HelixEventArgs<object>(CardClickEvent, item));
        };
        return card;
    }

    private void StartDrag(object item, Control card)
    {
        _dragged = item;
        _draggedCard = card;
        card.Opacity = 0.35;
        if (_overlay is null) return;
        // A copy of the card follows the pointer.
        _ghost = new Border
        {
            Width = card.Bounds.Width, CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), Padding = new Thickness(12, 10),
            Background = Res(HelixKeys.Surface), BorderBrush = Res(HelixKeys.Primary), Child = CardContent(item), Opacity = 0.95, IsHitTestVisible = false,
            BoxShadow = new BoxShadows(new BoxShadow { OffsetY = 10, Blur = 28, Color = Color.FromArgb(0x33, 0, 0, 0) }),
            RenderTransform = new RotateTransform(2),
        };
        _ghost.Bind(TextElement.ForegroundProperty, _ghost.GetResourceObservable(HelixKeys.Text));
        _overlay.Children.Add(_ghost);
    }

    private void DragTo(Point p)
    {
        if (_ghost is not null)
        {
            Canvas.SetLeft(_ghost, p.X - _grab.X);
            Canvas.SetTop(_ghost, p.Y - _grab.Y);
        }

        _target = null;
        foreach (var (list, column, columnBox) in _lists)
        {
            var origin = list.TranslatePoint(new Point(0, 0), this) ?? default;
            var box = columnBox.TranslatePoint(new Point(0, 0), this) is { } b ? new Rect(b, columnBox.Bounds.Size) : new Rect(origin, list.Bounds.Size);
            if (!box.Contains(p)) continue;
            if (CanMove?.Invoke(_dragged!, column.Key) == false) break;
            _target = (list, column, DropIndex(list, p.Y - origin.Y));
            break;
        }

        ShowPlaceholder();
    }

    private int DropIndex(StackPanel list, double y)
    {
        var index = 0;
        foreach (var child in list.Children.OfType<Border>().Where(b => b.Tag is not null && b != _placeholder && b != _draggedCard))
        {
            if (y > child.Bounds.Top + child.Bounds.Height / 2) index++;
        }

        return index;
    }

    private void ShowPlaceholder()
    {
        RemovePlaceholder();
        if (_target is not { } target) return;
        _placeholder = new Border
        {
            Height = Math.Max(44, _draggedCard?.Bounds.Height ?? 44), CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1.5), Margin = new Thickness(0, 0, 0, 8),
            IsHitTestVisible = false, BorderBrush = Res(HelixKeys.Primary), Background = Res(HelixKeys.PrimarySoft),
        };
        var cards = target.List.Children.OfType<Border>().Where(b => b.Tag is not null && b != _draggedCard).ToList();
        var at = target.Index < cards.Count ? target.List.Children.IndexOf(cards[target.Index]) : target.List.Children.Count;
        target.List.Children.Insert(at, _placeholder);
    }

    private void RemovePlaceholder()
    {
        if (_placeholder?.Parent is Panel parent) parent.Children.Remove(_placeholder);
        _placeholder = null;
    }

    private void EndDragVisuals()
    {
        RemovePlaceholder();
        if (_ghost is not null) _overlay?.Children.Remove(_ghost);
        _ghost = null;
        if (_draggedCard is not null) _draggedCard.Opacity = 1;
        _draggedCard = null;
    }

    private void CancelDrag()
    {
        EndDragVisuals();
        _dragged = null;
        _target = null;
    }

    private void Drop()
    {
        var item = _dragged;
        var target = _target;
        EndDragVisuals();
        _dragged = null;
        _target = null;
        if (item is not null && target is { } t) MoveCard(item, t.Column.Key, t.Index);
        else Build();
    }

    /// <summary>
    /// Moves a card (what a drop does): raises CardMoved, and unless a handler sets Handled, writes ColumnPath and reorders an IList source.
    /// </summary>
    public void MoveCard(object item, string to, int index)
    {
        if (CanMove?.Invoke(item, to) == false) return;
        var args = new HelixKanbanMoveEventArgs(CardMovedEvent, item, KeyOf(item), to, index);
        RaiseEvent(args);
        if (!args.Handled) ApplyMove(item, to, index);
        Build();
    }

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
    }
}

/// <summary>A hidden tile, as shown in the "hidden tiles" chips of <see cref="HelixDashboard"/>.</summary>
public sealed record HelixHiddenTile(HelixDashboardTile Tile, string Title);

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
[TemplatePart("PART_MarkerLayer", typeof(Canvas))]
public class HelixDashboard : ItemsControl
{
    public static readonly StyledProperty<int> ColumnCountProperty = AvaloniaProperty.Register<HelixDashboard, int>(nameof(ColumnCount), 3);
    public static readonly StyledProperty<double> GapProperty = AvaloniaProperty.Register<HelixDashboard, double>(nameof(Gap), 16);
    public static readonly StyledProperty<HelixDashboardLayout> LayoutModeProperty = AvaloniaProperty.Register<HelixDashboard, HelixDashboardLayout>(nameof(LayoutMode));
    public static readonly StyledProperty<double> ColumnMinWidthProperty = AvaloniaProperty.Register<HelixDashboard, double>(nameof(ColumnMinWidth));
    public static readonly StyledProperty<bool> StretchColumnsProperty = AvaloniaProperty.Register<HelixDashboard, bool>(nameof(StretchColumns));
    public static readonly StyledProperty<bool> IsEditingProperty = AvaloniaProperty.Register<HelixDashboard, bool>(nameof(IsEditing), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<IReadOnlyList<HelixTileState>?> LayoutProperty = AvaloniaProperty.Register<HelixDashboard, IReadOnlyList<HelixTileState>?>(nameof(Layout), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixDashboard, string?>(nameof(Title));
    public static readonly StyledProperty<object?> ToolbarProperty = AvaloniaProperty.Register<HelixDashboard, object?>(nameof(Toolbar));
    public static readonly StyledProperty<bool> ShowToolbarProperty = AvaloniaProperty.Register<HelixDashboard, bool>(nameof(ShowToolbar), true);
    public static readonly DirectProperty<HelixDashboard, IReadOnlyList<HelixDashboardTile>> HiddenTilesProperty =
        AvaloniaProperty.RegisterDirect<HelixDashboard, IReadOnlyList<HelixDashboardTile>>(nameof(HiddenTiles), d => d.HiddenTiles);
    public static readonly DirectProperty<HelixDashboard, IReadOnlyList<HelixHiddenTile>> HiddenChipsProperty =
        AvaloniaProperty.RegisterDirect<HelixDashboard, IReadOnlyList<HelixHiddenTile>>(nameof(HiddenChips), d => d.HiddenChips);
    public static readonly RoutedEvent<RoutedEventArgs> LayoutChangedEvent = RoutedEvent.Register<HelixDashboard, RoutedEventArgs>(nameof(LayoutChanged), RoutingStrategies.Bubble);

    private IReadOnlyList<HelixDashboardTile> _hiddenTiles = [];
    private IReadOnlyList<HelixHiddenTile> _hiddenChips = [];
    private bool _applying;

    protected override Type StyleKeyOverride => typeof(HelixDashboard);

    /// <summary>Grid columns (3 by default); a tile spans 1 to ColumnCount.</summary>
    public int ColumnCount { get => GetValue(ColumnCountProperty); set => SetValue(ColumnCountProperty, value); }
    public double Gap { get => GetValue(GapProperty); set => SetValue(GapProperty, value); }

    /// <summary>Rows (default) or Columns (tiles stack in their column, like cards on a board).</summary>
    public HelixDashboardLayout LayoutMode { get => GetValue(LayoutModeProperty); set => SetValue(LayoutModeProperty, value); }

    /// <summary>Minimum column width: below it the dashboard gets wider than its container (put it in a horizontal ScrollViewer).</summary>
    public double ColumnMinWidth { get => GetValue(ColumnMinWidthProperty); set => SetValue(ColumnMinWidthProperty, value); }

    /// <summary>Columns layout: the last tile of each column grows so that all columns end at the same height.</summary>
    public bool StretchColumns { get => GetValue(StretchColumnsProperty); set => SetValue(StretchColumnsProperty, value); }
    public bool IsEditing { get => GetValue(IsEditingProperty); set => SetValue(IsEditingProperty, value); }

    /// <summary>Order, column, span and visibility of the tiles; updated after each user change.</summary>
    public IReadOnlyList<HelixTileState>? Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? Toolbar { get => GetValue(ToolbarProperty); set => SetValue(ToolbarProperty, value); }

    /// <summary>The title row with "Edit layout" (and your Toolbar).</summary>
    public bool ShowToolbar { get => GetValue(ShowToolbarProperty); set => SetValue(ShowToolbarProperty, value); }
    public IReadOnlyList<HelixDashboardTile> HiddenTiles { get => _hiddenTiles; private set => SetAndRaise(HiddenTilesProperty, ref _hiddenTiles, value); }

    /// <summary>The hidden tiles as chips (title and tile), for the template.</summary>
    public IReadOnlyList<HelixHiddenTile> HiddenChips { get => _hiddenChips; private set => SetAndRaise(HiddenChipsProperty, ref _hiddenChips, value); }

    /// <summary>Raised after the user moved, resized, hid or restored a tile; read <see cref="Layout"/> to save it.</summary>
    public event EventHandler<RoutedEventArgs>? LayoutChanged { add => AddHandler(LayoutChangedEvent, value); remove => RemoveHandler(LayoutChangedEvent, value); }

    public static string EditText => HelixTexts.Current.EditLayout;
    public static string DoneText => HelixTexts.Current.Done;
    public static string HiddenText => HelixTexts.Current.HiddenTiles;

    /// <summary>Toggles edit mode (the toolbar button).</summary>
    public ICommand ToggleEditCommand => new HelixCommand(() => SetCurrentValue(IsEditingProperty, !IsEditing));

    /// <summary>Shows a hidden tile again (the hidden-tiles chips).</summary>
    public ICommand RestoreCommand => new HelixCommand<object>(o =>
    {
        if (o is HelixDashboardTile tile) SetHidden(tile, false);
        else if (o is HelixHiddenTile chip) SetHidden(chip.Tile, false);
    });

    /// <summary>The items panel (set by HelixDashboardPanel when it lays out).</summary>
    internal HelixDashboardPanel? Panel { get; set; }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not HelixDashboardTile;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new HelixDashboardTile();

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is HelixDashboardTile tile) tile.IsEditing = IsEditing;
    }

    internal IEnumerable<HelixDashboardTile> Tiles => Items.Cast<object>().Select(i => i as HelixDashboardTile ?? ContainerFromItem(i!) as HelixDashboardTile).OfType<HelixDashboardTile>();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == IsEditingProperty)
        {
            foreach (var tile in Tiles) tile.IsEditing = IsEditing;
            PseudoClasses.Set(":editing", IsEditing);
        }
        else if (p == LayoutProperty)
        {
            ApplyLayout();
        }
        else if (p == ColumnCountProperty || p == GapProperty || p == LayoutModeProperty || p == ColumnMinWidthProperty || p == StretchColumnsProperty)
        {
            Panel?.InvalidateMeasure();
            foreach (var tile in Tiles) tile.UpdateSpanText();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ApplyLayout();
    }

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
        Panel?.InvalidateMeasure();
        RaiseEvent(new RoutedEventArgs(LayoutChangedEvent, this));
    }

    private void UpdateHidden()
    {
        HiddenTiles = Tiles.Where(t => t.IsHidden && t.CanHide).ToList();
        HiddenChips = HiddenTiles.Select(t => new HelixHiddenTile(t, t.Title ?? t.TileId ?? "")).ToList();
        PseudoClasses.Set(":has-hidden", HiddenTiles.Count > 0);
    }

    /// <summary>Moves a tile one place earlier (-1) or later (+1) in the order (in its column for the Columns layout).</summary>
    public void Shift(HelixDashboardTile tile, int direction)
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
            var neighbour = direction < 0 ? sameColumn.LastOrDefault(t => Items.IndexOf(t) < index) : sameColumn.FirstOrDefault(t => Items.IndexOf(t) > index);
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
    public void ShiftColumn(HelixDashboardTile tile, int direction)
    {
        var current = Panel?.ColumnOf(tile) ?? Math.Max(0, tile.Column);
        var target = Math.Clamp(current + direction, 0, Math.Max(0, ColumnCount - Math.Clamp(tile.Span, 1, ColumnCount)));
        if (target == current && tile.Column >= 0) return;
        tile.Column = target;
        SaveLayout();
    }

    /// <summary>Makes a tile wider (+1) or narrower (-1), within MinSpan, MaxSpan and ColumnCount.</summary>
    public void Resize(HelixDashboardTile tile, int delta)
    {
        tile.Span = Math.Clamp(tile.Span + delta, Math.Max(1, tile.MinSpan), Math.Min(ColumnCount, tile.MaxSpan));
        if (tile.Column >= 0) tile.Column = Math.Min(tile.Column, ColumnCount - tile.Span);
        SaveLayout();
    }

    public void SetHidden(HelixDashboardTile tile, bool hidden)
    {
        tile.IsHidden = hidden;
        SaveLayout();
    }

    // ----- Drag (edit mode) ---------------------------------------------------------------------

    private Canvas? _markerLayer;
    private Border? _marker;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _markerLayer = e.NameScope.Find<Canvas>("PART_MarkerLayer");
    }

    /// <summary>Draws the drop marker (a rectangle in the panel's coordinates) over the tiles.</summary>
    internal void ShowMarker(Rect? rect, Control panel)
    {
        if (_markerLayer is null) return;
        if (rect is not { } r)
        {
            if (_marker is not null) _marker.IsVisible = false;
            return;
        }

        _marker ??= new Border { CornerRadius = new CornerRadius(1.5), IsHitTestVisible = false };
        if (_marker.Parent is null) _markerLayer.Children.Add(_marker);
        _marker.Background = HelixDraw.Brush(this, HelixKeys.Primary, Colors.SteelBlue);
        var origin = panel.TranslatePoint(new Point(r.X, r.Y), _markerLayer) ?? new Point(r.X, r.Y);
        Canvas.SetLeft(_marker, origin.X);
        Canvas.SetTop(_marker, origin.Y);
        _marker.Width = r.Width;
        _marker.Height = r.Height;
        _marker.IsVisible = true;
    }

    private HelixDashboardTile? _dragging;
    private HelixDashboardTile? _dropBefore;
    private bool _dropToEnd;
    private int _startColumn;

    internal void BeginDrag(HelixDashboardTile tile)
    {
        if (!IsEditing || ItemsSource is not null) return;
        _dragging = tile;
        _dropBefore = null;
        _dropToEnd = false;
        _startColumn = tile.Column;
        tile.IsDragging = true;
    }

    internal void DragOver(HelixDashboardTile tile, PointerEventArgs e)
    {
        if (_dragging != tile || Panel is not { } panel) return;
        var (column, before) = panel.DropTarget(tile, e.GetPosition(panel));

        // The column follows the pointer live; the place in the column is applied on drop.
        if (LayoutMode == HelixDashboardLayout.Columns && column != tile.Column) tile.Column = column;
        _dropBefore = before;
        _dropToEnd = before is null;
        panel.ShowDropMarker(before, column, tile);
    }

    internal void EndDrag(HelixDashboardTile tile, bool cancelled)
    {
        if (_dragging != tile) return;
        tile.IsDragging = false;
        _dragging = null;
        Panel?.ShowDropMarker(null, -1, null);
        if (cancelled)
        {
            tile.Column = _startColumn;
            return;
        }

        // The order changes once the drag is over.
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
}

/// <summary>
/// A dashboard tile: TileId (for the saved layout), Title, Actions, DefaultSpan / MinSpan / MaxSpan, Column (Columns layout),
/// CanHide, IsPlain. In edit mode, drag it by its header to move it.
/// </summary>
[TemplatePart("PART_Handle", typeof(Control))]
public class HelixDashboardTile : ContentControl
{
    public static readonly StyledProperty<string?> TileIdProperty = AvaloniaProperty.Register<HelixDashboardTile, string?>(nameof(TileId));
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixDashboardTile, string?>(nameof(Title));
    public static readonly StyledProperty<object?> ActionsProperty = AvaloniaProperty.Register<HelixDashboardTile, object?>(nameof(Actions));
    public static readonly StyledProperty<int> DefaultSpanProperty = AvaloniaProperty.Register<HelixDashboardTile, int>(nameof(DefaultSpan), 1);
    public static readonly StyledProperty<int> MinSpanProperty = AvaloniaProperty.Register<HelixDashboardTile, int>(nameof(MinSpan), 1);
    public static readonly StyledProperty<int> MaxSpanProperty = AvaloniaProperty.Register<HelixDashboardTile, int>(nameof(MaxSpan), 12);
    public static readonly StyledProperty<bool> CanHideProperty = AvaloniaProperty.Register<HelixDashboardTile, bool>(nameof(CanHide), true);
    public static readonly StyledProperty<bool> IsPlainProperty = AvaloniaProperty.Register<HelixDashboardTile, bool>(nameof(IsPlain));
    public static readonly StyledProperty<int> SpanProperty = AvaloniaProperty.Register<HelixDashboardTile, int>(nameof(Span), 1);
    public static readonly StyledProperty<int> ColumnProperty = AvaloniaProperty.Register<HelixDashboardTile, int>(nameof(Column), -1);
    public static readonly StyledProperty<bool> IsHiddenProperty = AvaloniaProperty.Register<HelixDashboardTile, bool>(nameof(IsHidden));
    public static readonly StyledProperty<bool> IsEditingProperty = AvaloniaProperty.Register<HelixDashboardTile, bool>(nameof(IsEditing));
    public static readonly StyledProperty<bool> IsDraggingProperty = AvaloniaProperty.Register<HelixDashboardTile, bool>(nameof(IsDragging));
    public static readonly DirectProperty<HelixDashboardTile, string> SpanTextProperty = AvaloniaProperty.RegisterDirect<HelixDashboardTile, string>(nameof(SpanText), t => t.SpanText);
    public static readonly DirectProperty<HelixDashboardTile, bool> IsColumnsLayoutProperty = AvaloniaProperty.RegisterDirect<HelixDashboardTile, bool>(nameof(IsColumnsLayout), t => t.IsColumnsLayout);

    private string _spanText = "1";
    private bool _isColumnsLayout;
    private Control? _handle;
    private Point? _down;
    private bool _spanSet;

    public string? TileId { get => GetValue(TileIdProperty); set => SetValue(TileIdProperty, value); }
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public int DefaultSpan { get => GetValue(DefaultSpanProperty); set => SetValue(DefaultSpanProperty, value); }
    public int MinSpan { get => GetValue(MinSpanProperty); set => SetValue(MinSpanProperty, value); }
    public int MaxSpan { get => GetValue(MaxSpanProperty); set => SetValue(MaxSpanProperty, value); }

    /// <summary>The user can hide it in edit mode. False for tiles whose visibility you drive (e.g. by a filter binding).</summary>
    public bool CanHide { get => GetValue(CanHideProperty); set => SetValue(CanHideProperty, value); }

    /// <summary>No card around the content (for a KPI row, a banner…).</summary>
    public bool IsPlain { get => GetValue(IsPlainProperty); set => SetValue(IsPlainProperty, value); }

    /// <summary>Columns taken (1 … ColumnCount).</summary>
    public int Span { get => GetValue(SpanProperty); set => SetValue(SpanProperty, value); }

    /// <summary>Columns layout: first column of the tile (0-based); -1 places it in the shortest column.</summary>
    public int Column { get => GetValue(ColumnProperty); set => SetValue(ColumnProperty, value); }
    public bool IsHidden { get => GetValue(IsHiddenProperty); set => SetValue(IsHiddenProperty, value); }

    /// <summary>Set by the dashboard: shows the edit buttons.</summary>
    public bool IsEditing { get => GetValue(IsEditingProperty); set => SetValue(IsEditingProperty, value); }

    /// <summary>True while the user drags this tile.</summary>
    public bool IsDragging { get => GetValue(IsDraggingProperty); internal set => SetValue(IsDraggingProperty, value); }
    public string SpanText { get => _spanText; private set => SetAndRaise(SpanTextProperty, ref _spanText, value); }
    public bool IsColumnsLayout { get => _isColumnsLayout; private set => SetAndRaise(IsColumnsLayoutProperty, ref _isColumnsLayout, value); }

    private HelixDashboard? Dashboard => this.FindLogicalAncestorOfType<HelixDashboard>() ?? this.FindAncestorOfType<HelixDashboard>();

    internal void UpdateSpanText()
    {
        SpanText = $"{Span} / {Dashboard?.ColumnCount ?? 3}";
        IsColumnsLayout = Dashboard?.LayoutMode == HelixDashboardLayout.Columns;
    }

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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!_spanSet) SetCurrentValue(SpanProperty, DefaultSpan);
        if (Dashboard is { } dashboard) IsEditing = dashboard.IsEditing;
        UpdateSpanText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SpanProperty)
        {
            if (change.Priority <= BindingPriority.LocalValue) _spanSet = true;
            UpdateSpanText();
        }

        if (change.Property == IsHiddenProperty) IsVisible = !IsHidden;
        if (change.Property == SpanProperty || change.Property == ColumnProperty || change.Property == IsHiddenProperty) (Parent as Panel ?? this.GetVisualParent() as Panel)?.InvalidateMeasure();
        if (change.Property == IsEditingProperty) PseudoClasses.Set(":editing", IsEditing);
        if (change.Property == IsDraggingProperty) PseudoClasses.Set(":dragging", IsDragging);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_handle is not null)
        {
            _handle.PointerPressed -= OnHandleDown;
            _handle.PointerMoved -= OnHandleMove;
            _handle.PointerReleased -= OnHandleUp;
            _handle.PointerCaptureLost -= OnHandleLost;
        }

        base.OnApplyTemplate(e);
        _handle = e.NameScope.Find<Control>("PART_Handle");
        if (_handle is not null)
        {
            _handle.PointerPressed += OnHandleDown;
            _handle.PointerMoved += OnHandleMove;
            _handle.PointerReleased += OnHandleUp;
            _handle.PointerCaptureLost += OnHandleLost;
        }
    }

    private void OnHandleDown(object? sender, PointerPressedEventArgs e)
    {
        if (Dashboard?.IsEditing != true || (e.Source as Visual)?.FindAncestorOfType<Button>(includeSelf: true) is not null) return;
        _down = e.GetPosition(this);
        e.Pointer.Capture(_handle);
        e.Handled = true;
    }

    private void OnHandleMove(object? sender, PointerEventArgs e)
    {
        if (_down is not { } down) return;
        if (!IsDragging)
        {
            var p = e.GetPosition(this);
            if (Math.Abs(p.X - down.X) < 4 && Math.Abs(p.Y - down.Y) < 4) return;
            Dashboard?.BeginDrag(this);
        }

        Dashboard?.DragOver(this, e);
    }

    private void OnHandleUp(object? sender, PointerReleasedEventArgs e)
    {
        _down = null;
        if (IsDragging) Dashboard?.EndDrag(this, cancelled: false);
        e.Pointer.Capture(null);
    }

    private void OnHandleLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _down = null;
        if (IsDragging) Dashboard?.EndDrag(this, cancelled: true);
    }
}

/// <summary>
/// Lays the dashboard tiles out: in rows (each tile takes Span columns), or in columns where tiles stack
/// in their column (Column, Span) without gaps.
/// </summary>
public class HelixDashboardPanel : Panel
{
    private HelixDashboard? _owner;
    private HelixDashboard? Owner => _owner ??= this.FindAncestorOfType<HelixDashboard>();

    private int Columns => Math.Max(1, Owner?.ColumnCount ?? 3);
    private double Gap => Owner?.Gap ?? 16;
    private bool IsColumns => Owner?.LayoutMode == HelixDashboardLayout.Columns;

    // Where each visible tile ended up during the last layout pass (for drag targets and column moves).
    private readonly Dictionary<Control, (Rect Rect, int Column)> _placed = [];
    private Rect? _marker;

    public HelixDashboardPanel() => Background = Brushes.Transparent;

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _owner = null;
    }

    internal int ColumnOf(HelixDashboardTile tile) => _placed.TryGetValue(tile, out var p) ? p.Column : Math.Max(0, tile.Column);

    /// <summary>Shows where the dragged tile will land: above <paramref name="before"/>, or at the bottom of its column.</summary>
    internal void ShowDropMarker(HelixDashboardTile? before, int column, HelixDashboardTile? dragged)
    {
        Rect? marker = null;
        if (dragged is not null)
        {
            var cell = CellWidth(Bounds.Width);
            var span = Math.Clamp(dragged.Span, 1, Columns);
            var x = (IsColumns ? column : 0) * (cell + Gap);
            var width = IsColumns ? SpanWidth(cell, span) : Bounds.Width;
            if (before is not null && _placed.TryGetValue(before, out var b))
            {
                marker = IsColumns ? new Rect(x, b.Rect.Top - Gap / 2 - 1.5, width, 3) : new Rect(b.Rect.Left - Gap / 2 - 1.5, b.Rect.Top, 3, b.Rect.Height);
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
            Owner?.ShowMarker(marker, this);
        }
    }

    private double PanelWidth(double available)
    {
        var min = (Owner?.ColumnMinWidth ?? 0) * Columns + Gap * (Columns - 1);
        var width = double.IsInfinity(available) ? 1000 : available;
        return Math.Max(width, min);
    }

    private double CellWidth(double width) => Math.Max(0, (width - Gap * (Columns - 1)) / Columns);

    private double SpanWidth(double cell, int span) => cell * span + Gap * (span - 1);

    private IEnumerable<Control> Visible => Children.Where(c => c.IsVisible);

    private IEnumerable<List<(Control Child, int Span)>> Rows()
    {
        var row = new List<(Control, int)>();
        var used = 0;
        foreach (var child in Visible)
        {
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
    private (List<(Control Child, Rect Rect, int Column)> Items, double Height) PlaceColumns(double width, bool measure)
    {
        var cell = CellWidth(width);
        var bottoms = new double[Columns];
        var result = new List<(Control, Rect, int)>();
        foreach (var child in Visible)
        {
            var tile = child as HelixDashboardTile;
            var span = Math.Clamp(tile?.Span ?? 1, 1, Columns);
            var column = tile?.Column ?? -1;
            if (column < 0) column = Enumerable.Range(0, Columns - span + 1).OrderBy(c => Enumerable.Range(c, span).Max(i => bottoms[i])).First();
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
                if (rect.Bottom < height) result[last.i] = (last.r.Item1, new Rect(rect.X, rect.Y, rect.Width, height - rect.Y), last.r.Item3);
            }
        }

        return (result, height);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Owner is { } owner) owner.Panel = this;
        var width = PanelWidth(availableSize.Width);
        if (IsColumns) return new Size(width, PlaceColumns(width, measure: true).Height);
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

        return new Size(width, finalSize.Height);
    }

    /// <summary>
    /// Where a dragged tile goes for a pointer position: its column (Columns layout) and the tile it is placed before
    /// (null = at the end of the order).
    /// </summary>
    internal (int Column, HelixDashboardTile? Before) DropTarget(HelixDashboardTile dragged, Point point)
    {
        var cell = CellWidth(Bounds.Width);
        var span = Math.Clamp(dragged.Span, 1, Columns);
        var others = _placed.Where(p => p.Key != dragged && p.Key is HelixDashboardTile)
            .Select(p => (Tile: (HelixDashboardTile)p.Key, p.Value.Rect, p.Value.Column))
            .OrderBy(p => Children.IndexOf(p.Tile))
            .ToList();

        if (IsColumns)
        {
            var column = (int)Math.Floor((point.X - (SpanWidth(cell, span) - cell) / 2) / Math.Max(1, cell + Gap));
            column = Math.Clamp(column, 0, Columns - span);
            var inColumn = others.Where(o => o.Column < column + span && column < o.Column + Math.Clamp(o.Tile.Span, 1, Columns)).ToList();
            var before = inColumn.FirstOrDefault(o => point.Y < o.Rect.Top + o.Rect.Height / 2).Tile;
            if (before is null && inColumn.Count > 0)
            {
                // Below the last tile of the column: just after it in the order.
                var lastIndex = Children.IndexOf(inColumn[^1].Tile);
                before = others.Select(o => o.Tile).FirstOrDefault(t => Children.IndexOf(t) > lastIndex);
            }

            return (column, before);
        }

        // Rows: the tile under the pointer, or the first tile further in the reading order.
        var hit = others.FirstOrDefault(o => o.Rect.Contains(point)).Tile
                  ?? others.FirstOrDefault(o => point.Y < o.Rect.Top || (point.Y < o.Rect.Bottom && point.X < o.Rect.Left)).Tile;
        return (dragged.Column, hit);
    }
}
