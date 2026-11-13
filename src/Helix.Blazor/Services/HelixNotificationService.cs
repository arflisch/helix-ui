namespace Helix.Blazor;

/// <summary>A notification kept in the notification center until it is dismissed.</summary>
public sealed class HelixNotification
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string Title { get; init; }
    public string? Text { get; init; }
    public HelixSeverity Severity { get; init; } = HelixSeverity.Info;
    public DateTime Time { get; init; }

    /// <summary>A page to open when the notification is clicked.</summary>
    public string? Link { get; init; }

    /// <summary>Label of an action button ("Open", "Download"…), with <see cref="Action"/> or <see cref="Link"/>.</summary>
    public string? ActionText { get; init; }

    /// <summary>Run when the notification (or its action) is clicked.</summary>
    public Func<Task>? Action { get; init; }

    /// <summary>A free key to find or group your notifications ("export", an order id…).</summary>
    public string? Tag { get; init; }

    public bool IsRead { get; internal set; }
}

/// <summary>
/// The notifications of the app, shown by <c>HelixNotificationCenter</c> (a bell with an unread count).
/// Inject it anywhere and call <c>Notifications.Warning("Deadline in 5 minutes")</c>.
/// </summary>
public sealed class HelixNotificationService(HelixToastService? toasts = null, TimeProvider? timeProvider = null)
{
    private readonly List<HelixNotification> _items = [];
    private readonly Lock _gate = new();
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    /// <summary>Oldest notifications are dropped beyond this number (100).</summary>
    public int MaxItems { get; set; } = 100;

    public event Action? Changed;

    /// <summary>Newest first.</summary>
    public IReadOnlyList<HelixNotification> Items
    {
        get { lock (_gate) return _items.ToArray(); }
    }

    public int UnreadCount
    {
        get { lock (_gate) return _items.Count(n => !n.IsRead); }
    }

    /// <param name="toast">Also show it as a toast.</param>
    /// <param name="time">When it happened (now by default), e.g. when you reload past notifications from a database.</param>
    public HelixNotification Add(string title, HelixSeverity severity = HelixSeverity.Info, string? text = null, string? link = null,
        string? actionText = null, Func<Task>? action = null, string? tag = null, bool toast = false, DateTime? time = null, bool read = false)
    {
        var notification = new HelixNotification
        {
            Title = title, Text = text, Severity = severity, Link = link, ActionText = actionText, Action = action, Tag = tag,
            Time = time ?? _clock.GetLocalNow().DateTime,
            IsRead = read,
        };
        lock (_gate)
        {
            var index = _items.FindIndex(n => n.Time <= notification.Time);
            _items.Insert(index < 0 ? _items.Count : index, notification);
            if (_items.Count > MaxItems) _items.RemoveRange(MaxItems, _items.Count - MaxItems);
        }

        if (toast) toasts?.Show(text ?? title, severity, text is null ? null : title);
        Changed?.Invoke();
        return notification;
    }

    public HelixNotification Info(string title, string? text = null, string? link = null, bool toast = false) => Add(title, HelixSeverity.Info, text, link, toast: toast);
    public HelixNotification Success(string title, string? text = null, string? link = null, bool toast = false) => Add(title, HelixSeverity.Success, text, link, toast: toast);
    public HelixNotification Warning(string title, string? text = null, string? link = null, bool toast = false) => Add(title, HelixSeverity.Warning, text, link, toast: toast);
    public HelixNotification Error(string title, string? text = null, string? link = null, bool toast = false) => Add(title, HelixSeverity.Danger, text, link, toast: toast);

    public void MarkRead(Guid id) => Update(() => _items.Where(n => n.Id == id && !n.IsRead).ToList().ForEach(n => n.IsRead = true));

    public void MarkAllRead() => Update(() => _items.ForEach(n => n.IsRead = true));

    public void Remove(Guid id) => Update(() => _items.RemoveAll(n => n.Id == id));

    /// <summary>Removes every notification with this tag (e.g. once the problem is solved).</summary>
    public void RemoveTag(string tag) => Update(() => _items.RemoveAll(n => n.Tag == tag));

    public void Clear() => Update(_items.Clear);

    private void Update(Action change)
    {
        lock (_gate) change();
        Changed?.Invoke();
    }
}
