
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Web.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(
            IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!User.IsInRole("Admin")) return RedirectToAction(nameof(Revenue));
            var model = await _dashboardService
                .GetDashboardAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Revenue()
        {
            return View(await _dashboardService.GetStaffRevenueAsync());
        }
    }
}
