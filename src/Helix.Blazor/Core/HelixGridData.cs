using System.Linq.Expressions;
using System.Reflection;

namespace Helix.Blazor;

/// <summary>What the grid asks for: one page, sorted and searched.</summary>
/// <param name="Skip">Rows to skip (page index × page size).</param>
/// <param name="Take">Rows to return; 0 means "all".</param>
/// <param name="SortBy">Column sort name (the property path, e.g. "Salary" or "Address.City"), or null.</param>
/// <param name="SortDescending">Sort direction.</param>
/// <param name="Search">Text typed in the search box, or null.</param>
public sealed record HelixGridRequest(int Skip, int Take, string? SortBy, bool SortDescending, string? Search);

/// <summary>One page of rows plus the total number of rows matching the search.</summary>
public sealed record HelixGridResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);

/// <summary>Loads a page of data for <see cref="HelixDataGrid{TItem}"/>. Honour the token: it is cancelled when a newer request supersedes this one.</summary>
public delegate Task<HelixGridResult<TItem>> HelixGridItemsProvider<TItem>(HelixGridRequest request, CancellationToken cancellationToken);

/// <summary>Applies a <see cref="HelixGridRequest"/> to any IQueryable (EF Core, MongoDB LINQ, in-memory…).</summary>
public static class HelixGridQueryableExtensions
{
    /// <summary>Sorts by <see cref="HelixGridRequest.SortBy"/>. Throws if the property path does not exist on T.</summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, HelixGridRequest request)
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
    public static IQueryable<T> ApplyPage<T>(this IQueryable<T> query, HelixGridRequest request) =>
        request.Take > 0 ? query.Skip(request.Skip).Take(request.Take) : query;
}
