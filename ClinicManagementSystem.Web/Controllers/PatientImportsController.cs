using System.IO.Compression;
using System.Security.Claims;
using System.Xml;
using ClinicManagementSystem.Web.Models.ViewModels.Patients;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Web.Controllers;

public sealed class PatientImportsController(PatientImportService imports) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new PatientImportViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Preview(IFormFile? file)
    {
        if (file == null || file.Length == 0 || file.Length > 10 * 1024 * 1024 ||
            !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Vui lòng chọn file KiotViet .xlsx, dung lượng tối đa 10 MB.");
            return View("Index", new PatientImportViewModel());
        }
        var owner = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (owner == null) return Challenge();
        try
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;
            var model = await imports.PreviewAsync(stream, file.FileName, owner);
            return View("Index", model);
        }
        catch (InvalidDataException ex) { ModelState.AddModelError("", ex.Message); }
        catch (Exception ex) when (ex is XmlException or ArgumentException or IOException or FormatException or OverflowException)
        { ModelState.AddModelError("", "Không đọc được file Excel. Vui lòng xuất lại danh sách khách hàng từ KiotViet."); }
        return View("Index", new PatientImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(string token)
    {
        var owner = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (owner == null) return Challenge();
        if (string.IsNullOrWhiteSpace(token) || token.Length != 32) return BadRequest();
        var result = await imports.ImportAsync(token, owner);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error!);
            return View("Index", new PatientImportViewModel());
        }
        TempData["SuccessMessage"] = $"Đã nhập {result.Imported:N0} khách hàng, giữ nguyên mã KH và tổng bán cũ. Bỏ qua {result.Skipped:N0} mã đã tồn tại.";
        return RedirectToAction("Index", "Patients");
    }
}
