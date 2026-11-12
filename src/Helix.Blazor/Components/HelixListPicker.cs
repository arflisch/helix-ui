using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Helix.Blazor;

/// <summary>Shared core of <see cref="HelixSelect{TValue}"/> and <see cref="HelixMultiSelect{TValue}"/>: field, options list, search and keyboard.</summary>
public abstract class HelixListPicker<TValue> : HelixComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The options. With a SearchProvider, the options shown before the first search (may be empty).</summary>
    [Parameter] public IEnumerable<TValue> Items { get; set; } = [];

    /// <summary>How an option is displayed and searched. Defaults to ToString().</summary>
    [Parameter] public Func<TValue, string>? ItemText { get; set; }

    /// <summary>Custom content of an option in the list (the field still shows ItemText).</summary>
    [Parameter] public RenderFragment<TValue>? ItemTemplate { get; set; }

    /// <summary>A search box at the top of the list that filters the options as you type.</summary>
    [Parameter] public bool Searchable { get; set; }

    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>
    /// Server search: called with the typed text (after SearchDelay) and a token cancelled by the next keystroke.
    /// Turns Searchable on. Pass a method, not a lambda that changes on every render.
    /// </summary>
    [Parameter] public Func<string, CancellationToken, Task<IEnumerable<TValue>>>? SearchProvider { get; set; }

    /// <summary>Wait after the last keystroke before calling SearchProvider.</summary>
    [Parameter] public TimeSpan SearchDelay { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>Text when no option matches.</summary>
    [Parameter] public string? EmptyText { get; set; }

    [Parameter] public string? Label { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Content at the start of the input (icon, sign, currency…).</summary>
    [Parameter] public RenderFragment? Prefix { get; set; }

    /// <summary>Content at the end of the input (unit, button…).</summary>
    [Parameter] public RenderFragment? Suffix { get; set; }

    /// <summary>Help text under the field, announced with it.</summary>
    [Parameter] public string? Hint { get; set; }

    /// <summary>Shows the field in error state with this message (replaces the hint).</summary>
    [Parameter] public string? Error { get; set; }
    [Parameter] public bool Required { get; set; }

    /// <summary>Focus color. Null: HelixOptions.Defaults.Field.Color, else primary.</summary>
    [Parameter] public string? Color { get; set; }
    [Parameter] public string? Radius { get; set; }
    [Parameter] public string? Background { get; set; }
    [Parameter] public string? BorderColor { get; set; }

    protected readonly string Id = HelixId.Next("select");
    protected List<TValue> AllItems = [];
    protected List<TValue> Visible = [];
    protected bool IsOpen;
    protected int Active = -1;
    protected string SearchText = "";
    protected bool Searching;
    protected ElementReference SearchBox;

    private IJSObjectReference? _module;
    private CancellationTokenSource? _searchCts;
    private bool _focusSearch;
    private List<TValue>? _serverItems;

    protected bool HasSearch => Searchable || SearchProvider is not null;

    protected string? FieldStyle => new StyleBuilder()
        .Color("input", Color)
        .Var("--helix-input-radius", Radius)
        .Var("--helix-input-bg", Background)
        .Var("--helix-input-border", BorderColor)
        .Raw(Style)
        .Build();

    protected override void OnParametersSet()
    {
        AllItems = Items.ToList();
        Refilter();
    }

    protected string TextOf(TValue item) => ItemText?.Invoke(item) ?? item?.ToString() ?? "";

    protected abstract bool IsSelected(TValue item);

    /// <summary>The user chose an option (click or Enter).</summary>
    protected abstract Task Choose(TValue item);

    /// <summary>The options to show: the server results, or the items filtered by the search text.</summary>
    private void Refilter()
    {
        if (_serverItems is not null)
        {
            Visible = _serverItems;
        }
        else if (HasSearch && SearchProvider is null && !string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            Visible = AllItems.Where(i => TextOf(i).Contains(term, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }
        else
        {
            Visible = AllItems;
        }

        Active = Math.Clamp(Active, -1, Visible.Count - 1);
    }

    protected void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    protected void Open()
    {
        IsOpen = true;
        SearchText = "";
        _serverItems = null;
        Refilter();
        Active = Visible.FindIndex(IsSelected);
        _focusSearch = HasSearch;
    }

    protected virtual void Close()
    {
        IsOpen = false;
        _searchCts?.Cancel();
        Searching = false;
    }

    protected async Task OnSearchInput(ChangeEventArgs e)
    {
        SearchText = e.Value?.ToString() ?? "";
        Active = -1;
        if (SearchProvider is null)
        {
            Refilter();
            Active = Visible.Count > 0 ? 0 : -1;
            return;
        }

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        var cts = new CancellationTokenSource();
        _searchCts = cts;
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            _serverItems = null;
            Searching = false;
            Refilter();
            return;
        }

        Searching = true;
        try
        {
            await Task.Delay(SearchDelay, cts.Token);
            var results = (await SearchProvider(SearchText.Trim(), cts.Token)).ToList();
            if (cts.IsCancellationRequested) return;
            _serverItems = results;
            Refilter();
            Active = Visible.Count > 0 ? 0 : -1;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return; // A newer keystroke owns the list.
        }
        finally
        {
            if (ReferenceEquals(_searchCts, cts)) Searching = false;
        }
    }

    /// <summary>Arrows move in the list, Enter chooses, Escape and Tab close.</summary>
    protected async Task OnListKey(KeyboardEventArgs e)
    {
        if (!IsOpen)
        {
            if (e.Key is "ArrowDown" or "ArrowUp") Open();
            return;
        }

        switch (e.Key)
        {
            case "ArrowDown": Active = Math.Min(Active + 1, Visible.Count - 1); break;
            case "ArrowUp": Active = Math.Max(Active - 1, 0); break;
            case "Home" when !HasSearch: Active = Visible.Count > 0 ? 0 : -1; break;
            case "End" when !HasSearch: Active = Visible.Count - 1; break;
            case "Enter" when Active >= 0 && Active < Visible.Count: await Choose(Visible[Active]); break;
            case " " when !HasSearch && Active >= 0 && Active < Visible.Count: await Choose(Visible[Active]); break;
            case "Escape" or "Tab": Close(); break;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_focusSearch || !IsOpen)
        {
            return;
        }

        _focusSearch = false;
        try
        {
            await SearchBox.FocusAsync();
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", HelixJs.Path);
            await _module.InvokeVoidAsync("preventKeys", SearchBox, new[] { "ArrowDown", "ArrowUp", "Enter" });
        }
        catch (JSDisconnectedException)
        {
            // Circuit closed.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        await HelixJs.DisposeQuietlyAsync(_module);
        GC.SuppressFinalize(this);
    }
}
