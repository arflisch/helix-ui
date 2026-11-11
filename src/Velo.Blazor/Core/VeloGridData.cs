using System.Linq.Expressions;
using System.Reflection;

namespace Velo.Blazor;

/// <summary>What the grid asks for: one page, sorted and searched.</summary>
/// <param name="Skip">Rows to skip (page index × page size).</param>
/// <param name="Take">Rows to return; 0 means "all".</param>
/// <param name="SortBy">Column sort name (the property path, e.g. "Salary" or "Address.City"), or null.</param>
/// <param name="SortDescending">Sort direction.</param>
/// <param name="Search">Text typed in the search box, or null.</param>
public sealed record VeloGridRequest(int Skip, int Take, string? SortBy, bool SortDescending, string? Search);

/// <summary>One page of rows plus the total number of rows matching the search.</summary>
public sealed record VeloGridResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);

/// <summary>Loads a page of data for <see cref="VeloDataGrid{TItem}"/>. Honour the token: it is cancelled when a newer request supersedes this one.</summary>
public delegate Task<VeloGridResult<TItem>> VeloGridItemsProvider<TItem>(VeloGridRequest request, CancellationToken cancellationToken);

/// <summary>Applies a <see cref="VeloGridRequest"/> to any IQueryable (EF Core, MongoDB LINQ, in-memory…).</summary>
public static class VeloGridQueryableExtensions
{
    /// <summary>Sorts by <see cref="VeloGridRequest.SortBy"/>. Throws if the property path does not exist on T.</summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, VeloGridRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SortBy))
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression body = parameter;
        foreach (var name in request.SortBy.Split('.'))
        {
            var member = body.Type.GetMember(name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                .FirstOrDefault()
                ?? throw new ArgumentException($"'{request.SortBy}' is not a sortable member of {typeof(T).Name}.", nameof(request));
            body = Expression.MakeMemberAccess(body, member);
        }

        var keySelector = Expression.Lambda(body, parameter);
        var method = request.SortDescending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
        var call = Expression.Call(typeof(Queryable), method, [typeof(T), body.Type], query.Expression, Expression.Quote(keySelector));
        return query.Provider.CreateQuery<T>(call);
    }

    /// <summary>Skips and takes the requested page (Take = 0 returns everything).</summary>
    public static IQueryable<T> ApplyPage<T>(this IQueryable<T> query, VeloGridRequest request) =>
        request.Take > 0 ? query.Skip(request.Skip).Take(request.Take) : query;
}
