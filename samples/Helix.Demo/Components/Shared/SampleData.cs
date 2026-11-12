namespace Helix.Demo.Components.Shared;

public enum EmployeeStatus { Active, Remote, Leave, Offboarding }

public sealed record Employee(int Id, string Name, string Email, string Department, string Role, EmployeeStatus Status, decimal Salary, DateOnly StartDate);

/// <summary>Deterministic fake data for the demo.</summary>
public static class SampleData
{
    private static readonly string[] FirstNames =
        ["Camille", "Lucas", "Léa", "Hugo", "Chloé", "Louis", "Manon", "Gabriel", "Inès", "Arthur", "Jade", "Nathan", "Sarah", "Adam", "Zoé", "Théo"];

    private static readonly string[] LastNames =
        ["Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard", "Petit", "Durand", "Leroy", "Moreau", "Simon", "Laurent", "Lefebvre", "Michel", "Garcia", "Fontaine"];

    private static readonly (string Department, string[] Roles)[] Departments =
    [
        ("Engineering", ["Developer", "Tech lead", "DevOps", "QA"]),
        ("Design", ["Product designer", "UI designer"]),
        ("Product", ["Product manager", "Product owner"]),
        ("Sales", ["Account executive", "Sales manager"]),
        ("Support", ["Support agent", "Support lead"]),
    ];

    public static readonly IReadOnlyList<Employee> Employees = Build(48);

    private static List<Employee> Build(int count)
    {
        var random = new Random(42);
        var list = new List<Employee>(count);
        for (var i = 1; i <= count; i++)
        {
            var first = FirstNames[random.Next(FirstNames.Length)];
            var last = LastNames[random.Next(LastNames.Length)];
            var (department, roles) = Departments[random.Next(Departments.Length)];
            var status = random.Next(10) switch
            {
                < 6 => EmployeeStatus.Active,
                < 8 => EmployeeStatus.Remote,
                9 => EmployeeStatus.Offboarding,
                _ => EmployeeStatus.Leave,
            };
            var email = $"{Slug(first)}.{Slug(last)}@example.com";
            var salary = 36_000 + random.Next(0, 56) * 1_000;
            var start = new DateOnly(2016, 1, 1).AddDays(random.Next(0, 3500));
            list.Add(new Employee(i, $"{first} {last}", email, department, roles[random.Next(roles.Length)], status, salary, start));
        }

        return list;
    }

    private static string Slug(string value) => value.ToLowerInvariant()
        .Replace('é', 'e').Replace('è', 'e').Replace('ë', 'e').Replace('ï', 'i').Replace('ô', 'o');
}
