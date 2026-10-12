using Microsoft.AspNetCore.Mvc;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using ClinicManagementSystem.Web.Models.ViewModels.ProductCategories;
namespace ClinicManagementSystem.Web.Controllers
{
    public class ProductCategoriesController : Controller
    {
        private readonly IProductCategoryService _service;

        public ProductCategoriesController(IProductCategoryService service)
        {
            _service = service;
        }

        //GET : /ProductCategories
        public async Task<IActionResult> Index()
        {
            var categories = await _service.GetAllAsync();
            return View(categories);
        }

        //GET : /ProductCategories/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new ProductCategoryFormViewModel());

        }

        //POST : /ProductCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ProductCategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _service.CreateAsync(model);

            if (!result)
            {
                ModelState.AddModelError(
                    "Code",
                    "Mã danh mục đã tồn tại."
                );

                return View(model);
            }

            TempData["Success"] = "Đã thêm danh mục sản phẩm.";

            return RedirectToAction(nameof(Index));
        }

        //GET : /ProductCategories/Edit/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(long id)
        {
            var model  = await _service.GetByIdAsync(id);
            if (model == null)
            {
                return NotFound();
            }
            return View(model);
        }
        //POST : /ProductCategories/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(
        long id,
        ProductCategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _service.UpdateAsync(id, model);

            if (!result)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
        //POST : /ProductCategories/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result.Success)
            {
                if (result.Error == "Không tìm thấy danh mục sản phẩm.")
                {
                    return NotFound();
                }

                TempData["Error"] = result.Error;

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã xóa danh mục sản phẩm.";

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var result = await _service.ToggleStatusAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
