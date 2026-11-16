
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
        public async Task<IActionResult> Index(string? start, string? end, string? period = "custom", int top = 10)
        {
            if (!User.IsInRole("Admin")) return RedirectToAction(nameof(Revenue));
            if (!ParseDate(start, out var reportStart) || !ParseDate(end, out var reportEnd) ||
                (reportStart.HasValue != reportEnd.HasValue) || (reportStart.HasValue && reportEnd < reportStart) ||
                (reportStart.HasValue && reportEnd!.Value.DayNumber - reportStart.Value.DayNumber > 3660) ||
                !new[] { 10, 20, 50, 100 }.Contains(top) || !new[] { "custom", "month", "year" }.Contains(period ?? "custom"))
                return BadRequest("Khoảng thời gian hoặc số lượng top không hợp lệ.");
            var model = await _dashboardService.GetDashboardAsync(reportStart, reportEnd, period ?? "custom", top);

            return View(model);
        }

        private static bool ParseDate(string? value, out DateOnly? date)
        {
            date = null;
            if (string.IsNullOrWhiteSpace(value)) return true;
            if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed)) return false;
            date = parsed; return true;
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
