using ClinicManagementSystem.Web.Models.ViewModels.Patients;

var cases = new[] {
    (Count: 0, Requested: 1, Page: 1, Pages: 1, Offset: 0),
    (Count: 10, Requested: 1, Page: 1, Pages: 1, Offset: 0),
    (Count: 11, Requested: 2, Page: 2, Pages: 2, Offset: 10),
    (Count: 25, Requested: 3, Page: 3, Pages: 3, Offset: 20),
    (Count: 25, Requested: -10, Page: 1, Pages: 3, Offset: 0),
    (Count: 25, Requested: int.MaxValue, Page: 3, Pages: 3, Offset: 20),
    (Count: 0, Requested: int.MaxValue, Page: 1, Pages: 1, Offset: 0),
    (Count: int.MaxValue, Requested: int.MaxValue, Page: 214748365, Pages: 214748365, Offset: 2147483640)
};
foreach (var c in cases) {
    var history = new PatientPurchaseHistoryViewModel(c.Count, c.Requested);
    if (history.Page != c.Page || history.TotalPages != c.Pages || history.Offset != c.Offset || history.TotalCount != c.Count)
        throw new Exception($"Pagination failed for count {c.Count}, requested page {c.Requested}.");
}
var ids = Enumerable.Range(1, 25).ToArray();
var pages = Enumerable.Range(1, 3).Select(page => {
    var history = new PatientPurchaseHistoryViewModel(ids.Length, page);
    return ids.Skip(history.Offset).Take(PatientPurchaseHistoryViewModel.PageSize).ToArray();
}).ToArray();
if (pages[0].Length != 10 || pages[1].Length != 10 || pages[2].Length != 5 || !pages.SelectMany(page => page).SequenceEqual(ids))
    throw new Exception("Page boundaries duplicated or omitted invoices.");
Console.WriteLine("Passed: 8 pagination boundary cases and 25-invoice coverage (10 / 10 / 5).");
