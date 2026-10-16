using ClinicManagementSystem.Web.Models.ViewModels.Appointments;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Web.Controllers;

[Authorize(Roles = "Admin,Staff")]
public sealed class AppointmentsController(AppointmentService appointments) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? fromDate, DateOnly? toDate, string? searchTerm,
        string? status, string? sort, int page = 1)
    {
        // An explicit empty date filter means all dates; the first visit starts today.
        if (!Request.Query.ContainsKey("fromDate") && !Request.Query.ContainsKey("toDate")) fromDate = AppointmentService.Today;
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            ModelState.AddModelError("toDate", "Đến ngày phải từ ngày bắt đầu trở đi.");
        if (status is not (null or "" or "Pending" or "Attended" or "Cancelled"))
            ModelState.AddModelError("status", "Trạng thái không hợp lệ.");
        var model = new AppointmentListViewModel
        {
            FromDate = fromDate, ToDate = toDate, SearchTerm = searchTerm,
            Status = status, Sort = sort ?? "soonest", Page = page
        };
        return View(ModelState.IsValid ? await appointments.GetListAsync(model) : model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long saleId)
    {
        var model = await appointments.GetFormAsync(saleId);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AppointmentFormViewModel model)
    {
        var current = await appointments.GetFormAsync(model.SaleId);
        if (current == null) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                await appointments.SaveAsync(model, Actor);
                TempData["SuccessMessage"] = "Đã lưu lịch hẹn tái khám.";
                return RedirectToAction(nameof(Edit), new { saleId = model.SaleId });
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        }
        // Display patient/invoice information from the database, never the posted form.
        model.SaleCode = current.SaleCode;
        model.PatientName = current.PatientName;
        model.PatientCode = current.PatientCode;
        model.PhoneNumber = current.PhoneNumber;
        model.Exists = current.Exists;
        model.Status = current.Status;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(long id, string status, string version, string? returnUrl)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await appointments.ChangeStatusAsync(id, status, version, Actor);
                TempData["SuccessMessage"] = status == "Attended" ? "Đã ghi nhận bệnh nhân đến tái khám." : "Đã hủy lịch hẹn.";
            }
            catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        }
        else TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng tải lại trang.";
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    private string Actor
    {
        get
        {
            var actor = User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "Nhân viên";
            return actor[..Math.Min(256, actor.Length)];
        }
    }
}
