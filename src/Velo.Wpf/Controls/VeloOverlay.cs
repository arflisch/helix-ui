using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Velo.Wpf;

/// <summary>
/// Wrap a window's content once: it shows Velo toasts, dialogs and confirmations above it.
/// &lt;velo:VeloHost&gt; …your layout… &lt;/velo:VeloHost&gt;
/// </summary>
public class VeloHost : ContentControl
{
    private static readonly List<WeakReference<VeloHost>> Hosts = [];

    static VeloHost() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloHost), new FrameworkPropertyMetadata(typeof(VeloHost)));

    public VeloHost()
    {
        Loaded += (_, _) => Register(this);
        Unloaded += (_, _) => Unregister(this);
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public static readonly DependencyProperty ToastHorizontalAlignmentProperty = VeloProperty.Register<VeloHost, HorizontalAlignment>(nameof(ToastHorizontalAlignment), HorizontalAlignment.Right);
    public static readonly DependencyProperty ToastVerticalAlignmentProperty = VeloProperty.Register<VeloHost, VerticalAlignment>(nameof(ToastVerticalAlignment), VerticalAlignment.Bottom);

    /// <summary>Where toasts appear (Right by default).</summary>
    public HorizontalAlignment ToastHorizontalAlignment { get => (HorizontalAlignment)GetValue(ToastHorizontalAlignmentProperty); set => SetValue(ToastHorizontalAlignmentProperty, value); }

    /// <summary>Where toasts appear (Bottom by default).</summary>
    public VerticalAlignment ToastVerticalAlignment { get => (VerticalAlignment)GetValue(ToastVerticalAlignmentProperty); set => SetValue(ToastVerticalAlignmentProperty, value); }

    /// <summary>Open dialogs and confirmations, top-most last.</summary>
    public ObservableCollection<object> Overlays { get; } = [];

    /// <summary>Toasts shown by <see cref="VeloToasts"/>.</summary>
    public ObservableCollection<VeloToast> Toasts => VeloToasts.Items;

    /// <summary>The host of the active window (else the first one).</summary>
    internal static VeloHost? Active
    {
        get
        {
            lock (Hosts)
            {
                var alive = Hosts.Select(w => w.TryGetTarget(out var h) ? h : null).OfType<VeloHost>().ToList();
                return alive.FirstOrDefault(h => Window.GetWindow(h)?.IsActive == true) ?? alive.FirstOrDefault();
            }
        }
    }

    internal static VeloHost? For(DependencyObject element)
    {
        for (DependencyObject? current = element; current is not null;
             current = LogicalTreeHelper.GetParent(current) ?? (current is System.Windows.Media.Visual ? System.Windows.Media.VisualTreeHelper.GetParent(current) : null))
        {
            if (current is VeloHost host)
            {
                return host;
            }
        }

        return Active;
    }

    private static void Register(VeloHost host)
    {
        lock (Hosts)
        {
            Hosts.RemoveAll(w => !w.TryGetTarget(out var h) || h == host);
            Hosts.Add(new WeakReference<VeloHost>(host));
        }
    }

    private static void Unregister(VeloHost host)
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
            case VeloDialogSlot { Dialog.CloseOnEscape: true } slot:
                slot.Dialog.SetCurrentValue(VeloDialog.IsOpenProperty, false);
                e.Handled = true;
                break;
            case VeloDrawerSlot { Dialog.CloseOnEscape: true } drawer:
                drawer.Dialog.SetCurrentValue(VeloDialog.IsOpenProperty, false);
                e.Handled = true;
                break;
            case VeloConfirmRequest request:
                request.Resolve(false);
                e.Handled = true;
                break;
        }
    }
}

/// <summary>
/// A modal dialog over the window (needs a <see cref="VeloHost"/>). Bind IsOpen; Title, Description, content and Footer.
/// The content keeps your DataContext.
/// </summary>
public class VeloDialog : ContentControl
{
    static VeloDialog() => DefaultStyleKeyProperty.OverrideMetadata(typeof(VeloDialog), new FrameworkPropertyMetadata(typeof(VeloDialog)));

    public VeloDialog() => Loaded += (_, _) => Sync();

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(VeloDialog),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((VeloDialog)d).Sync()));
    public static readonly DependencyProperty TitleProperty = VeloProperty.Register<VeloDialog, string?>(nameof(Title), null);
    public static readonly DependencyProperty DescriptionProperty = VeloProperty.Register<VeloDialog, string?>(nameof(Description), null);
    public static readonly DependencyProperty FooterProperty = VeloProperty.Register<VeloDialog, object?>(nameof(Footer), null, (d, e) => ((VeloDialog)d).OnFooterChanged(e));
    public static readonly DependencyProperty DialogWidthProperty = VeloProperty.Register<VeloDialog, double>(nameof(DialogWidth), 480d);
    public static readonly DependencyProperty CornerRadiusProperty = VeloProperty.Register<VeloDialog, CornerRadius>(nameof(CornerRadius), new CornerRadius(16));
    public static readonly DependencyProperty ShowCloseProperty = VeloProperty.Register<VeloDialog, bool>(nameof(ShowClose), true);
    public static readonly DependencyProperty CloseOnBackdropProperty = VeloProperty.Register<VeloDialog, bool>(nameof(CloseOnBackdrop), true);
    public static readonly DependencyProperty CloseOnEscapeProperty = VeloProperty.Register<VeloDialog, bool>(nameof(CloseOnEscape), true);

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
    public ICommand CloseCommand => new VeloCommand(() => SetCurrentValue(IsOpenProperty, false));

    /// <summary>Closes on a backdrop click when allowed.</summary>
    public ICommand BackdropCommand => new VeloCommand(() =>
    {
        if (CloseOnBackdrop)
        {
            SetCurrentValue(IsOpenProperty, false);
        }
    });

    private VeloHost? _host;

    private void Sync()
    {
        if (!IsLoaded && IsOpen)
        {
            return; // shown on Loaded
        }

        _host ??= VeloHost.For(this);
        if (_host is null)
        {
            return;
        }

        var slot = _host.Overlays.FirstOrDefault(s => s is VeloDialogSlot d && d.Dialog == this || s is VeloDrawerSlot r && r.Dialog == this);
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
    internal virtual object CreateSlot() => new VeloDialogSlot(this);

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
public sealed class VeloDialogSlot(VeloDialog dialog)
{
    public VeloDialog Dialog { get; } = dialog;
}

/// <summary>A pending confirmation shown by <see cref="VeloConfirm"/>.</summary>
public sealed class VeloConfirmRequest
{
    private readonly TaskCompletionSource<bool> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly VeloHost _host;

    internal VeloConfirmRequest(VeloHost host, string title, string? message, bool danger, string? confirmText, string? cancelText)
    {
        _host = host;
        Title = title;
        Message = message;
        IsDanger = danger;
        ConfirmText = confirmText ?? VeloTexts.Current.Confirm;
        CancelText = cancelText ?? VeloTexts.Current.Cancel;
        ConfirmCommand = new VeloCommand(() => Resolve(true));
        CancelCommand = new VeloCommand(() => Resolve(false));
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

/// <summary>Promise-style confirmation: if (await VeloConfirm.AskAsync("Delete?", "This cannot be undone.", danger: true)) …</summary>
public static class VeloConfirm
{
    public static Task<bool> AskAsync(string title, string? message = null, bool danger = false, string? confirmText = null, string? cancelText = null)
    {
        var host = VeloHost.Active;
        if (host is null)
        {
            // No VeloHost: fall back to the standard message box.
            var answer = MessageBox.Show(message ?? title, title, MessageBoxButton.OKCancel, danger ? MessageBoxImage.Warning : MessageBoxImage.Question);
            return Task.FromResult(answer == MessageBoxResult.OK);
        }

        var request = new VeloConfirmRequest(host, title, message, danger, confirmText, cancelText);
        host.Overlays.Add(request);
        return request.Task;
    }
}

/// <summary>One toast notification.</summary>
public sealed class VeloToast
{
    internal VeloToast(string message, string? title, VeloSeverity severity)
    {
        Message = message;
        Title = title;
        Severity = severity;
        CloseCommand = new VeloCommand(() => VeloToasts.Dismiss(this));
    }

    public string Message { get; }
    public string? Title { get; }
    public VeloSeverity Severity { get; }
    public string Icon => Severity switch
    {
        VeloSeverity.Success => "success",
        VeloSeverity.Warning => "warning",
        VeloSeverity.Danger => "danger",
        _ => "info",
    };

    public ICommand CloseCommand { get; }
    public string CloseText => VeloTexts.Current.Close;
}

/// <summary>Toasts: VeloToasts.Success("Saved"). Shown by the <see cref="VeloHost"/> of each window.</summary>
public static class VeloToasts
{
    /// <summary>How long a toast stays (4 s by default).</summary>
    public static TimeSpan DefaultDuration { get; set; } = TimeSpan.FromSeconds(4);

    /// <summary>At most this many toasts at once (the oldest leaves first).</summary>
    public static int MaxVisible { get; set; } = 4;

    public static ObservableCollection<VeloToast> Items { get; } = [];

    public static VeloToast Show(string message, VeloSeverity severity = VeloSeverity.Info, string? title = null, TimeSpan? duration = null)
    {
        var toast = new VeloToast(message, title, severity);
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

    public static VeloToast Info(string message, string? title = null) => Show(message, VeloSeverity.Info, title);
    public static VeloToast Success(string message, string? title = null) => Show(message, VeloSeverity.Success, title);
    public static VeloToast Warning(string message, string? title = null) => Show(message, VeloSeverity.Warning, title);
    public static VeloToast Error(string message, string? title = null) => Show(message, VeloSeverity.Danger, title);

    public static void Dismiss(VeloToast toast)
    {
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        dispatcher.Invoke(() => Items.Remove(toast));
    }
}

/// <summary>Minimal ICommand for the templates.</summary>
internal sealed class VeloCommand(Action execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute();
}
