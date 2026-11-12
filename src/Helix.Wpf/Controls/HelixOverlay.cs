using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Helix.Wpf;

/// <summary>
/// Wrap a window's content once: it shows Helix toasts, dialogs and confirmations above it.
/// &lt;helix:HelixHost&gt; …your layout… &lt;/helix:HelixHost&gt;
/// </summary>
public class HelixHost : ContentControl
{
    private static readonly List<WeakReference<HelixHost>> Hosts = [];

    static HelixHost() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixHost), new FrameworkPropertyMetadata(typeof(HelixHost)));

    public HelixHost()
    {
        Loaded += (_, _) => Register(this);
        Unloaded += (_, _) => Unregister(this);
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public static readonly DependencyProperty ToastHorizontalAlignmentProperty = HelixProperty.Register<HelixHost, HorizontalAlignment>(nameof(ToastHorizontalAlignment), HorizontalAlignment.Right);
    public static readonly DependencyProperty ToastVerticalAlignmentProperty = HelixProperty.Register<HelixHost, VerticalAlignment>(nameof(ToastVerticalAlignment), VerticalAlignment.Bottom);

    /// <summary>Where toasts appear (Right by default).</summary>
    public HorizontalAlignment ToastHorizontalAlignment { get => (HorizontalAlignment)GetValue(ToastHorizontalAlignmentProperty); set => SetValue(ToastHorizontalAlignmentProperty, value); }

    /// <summary>Where toasts appear (Bottom by default).</summary>
    public VerticalAlignment ToastVerticalAlignment { get => (VerticalAlignment)GetValue(ToastVerticalAlignmentProperty); set => SetValue(ToastVerticalAlignmentProperty, value); }

    /// <summary>Open dialogs and confirmations, top-most last.</summary>
    public ObservableCollection<object> Overlays { get; } = [];

    /// <summary>Toasts shown by <see cref="HelixToasts"/>.</summary>
    public ObservableCollection<HelixToast> Toasts => HelixToasts.Items;

    /// <summary>The host of the active window (else the first one).</summary>
    internal static HelixHost? Active
    {
        get
        {
            lock (Hosts)
            {
                var alive = Hosts.Select(w => w.TryGetTarget(out var h) ? h : null).OfType<HelixHost>().ToList();
                return alive.FirstOrDefault(h => Window.GetWindow(h)?.IsActive == true) ?? alive.FirstOrDefault();
            }
        }
    }

    internal static HelixHost? For(DependencyObject element)
    {
        for (DependencyObject? current = element; current is not null;
             current = LogicalTreeHelper.GetParent(current) ?? (current is System.Windows.Media.Visual ? System.Windows.Media.VisualTreeHelper.GetParent(current) : null))
        {
            if (current is HelixHost host)
            {
                return host;
            }
        }

        return Active;
    }

    private static void Register(HelixHost host)
    {
        lock (Hosts)
        {
            Hosts.RemoveAll(w => !w.TryGetTarget(out var h) || h == host);
            Hosts.Add(new WeakReference<HelixHost>(host));
        }
    }

    private static void Unregister(HelixHost host)
    {
        lock (Hosts)
        {
            Hosts.RemoveAll(w => !w.TryGetTarget(out var h) || h == host);
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || Overlays.Count == 0)
        {
            return;
        }

        switch (Overlays[^1])
        {
            case HelixDialogSlot { Dialog.CloseOnEscape: true } slot:
                slot.Dialog.SetCurrentValue(HelixDialog.IsOpenProperty, false);
                e.Handled = true;
                break;
            case HelixDrawerSlot { Dialog.CloseOnEscape: true } drawer:
                drawer.Dialog.SetCurrentValue(HelixDialog.IsOpenProperty, false);
                e.Handled = true;
                break;
            case HelixConfirmRequest request:
                request.Resolve(false);
                e.Handled = true;
                break;
        }
    }
}

/// <summary>
/// A modal dialog over the window (needs a <see cref="HelixHost"/>). Bind IsOpen; Title, Description, content and Footer.
/// The content keeps your DataContext.
/// </summary>
public class HelixDialog : ContentControl
{
    static HelixDialog() => DefaultStyleKeyProperty.OverrideMetadata(typeof(HelixDialog), new FrameworkPropertyMetadata(typeof(HelixDialog)));

    public HelixDialog() => Loaded += (_, _) => Sync();

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(HelixDialog),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((HelixDialog)d).Sync()));
    public static readonly DependencyProperty TitleProperty = HelixProperty.Register<HelixDialog, string?>(nameof(Title), null);
    public static readonly DependencyProperty DescriptionProperty = HelixProperty.Register<HelixDialog, string?>(nameof(Description), null);
    public static readonly DependencyProperty FooterProperty = HelixProperty.Register<HelixDialog, object?>(nameof(Footer), null, (d, e) => ((HelixDialog)d).OnFooterChanged(e));
    public static readonly DependencyProperty DialogWidthProperty = HelixProperty.Register<HelixDialog, double>(nameof(DialogWidth), 480d);
    public static readonly DependencyProperty CornerRadiusProperty = HelixProperty.Register<HelixDialog, CornerRadius>(nameof(CornerRadius), new CornerRadius(16));
    public static readonly DependencyProperty ShowCloseProperty = HelixProperty.Register<HelixDialog, bool>(nameof(ShowClose), true);
    public static readonly DependencyProperty CloseOnBackdropProperty = HelixProperty.Register<HelixDialog, bool>(nameof(CloseOnBackdrop), true);
    public static readonly DependencyProperty CloseOnEscapeProperty = HelixProperty.Register<HelixDialog, bool>(nameof(CloseOnEscape), true);

    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Content at the bottom right, usually buttons.</summary>
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }
    public double DialogWidth { get => (double)GetValue(DialogWidthProperty); set => SetValue(DialogWidthProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public bool ShowClose { get => (bool)GetValue(ShowCloseProperty); set => SetValue(ShowCloseProperty, value); }
    public bool CloseOnBackdrop { get => (bool)GetValue(CloseOnBackdropProperty); set => SetValue(CloseOnBackdropProperty, value); }
    public bool CloseOnEscape { get => (bool)GetValue(CloseOnEscapeProperty); set => SetValue(CloseOnEscapeProperty, value); }

    /// <summary>Closes the dialog (for the × button and the backdrop).</summary>
    public ICommand CloseCommand => new HelixCommand(() => SetCurrentValue(IsOpenProperty, false));

    /// <summary>Closes on a backdrop click when allowed.</summary>
    public ICommand BackdropCommand => new HelixCommand(() =>
    {
        if (CloseOnBackdrop)
        {
            SetCurrentValue(IsOpenProperty, false);
        }
    });

    private HelixHost? _host;

    private void Sync()
    {
        if (!IsLoaded && IsOpen)
        {
            return; // shown on Loaded
        }

        _host ??= HelixHost.For(this);
        if (_host is null)
        {
            return;
        }

        var slot = _host.Overlays.FirstOrDefault(s => s is HelixDialogSlot d && d.Dialog == this || s is HelixDrawerSlot r && r.Dialog == this);
        if (IsOpen && slot is null)
        {
            _host.Overlays.Add(CreateSlot());
        }
        else if (!IsOpen && slot is not null)
        {
            _host.Overlays.Remove(slot);
        }
    }

    /// <summary>What the host shows (a dialog box, a drawer…).</summary>
    internal virtual object CreateSlot() => new HelixDialogSlot(this);

    private void OnFooterChanged(DependencyPropertyChangedEventArgs e)
    {
        // Logical child: the footer's bindings see your DataContext.
        if (e.OldValue is DependencyObject old) RemoveLogicalChild(old);
        if (e.NewValue is DependencyObject added) AddLogicalChild(added);
    }
}

/// <summary>
/// An open dialog in the host's overlay layer. A plain object (not an element), so the host's ItemsControl
/// draws it with a template instead of trying to re-parent the dialog itself.
/// </summary>
public sealed class HelixDialogSlot(HelixDialog dialog)
{
    public HelixDialog Dialog { get; } = dialog;
}

/// <summary>A pending confirmation shown by <see cref="HelixConfirm"/>.</summary>
public sealed class HelixConfirmRequest
{
    private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly HelixHost _host;

    internal HelixConfirmRequest(HelixHost host, string title, string? message, bool danger, string? confirmText, string? cancelText)
    {
        _host = host;
        Title = title;
        Message = message;
        IsDanger = danger;
        ConfirmText = confirmText ?? HelixTexts.Current.Confirm;
        CancelText = cancelText ?? HelixTexts.Current.Cancel;
        ConfirmCommand = new HelixCommand(() => Resolve(true));
        CancelCommand = new HelixCommand(() => Resolve(false));
    }

    public string Title { get; }
    public string? Message { get; }
    public bool IsDanger { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
    internal Task<bool> Task => _result.Task;

    internal void Resolve(bool confirmed)
    {
        _host.Overlays.Remove(this);
        _result.TrySetResult(confirmed);
    }
}

/// <summary>Promise-style confirmation: if (await HelixConfirm.AskAsync("Delete?", "This cannot be undone.", danger: true)) …</summary>
public static class HelixConfirm
{
    public static Task<bool> AskAsync(string title, string? message = null, bool danger = false, string? confirmText = null, string? cancelText = null)
    {
        var host = HelixHost.Active;
        if (host is null)
        {
            // No HelixHost: fall back to the standard message box.
            var answer = MessageBox.Show(message ?? title, title, MessageBoxButton.OKCancel, danger ? MessageBoxImage.Warning : MessageBoxImage.Question);
            return Task.FromResult(answer == MessageBoxResult.OK);
        }

        var request = new HelixConfirmRequest(host, title, message, danger, confirmText, cancelText);
        host.Overlays.Add(request);
        return request.Task;
    }
}

/// <summary>One toast notification.</summary>
public sealed class HelixToast
{
    internal HelixToast(string message, string? title, HelixSeverity severity)
    {
        Message = message;
        Title = title;
        Severity = severity;
        CloseCommand = new HelixCommand(() => HelixToasts.Dismiss(this));
    }

    public string Message { get; }
    public string? Title { get; }
    public HelixSeverity Severity { get; }
    public string Icon => Severity switch
    {
        HelixSeverity.Success => "success",
        HelixSeverity.Warning => "warning",
        HelixSeverity.Danger => "danger",
        _ => "info",
    };

    public ICommand CloseCommand { get; }
    public string CloseText => HelixTexts.Current.Close;
}

/// <summary>Toasts: HelixToasts.Success("Saved"). Shown by the <see cref="HelixHost"/> of each window.</summary>
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
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        dispatcher.Invoke(() =>
        {
            Items.Add(toast);
            while (Items.Count > MaxVisible)
            {
                Items.RemoveAt(0);
            }

            var timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher) { Interval = duration ?? DefaultDuration };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Items.Remove(toast);
            };
            timer.Start();
        });
        return toast;
    }

    public static HelixToast Info(string message, string? title = null) => Show(message, HelixSeverity.Info, title);
    public static HelixToast Success(string message, string? title = null) => Show(message, HelixSeverity.Success, title);
    public static HelixToast Warning(string message, string? title = null) => Show(message, HelixSeverity.Warning, title);
    public static HelixToast Error(string message, string? title = null) => Show(message, HelixSeverity.Danger, title);

    public static void Dismiss(HelixToast toast)
    {
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        dispatcher.Invoke(() => Items.Remove(toast));
    }
}

/// <summary>Minimal ICommand for the templates.</summary>
internal sealed class HelixCommand(Action execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute();
}
