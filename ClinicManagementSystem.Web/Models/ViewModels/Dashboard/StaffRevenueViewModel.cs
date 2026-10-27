namespace ClinicManagementSystem.Web.Models.ViewModels.Dashboard;

public sealed class StaffRevenueViewModel
{
    public DateOnly SelectedDate { get; set; }
    public decimal SelectedRevenue { get; set; }
    public int SelectedSalesCount { get; set; }
    public List<RevenueSaleViewModel> Sales { get; set; } = new();
    public DateTime Today { get; set; }
    public DateTime WeekStart { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal WeekRevenue { get; set; }
}

public sealed class RevenueSaleViewModel
{
    public long Id { get; set; }
    public string SaleCode { get; set; } = "";
    public DateTime SaleDate { get; set; }
    public string PatientName { get; set; } = "";
    public decimal TotalAmount { get; set; }
}
