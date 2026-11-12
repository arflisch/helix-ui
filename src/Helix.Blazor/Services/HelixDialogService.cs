using Microsoft.AspNetCore.Components;

namespace Helix.Blazor;

/// <summary>
/// Opens one of your components in a Helix dialog and waits for its result:
/// <c>var order = await Dialogs.ShowAsync&lt;EditOrder, Order&gt;("Edit order", new() { ["Order"] = order });</c>
/// The component receives a cascading <see cref="HelixDialogInstance"/> to close itself with a result. HelixProvider shows it.
/// </summary>
public sealed class HelixDialogService
{
    private readonly List<HelixDialogInstance> _open = [];

    internal IReadOnlyList<HelixDialogInstance> Open => _open;

    internal event Action? Changed;

    /// <summary>Shows the component; completes with its result, or null/default when closed or cancelled.</summary>
    public async Task<TResult?> ShowAsync<TComponent, TResult>(string? title, IDictionary<string, object?>? parameters = null, HelixDialogShowOptions? options = null)
        where TComponent : IComponent
    {
        var result = await ShowAsync(typeof(TComponent), title, parameters, options);
        return result is TResult typed ? typed : default;
    }

    /// <summary>Shows the component; completes with what it passed to Close, or null when cancelled.</summary>
    public Task<object?> ShowAsync<TComponent>(string? title, IDictionary<string, object?>? parameters = null, HelixDialogShowOptions? options = null)
        where TComponent : IComponent => ShowAsync(typeof(TComponent), title, parameters, options);

    public Task<object?> ShowAsync(Type component, string? title, IDictionary<string, object?>? parameters = null, HelixDialogShowOptions? options = null)
    {
        var instance = new HelixDialogInstance(this, component, title, parameters ?? new Dictionary<string, object?>(), options ?? new HelixDialogShowOptions());
        _open.Add(instance);
        Changed?.Invoke();
        return instance.Task;
    }

    internal void Remove(HelixDialogInstance instance)
    {
        if (_open.Remove(instance)) Changed?.Invoke();
    }
}

/// <summary>Size and behavior of a dialog opened with <see cref="HelixDialogService"/>.</summary>
public sealed record HelixDialogShowOptions
{
    public string? Description { get; init; }

    /// <summary>Maximum width, e.g. "560px" (480px).</summary>
    public string? Width { get; init; }

    public bool CloseOnBackdrop { get; init; } = true;
    public bool ShowClose { get; init; } = true;
}

/// <summary>Given (cascading) to the component shown by <see cref="HelixDialogService"/>: close it with a result, or cancel.</summary>
public sealed class HelixDialogInstance
{
    private readonly HelixDialogService _service;
    private readonly TaskCompletionSource<object?> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal HelixDialogInstance(HelixDialogService service, Type component, string? title, IDictionary<string, object?> parameters, HelixDialogShowOptions options)
    {
        _service = service;
        Component = component;
        Title = title;
        Parameters = parameters;
        Options = options;
    }

    internal Type Component { get; }
    internal IDictionary<string, object?> Parameters { get; }
    internal HelixDialogShowOptions Options { get; }
    internal Task<object?> Task => _result.Task;

    /// <summary>The dialog title; the component may change it.</summary>
    public string? Title { get; set; }

    /// <summary>Closes the dialog; ShowAsync completes with this result.</summary>
    public void Close(object? result = null)
    {
        _service.Remove(this);
        _result.TrySetResult(result);
    }

    /// <summary>Closes the dialog without a result.</summary>
    public void Cancel() => Close(null);
}
