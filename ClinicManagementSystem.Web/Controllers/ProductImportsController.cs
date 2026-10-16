using System.IO.Compression;
using System.Security.Claims;
using System.Xml;
using ClinicManagementSystem.Web.Models.ViewModels.Products;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagementSystem.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ProductImportsController(ProductImportService imports) : Controller
{
    [HttpGet]
    public IActionResult Index(string? token = null)
    {
        if (string.IsNullOrEmpty(token)) return View(new ProductImportViewModel());
        var owner = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (owner == null) return Challenge();
        var model = imports.GetPreview(token, owner);
        if (model == null)
        {
            ModelState.AddModelError("", "Bản xem trước đã hết hạn. Vui lòng tải lại file.");
            return View(new ProductImportViewModel());
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Preview(IFormFile? file, bool importStock = false, bool resetNegativeStock = false)
    {
        if (file == null || file.Length == 0 || file.Length > 10 * 1024 * 1024 ||
            !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Vui lòng chọn file KiotViet .xlsx, dung lượng tối đa 10 MB.");
            return View("Index", new ProductImportViewModel());
        }
        var owner = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (owner == null) return Challenge();
        try
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;
            var model = await imports.PreviewAsync(stream, file.FileName, owner, importStock, resetNegativeStock);
            return View("Index", model);
        }
        catch (InvalidDataException ex) { ModelState.AddModelError("", ex.Message); }
        catch (Exception ex) when (ex is XmlException or ArgumentException or IOException or FormatException or OverflowException)
        { ModelState.AddModelError("", "Không đọc được file Excel. Vui lòng xuất lại danh sách hàng hóa từ KiotViet."); }
        return View("Index", new ProductImportViewModel());
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
            return View("Index", new ProductImportViewModel());
        }
        TempData["Success"] = $"Đã nhập {result.Imported:N0} hàng hóa, giữ nguyên mã hàng. Bỏ qua {result.Skipped:N0} mã hàng đã tồn tại.";
        return RedirectToAction("Index", "Products");
    }
}
