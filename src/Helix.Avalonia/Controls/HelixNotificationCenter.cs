using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;

namespace Helix.Avalonia;

/// <summary>A notification kept in the notification center until it is dismissed.</summary>
public sealed class HelixNotification : INotifyPropertyChanged
{
    private bool _isRead;
    private string _timeText = "";
    private string _group = "";

    public Guid Id { get; } = Guid.NewGuid();
    public required string Title { get; init; }
    public string? Text { get; init; }
    public HelixSeverity Severity { get; init; } = HelixSeverity.Info;
    public DateTime Time { get; init; }

    /// <summary>Label of the action shown under the text ("Open", "Download"…).</summary>
    public string? ActionText { get; init; }

    /// <summary>Run when the notification is clicked.</summary>
    public Action? Action { get; init; }

    /// <summary>A free key to find or group your notifications.</summary>
    public string? Tag { get; init; }

    public bool IsRead { get => _isRead; internal set => Set(ref _isRead, value); }

    public bool HasText => !string.IsNullOrEmpty(Text);
    public bool HasAction => !string.IsNullOrEmpty(ActionText);

    /// <summary>"  ·  Open", for the template.</summary>
    public string ActionLine => HasAction ? $"  ·  {ActionText}" : "";

    /// <summary>Marks it as read and runs its Action (clicking it in the panel).</summary>
    public ICommand OpenCommand => _open ??= new HelixCommand(() => HelixNotifications.Open(this));

    public ICommand DismissCommand => _dismiss ??= new HelixCommand(() => HelixNotifications.Remove(this));

    private ICommand? _open, _dismiss;

    /// <summary>"2 min ago", refreshed by the notification center.</summary>
    public string TimeText { get => _timeText; private set => Set(ref _timeText, value); }

    /// <summary>"Today", "Yesterday" or "Earlier": the group header in the panel.</summary>
    public string Group { get => _group; private set => Set(ref _group, value); }

    internal void Refresh(DateTime now)
    {
        var texts = HelixTexts.Current;
        var age = now - Time;
        TimeText = age < TimeSpan.FromMinutes(1) ? texts.JustNow
            : age < TimeSpan.FromHours(1) ? string.Format(CultureInfo.CurrentCulture, texts.MinutesAgo, (int)age.TotalMinutes)
            : age < TimeSpan.FromHours(12) && Time.Date == now.Date ? string.Format(CultureInfo.CurrentCulture, texts.HoursAgo, (int)age.TotalHours)
            : Time.Date >= now.Date.AddDays(-1) ? Time.ToString("t", CultureInfo.CurrentCulture)
            : Time.ToString("g", CultureInfo.CurrentCulture);
        var days = (now.Date - Time.Date).Days;
        Group = days <= 0 ? texts.Today : days == 1 ? texts.Yesterday : texts.Earlier;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>A day header in the notification panel ("Today", "Yesterday", "Earlier").</summary>
public sealed record HelixNotificationGroup(string Name);

/// <summary>
/// The notifications of the app, shown by <see cref="HelixNotificationCenter"/> (a bell with an unread count).
/// Call it from anywhere, any thread: <c>HelixNotifications.Warning("Deadline in 5 minutes")</c>.
/// </summary>
public static class HelixNotifications
{
    /// <summary>Newest first. Change it through the methods below (they run on the UI thread).</summary>
    public static ObservableCollection<HelixNotification> Items { get; } = [];

    /// <summary>Oldest notifications are dropped beyond this number (100).</summary>
    public static int MaxItems { get; set; } = 100;

    /// <summary>A testable clock (system clock by default).</summary>
    public static TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    public static int UnreadCount => Items.Count(n => !n.IsRead);

    /// <summary>Raised after any change (added, read, removed).</summary>
    public static event EventHandler? Changed;

    /// <param name="toast">Also show it as a toast.</param>
    /// <param name="time">When it happened (now by default), e.g. past notifications from a database.</param>
    public static HelixNotification Add(string title, HelixSeverity severity = HelixSeverity.Info, string? text = null,
        string? actionText = null, Action? action = null, string? tag = null, bool toast = false, DateTime? time = null, bool read = false)
    {
        var notification = new HelixNotification
        {
            Title = title, Text = text, Severity = severity, ActionText = actionText, Action = action, Tag = tag,
            Time = time ?? TimeProvider.GetLocalNow().DateTime,
        };
        notification.IsRead = read;
        notification.Refresh(TimeProvider.GetLocalNow().DateTime);
        OnUi(() =>
        {
            var index = 0;
            while (index < Items.Count && Items[index].Time > notification.Time) index++;
            Items.Insert(index, notification);
            while (Items.Count > MaxItems) Items.RemoveAt(Items.Count - 1);
        });
        if (toast) HelixToasts.Show(text ?? title, severity, text is null ? null : title);
        return notification;
    }

    public static HelixNotification Info(string title, string? text = null, Action? action = null, string? actionText = null) => Add(title, HelixSeverity.Info, text, actionText, action);
    public static HelixNotification Success(string title, string? text = null, Action? action = null, string? actionText = null) => Add(title, HelixSeverity.Success, text, actionText, action);
    public static HelixNotification Warning(string title, string? text = null, Action? action = null, string? actionText = null) => Add(title, HelixSeverity.Warning, text, actionText, action);
    public static HelixNotification Error(string title, string? text = null, Action? action = null, string? actionText = null) => Add(title, HelixSeverity.Danger, text, actionText, action);

    public static void MarkRead(HelixNotification notification) => OnUi(() => notification.IsRead = true);

    /// <summary>Raised when a notification is opened (clicked), before its Action runs.</summary>
    public static event EventHandler<HelixNotification>? Opened;

    /// <summary>Marks it as read, raises <see cref="Opened"/> and runs its Action.</summary>
    public static void Open(HelixNotification notification)
    {
        MarkRead(notification);
        Opened?.Invoke(null, notification);
        notification.Action?.Invoke();
    }

    public static void MarkAllRead() => OnUi(() =>
    {
        foreach (var n in Items) n.IsRead = true;
    });

    public static void Remove(HelixNotification notification) => OnUi(() => Items.Remove(notification));

    /// <summary>Removes every notification with this tag (e.g. once the problem is solved).</summary>
    public static void RemoveTag(string tag) => OnUi(() =>
    {
        foreach (var n in Items.Where(n => n.Tag == tag).ToList()) Items.Remove(n);
    });

    public static void Clear() => OnUi(Items.Clear);

    private static void OnUi(Action change)
    {
        void Run()
        {
            change();
            Changed?.Invoke(null, EventArgs.Empty);
        }

        if (Dispatcher.UIThread.CheckAccess()) Run();
        else Dispatcher.UIThread.Invoke(Run);
    }
}

/// <summary>A bell with the unread count; opens the notifications grouped by day. Feed it with <see cref="HelixNotifications"/>.</summary>
[TemplatePart("PART_Popup", typeof(Popup))]
public class HelixNotificationCenter : TemplatedControl
{
    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<HelixNotificationCenter, bool>(nameof(IsOpen), defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixNotificationCenter, string?>(nameof(Title));
    public static readonly StyledProperty<double> PanelWidthProperty = AvaloniaProperty.Register<HelixNotificationCenter, double>(nameof(PanelWidth), 360);
    public static readonly StyledProperty<HorizontalAlignment> PanelAlignmentProperty = AvaloniaProperty.Register<HelixNotificationCenter, HorizontalAlignment>(nameof(PanelAlignment), HorizontalAlignment.Right);
    public static readonly StyledProperty<bool> MarkReadOnOpenProperty = AvaloniaProperty.Register<HelixNotificationCenter, bool>(nameof(MarkReadOnOpen));
    public static readonly DirectProperty<HelixNotificationCenter, int> UnreadCountProperty = AvaloniaProperty.RegisterDirect<HelixNotificationCenter, int>(nameof(UnreadCount), c => c.UnreadCount);
    public static readonly DirectProperty<HelixNotificationCenter, string> UnreadTextProperty = AvaloniaProperty.RegisterDirect<HelixNotificationCenter, string>(nameof(UnreadText), c => c.UnreadText);
    public static readonly DirectProperty<HelixNotificationCenter, bool> HasUnreadProperty = AvaloniaProperty.RegisterDirect<HelixNotificationCenter, bool>(nameof(HasUnread), c => c.HasUnread);
    public static readonly DirectProperty<HelixNotificationCenter, bool> HasItemsProperty = AvaloniaProperty.RegisterDirect<HelixNotificationCenter, bool>(nameof(HasItems), c => c.HasItems);
    public static readonly DirectProperty<HelixNotificationCenter, string> PanelTitleProperty = AvaloniaProperty.RegisterDirect<HelixNotificationCenter, string>(nameof(PanelTitle), c => c.PanelTitle);
    public static readonly DirectProperty<HelixNotificationCenter, IReadOnlyList<object>> RowsProperty = AvaloniaProperty.RegisterDirect<HelixNotificationCenter, IReadOnlyList<object>>(nameof(Rows), c => c.Rows);

    private int _unreadCount;
    private string _unreadText = "0";
    private bool _hasUnread, _hasItems;
    private string _panelTitle = HelixTexts.Current.Notifications;
    private IReadOnlyList<object> _rows = [];
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
    private Popup? _popup;

    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Panel title ("Notifications").</summary>
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public double PanelWidth { get => GetValue(PanelWidthProperty); set => SetValue(PanelWidthProperty, value); }

    /// <summary>Right (default): the panel's right edge on the bell, for a top bar; Left: for a side bar.</summary>
    public HorizontalAlignment PanelAlignment { get => GetValue(PanelAlignmentProperty); set => SetValue(PanelAlignmentProperty, value); }

    /// <summary>Opening the panel marks everything as read.</summary>
    public bool MarkReadOnOpen { get => GetValue(MarkReadOnOpenProperty); set => SetValue(MarkReadOnOpenProperty, value); }

    public int UnreadCount { get => _unreadCount; private set => SetAndRaise(UnreadCountProperty, ref _unreadCount, value); }

    /// <summary>"3", or "99+".</summary>
    public string UnreadText { get => _unreadText; private set => SetAndRaise(UnreadTextProperty, ref _unreadText, value); }
    public bool HasUnread { get => _hasUnread; private set => SetAndRaise(HasUnreadProperty, ref _hasUnread, value); }
    public bool HasItems { get => _hasItems; private set => SetAndRaise(HasItemsProperty, ref _hasItems, value); }
    public string PanelTitle { get => _panelTitle; private set => SetAndRaise(PanelTitleProperty, ref _panelTitle, value); }

    /// <summary>Day headers (<see cref="HelixNotificationGroup"/>) and notifications, in display order, for the template.</summary>
    public IReadOnlyList<object> Rows { get => _rows; private set => SetAndRaise(RowsProperty, ref _rows, value); }

    public ICommand MarkAllReadCommand { get; } = new HelixCommand(HelixNotifications.MarkAllRead);
    public ICommand ClearCommand { get; } = new HelixCommand(HelixNotifications.Clear);

    public static string NotificationsText => HelixTexts.Current.Notifications;
    public static string MarkAllReadText => HelixTexts.Current.MarkAllRead;
    public static string EmptyText => HelixTexts.Current.NoNotifications;
    public static string ClearAllText => HelixTexts.Current.ClearAll;
    public static string DismissText => HelixTexts.Current.Dismiss;

    /// <summary>Raised when a notification is clicked (after its Action).</summary>
    public event EventHandler<HelixNotification>? NotificationClicked;

    public HelixNotificationCenter() => _timer.Tick += (_, _) => RefreshTimes();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        HelixNotifications.Changed += OnChanged;
        HelixNotifications.Opened += OnOpened;
        HelixNotifications.Items.CollectionChanged += OnCollectionChanged;
        RefreshTimes();
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        HelixNotifications.Changed -= OnChanged;
        HelixNotifications.Opened -= OnOpened;
        HelixNotifications.Items.CollectionChanged -= OnCollectionChanged;
        _timer.Stop();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        Place();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty) Update();
        else if (change.Property == PanelAlignmentProperty) Place();
        else if (change.Property == IsOpenProperty && IsOpen)
        {
            RefreshTimes();
            if (MarkReadOnOpen) HelixNotifications.MarkAllRead();
        }
    }

    private void Place()
    {
        if (_popup is null) return;
        // Panel edge on the bell edge (the 12px margin around the panel leaves room for its shadow), 6px below it.
        _popup.Placement = PanelAlignment == HorizontalAlignment.Left ? PlacementMode.BottomEdgeAlignedLeft : PlacementMode.BottomEdgeAlignedRight;
        _popup.HorizontalOffset = PanelAlignment == HorizontalAlignment.Left ? -12 : 12;
        _popup.VerticalOffset = 6;
    }

    private void OnChanged(object? sender, EventArgs e) => Update();

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Update();

    private void Update()
    {
        var unread = HelixNotifications.UnreadCount;
        UnreadCount = unread;
        UnreadText = unread > 99 ? "99+" : unread.ToString(CultureInfo.CurrentCulture);
        HasUnread = unread > 0;
        HasItems = HelixNotifications.Items.Count > 0;
        PanelTitle = Title ?? NotificationsText;
        ToolTip.SetTip(this, unread > 0 ? $"{PanelTitle} · {string.Format(CultureInfo.CurrentCulture, HelixTexts.Current.UnreadCount, unread)}" : PanelTitle);

        var rows = new List<object>();
        string? group = null;
        foreach (var n in HelixNotifications.Items)
        {
            if (n.Group != group) rows.Add(new HelixNotificationGroup(group = n.Group));
            rows.Add(n);
        }

        Rows = rows;
    }

    private void RefreshTimes()
    {
        var now = HelixNotifications.TimeProvider.GetLocalNow().DateTime;
        foreach (var n in HelixNotifications.Items) n.Refresh(now);
        Update();
    }

    private void OnOpened(object? sender, HelixNotification notification)
    {
        if (!IsOpen) return;
        if (notification.Action is not null) SetCurrentValue(IsOpenProperty, false);
        NotificationClicked?.Invoke(this, notification);
    }
}
