namespace ClinicManagementSystem.Web.Models.ViewModels.Dashboard;

public sealed class StaffRevenueViewModel
{
    public DateTime Today { get; set; }
    public DateTime WeekStart { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal WeekRevenue { get; set; }
}
