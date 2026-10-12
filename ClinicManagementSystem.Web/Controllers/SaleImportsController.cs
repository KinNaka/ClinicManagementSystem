using System.Security.Claims;
using System.Xml;
using ClinicManagementSystem.Web.Models.ViewModels.Sales;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Web.Controllers;

public sealed class SaleImportsController(SaleImportService imports) : Controller
{
    [HttpGet] public IActionResult Index() => View(new SaleImportViewModel());
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(64 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 64 * 1024 * 1024)]
    public async Task<IActionResult> Preview(List<IFormFile>? files, bool createMissingRecords = false, bool useWalkInCustomer = false)
    {
        var model = new SaleImportViewModel { CreateMissingRecords = createMissingRecords, UseWalkInCustomer = useWalkInCustomer };
        if (files == null || files.Count is 0 or > 4 || files.Any(f => f.Length == 0 || f.Length > 20 * 1024 * 1024 || !string.Equals(Path.GetExtension(f.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase)) || files.Sum(f => f.Length) > 60 * 1024 * 1024)
        { ModelState.AddModelError("", "Chọn 1–4 file .xlsx, tối đa 20 MB/file và 60 MB/đợt."); return View("Index", model); }
        var owner = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (owner == null) return Challenge();
        var streams = new List<(Stream Stream, string Name)>();
        try
        {
            foreach (var file in files)
            {
                var stream = new MemoryStream(); streams.Add((stream, file.FileName));
                await file.CopyToAsync(stream); stream.Position = 0;
            }
            return View("Index", await imports.PreviewAsync(streams, owner, createMissingRecords, useWalkInCustomer));
        }
        catch (InvalidDataException ex) { ModelState.AddModelError("", ex.Message); }
        catch (Exception ex) when (ex is XmlException or IOException or ArgumentException or FormatException or OverflowException)
        { ModelState.AddModelError("", "Không đọc được file. Vui lòng xuất lại danh sách chi tiết hóa đơn từ KiotViet."); }
        finally { foreach (var file in streams) await file.Stream.DisposeAsync(); }
        return View("Index", model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(string token)
    {
        var owner = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (owner == null) return Challenge();
        if (string.IsNullOrWhiteSpace(token) || token.Length != 32) return BadRequest();
        var result = await imports.ImportAsync(token, owner);
        if (!result.Success) { ModelState.AddModelError("", result.Error!); return View("Index", new SaleImportViewModel()); }
        TempData["SuccessMessage"] = $"Đã nhập {result.Imported:N0} hóa đơn lịch sử KiotViet; bỏ qua {result.Skipped:N0} mã đã tồn tại. Không trừ kho hoặc cộng lại tổng bán cũ.";
        return RedirectToAction("Index", "Sales");
    }
}
