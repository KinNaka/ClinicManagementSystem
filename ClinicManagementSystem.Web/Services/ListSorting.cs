using System.Linq.Expressions;

namespace ClinicManagementSystem.Web.Services;

public static class ListSorting
{
    public static string Normalize(string? sort) => sort is "oldest" or "name_asc" or "name_desc" ? sort : "newest";

    // Apply ordering before Skip/Take and use a unique key for stable page boundaries.
    public static IOrderedQueryable<T> Apply<T, TDate, TId>(
        IQueryable<T> query, string? sort,
        Expression<Func<T, TDate>> date,
        Expression<Func<T, string>> name,
        Expression<Func<T, TId>> id) => Normalize(sort) switch
        {
            "oldest" => query.OrderBy(date).ThenBy(id),
            "name_asc" => query.OrderBy(name).ThenBy(id),
            "name_desc" => query.OrderByDescending(name).ThenByDescending(id),
            _ => query.OrderByDescending(date).ThenByDescending(id)
        };
}

public sealed record ListSortOptions(string Key = "sort", string NameLabel = "Tên", bool HasDates = true);
