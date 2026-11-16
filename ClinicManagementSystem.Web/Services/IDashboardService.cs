
using ClinicManagementSystem.Web.Models.ViewModels.Dashboard;

namespace ClinicManagementSystem.Web.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardAsync(DateOnly? start = null, DateOnly? end = null, string periodMode = "custom", int topCount = 10);
        Task<StaffRevenueViewModel> GetStaffRevenueAsync(DateOnly? date = null, ListPaging? paging = null);
    }
}
