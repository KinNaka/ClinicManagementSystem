
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
        public async Task<IActionResult> Revenue(string? date = null, int page = 1)
        {
            DateOnly? selectedDate = null;
            if (!string.IsNullOrWhiteSpace(date))
            {
                if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsed) || parsed == DateOnly.MaxValue)
                    return BadRequest("Ngày không hợp lệ. Vui lòng chọn lại ngày theo định dạng yyyy-MM-dd.");
                selectedDate = parsed;
            }
            var paging = new ListPaging(page);
            ViewData["Paging"] = paging;
            return View(await _dashboardService.GetStaffRevenueAsync(selectedDate, paging));
        }
    }
}
