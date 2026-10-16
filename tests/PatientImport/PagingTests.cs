using ClinicManagementSystem.Web.Services;
using ClinicManagementSystem.Web.Models.ViewModels.Sales;

internal static class PagingTests
{
    public static void Run()
    {
        static void Check(bool condition) { if (!condition) throw new Exception("Paging regression"); }
        var rows = Enumerable.Range(1, 23).ToArray();
        var first = new ListPaging(-1);
        Check(first.Slice(rows).SequenceEqual(Enumerable.Range(1, 10)) && first.Page == 1 && first.TotalPages == 3);
        var second = new ListPaging(2);
        Check(second.Slice(rows).SequenceEqual(Enumerable.Range(11, 10)));
        var last = new ListPaging(int.MaxValue);
        Check(last.Slice(rows).SequenceEqual(Enumerable.Range(21, 3)) && last.Page == 3);
        var empty = new ListPaging(9);
        Check(!empty.Slice(Array.Empty<int>()).Any() && empty.Page == 1 && empty.TotalPages == 1);
        Check(SaleIndexViewModel.PageSize == 10);
        Console.WriteLine("Paging boundary checks passed.");
    }
}
