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
            case HelixPromptRequest prompt:
                prompt.Resolve(null);
                e.Handled = true;
                break;
            case HelixViewRequest view:
                view.Close(null);
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

    /// <summary>Asks for a short text: the typed text, or null when cancelled. Validate returns an error message, or null when the text is fine.</summary>
    public static Task<string?> PromptAsync(string title, string? message = null, string? value = null, string? placeholder = null,
        string? confirmText = null, Func<string, string?>? validate = null)
    {
        var host = HelixHost.Active;
        if (host is null)
        {
            System.Diagnostics.Trace.TraceWarning("HelixConfirm.PromptAsync needs a HelixHost in the window.");
            return Task.FromResult<string?>(null);
        }

        var request = new HelixPromptRequest(host, title, message, value, placeholder, confirmText, validate);
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

/// <summary>An open text prompt (<see cref="HelixConfirm.PromptAsync"/>) in the host's overlay layer.</summary>
public sealed class HelixPromptRequest : System.ComponentModel.INotifyPropertyChanged
{
    private readonly TaskCompletionSource<string?> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly HelixHost _host;
    private readonly Func<string, string?>? _validate;
    private string _text;
    private string? _error;

    internal HelixPromptRequest(HelixHost host, string title, string? message, string? value, string? placeholder, string? confirmText, Func<string, string?>? validate)
    {
        _host = host;
        _validate = validate;
        _text = value ?? "";
        Title = title;
        Message = message;
        Placeholder = placeholder;
        ConfirmText = confirmText ?? HelixTexts.Current.Confirm;
        CancelText = HelixTexts.Current.Cancel;
        ConfirmCommand = new HelixCommand(Submit);
        CancelCommand = new HelixCommand(() => Resolve(null));
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; }
    public string? Message { get; }
    public string? Placeholder { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    /// <summary>The typed text (two-way).</summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            Error = null;
            PropertyChanged?.Invoke(this, new(nameof(Text)));
        }
    }

    /// <summary>The validation message, shown under the field.</summary>
    public string? Error
    {
        get => _error;
        private set
        {
            _error = value;
            PropertyChanged?.Invoke(this, new(nameof(Error)));
        }
    }

    internal Task<string?> Task => _result.Task;

    private void Submit()
    {
        Error = _validate?.Invoke(Text);
        if (Error is null) Resolve(Text);
    }

    internal void Resolve(string? text)
    {
        _host.Overlays.Remove(this);
        _result.TrySetResult(text);
    }
}

/// <summary>A view shown by <see cref="HelixDialogs.ShowAsync{TResult}(object, string?, string?, double)"/> in the host's overlay layer.</summary>
public sealed class HelixViewRequest
{
    private readonly TaskCompletionSource<object?> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly HelixHost _host;

    internal HelixViewRequest(HelixHost host, string? title, string? description, double width)
    {
        _host = host;
        Title = title;
        Description = description;
        DialogWidth = width;
        CloseCommand = new HelixCommand(() => Close(null));
    }

    public string? Title { get; }
    public string? Description { get; }
    public double DialogWidth { get; }
    public object? View { get; internal set; }
    public ICommand CloseCommand { get; }
    internal Task<object?> Task => _result.Task;

    /// <summary>Closes the dialog; ShowAsync completes with this result.</summary>
    public void Close(object? result)
    {
        _host.Overlays.Remove(this);
        _result.TrySetResult(result);
    }
}

/// <summary>Closes the dialog it was given to (ShowAsync's builder overload).</summary>
public sealed class HelixDialogHandle<TResult>(HelixViewRequest request)
{
    public void Close(TResult? result) => request.Close(result);

    public void Cancel() => request.Close(null);
}

/// <summary>
/// Opens any view in a Helix dialog and waits for its result, without Prism or a window style to repeat:
/// <c>var site = await HelixDialogs.ShowAsync&lt;Site&gt;(new EditSiteView { DataContext = vm }, "Edit the site");</c>
/// The view closes it with <see cref="Close"/>; Escape, the × and the backdrop cancel (null / default).
/// </summary>
public static class HelixDialogs
{
    public static async Task<TResult?> ShowAsync<TResult>(object view, string? title = null, string? description = null, double width = 480)
    {
        var request = Open(title, description, width);
        if (request is null) return default;
        request.View = view;
        var result = await request.Task;
        return result is TResult typed ? typed : default;
    }

    /// <summary>The builder receives a handle to close the dialog (pass it to your view model).</summary>
    public static async Task<TResult?> ShowAsync<TResult>(Func<HelixDialogHandle<TResult>, object> build, string? title = null, string? description = null, double width = 480)
    {
        var request = Open(title, description, width);
        if (request is null) return default;
        request.View = build(new HelixDialogHandle<TResult>(request));
        var result = await request.Task;
        return result is TResult typed ? typed : default;
    }

    /// <summary>Closes the dialog that contains this element, with a result (null cancels).</summary>
    public static void Close(DependencyObject fromInside, object? result = null)
    {
        for (var current = fromInside; current is not null; current = (current is System.Windows.Media.Visual ? System.Windows.Media.VisualTreeHelper.GetParent(current) : null) ?? LogicalTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement { DataContext: HelixViewRequest request } && !ReferenceEquals(current, request.View))
            {
                request.Close(result);
                return;
            }
        }
    }

    private static HelixViewRequest? Open(string? title, string? description, double width)
    {
        var host = HelixHost.Active;
        if (host is null)
        {
            System.Diagnostics.Trace.TraceWarning("HelixDialogs.ShowAsync needs a HelixHost in the window.");
            return null;
        }

        var request = new HelixViewRequest(host, title, description, width);
        host.Overlays.Add(request);
        return request;
    }
}
