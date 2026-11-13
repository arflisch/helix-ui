using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace Helix.Avalonia;

/// <summary>A toast shown by <see cref="HelixToastHost"/>.</summary>
public sealed class HelixToast(string message, string? title, HelixSeverity severity)
{
    public string Message { get; } = message;
    public string? Title { get; } = title;
    public bool HasTitle => !string.IsNullOrEmpty(Title);
    public HelixSeverity Severity { get; } = severity;
    public string Icon { get; } = severity switch { HelixSeverity.Success => "success", HelixSeverity.Warning => "warning", HelixSeverity.Danger => "danger", _ => "info" };
    public ICommand CloseCommand => _close ??= new HelixCommand(() => HelixToasts.Dismiss(this));
    public string CloseText => HelixTexts.Current.Close;

    private ICommand? _close;
}

/// <summary>Shows short-lived notifications from anywhere (any thread): <c>HelixToasts.Success("Saved")</c>. Needs a HelixToastHost in the window.</summary>
public static class HelixToasts
{
    /// <summary>How long a toast stays (4 s by default).</summary>
    public static TimeSpan DefaultDuration { get; set; } = TimeSpan.FromSeconds(4);

    /// <summary>At most this many toasts at once (the oldest leaves first).</summary>
    public static int MaxVisible { get; set; } = 4;

    public static ObservableCollection<HelixToast> Items { get; } = [];

    public static HelixToast Show(string message, HelixSeverity severity = HelixSeverity.Info, string? title = null, TimeSpan? duration = null)
    {
        var toast = new HelixToast(message, title, severity);
        Dispatcher.UIThread.Post(() =>
        {
            Items.Add(toast);
            while (Items.Count > MaxVisible) Items.RemoveAt(0);
            var delay = duration ?? DefaultDuration;
            if (delay > TimeSpan.Zero) DispatcherTimer.RunOnce(() => Items.Remove(toast), delay);
        });
        return toast;
    }

    public static HelixToast Info(string message, string? title = null) => Show(message, HelixSeverity.Info, title);
    public static HelixToast Success(string message, string? title = null) => Show(message, HelixSeverity.Success, title);
    public static HelixToast Warning(string message, string? title = null) => Show(message, HelixSeverity.Warning, title);
    public static HelixToast Error(string message, string? title = null) => Show(message, HelixSeverity.Danger, title);

    public static void Dismiss(HelixToast toast) => Dispatcher.UIThread.Post(() => Items.Remove(toast));
}

/// <summary>Where toasts appear: put it once over the window content (last child of the root Panel/Grid).</summary>
public class HelixToastHost : TemplatedControl
{
    public static readonly StyledProperty<global::Avalonia.Layout.HorizontalAlignment> ToastHorizontalAlignmentProperty =
        AvaloniaProperty.Register<HelixToastHost, global::Avalonia.Layout.HorizontalAlignment>(nameof(ToastHorizontalAlignment), global::Avalonia.Layout.HorizontalAlignment.Right);
    public static readonly StyledProperty<global::Avalonia.Layout.VerticalAlignment> ToastVerticalAlignmentProperty =
        AvaloniaProperty.Register<HelixToastHost, global::Avalonia.Layout.VerticalAlignment>(nameof(ToastVerticalAlignment), global::Avalonia.Layout.VerticalAlignment.Bottom);

    public global::Avalonia.Layout.HorizontalAlignment ToastHorizontalAlignment { get => GetValue(ToastHorizontalAlignmentProperty); set => SetValue(ToastHorizontalAlignmentProperty, value); }
    public global::Avalonia.Layout.VerticalAlignment ToastVerticalAlignment { get => GetValue(ToastVerticalAlignmentProperty); set => SetValue(ToastVerticalAlignmentProperty, value); }

    /// <summary>The toasts, for the template.</summary>
    public ObservableCollection<HelixToast> Toasts => HelixToasts.Items;
}
