
using ClinicManagementSystem.Web.Models.ViewModels.Sales;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagementSystem.Web.Controllers
{
    public class SalesController : Controller
    {
        private readonly ISaleService _saleService;

        public SalesController(ISaleService saleService)
        {
            _saleService = saleService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? statusFilter, int page = 1, string? sort = null)
        {
            var model = await _saleService.GetAllAsync(
                searchTerm, statusFilter, page, sort);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(long? copyFromId)
        {
            if (copyFromId.HasValue)
            {
                var copy = await _saleService.PrepareCopyAsync(copyFromId.Value);
                return copy == null ? NotFound() : View(copy);
            }
            var model = await _saleService.PrepareFormAsync(
                new SaleFormViewModel
                {
                    Status = "Draft",
                    Details = new List<SaleDetailFormViewModel>
                    {
                        new()
                    }
                });

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            SaleFormViewModel model)
        {
            ValidateSale(model);

            if (!ModelState.IsValid)
            {
                await _saleService.PrepareFormAsync(model);
                return View(model);
            }

            try
            {
                var id = await _saleService.CreateAsync(model);

                TempData["SuccessMessage"] =
                    "Đã tạo hóa đơn nháp.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    string.Empty, ex.Message);

                await _saleService.PrepareFormAsync(model);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var model = await _saleService.GetDraftAsync(id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            long id,
            SaleFormViewModel model)
        {
            ValidateSale(model);

            if (!ModelState.IsValid)
            {
                await _saleService.PrepareFormAsync(model);
                return View(model);
            }

            try
            {
                await _saleService.UpdateDraftAsync(id, model);

                TempData["SuccessMessage"] =
                    "Đã cập nhật hóa đơn nháp.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    string.Empty, ex.Message);

                await _saleService.PrepareFormAsync(model);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var model = await _saleService.GetByIdAsync(id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Print(long id)
        {
            var model = await _saleService.GetByIdAsync(id);
            return model == null ? NotFound() : View(model);
        }

        [HttpGet]
        public async Task<IActionResult> PrintPrescription(long id)
        {
            var model = await _saleService.GetByIdAsync(id);
            return model == null ? NotFound() : View(model);
        }

        [HttpGet]
        public async Task<IActionResult> PrintSelectedProducts(long id)
        {
            var model = await _saleService.GetByIdAsync(id);
            return model == null ? NotFound() : View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(long id)
        {
            try
            {
                await _saleService.ConfirmAsync(id);

                TempData["SuccessMessage"] =
                    "Đã xác nhận hóa đơn và xuất kho.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(
                nameof(Details), new { id });
        }


        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var deleted = await _saleService.DeleteAsync(id, User.Identity?.Name);
                TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                    ? "Đã xóa hóa đơn. Hóa đơn đã xác nhận được loại khỏi doanh thu và hoàn kho theo phiếu xuất gốc."
                    : "Hóa đơn không còn tồn tại.";
            }
            catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction(nameof(Index));
        }

        private void ValidateSale(SaleFormViewModel model)
        {
            if (model.PatientId <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.PatientId),
                    "Vui lòng chọn bệnh nhân.");
            }

            if (model.Details == null ||
                model.Details.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Hóa đơn phải có ít nhất một sản phẩm.");
                return;
            }

            if (model.Details.Any(d =>
                d.ProductId <= 0 || d.Quantity <= 0 || d.Quantity > 999999999 ||
                d.Quantity != decimal.Truncate(d.Quantity)))
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Vui lòng chọn sản phẩm và nhập số lượng nguyên từ 1 đến 999999999.");
            }

            if (model.Details
                .GroupBy(d => d.ProductId)
                .Any(g => g.Count() > 1 && (g.Any(d => d.SourceDetailId <= 0) || g.Select(d => d.SourceDetailId).Distinct().Count() != g.Count())))
            {
                ModelState.AddModelError(
                    nameof(model.Details),
                    "Không được thêm trùng sản phẩm.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductPrice(long productId)
        {
            var price = await _saleService
                .GetProductPriceAsync(productId);

            if (price == null)
                return NotFound();

            return Json(new
            {
                sellingPrice = price.Value
            });
        }
    }
}
