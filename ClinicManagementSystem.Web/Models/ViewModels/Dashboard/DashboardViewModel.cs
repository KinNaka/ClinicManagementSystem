
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
        public DateOnly ReportStart { get; set; }
        public DateOnly ReportEnd { get; set; }
        public string PeriodMode { get; set; } = "custom";
        public int TopCount { get; set; } = 10;
        public decimal ReportRevenue { get; set; }
        public List<DashboardCustomerRow> TopCustomers { get; set; } = new();
        public List<DashboardProductRow> TopProducts { get; set; } = new();
    }

    public class DashboardCustomerRow { public long PatientId { get; set; } public string Name { get; set; } = ""; public int SaleCount { get; set; } public decimal Amount { get; set; } }
    public class DashboardProductRow { public long ProductId { get; set; } public string Name { get; set; } = ""; public decimal Quantity { get; set; } public decimal Amount { get; set; } }

    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }
    }
}
