using Microsoft.EntityFrameworkCore;
namespace ClinicManagementSystem.Web.Services;
public sealed class ListPaging(int page = 1, string pageKey = "page")
{
    public const int PageSize = 10;
    public string PageKey { get; } = pageKey;
    public int Page { get; private set; } = Math.Max(1, page);
    public int TotalCount { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public IEnumerable<T> Slice<T>(IEnumerable<T> rows)
    {
        TotalCount = rows.Count();
        Page = Math.Clamp(Page, 1, TotalPages);
        return rows.Skip((Page - 1) * PageSize).Take(PageSize);
    }

    public async Task<IQueryable<T>> ApplyAsync<T>(IQueryable<T> query)
    {
        TotalCount = await query.CountAsync();
        Page = Math.Clamp(Page, 1, TotalPages);
        return query.Skip((Page - 1) * PageSize).Take(PageSize);
    }
}
