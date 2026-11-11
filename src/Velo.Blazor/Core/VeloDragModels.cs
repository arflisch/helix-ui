using Microsoft.JSInterop;

namespace Velo.Blazor;

/// <summary>A Kanban column.</summary>
/// <param name="Limit">Work-in-progress limit: the column is flagged when it holds more cards.</param>
public sealed record VeloKanbanColumn(string Key, string Title, string? Color = null, int? Limit = null);

/// <summary>A card was dropped (or moved with the keyboard).</summary>
/// <param name="Index">Position in the target column, counted without the moved card.</param>
public sealed record VeloKanbanMove<TItem>(TItem Item, string From, string To, int Index)
{
    /// <summary>
    /// Returns a new list with the card moved: list = move.ApplyTo(list, t => t.Status, (t, col) => t with { Status = col }).
    /// </summary>
    public List<TItem> ApplyTo(IEnumerable<TItem> items, Func<TItem, string> columnOf, Func<TItem, string, TItem> withColumn)
    {
        var list = items.ToList();
        list.Remove(Item);
        var moved = withColumn(Item, To);
        var inColumn = list.Where(i => columnOf(i) == To).ToList();
        var insertAt = Index < inColumn.Count
            ? list.IndexOf(inColumn[Index])
            : inColumn.Count > 0 ? list.IndexOf(inColumn[^1]) + 1 : list.Count;
        list.Insert(insertAt, moved);
        return list;
    }
}

/// <summary>A DataGrid column as the reader arranged it.</summary>
public sealed record VeloColumnState(string Key, bool Visible = true);

/// <summary>One heatmap cell. Value = null is shown as "no data".</summary>
public sealed record VeloHeatCell(string Row, string Column, double? Value);

/// <summary>Position and size of a dashboard tile, the part worth saving per user.</summary>
public sealed record VeloTileState(string Id, int Span = 1, bool Hidden = false);

/// <summary>Loads velo.js once per component and turns on the Firefox drag fix.</summary>
internal static class VeloJs
{
    public const string Path = "./_content/Velo.Blazor/velo.js";

    public static async Task<IJSObjectReference?> EnableDndAsync(IJSRuntime js)
    {
        try
        {
            var module = await js.InvokeAsync<IJSObjectReference>("import", Path);
            await module.InvokeVoidAsync("enableDnd");
            return module;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
    }

    public static async ValueTask DisposeQuietlyAsync(IJSObjectReference? module)
    {
        if (module is null)
        {
            return;
        }

        try
        {
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit already closed.
        }
    }
}
