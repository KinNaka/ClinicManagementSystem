using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Services;
using Microsoft.EntityFrameworkCore;

internal static class SortingTests
{
    public static void Run()
    {
        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        // IDs deliberately disagree with dates; two rows share the same date/name.
        var rows = new[]
        {
            new Patient { Id = 3, FullName = "An", CreatedAt = new DateTime(2020, 1, 1) },
            new Patient { Id = 1, FullName = "Binh", CreatedAt = new DateTime(2025, 1, 1) },
            new Patient { Id = 2, FullName = "An", CreatedAt = new DateTime(2025, 1, 1) }
        }.AsQueryable();
        long[] Ordered(string? sort) => ListSorting.Apply(rows, sort, p => p.CreatedAt, p => p.FullName, p => p.Id)
            .Select(p => p.Id).ToArray();
        Check(Ordered(null).SequenceEqual(new long[] { 2, 1, 3 }), "Default must be newest by creation date, with a stable ID tie-breaker.");
        Check(Ordered("oldest").SequenceEqual(new long[] { 3, 1, 2 }), "Oldest ordering failed.");
        Check(Ordered("name_asc").SequenceEqual(new long[] { 2, 3, 1 }), "Name ascending failed.");
        Check(Ordered("name_desc").SequenceEqual(new long[] { 1, 3, 2 }), "Name descending failed.");
        Check(Ordered("unrecognized").SequenceEqual(Ordered(null)), "Unknown sort must fall back to newest.");

        var many = Enumerable.Range(1, 23).Select(i => new Patient
        {
            Id = i, FullName = "Same name", CreatedAt = new DateTime(2020, 1, 1).AddDays(i)
        }).Reverse().AsQueryable();
        var sorted = ListSorting.Apply(many.Where(p => p.Id > 2), null, p => p.CreatedAt, p => p.FullName, p => p.Id);
        var first = new ListPaging(1).Slice(sorted).Select(p => p.Id).ToArray();
        var second = new ListPaging(2).Slice(sorted).Select(p => p.Id).ToArray();
        Check(first.SequenceEqual(Enumerable.Range(14, 10).Reverse().Select(i => (long)i)), "Ordering must happen before pagination.");
        Check(second.SequenceEqual(Enumerable.Range(4, 10).Reverse().Select(i => (long)i)) && !first.Intersect(second).Any(), "Adjacent pages must not overlap or skip rows.");

        // Translation checks do not connect to or modify a database.
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=SortingTranslationOnly;Integrated Security=True;TrustServerCertificate=True").Options);
        foreach (var sort in new[] { "newest", "oldest", "name_asc", "name_desc" })
        {
            var sql = ListSorting.Apply(db.Patients.Where(p => p.Status == "Active"), sort, p => p.CreatedAt, p => p.FullName, p => p.Id)
                .Skip(10).Take(10).ToQueryString();
            Check(sql.Contains("ORDER BY") && sql.Contains("OFFSET") && sql.Contains("FETCH NEXT"), "Sorting/pagination SQL did not translate.");
            var key = sort.StartsWith("name") ? "FullName" : "CreatedAt";
            var descending = sort is "newest" or "name_desc" ? " DESC" : "";
            Check(sql.Contains($"ORDER BY [p].[{key}]{descending}, [p].[Id]"), "SQL sort key or direction is incorrect.");
        }
        Console.WriteLine("Sorting checks passed: date/name directions, defaults, stable ties, filtering, page boundaries and SQL translation.");
    }
}
