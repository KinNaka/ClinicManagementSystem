
using ClinicManagementSystem.Web.Models.ViewModels.Products;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicManagementSystem.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IProductCategoryService _categoryService;

    public ProductsController(
        IProductService productService,
        IProductCategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(
     string? searchTerm,
     long? categoryId,
     string? statusFilter)
    {
        var products = await _productService.GetAllAsync(
            searchTerm, categoryId, statusFilter);

        var categories = await _categoryService.GetAllAsync();

        ViewBag.Categories = categories;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CategoryId = categoryId;
        ViewBag.StatusFilter = statusFilter;

        return View(products);
    }
    public async Task<IActionResult> Create()
    {
        var model = new ProductFormViewModel();
        await LoadCategoriesAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model);
            return View(model);
        }

        var result = await _productService.CreateAsync(model);

        if (!result)
        {
            ModelState.AddModelError(
                nameof(model.ProductCode),
                "Product code already exists or the selected category is invalid.");

            await LoadCategoriesAsync(model);
            return View(model);
        }

        TempData["Success"] = "Product created successfully.";
        return RedirectToAction(nameof(Index));
    }

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
    public async Task<IActionResult> Edit(
        long id,
        ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync(model);
            return View(model);
        }

        var result = await _productService.UpdateAsync(id, model);

        if (!result)
        {
            ModelState.AddModelError(
                nameof(model.ProductCode),
                "Product could not be updated. Check the product code and category.");

            await LoadCategoriesAsync(model);
            return View(model);
        }

        TempData["Success"] = "Product updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _productService.DeleteAsync(id);

        if (!result)
        {
            TempData["Error"] = "Product could not be deleted.";
        }
        else
        {
            TempData["Success"] = "Product deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync(ProductFormViewModel model)
    {
        var categories = await _categoryService.GetAllAsync();

        model.Categories = categories
            .Where(c => c.Status == "ACTIVE")
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Code} - {c.Name}"
            })
            .ToList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
