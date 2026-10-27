
using ClinicManagementSystem.Web.Models.ViewModels.Dashboard;

namespace ClinicManagementSystem.Web.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardAsync();
        Task<StaffRevenueViewModel> GetStaffRevenueAsync(DateOnly? date = null, ListPaging? paging = null);
    }
}
