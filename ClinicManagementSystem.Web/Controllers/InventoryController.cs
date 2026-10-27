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

        public async Task<IActionResult> Index(string? searchTerm, bool lowStockOnly = false, int page = 1, string? sort = null)
        {
            var paging = new ListPaging(page);
            ViewData["Paging"] = paging;
            var items = await _inventoryService.GetStockAsync(searchTerm, lowStockOnly, paging, sort);
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
                return RedirectToAction(nameof(Receipts));
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
                model.Patients = await _context.Patients.AsNoTracking().OrderBy(p => p.FullName).Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.PatientCode + " - " + p.FullName }).ToListAsync();

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
                model.Patients = await _context.Patients.AsNoTracking().OrderBy(p => p.FullName).Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.PatientCode + " - " + p.FullName }).ToListAsync();
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
                model.Patients = await _context.Patients.AsNoTracking().OrderBy(p => p.FullName).Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.PatientCode + " - " + p.FullName }).ToListAsync();
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
                model.Patients = await _context.Patients.AsNoTracking().OrderBy(p => p.FullName).Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.PatientCode + " - " + p.FullName }).ToListAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Receipts(int page = 1, string? sort = null, string? period = null, string? searchTerm = null, string? payment = null)
        {
            var query = _context.InventoryTransactions.AsNoTracking().Where(t => t.TransactionType == "StockIn");
            var (start, end) = DatePeriod.Bounds(period);
            if (start.HasValue) query = query.Where(t => t.TransactionDate >= start.Value && t.TransactionDate < end!.Value);
            if (payment == "unpaid") query = query.Where(t => t.PaidAt == null);
            if (payment == "paid") query = query.Where(t => t.PaidAt != null);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(t => t.TransactionCode.Contains(term) ||
                    (t.ReferenceNumber != null && t.ReferenceNumber.Contains(term)) ||
                    t.Details.Any(d => d.Product.Name.Contains(term) || d.Product.ProductCode.Contains(term)));
            }
            var paging = new ListPaging(page);
            ViewData["Paging"] = paging;
            query = await paging.ApplyAsync(ListSorting.Apply(query, sort, t => t.TransactionDate, t => t.TransactionCode, t => t.Id));
            return View(await query.Include(t => t.Details).ThenInclude(d => d.Product).ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReceipt(long id)
        {
            // Delete the paid document only. Goods already received remain in stock.
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var receipt = await _context.InventoryTransactions.AsNoTracking()
                .Where(t => t.Id == id && t.TransactionType == "StockIn")
                .Select(t => new { t.PaidAt }).SingleOrDefaultAsync();
            if (receipt == null) return NotFound();
            if (!receipt.PaidAt.HasValue)
            {
                TempData["Error"] = "Chỉ được xóa phiếu nhập đã thanh toán.";
                return RedirectToAction(nameof(Receipts));
            }
            await _context.InventoryTransactionDetails.Where(d => d.InventoryTransactionId == id).ExecuteDeleteAsync();
            await _context.InventoryTransactions.Where(t => t.Id == id && t.TransactionType == "StockIn" && t.PaidAt != null).ExecuteDeleteAsync();
            await transaction.CommitAsync();
            TempData["Success"] = "Đã xóa phiếu nhập đã thanh toán và chi tiết phiếu. Tồn kho không thay đổi.";
            return RedirectToAction(nameof(Receipts));
        }
        public async Task<IActionResult> Transactions(int page = 1, string? sort = null, string? period = null, string? searchTerm = null, string? type = null, string? payment = null)
        {
            var query = _context.InventoryTransactions.AsNoTracking().AsQueryable();
            var (start, end) = DatePeriod.Bounds(period);
            if (start.HasValue) query = query.Where(t => t.TransactionDate >= start.Value && t.TransactionDate < end!.Value);
            if (type is "StockIn" or "StockOut" or "Adjustment") query = query.Where(t => t.TransactionType == type);
            if (payment == "unpaid") query = query.Where(t => t.TransactionType == "StockIn" && t.PaidAt == null);
            if (payment == "paid") query = query.Where(t => t.TransactionType == "StockIn" && t.PaidAt != null);
            if (!string.IsNullOrWhiteSpace(searchTerm)) {
                var term = searchTerm.Trim();
                query = query.Where(t => t.TransactionCode.Contains(term) || (t.ReferenceNumber != null && t.ReferenceNumber.Contains(term)) ||
                    (t.Patient != null && (t.Patient.FullName.Contains(term) || t.Patient.PatientCode.Contains(term))) ||
                    t.Details.Any(d => d.Product.Name.Contains(term) || d.Product.ProductCode.Contains(term)) ||
                    _context.Sales.Any(s => s.SaleCode == t.ReferenceNumber && (s.Patient.FullName.Contains(term) || s.Patient.PatientCode.Contains(term))));
            }
            var paging = new ListPaging(page);
            ViewData["Paging"] = paging;
            query = await paging.ApplyAsync(ListSorting.Apply(query, sort, t => t.TransactionDate, t => t.TransactionCode, t => t.Id));
            var transactions = await query.Include(t => t.Patient).Include(t => t.Details).ThenInclude(d => d.Product).ToListAsync();
            var references = transactions.Where(t => t.ReferenceNumber != null).Select(t => t.ReferenceNumber!).ToList();
            ViewBag.LinkedSales = await _context.Sales.AsNoTracking().Include(s => s.Patient).Where(s => references.Contains(s.SaleCode)).ToDictionaryAsync(s => s.SaleCode);
            return View(transactions);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkPaid(long id)
        {
            var actor = User.Identity?.Name;
            if (actor?.Length > 100) actor = actor[..100];
            var affected = await _context.InventoryTransactions.Where(t => t.Id == id && t.TransactionType == "StockIn" && t.PaidAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.PaidAt, DateTime.Now).SetProperty(t => t.PaidBy, actor));
            TempData[affected == 1 ? "Success" : "Error"] = affected == 1 ? "Đã ghi nhận thanh toán phiếu nhập." : "Phiếu không tồn tại hoặc đã được thanh toán.";
            return RedirectToAction(nameof(Receipts), new { payment = "unpaid" });
        }
        [HttpGet, Authorize(Roles = "Admin")]
        public async Task<IActionResult> StockCount(int page = 1, string? searchTerm = null, string? sort = null, bool archived = false)
        {
            var paging = new ListPaging(page);
            ViewData["Paging"] = paging;
            ViewData["SearchTerm"] = searchTerm; ViewData["Archived"] = archived;
            return View(await _inventoryService.GetStockCountAsync(paging, searchTerm, sort, archived));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin"), RequestFormLimits(ValueCountLimit = 30000)]
        public async Task<IActionResult> StockCount(StockAdjustmentViewModel model, int page = 1, string? searchTerm = null, string? sort = null, bool archived = false)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var count = await _inventoryService.AdjustStockAsync(model, User.Identity?.Name);
                    TempData["Success"] = count == 0 ? "Tồn thực tế khớp hệ thống, không cần điều chỉnh." : $"Đã cân tồn kho cho {count} sản phẩm và lưu phiếu điều chỉnh.";
                    return RedirectToAction(nameof(StockCount), new { archived, page, searchTerm, sort });
                }
                catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
            }
            var paging = new ListPaging(page); ViewData["Paging"] = paging; ViewData["SearchTerm"] = searchTerm; ViewData["Archived"] = archived; var current = await _inventoryService.GetStockCountAsync(paging, searchTerm, sort, archived);
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
