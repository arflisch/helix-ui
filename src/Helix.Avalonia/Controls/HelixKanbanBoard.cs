using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Helix.Avalonia;

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
    public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<HelixKanbanBoard, bool>(nameof(IsReadOnly));
    public static readonly StyledProperty<string?> DoneColumnProperty = AvaloniaProperty.Register<HelixKanbanBoard, string?>(nameof(DoneColumn));
    public static readonly StyledProperty<IReadOnlyList<string?>> ColorsProperty =
        AvaloniaProperty.Register<HelixKanbanBoard, IReadOnlyList<string?>>(nameof(Colors), [null, "primary", "info", "success", "warning", "danger"]);
    public static readonly RoutedEvent<HelixEventArgs<HelixKanbanCard>> CardAddedEvent = RoutedEvent.Register<HelixKanbanBoard, HelixEventArgs<HelixKanbanCard>>(nameof(CardAdded), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<HelixEventArgs<HelixKanbanCard>> CardChangedEvent = RoutedEvent.Register<HelixKanbanBoard, HelixEventArgs<HelixKanbanCard>>(nameof(CardChanged), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<HelixEventArgs<HelixKanbanCard>> CardDeletedEvent = RoutedEvent.Register<HelixKanbanBoard, HelixEventArgs<HelixKanbanCard>>(nameof(CardDeleted), RoutingStrategies.Bubble);

    protected override Type StyleKeyOverride => typeof(HelixKanban);

    public HelixKanbanBoard()
    {
        ColumnPath = nameof(HelixKanbanCard.Column);
        ShowAddButton = true;
        CardTemplate = new FuncDataTemplate<HelixKanbanCard>((card, _) => CardView(card), supportsRecycling: false);
        AddRequested += async (_, e) => await AddAsync(e.Item);
        CardClick += async (_, e) => { if (e.Item is HelixKanbanCard card) await EditAsync(card); };
    }

    /// <summary>No add, edit or delete; cards still move (use CanMove to stop that).</summary>
    public bool IsReadOnly { get => GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>Cards in this column are never shown overdue (the last column by default).</summary>
    public string? DoneColumn { get => GetValue(DoneColumnProperty); set => SetValue(DoneColumnProperty, value); }

    /// <summary>The colors offered in the editor (null = no color).</summary>
    public IReadOnlyList<string?> Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }

    /// <summary>A card was added (save it here).</summary>
    public event EventHandler<HelixEventArgs<HelixKanbanCard>>? CardAdded { add => AddHandler(CardAddedEvent, value); remove => RemoveHandler(CardAddedEvent, value); }
    public event EventHandler<HelixEventArgs<HelixKanbanCard>>? CardChanged { add => AddHandler(CardChangedEvent, value); remove => RemoveHandler(CardChangedEvent, value); }
    public event EventHandler<HelixEventArgs<HelixKanbanCard>>? CardDeleted { add => AddHandler(CardDeletedEvent, value); remove => RemoveHandler(CardDeletedEvent, value); }

    private System.Collections.IList? Cards => ItemsSource as System.Collections.IList;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsReadOnlyProperty) ShowAddButton = !IsReadOnly;
    }

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

    private Control Editor(HelixKanbanCard card, HelixDialogHandle<bool> handle, bool isNew)
    {
        var texts = HelixTexts.Current;
        var title = new HelixTextField { Label = texts.KanbanTitle, Text = card.Title, IsRequired = true };
        var description = new HelixTextField { Label = texts.KanbanDescription, Text = card.Description, AcceptsReturn = true, MinHeight = 90 };
        var columns = Columns?.ToList() ?? [];
        var column = new HelixSelect { Label = texts.KanbanColumn, ItemsSource = columns, DisplayMemberPath = nameof(HelixKanbanColumn.Title),
                                       SelectedItem = columns.FirstOrDefault(c => c.Key == card.Column), HorizontalAlignment = HorizontalAlignment.Stretch };
        var due = new HelixDatePicker { Label = texts.KanbanDueDate, SelectedDate = card.DueDate };
        var assignee = new HelixTextField { Label = texts.KanbanAssignee, Text = card.Assignee, Icon = "user", IsClearable = true };
        var tags = new HelixTextField { Label = texts.KanbanTags, Text = string.Join(", ", card.Tags), Hint = texts.KanbanTagsHint };
        var color = card.Color;
        var swatches = new WrapPanel { ItemSpacing = 8, LineSpacing = 8 };
        void PaintSwatches()
        {
            foreach (var child in swatches.Children.OfType<Button>()) child.BorderBrush = Equals(child.Tag, color) ? Res("Helix.Text") : Res("Helix.BorderStrong");
        }

        foreach (var option in Colors)
        {
            var swatch = new Button
            {
                Tag = option, Width = 26, Height = 26, Padding = default, CornerRadius = new CornerRadius(13), BorderThickness = new Thickness(2),
                Background = option is null ? Res("Helix.Surface") : HelixDraw.Resolve(this, option) ?? Brushes.Transparent,
                Content = option is null ? new HelixIcon { Icon = "x", Size = 12, Foreground = Res("Helix.TextSubtle") } : null,
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand),
            };
            ToolTip.SetTip(swatch, option ?? texts.KanbanNoColor);
            swatch.Click += (_, _) => { color = option; PaintSwatches(); };
            swatches.Children.Add(swatch);
        }

        PaintSwatches();

        var save = new HelixButton { Content = texts.Save, Icon = "check" };
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
        var cancel = new HelixButton { Content = texts.Cancel, Variant = HelixVariant.Outline, Tone = HelixTone.Neutral };
        cancel.Click += (_, _) => handle.Close(false);
        var footer = new DockPanel { Margin = new Thickness(0, 6, 0, 0) };
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

        footer.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { cancel, save } });
        static Grid Row(Control a, Control b)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,*") };
            Grid.SetColumn(b, 2);
            grid.Children.Add(a);
            grid.Children.Add(b);
            return grid;
        }

        return new StackPanel
        {
            Spacing = 14,
            Children =
            {
                title, description, Row(column, due), Row(assignee, tags),
                new StackPanel { Spacing = 6, Children = { new TextBlock { Text = texts.KanbanColor, FontSize = 13, FontWeight = FontWeight.SemiBold }, swatches } },
                footer,
            },
        };
    }

    private IBrush Res(string key) => HelixDraw.Brush(this, key, global::Avalonia.Media.Colors.Gray);

    private bool IsDone(HelixKanbanCard card) => card.Column == (DoneColumn ?? Columns?.LastOrDefault()?.Key);

    private Control CardView(HelixKanbanCard card)
    {
        var body = new StackPanel { Spacing = 6 };
        body.Children.Add(new TextBlock { Text = card.Title, FontSize = 13.5, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, Foreground = Res("Helix.Text") });
        if (!string.IsNullOrWhiteSpace(card.Description))
        {
            body.Children.Add(new TextBlock { Text = card.Description, FontSize = 12.5, TextWrapping = TextWrapping.Wrap, MaxLines = 2, TextTrimming = TextTrimming.CharacterEllipsis, Foreground = Res("Helix.TextMuted") });
        }

        if (card.Tags.Count > 0)
        {
            var tags = new WrapPanel { ItemSpacing = 4, LineSpacing = 4 };
            foreach (var tag in card.Tags)
            {
                tags.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(999), Padding = new Thickness(8, 1), Background = Res("Helix.Surface2"),
                    Child = new TextBlock { Text = tag, FontSize = 11.5, FontWeight = FontWeight.SemiBold, Foreground = Res("Helix.TextMuted") },
                });
            }

            body.Children.Add(tags);
        }

        if (card.DueDate is not null || !string.IsNullOrWhiteSpace(card.Assignee))
        {
            var footer = new DockPanel { Margin = new Thickness(0, 2, 0, 0) };
            if (!string.IsNullOrWhiteSpace(card.Assignee))
            {
                var avatar = new HelixAvatar { DisplayName = card.Assignee, Size = HelixSize.Sm };
                ToolTip.SetTip(avatar, card.Assignee);
                DockPanel.SetDock(avatar, Dock.Right);
                footer.Children.Add(avatar);
            }

            if (card.DueDate is { } due)
            {
                var overdue = due.Date < DateTime.Today && !IsDone(card);
                var brush = overdue ? Res("Helix.Danger") : Res("Helix.TextMuted");
                var date = new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new HelixIcon { Icon = "calendar", Size = 13, Foreground = brush },
                        new TextBlock { Text = due.ToString("d MMM", CultureInfo.CurrentCulture), FontSize = 12, Foreground = brush, FontWeight = overdue ? FontWeight.SemiBold : FontWeight.Normal },
                    },
                };
                if (overdue) ToolTip.SetTip(date, HelixTexts.Current.KanbanOverdue);
                footer.Children.Add(date);
            }

            body.Children.Add(footer);
        }

        if (card.Color is not { } color) return body;
        // A colored stripe on the left edge of the card.
        return new Panel
        {
            Children =
            {
                new Border { Width = 4, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(-12, -12, 0, -12), CornerRadius = new CornerRadius(4, 0, 0, 4),
                             Background = HelixDraw.Resolve(this, color) ?? Brushes.Transparent },
                body,
            },
        };
    }
}
