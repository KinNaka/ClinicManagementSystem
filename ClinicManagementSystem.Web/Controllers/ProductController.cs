using ClinicManagementSystem.Web.Data;
using Microsoft.EntityFrameworkCore;

using ClinicManagementSystem.Web.Models.ViewModels.Products;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagementSystem.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IProductService _productService;
    private readonly IProductCategoryService _categoryService;

    public ProductsController(
        IProductService productService,
        IProductCategoryService categoryService, ApplicationDbContext context)
    {
        _context = context;
        _productService = productService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(
     string? searchTerm,
     long? categoryId,
     string? statusFilter, int page = 1)
    {
            var paging = new ListPaging(page);
            ViewData["Paging"] = paging;
        var products = await _productService.GetAllAsync(
            searchTerm, categoryId, statusFilter, paging);

        var categories = await _categoryService.GetAllAsync();

        ViewBag.Categories = categories;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CategoryId = categoryId;
        ViewBag.StatusFilter = statusFilter;

        return View(products);
    }
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        var model = new ProductFormViewModel();
        await LoadCategoriesAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model);
            return View(model);
        }

        bool result;
        try { result = await _productService.CreateAsync(model); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); await LoadCategoriesAsync(model); return View(model); }

        if (!result)
        {
            ModelState.AddModelError(
                nameof(model.ProductCode),
                "Mã sản phẩm đã tồn tại hoặc danh mục đã chọn không hợp lệ.");

            await LoadCategoriesAsync(model);
            return View(model);
        }

        TempData["Success"] = "Đã thêm sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(long id)
    {
        var model = await _productService.GetByIdAsync(id);

        if (model == null)
            return NotFound();

        await LoadCategoriesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(
        long id,
        ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model);
            return View(model);
        }

        bool result;
        try { result = await _productService.UpdateAsync(id, model); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); await LoadCategoriesAsync(model); return View(model); }

        if (!result)
        {
            ModelState.AddModelError(
                nameof(model.ProductCode),
                "Không thể cập nhật sản phẩm. Vui lòng kiểm tra mã sản phẩm và danh mục.");

            await LoadCategoriesAsync(model);
            return View(model);
        }

        TempData["Success"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _productService.DeleteAsync(id);

        if (!result)
        {
            TempData["Error"] = "Không thể xóa sản phẩm.";
        }
        else
        {
            TempData["Success"] = "Đã xóa sản phẩm.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync(ProductFormViewModel model)
    {
        model.ComponentOptions = await _context.Products.AsNoTracking().Where(p => !p.IsCombo && !p.IsService && p.Id != model.Id && (p.Status == "Active" || p.Status == "ACTIVE" || model.Components.Select(c => c.ComponentProductId).Contains(p.Id))).OrderBy(p => p.Name).Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.ProductCode + " - " + p.Name + " (" + p.Unit + ")" }).ToListAsync();
        var categories = await _categoryService.GetAllAsync();

        model.Categories = categories
            .Where(c => c.Status == "ACTIVE" || c.Status == "Active")
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Code} - {c.Name}"
            })
            .ToList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleStatus(long id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
        {
            TempData["Error"] = "Không tìm thấy sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        product.Status = product.Status == "ACTIVE"
            ? "INACTIVE"
            : "ACTIVE";

        var result = await _productService.UpdateAsync(id, product);

        if (!result)
        {
            TempData["Error"] = "Không thể thay đổi trạng thái sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = product.Status == "ACTIVE"
            ? "Đã kích hoạt sản phẩm."
            : "Đã ngừng kinh doanh sản phẩm.";

        return RedirectToAction(nameof(Index));
    }

}
