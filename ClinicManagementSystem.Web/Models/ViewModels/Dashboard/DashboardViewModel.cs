
namespace ClinicManagementSystem.Web.Models.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        public int TotalPatients { get; set; }

        public int TotalProducts { get; set; }

        public int LowStockProducts { get; set; }

        public int TodaySalesCount { get; set; }

        public decimal TodayRevenue { get; set; }

        public List<DailyRevenueViewModel> RevenueLast7Days
        { get; set; } = new();
    }

    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }
    }
}
