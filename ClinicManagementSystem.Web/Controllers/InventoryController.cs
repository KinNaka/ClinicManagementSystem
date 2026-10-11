using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Services;
using ClinicManagementSystem.Web.ViewModels.Inventory;

namespace ClinicManagementSystem.Web.Controllers
{
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly ApplicationDbContext _context;

        public InventoryController(
            IInventoryService inventoryService,
            ApplicationDbContext context)
        {
            _inventoryService = inventoryService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchTerm)
        {
            var items = await _inventoryService.GetStockAsync(searchTerm);

            ViewBag.SearchTerm = searchTerm;

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> StockIn()
        {
            var model = new StockInViewModel();
            await LoadProductsAsync(model.Products);

            model.Details.Add(new StockInDetailViewModel());

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockIn(StockInViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadProductsAsync(model.Products);
                return View(model);
            }

            try
            {
                var success = await _inventoryService.StockInAsync(model);

                if (!success)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Dữ liệu nhập kho không hợp lệ.");
                    await LoadProductsAsync(model.Products);
                    return View(model);
                }

                TempData["Success"] = "Nhập kho thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không thể nhập kho. Vui lòng kiểm tra dữ liệu và thử lại.");

                await LoadProductsAsync(model.Products);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> StockOut()
        {
            var model = new StockOutViewModel();
            await LoadProductsAsync(model.Products);

            model.Details.Add(new StockInDetailViewModel());

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockOut(StockOutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadProductsAsync(model.Products);
                return View(model);
            }

            try
            {
                var success = await _inventoryService.StockOutAsync(model);

                if (!success)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Không thể xuất kho. Có thể số lượng không hợp lệ hoặc tồn kho không đủ.");

                    await LoadProductsAsync(model.Products);
                    return View(model);
                }

                TempData["Success"] = "Xuất kho thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không thể xuất kho. Vui lòng thử lại.");

                await LoadProductsAsync(model.Products);
                return View(model);
            }
        }

        public async Task<IActionResult> Transactions()
        {
            var transactions =
                await _inventoryService.GetTransactionsAsync();

            return View(transactions);
        }

        private async Task LoadProductsAsync(
            List<SelectListItem> options)
        {
            options.Clear();

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.Status == "Active")
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.ProductCode,
                    p.Name,
                    p.CurrentStock,
                    p.Unit
                })
                .ToListAsync();

            foreach (var product in products)
            {
                options.Add(new SelectListItem
                {
                    Value = product.Id.ToString(),
                    Text = $"{product.ProductCode} - {product.Name} " +
                           $"(Tồn: {product.CurrentStock} {product.Unit})"
                });
            }
        }
    }
}