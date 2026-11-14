using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Helix.Wpf;

/// <summary>
/// A ready-made Kanban card, edited by <see cref="HelixKanbanBoard"/>. Changes notify (bind it, save it in the board's events).
/// Put your own object in <see cref="Data"/> to keep a link to it.
/// </summary>
public sealed class HelixKanbanCard : INotifyPropertyChanged
{
    private string _title, _column;
    private string? _description, _assignee, _color;
    private DateTime? _dueDate;
    private IReadOnlyList<string> _tags = [];

    public HelixKanbanCard(string title, string column) => (_title, _column) = (title, column);

    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get => _title; set => Set(ref _title, value); }

    /// <summary>The key of the column the card is in (the board writes it on a move).</summary>
    public string Column { get => _column; set => Set(ref _column, value); }
    public string? Description { get => _description; set => Set(ref _description, value); }
    public string? Assignee { get => _assignee; set => Set(ref _assignee, value); }
    public DateTime? DueDate { get => _dueDate; set => Set(ref _dueDate, value); }

    /// <summary>A theme color name ("primary", "success", "warning", "danger", "info") or "#RRGGBB"; null for none.</summary>
    public string? Color { get => _color; set => Set(ref _color, value); }
    public IReadOnlyList<string> Tags { get => _tags; set => Set(ref _tags, value); }

    /// <summary>Your own object behind the card (an entity, an id…).</summary>
    public object? Data { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// A Kanban that edits its cards itself: + opens the editor on a new card of that column, a click (or Enter) opens the editor
/// (title, description, column, due date, assignee, tags, color), the editor deletes with a confirmation. Needs a HelixHost.
/// Give it an ObservableCollection of <see cref="HelixKanbanCard"/>: the board adds, edits, moves and removes cards in it.
/// </summary>
public class HelixKanbanBoard : HelixKanban
{
    static HelixKanbanBoard() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixKanbanBoard), new FrameworkPropertyMetadata(typeof(HelixKanban)));

    public static readonly DependencyProperty IsReadOnlyProperty = HelixProperty.Register<HelixKanbanBoard, bool>(nameof(IsReadOnly), false,
        (d, _) => ((HelixKanbanBoard)d).ShowAddButton = !((HelixKanbanBoard)d).IsReadOnly);
    public static readonly DependencyProperty DoneColumnProperty = HelixProperty.Register<HelixKanbanBoard, string?>(nameof(DoneColumn), null, (d, _) => ((HelixKanbanBoard)d).Build());
    public static readonly DependencyProperty ColorsProperty = HelixProperty.Register<HelixKanbanBoard, IReadOnlyList<string?>>(nameof(Colors),
        new string?[] { null, "primary", "info", "success", "warning", "danger" });
    public static readonly RoutedEvent CardAddedEvent = EventManager.RegisterRoutedEvent(nameof(CardAdded), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixKanbanBoard));
    public static readonly RoutedEvent CardChangedEvent = EventManager.RegisterRoutedEvent(nameof(CardChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixKanbanBoard));
    public static readonly RoutedEvent CardDeletedEvent = EventManager.RegisterRoutedEvent(nameof(CardDeleted), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HelixKanbanBoard));

    public HelixKanbanBoard()
    {
        ColumnPath = nameof(HelixKanbanCard.Column);
        ShowAddButton = true;
        CardTemplate = new DataTemplate(typeof(HelixKanbanCard)) { VisualTree = new FrameworkElementFactory(typeof(HelixKanbanCardView)) };
        AddRequested += async (_, e) => { if (e is HelixEventArgs<string> args) await AddAsync(args.Value); };
        CardClick += async (_, e) => { if (e is HelixEventArgs<object> { Value: HelixKanbanCard card }) await EditAsync(card); };
    }

    /// <summary>No add, edit or delete; cards still move (use CanMove to stop that).</summary>
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>Cards in this column are never shown overdue (the last column by default).</summary>
    public string? DoneColumn { get => (string?)GetValue(DoneColumnProperty); set => SetValue(DoneColumnProperty, value); }

    /// <summary>The colors offered in the editor (null = no color).</summary>
    public IReadOnlyList<string?> Colors { get => (IReadOnlyList<string?>)GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }

    /// <summary>A card was added (save it here). Raised with <see cref="HelixEventArgs{T}"/> of the card.</summary>
    public event RoutedEventHandler CardAdded { add => AddHandler(CardAddedEvent, value); remove => RemoveHandler(CardAddedEvent, value); }

    /// <summary>A card was saved in the editor. Raised with <see cref="HelixEventArgs{T}"/> of the card.</summary>
    public event RoutedEventHandler CardChanged { add => AddHandler(CardChangedEvent, value); remove => RemoveHandler(CardChangedEvent, value); }

    /// <summary>A card was deleted. Raised with <see cref="HelixEventArgs{T}"/> of the card.</summary>
    public event RoutedEventHandler CardDeleted { add => AddHandler(CardDeletedEvent, value); remove => RemoveHandler(CardDeletedEvent, value); }

    private IList? Cards => ItemsSource as IList;

    /// <summary>Opens the editor on a new card of <paramref name="column"/> (the + button does it).</summary>
    public async Task<HelixKanbanCard?> AddAsync(string? column = null)
    {
        if (IsReadOnly || Cards is not { IsReadOnly: false } cards) return null;
        var card = new HelixKanbanCard("", column ?? Columns?.FirstOrDefault()?.Key ?? "");
        if (!await HelixDialogs.ShowAsync<bool>(handle => Editor(card, handle, isNew: true), HelixTexts.Current.KanbanNewCard, width: 520)) return null;
        cards.Add(card);
        Build();
        RaiseEvent(new HelixEventArgs<HelixKanbanCard>(CardAddedEvent, card));
        return card;
    }

    /// <summary>Opens the editor on a card (a click does it). True when the card was saved or deleted.</summary>
    public async Task<bool> EditAsync(HelixKanbanCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (IsReadOnly) return false;
        var column = card.Column;
        if (!await HelixDialogs.ShowAsync<bool>(handle => Editor(card, handle, isNew: false), HelixTexts.Current.KanbanEditCard, width: 520)) return false;
        if (Cards is { } cards && !cards.Contains(card))
        {
            Build();
            RaiseEvent(new HelixEventArgs<HelixKanbanCard>(CardDeletedEvent, card));
            return true;
        }

        // Moved to another column in the editor: it goes to the end of that column.
        if (card.Column != column && Cards is { IsReadOnly: false } list)
        {
            list.Remove(card);
            list.Add(card);
        }

        Build();
        RaiseEvent(new HelixEventArgs<HelixKanbanCard>(CardChangedEvent, card));
        return true;
    }

    /// <summary>Removes a card after a confirmation; true when it was removed.</summary>
    public async Task<bool> DeleteAsync(HelixKanbanCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        var texts = HelixTexts.Current;
        if (Cards is not { IsReadOnly: false } cards
            || !await HelixConfirm.AskAsync(string.Format(CultureInfo.CurrentCulture, texts.KanbanDeleteQuestion, card.Title), texts.KanbanDeleteMessage, danger: true, confirmText: texts.KanbanDeleteCard))
        {
            return false;
        }

        cards.Remove(card);
        return true;
    }

    internal bool IsDone(HelixKanbanCard card) => card.Column == (DoneColumn ?? Columns?.LastOrDefault()?.Key);

    private FrameworkElement Editor(HelixKanbanCard card, HelixDialogHandle<bool> handle, bool isNew)
    {
        var texts = HelixTexts.Current;
        var title = new HelixTextField { Label = texts.KanbanTitle, Text = card.Title, IsRequired = true };
        var description = new HelixTextField { Label = texts.KanbanDescription, Text = card.Description, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 90, VerticalContentAlignment = VerticalAlignment.Top };
        var columns = Columns?.ToList() ?? [];
        var column = new HelixSelect { Label = texts.KanbanColumn, ItemsSource = columns, DisplayMemberPath = nameof(HelixKanbanColumn.Title), SelectedItem = columns.FirstOrDefault(c => c.Key == card.Column) };
        var due = new HelixDatePicker { Label = texts.KanbanDueDate, SelectedDate = card.DueDate };
        var assignee = new HelixTextField { Label = texts.KanbanAssignee, Text = card.Assignee, Icon = "user", IsClearable = true };
        var tags = new HelixTextField { Label = texts.KanbanTags, Text = string.Join(", ", card.Tags), Hint = texts.KanbanTagsHint };
        var color = card.Color;
        var swatches = new WrapPanel();
        void PaintSwatches()
        {
            foreach (var child in swatches.Children.OfType<Button>()) child.SetResourceReference(BorderBrushProperty, Equals(child.Tag, color) ? HelixKeys.Text : HelixKeys.BorderStrong);
        }

        foreach (var option in Colors)
        {
            var swatch = new Button
            {
                Tag = option, Width = 26, Height = 26, Margin = new Thickness(0, 0, 8, 8), Padding = default, BorderThickness = new Thickness(2), Cursor = Cursors.Hand,
                ToolTip = option ?? texts.KanbanNoColor,
                Template = SwatchTemplate(),
                Background = option is null ? HelixTheme.Brush(this, HelixKeys.Surface, System.Windows.Media.Colors.White) : HelixTheme.Resolve(this, option) ?? Brushes.Transparent,
                Content = option is null ? new HelixIcon { Icon = "x", Size = 12, Foreground = HelixTheme.Brush(this, HelixKeys.TextSubtle, System.Windows.Media.Colors.Gray) } : null,
            };
            swatch.Click += (_, _) => { color = option; PaintSwatches(); };
            swatches.Children.Add(swatch);
        }

        PaintSwatches();

        var save = new HelixButton { Content = texts.Save, Icon = "check", IsDefault = true };
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(title.Text))
            {
                title.Error = texts.KanbanTitleRequired;
                return;
            }

            card.Title = title.Text.Trim();
            card.Description = string.IsNullOrWhiteSpace(description.Text) ? null : description.Text.Trim();
            card.Assignee = string.IsNullOrWhiteSpace(assignee.Text) ? null : assignee.Text.Trim();
            card.Tags = (tags.Text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
            card.DueDate = due.SelectedDate;
            card.Color = color;
            if (column.SelectedItem is HelixKanbanColumn chosen) card.Column = chosen.Key;
            handle.Close(true);
        };
        var cancel = new HelixButton { Content = texts.Cancel, Variant = HelixVariant.Outline, Tone = HelixTone.Neutral, Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => handle.Close(false);
        var footer = new DockPanel { Margin = new Thickness(0, 6, 0, 0), LastChildFill = false };
        if (!isNew)
        {
            var delete = new HelixButton { Content = texts.KanbanDeleteCard, Icon = "trash", Variant = HelixVariant.Ghost, Tone = HelixTone.Danger };
            delete.Click += async (_, _) =>
            {
                if (await DeleteAsync(card)) handle.Close(true);
            };
            DockPanel.SetDock(delete, Dock.Left);
            footer.Children.Add(delete);
        }

        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        DockPanel.SetDock(buttons, Dock.Right);
        footer.Children.Add(buttons);

        static Grid Row(FrameworkElement a, FrameworkElement b)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(b, 2);
            grid.Children.Add(a);
            grid.Children.Add(b);
            return grid;
        }

        var colorLabel = new TextBlock { Text = texts.KanbanColor, FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) };
        colorLabel.SetResourceReference(TextBlock.ForegroundProperty, HelixKeys.Text);
        var root = new StackPanel();
        foreach (var part in new FrameworkElement[] { title, description, Row(column, due), Row(assignee, tags), colorLabel })
        {
            part.Margin = new Thickness(0, 0, 0, part == colorLabel ? 6 : 14);
            root.Children.Add(part);
        }

        root.Children.Add(swatches);
        root.Children.Add(footer);
        root.Loaded += (_, _) => title.Focus();
        return root;
    }

    private static ControlTemplate SwatchTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(13));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding(nameof(Background)) { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding(nameof(BorderBrush)) { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding(nameof(BorderThickness)) { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        content.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(content);
        return new ControlTemplate(typeof(Button)) { VisualTree = border };
    }
}

/// <summary>The default look of a <see cref="HelixKanbanCard"/> on a <see cref="HelixKanbanBoard"/> (its DataContext is the card).</summary>
public class HelixKanbanCardView : ContentControl
{
    public HelixKanbanCardView()
    {
        Focusable = false;
        DataContextChanged += (_, _) => Draw();
        Loaded += (_, _) => Draw();
    }

    private void Draw()
    {
        if (DataContext is not HelixKanbanCard card)
        {
            Content = null;
            return;
        }

        var board = FindBoard();
        var body = new StackPanel();
        body.Children.Add(Text(card.Title, 13.5, HelixKeys.Text, FontWeights.SemiBold));
        if (!string.IsNullOrWhiteSpace(card.Description))
        {
            var description = Text(card.Description, 12.5, HelixKeys.TextMuted, FontWeights.Normal);
            description.MaxHeight = 36;
            description.TextTrimming = TextTrimming.CharacterEllipsis;
            description.Margin = new Thickness(0, 4, 0, 0);
            body.Children.Add(description);
        }

        if (card.Tags.Count > 0)
        {
            var tags = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            foreach (var tag in card.Tags)
            {
                var chip = new Border { CornerRadius = new CornerRadius(999), Padding = new Thickness(8, 1, 8, 1), Margin = new Thickness(0, 0, 4, 4), Child = Text(tag, 11.5, HelixKeys.TextMuted, FontWeights.SemiBold) };
                chip.SetResourceReference(Border.BackgroundProperty, HelixKeys.Surface2);
                tags.Children.Add(chip);
            }

            body.Children.Add(tags);
        }

        if (card.DueDate is not null || !string.IsNullOrWhiteSpace(card.Assignee))
        {
            var footer = new DockPanel { Margin = new Thickness(0, 6, 0, 0) };
            if (!string.IsNullOrWhiteSpace(card.Assignee))
            {
                var avatar = new HelixAvatar { DisplayName = card.Assignee, Size = HelixSize.Sm, ToolTip = card.Assignee };
                DockPanel.SetDock(avatar, Dock.Right);
                footer.Children.Add(avatar);
            }

            if (card.DueDate is { } due)
            {
                var overdue = due.Date < DateTime.Today && board?.IsDone(card) != true;
                var key = overdue ? HelixKeys.Danger : HelixKeys.TextMuted;
                var date = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, ToolTip = overdue ? HelixTexts.Current.KanbanOverdue : null };
                var icon = new HelixIcon { Icon = "calendar", Size = 13, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center };
                icon.SetResourceReference(HelixIcon.ForegroundProperty, key);
                date.Children.Add(icon);
                date.Children.Add(Text(due.ToString("d MMM", CultureInfo.CurrentCulture), 12, key, overdue ? FontWeights.SemiBold : FontWeights.Normal));
                footer.Children.Add(date);
            }

            body.Children.Add(footer);
        }

        if (card.Color is not { } color)
        {
            Content = body;
            return;
        }

        // A colored stripe on the left edge of the card.
        var stripe = new Border
        {
            Width = 4, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(-12, -10, 0, -10), CornerRadius = new CornerRadius(4, 0, 0, 4),
            Background = HelixTheme.Resolve(this, color) ?? Brushes.Transparent,
        };
        var grid = new Grid();
        grid.Children.Add(stripe);
        grid.Children.Add(body);
        Content = grid;
    }

    private HelixKanbanBoard? FindBoard()
    {
        DependencyObject? node = this;
        while (node is not null and not HelixKanbanBoard) node = VisualTreeHelper.GetParent(node);
        return node as HelixKanbanBoard;
    }

    private static TextBlock Text(string text, double size, string brush, FontWeight weight)
    {
        var block = new TextBlock { Text = text, FontSize = size, FontWeight = weight, TextWrapping = TextWrapping.Wrap };
        block.SetResourceReference(TextBlock.ForegroundProperty, brush);
        return block;
    }
}
