using Microsoft.AspNetCore.Mvc;
using ClinicManagementSystem.Web.Services;
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
        public IActionResult Create()
        {
            return View(new ProductCategoryFormViewModel());

        }

        //POST : /ProductCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                    "Category code already exists."
                );

                return View(model);
            }

            TempData["Success"] = "Category created successfully.";

            return RedirectToAction(nameof(Index));
        }

        //GET : /ProductCategories/Edit/{id}
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
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result.Success)
            {
                if (result.Error == "Category not found.")
                {
                    return NotFound();
                }

                TempData["Error"] = result.Error;

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Category deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
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
