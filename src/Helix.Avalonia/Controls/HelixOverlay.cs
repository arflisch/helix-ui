using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Helix.Avalonia;

/// <summary>
/// Wrap a window's content once: it shows Helix dialogs, drawers, confirmations, prompts and toasts above it.
/// &lt;helix:HelixHost&gt; …your layout… &lt;/helix:HelixHost&gt;
/// </summary>
public class HelixHost : ContentControl
{
    private static readonly List<WeakReference<HelixHost>> Hosts = [];

    public static readonly StyledProperty<global::Avalonia.Layout.HorizontalAlignment> ToastHorizontalAlignmentProperty =
        AvaloniaProperty.Register<HelixHost, global::Avalonia.Layout.HorizontalAlignment>(nameof(ToastHorizontalAlignment), global::Avalonia.Layout.HorizontalAlignment.Right);
    public static readonly StyledProperty<global::Avalonia.Layout.VerticalAlignment> ToastVerticalAlignmentProperty =
        AvaloniaProperty.Register<HelixHost, global::Avalonia.Layout.VerticalAlignment>(nameof(ToastVerticalAlignment), global::Avalonia.Layout.VerticalAlignment.Bottom);
    public static readonly DirectProperty<HelixHost, bool> HasOverlayProperty = AvaloniaProperty.RegisterDirect<HelixHost, bool>(nameof(HasOverlay), h => h.HasOverlay);

    private bool _hasOverlay;
    private TopLevel? _topLevel;

    public HelixHost()
    {
        Overlays.CollectionChanged += (_, e) =>
        {
            HasOverlay = Overlays.Count > 0;
            if (e.NewItems is { Count: > 0 }) Dispatcher.UIThread.Post(FocusTop, DispatcherPriority.Loaded);
        };
    }

    /// <summary>Where toasts appear (Right by default).</summary>
    public global::Avalonia.Layout.HorizontalAlignment ToastHorizontalAlignment { get => GetValue(ToastHorizontalAlignmentProperty); set => SetValue(ToastHorizontalAlignmentProperty, value); }

    /// <summary>Where toasts appear (Bottom by default).</summary>
    public global::Avalonia.Layout.VerticalAlignment ToastVerticalAlignment { get => GetValue(ToastVerticalAlignmentProperty); set => SetValue(ToastVerticalAlignmentProperty, value); }

    public static string CloseText => HelixTexts.Current.Close;

    /// <summary>Open dialogs, drawers, confirmations and prompts, top-most last.</summary>
    public ObservableCollection<object> Overlays { get; } = [];

    /// <summary>True while something is open (the page behind is not reachable).</summary>
    public bool HasOverlay { get => _hasOverlay; private set => SetAndRaise(HasOverlayProperty, ref _hasOverlay, value); }

    /// <summary>The host of the active window (else the first one).</summary>
    public static HelixHost? Active
    {
        get
        {
            lock (Hosts)
            {
                var alive = Hosts.Select(w => w.TryGetTarget(out var h) ? h : null).OfType<HelixHost>().ToList();
                return alive.FirstOrDefault(h => (TopLevel.GetTopLevel(h) as Window)?.IsActive == true) ?? alive.LastOrDefault();
            }
        }
    }

    internal static HelixHost? For(Visual element) =>
        element.FindLogicalAncestorOfType<HelixHost>() ?? element.FindAncestorOfType<HelixHost>() ?? Active;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        lock (Hosts)
        {
            Hosts.RemoveAll(w => !w.TryGetTarget(out var h) || h == this);
            Hosts.Add(new WeakReference<HelixHost>(this));
        }

        // Escape is caught at the window, so it works whatever has the focus (or nothing).
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnKey, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        lock (Hosts) Hosts.RemoveAll(w => !w.TryGetTarget(out var h) || h == this);
        _topLevel?.RemoveHandler(KeyDownEvent, OnKey);
        _topLevel = null;
    }

    /// <summary>Moves the focus into the top-most overlay: its first text box, else its first button.</summary>
    private void FocusTop()
    {
        if (Overlays.Count == 0) return;
        var top = this.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault(p => ReferenceEquals(p.Content, Overlays[^1]));
        if (top is null) return;
        var targets = top.GetVisualDescendants().OfType<InputElement>().Where(c => c.Focusable && c.IsEffectivelyVisible && c.IsEffectivelyEnabled).ToList();
        (targets.OfType<TextBox>().FirstOrDefault() ?? targets.OfType<Button>().LastOrDefault(b => b.Theme is null || b is HelixButton) ?? targets.FirstOrDefault())?.Focus();
    }

    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || Overlays.Count == 0) return;
        e.Handled = CloseTop();
    }

    /// <summary>Closes the top-most overlay as Escape does; false when it refuses (CloseOnEscape = false).</summary>
    public bool CloseTop()
    {
        switch (Overlays.Count == 0 ? null : Overlays[^1])
        {
            case HelixDialogSlot { Dialog.CloseOnEscape: true } slot:
                slot.Dialog.SetCurrentValue(HelixDialog.IsOpenProperty, false);
                return true;
            case HelixConfirmRequest request:
                request.Resolve(false);
                return true;
            case HelixPromptRequest prompt:
                prompt.Resolve(null);
                return true;
            case HelixViewRequest view:
                view.Close(null);
                return true;
            default:
                return false;
        }
    }
}

/// <summary>
/// A modal dialog over the window (needs a <see cref="HelixHost"/>). Bind IsOpen; Title, Description, content and Footer.
/// The content and the footer keep your DataContext.
/// </summary>
public class HelixDialog : ContentControl
{
    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<HelixDialog, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<HelixDialog, string?>(nameof(Title));
    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<HelixDialog, string?>(nameof(Description));
    public static readonly StyledProperty<object?> FooterProperty = AvaloniaProperty.Register<HelixDialog, object?>(nameof(Footer));
    public static readonly StyledProperty<double> DialogWidthProperty = AvaloniaProperty.Register<HelixDialog, double>(nameof(DialogWidth), 480);
    public static readonly StyledProperty<bool> ShowCloseProperty = AvaloniaProperty.Register<HelixDialog, bool>(nameof(ShowClose), true);
    public static readonly StyledProperty<bool> CloseOnBackdropProperty = AvaloniaProperty.Register<HelixDialog, bool>(nameof(CloseOnBackdrop), true);
    public static readonly StyledProperty<bool> CloseOnEscapeProperty = AvaloniaProperty.Register<HelixDialog, bool>(nameof(CloseOnEscape), true);

    private HelixHost? _host;

    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Content at the bottom right, usually buttons.</summary>
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }
    public double DialogWidth { get => GetValue(DialogWidthProperty); set => SetValue(DialogWidthProperty, value); }
    public bool ShowClose { get => GetValue(ShowCloseProperty); set => SetValue(ShowCloseProperty, value); }
    public bool CloseOnBackdrop { get => GetValue(CloseOnBackdropProperty); set => SetValue(CloseOnBackdropProperty, value); }
    public bool CloseOnEscape { get => GetValue(CloseOnEscapeProperty); set => SetValue(CloseOnEscapeProperty, value); }

    /// <summary>Closes the dialog (for the × button).</summary>
    public ICommand CloseCommand => new HelixCommand(() => SetCurrentValue(IsOpenProperty, false));

    /// <summary>Closes on a backdrop click when allowed.</summary>
    public ICommand BackdropCommand => new HelixCommand(() =>
    {
        if (CloseOnBackdrop) SetCurrentValue(IsOpenProperty, false);
    });

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOpenProperty) Sync();
        if (change.Property == FooterProperty)
        {
            // Logical child: the footer's bindings see your DataContext.
            if (change.OldValue is ILogical old) LogicalChildren.Remove(old);
            if (change.NewValue is ILogical added && added.LogicalParent is null) LogicalChildren.Add(added);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Sync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_host?.Overlays.FirstOrDefault(s => s is HelixDialogSlot d && d.Dialog == this) is { } slot) _host.Overlays.Remove(slot);
    }

    private void Sync()
    {
        if (!this.IsAttachedToVisualTree()) return; // shown once attached
        _host ??= HelixHost.For(this);
        if (_host is null) return;
        var slot = _host.Overlays.FirstOrDefault(s => s is HelixDialogSlot d && d.Dialog == this);
        if (IsOpen && slot is null) _host.Overlays.Add(CreateSlot());
        else if (!IsOpen && slot is not null) _host.Overlays.Remove(slot);
    }

    /// <summary>What the host shows (a dialog box, a drawer…).</summary>
    internal virtual HelixDialogSlot CreateSlot() => new(this);
}

public enum HelixDrawerPosition { Right, Left, Bottom }

/// <summary>A panel sliding from a side of the window (needs a <see cref="HelixHost"/>): Position, DrawerSize, Title, content, Footer.</summary>
public class HelixDrawer : HelixDialog
{
    public static readonly StyledProperty<HelixDrawerPosition> PositionProperty = AvaloniaProperty.Register<HelixDrawer, HelixDrawerPosition>(nameof(Position));
    public static readonly StyledProperty<double> DrawerSizeProperty = AvaloniaProperty.Register<HelixDrawer, double>(nameof(DrawerSize), 420);

    public HelixDrawerPosition Position { get => GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    /// <summary>Width (left / right) or height (bottom).</summary>
    public double DrawerSize { get => GetValue(DrawerSizeProperty); set => SetValue(DrawerSizeProperty, value); }

    internal override HelixDialogSlot CreateSlot() => new HelixDrawerSlot(this);
}

/// <summary>An open dialog in the host's overlay layer (a plain object, drawn by the host's templates).</summary>
public class HelixDialogSlot(HelixDialog dialog)
{
    public HelixDialog Dialog { get; } = dialog;
}

/// <summary>An open drawer in the host's overlay layer.</summary>
public sealed class HelixDrawerSlot(HelixDrawer drawer) : HelixDialogSlot(drawer)
{
    public HelixDrawer Drawer { get; } = drawer;
    public bool IsLeft => Drawer.Position == HelixDrawerPosition.Left;
    public bool IsBottom => Drawer.Position == HelixDrawerPosition.Bottom;
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
    public bool HasMessage => !string.IsNullOrEmpty(Message);
    public bool IsDanger { get; }
    public HelixTone ConfirmTone => IsDanger ? HelixTone.Danger : HelixTone.Primary;
    public string Icon => IsDanger ? "warning" : "info";
    public string ConfirmText { get; }
    public string CancelText { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
    internal Task<bool> Task => _result.Task;

    public void Resolve(bool confirmed)
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
        if (HelixHost.Active is not { } host)
        {
            System.Diagnostics.Trace.TraceWarning("HelixConfirm.AskAsync needs a HelixHost in the window.");
            return Task.FromResult(false);
        }

        var request = new HelixConfirmRequest(host, title, message, danger, confirmText, cancelText);
        host.Overlays.Add(request);
        return request.Task;
    }

    /// <summary>Asks for a short text: the typed text, or null when cancelled. Validate returns an error message, or null when the text is fine.</summary>
    public static Task<string?> PromptAsync(string title, string? message = null, string? value = null, string? placeholder = null,
        string? confirmText = null, Func<string, string?>? validate = null)
    {
        if (HelixHost.Active is not { } host)
        {
            System.Diagnostics.Trace.TraceWarning("HelixConfirm.PromptAsync needs a HelixHost in the window.");
            return Task.FromResult<string?>(null);
        }

        var request = new HelixPromptRequest(host, title, message, value, placeholder, confirmText, validate);
        host.Overlays.Add(request);
        return request.Task;
    }
}

/// <summary>An open text prompt (<see cref="HelixConfirm.PromptAsync"/>) in the host's overlay layer.</summary>
public sealed class HelixPromptRequest : INotifyPropertyChanged
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; }
    public string? Message { get; }
    public bool HasMessage => !string.IsNullOrEmpty(Message);
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

    /// <summary>Validates and closes with the text (the confirm button and Enter).</summary>
    public void Submit()
    {
        Error = _validate?.Invoke(Text);
        if (Error is null) Resolve(Text);
    }

    public void Resolve(string? text)
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
    public bool HasTitle => !string.IsNullOrEmpty(Title);
    public bool HasDescription => !string.IsNullOrEmpty(Description);
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
/// Opens any view in a Helix dialog and waits for its result:
/// <c>var site = await HelixDialogs.ShowAsync&lt;Site&gt;(new EditSiteView { DataContext = vm }, "Edit the site");</c>
/// The view closes it with <see cref="Close"/>; Escape and the × cancel (null / default).
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
    public static void Close(Visual fromInside, object? result = null)
    {
        for (Visual? current = fromInside; current is not null; current = current.GetVisualParent())
        {
            if (current is StyledElement { DataContext: HelixViewRequest request } && !ReferenceEquals(current, request.View))
            {
                request.Close(result);
                return;
            }
        }
    }

    private static HelixViewRequest? Open(string? title, string? description, double width)
    {
        if (HelixHost.Active is not { } host)
        {
            System.Diagnostics.Trace.TraceWarning("HelixDialogs.ShowAsync needs a HelixHost in the window.");
            return null;
        }

        var request = new HelixViewRequest(host, title, description, width);
        host.Overlays.Add(request);
        return request;
    }
}
