using ClinicManagementSystem.Web.Models.ViewModels.Sales;

namespace ClinicManagementSystem.Web.Models.ViewModels.Patients;

public class PatientDetailsViewModel
{
    public long Id { get; set; }
    public PatientFormViewModel Patient { get; set; } = new();
    public PatientPurchaseHistoryViewModel PurchaseHistory { get; set; } = new(0, 1);
    public decimal LegacyTotalSales { get; set; }
    public decimal CurrentTotalSales { get; set; }
    public decimal TotalSales => LegacyTotalSales + CurrentTotalSales;
}

public class PatientPurchaseHistoryViewModel
{
    public const int PageSize = 10;

    public PatientPurchaseHistoryViewModel(int totalCount, int requestedPage)
    {
        TotalCount = Math.Max(0, totalCount);
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        Page = Math.Clamp(requestedPage, 1, TotalPages);
    }

    public int TotalCount { get; }
    public int TotalPages { get; }
    public int Page { get; }
    public int Offset => (Page - 1) * PageSize;
    public List<SaleListViewModel> Sales { get; set; } = new();
}
