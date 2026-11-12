using Helix.Blazor;
using Helix.Demo.Components.Shared;

namespace Helix.Demo;

/// <summary>
/// Pretends to be a remote API or a database: latency, server-side search, sort and paging, optional failures.
/// In a real app this is an EF Core / MongoDB query or an HttpClient call.
/// </summary>
public sealed class EmployeeApi(TimeProvider timeProvider)
{
    private readonly List<Employee> _employees = SampleData.Employees.ToList();
    private readonly Lock _gate = new();

    public TimeSpan Latency { get; set; } = TimeSpan.FromMilliseconds(600);

    public bool SimulateFailure { get; set; }

    public async Task<HelixGridResult<Employee>> QueryAsync(HelixGridRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(Latency, timeProvider, cancellationToken);

        if (SimulateFailure)
        {
            throw new HttpRequestException("Le serveur a répondu 503 (service indisponible).");
        }

        IQueryable<Employee> query;
        lock (_gate)
        {
            query = _employees.ToList().AsQueryable();
        }

        if (request.Search is { } search)
        {
            query = query.Where(e =>
                e.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                e.Email.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                e.Department.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                e.Role.Contains(search, StringComparison.CurrentCultureIgnoreCase));
        }

        var total = query.Count();
        var items = query.ApplySort(request).ApplyPage(request).ToList();
        return new HelixGridResult<Employee>(items, total);
    }

    public async Task DeleteAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        await Task.Delay(Latency / 2, timeProvider, cancellationToken);
        lock (_gate)
        {
            _employees.RemoveAll(e => ids.Contains(e.Id));
        }
    }
}
