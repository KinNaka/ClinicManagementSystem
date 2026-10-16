using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Services;
using ClinicManagementSystem.Web.ViewModels.Inventory;
using ClinicManagementSystem.Web.Models.ViewModels.Inventory;
using Microsoft.AspNetCore.Authorization;

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

        public async Task<IActionResult> Index(string? searchTerm, bool lowStockOnly = false, int page = 1)
        {
            var paging = new ListPaging(page);
            ViewData["Paging"] = paging;
            var items = await _inventoryService.GetStockAsync(searchTerm, lowStockOnly, paging);
            ViewBag.LowStockOnly = lowStockOnly;

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

        public async Task<IActionResult> Transactions(int page = 1)
        {
            var paging = new ListPaging(page);
            ViewData["Paging"] = paging;
            var transactions =
                await _inventoryService.GetTransactionsAsync(paging);

            return View(transactions);
        }

        [HttpGet, Authorize(Roles = "Admin")]
        public async Task<IActionResult> StockCount(int page = 1, string? searchTerm = null) { var paging = new ListPaging(page); ViewData["Paging"] = paging; ViewData["SearchTerm"] = searchTerm; return View(await _inventoryService.GetStockCountAsync(paging, searchTerm)); }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin"), RequestFormLimits(ValueCountLimit = 30000)]
        public async Task<IActionResult> StockCount(StockAdjustmentViewModel model, int page = 1, string? searchTerm = null)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var count = await _inventoryService.AdjustStockAsync(model, User.Identity?.Name);
                    TempData["Success"] = count == 0 ? "Tồn thực tế khớp hệ thống, không cần điều chỉnh." : $"Đã cân tồn kho cho {count} sản phẩm và lưu phiếu điều chỉnh.";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
            }
            var paging = new ListPaging(page); ViewData["Paging"] = paging; ViewData["SearchTerm"] = searchTerm; var current = await _inventoryService.GetStockCountAsync(paging, searchTerm);
            var entered = model.Details.GroupBy(d => d.ProductId).ToDictionary(g => g.Key, g => g.First());
            current.Reason = model.Reason;
            foreach (var row in current.Details)
                if (entered.TryGetValue(row.ProductId, out var original)) { row.CountedStock = original.CountedStock; row.Note = original.Note; }
            // Render current stock and retained counts together for review after a conflict.
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(e => e.Length > 0).ToList();
            ModelState.Clear(); foreach (var error in errors) ModelState.AddModelError("", error);
            return View(current);
        }

        private async Task LoadProductsAsync(
            List<SelectListItem> options)
        {
            options.Clear();

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => !p.IsCombo && !p.IsService && (p.Status == "Active" || p.Status == "ACTIVE"))
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
