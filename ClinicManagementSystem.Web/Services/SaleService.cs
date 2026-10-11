
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Entities;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Sales;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Web.Services
{
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _context;

        public SaleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SaleIndexViewModel> GetAllAsync(
            string? searchTerm, string? statusFilter)
        {
            var query = _context.Sales
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();

                query = query.Where(s =>
                    s.SaleCode.Contains(keyword) ||
                    s.Patient.PatientCode.Contains(keyword) ||
                    s.Patient.FullName.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(s =>
                    s.Status == statusFilter);
            }

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new SaleListViewModel
                {
                    Id = s.Id,
                    SaleCode = s.SaleCode,
                    PatientId = s.PatientId,
                    PatientCode = s.Patient.PatientCode,
                    PatientName = s.Patient.FullName,
                    SaleDate = s.SaleDate,
                    TotalAmount = s.TotalAmount,
                    Status = s.Status
                })
                .ToListAsync();

            return new SaleIndexViewModel
            {
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                Sales = sales
            };
        }

        public async Task<SaleDetailsViewModel?> GetByIdAsync(
            long id)
        {
            return await _context.Sales
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new SaleDetailsViewModel
                {
                    Id = s.Id,
                    SaleCode = s.SaleCode,
                    PatientCode = s.Patient.PatientCode,
                    PatientName = s.Patient.FullName,
                    SaleDate = s.SaleDate,
                    Status = s.Status,
                    Notes = s.Notes,
                    TotalAmount = s.TotalAmount,
                    Details = s.Details.Select(d =>
                        new SaleDetailDisplayViewModel
                        {
                            ProductCode = d.Product.ProductCode,
                            Notes = d.Notes,
                            ProductName = d.Product.Name,
                            Unit = d.Product.Unit,
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice,
                            LineTotal = d.LineTotal
                        }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<SaleFormViewModel> PrepareFormAsync(
            SaleFormViewModel model)
        {
            model.Patients = await _context.Patients
                .AsNoTracking()
                .Where(p => p.Status == "Active")
                .OrderBy(p => p.FullName)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.PatientCode + " - " + p.FullName
                })
                .ToListAsync();

            model.Products = await _context.Products
                .AsNoTracking()
                .Where(p => p.Status == "Active")
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.ProductCode + " - " + p.Name
                })
                .ToListAsync();

            return model;
        }

        private async Task<List<SaleDetail>> BuildDetailsAsync(
            SaleFormViewModel model)
        {
            if (model.Details == null ||
                model.Details.Count == 0)
                throw new InvalidOperationException(
                    "Hóa đơn phải có ít nhất một sản phẩm.");

            if (model.Details.Any(d =>
                d.ProductId <= 0 || d.Quantity < 1 || d.Quantity > 999999999 ||
                d.Quantity != decimal.Truncate(d.Quantity)))
                throw new InvalidOperationException(
                    "Sản phẩm không hợp lệ hoặc số lượng không phải số nguyên từ 1 đến 999999999.");

            var productIds = model.Details
                .Select(d => d.ProductId)
                .Distinct()
                .ToList();

            if (productIds.Count != model.Details.Count)
                throw new InvalidOperationException(
                    "Không được thêm trùng sản phẩm.");

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id) &&
                            p.Status == "Active")
                .ToDictionaryAsync(p => p.Id);

            if (products.Count != productIds.Count)
                throw new InvalidOperationException(
                    "Có sản phẩm không tồn tại hoặc ngừng hoạt động.");

            var details = new List<SaleDetail>();

            foreach (var item in model.Details)
            {
                var product = products[item.ProductId];

                if (product.SellingPrice < 0)
                    throw new InvalidOperationException(
                        "Giá bán sản phẩm không hợp lệ.");

                decimal lineTotal;

                try
                {
                    lineTotal = item.Quantity *
                                product.SellingPrice;
                }
                catch (OverflowException)
                {
                    throw new InvalidOperationException(
                        "Giá trị hóa đơn vượt giới hạn.");
                }

                details.Add(new SaleDetail
                {
                    ProductId = product.Id,
                    Notes = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes.Trim(),
                    Quantity = item.Quantity,
                    UnitPrice = product.SellingPrice,
                    LineTotal = lineTotal
                });
            }

            return details;
        }

        private async Task ValidatePatientAsync(long patientId)
        {
            var exists = await _context.Patients
                .AnyAsync(p => p.Id == patientId &&
                               p.Status == "Active");

            if (!exists)
                throw new InvalidOperationException(
                    "Vui lòng chọn bệnh nhân đang hoạt động.");
        }

        public async Task<long> CreateAsync(
            SaleFormViewModel model)
        {
            await ValidatePatientAsync(model.PatientId);

            var details = await BuildDetailsAsync(model);

            var sale = new Sale
            {
                SaleCode = "SALE-" +
                    Guid.NewGuid().ToString("N").ToUpperInvariant(),
                PatientId = model.PatientId,
                SaleDate = DateTime.Now,
                Status = "Draft",
                Notes = model.Notes,
                TotalAmount = details.Sum(d => d.LineTotal),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Details = details
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            return sale.Id;
        }

        public async Task<SaleFormViewModel?> GetDraftAsync(
            long id)
        {
            var sale = await _context.Sales
                .AsNoTracking()
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(s =>
                    s.Id == id && s.Status == "Draft");

            if (sale == null)
                return null;

            return await PrepareFormAsync(
                new SaleFormViewModel
                {
                    Id = sale.Id,
                    SaleCode = sale.SaleCode,
                    PatientId = sale.PatientId,
                    Notes = sale.Notes,
                    Status = sale.Status,
                    TotalAmount = sale.TotalAmount,
                    Details = sale.Details.Select(d =>
                        new SaleDetailFormViewModel
                        {
                            ProductId = d.ProductId,
                            Notes = d.Notes,
                            ProductName = d.Product.Name,
                            Unit = d.Product.Unit,
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice,
                            LineTotal = d.LineTotal
                        }).ToList()
                });
        }

        public async Task UpdateDraftAsync(
            long id, SaleFormViewModel model)
        {
            await ValidatePatientAsync(model.PatientId);
            var details = await BuildDetailsAsync(model);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var sale = await _context.Sales
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null || sale.Status != "Draft")
                throw new InvalidOperationException(
                    "Chỉ được sửa hóa đơn nháp.");

            // Chặn việc xác nhận đồng thời trong lúc sửa.
            var updated = await _context.Sales
                .Where(s => s.Id == id && s.Status == "Draft")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.UpdatedAt, DateTime.Now));

            if (updated != 1)
                throw new InvalidOperationException(
                    "Hóa đơn đã thay đổi trạng thái.");

            _context.SaleDetails.RemoveRange(sale.Details);

            sale.PatientId = model.PatientId;
            sale.Notes = model.Notes;
            sale.TotalAmount = details.Sum(d => d.LineTotal);
            sale.UpdatedAt = DateTime.Now;
            sale.Details = details;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task ConfirmAsync(long id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var sale = await _context.Sales
                .AsNoTracking()
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null || sale.Status != "Draft")
                throw new InvalidOperationException(
                    "Hóa đơn không tồn tại hoặc đã xác nhận.");

            if (sale.Details.Count == 0)
                throw new InvalidOperationException(
                    "Hóa đơn chưa có sản phẩm.");

            // Đổi trạng thái có điều kiện, tránh xác nhận lặp.
            var now = DateTime.Now;

            var changed = await _context.Sales
                .Where(s => s.Id == id && s.Status == "Draft")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.Status, "Confirmed")
                    .SetProperty(s => s.UpdatedAt, now));

            if (changed != 1)
                throw new InvalidOperationException(
                    "Hóa đơn đã được xử lý.");

            var inventoryTransaction = new InventoryTransaction
            {
                TransactionCode = "OUT-" +
                    Guid.NewGuid().ToString("N").ToUpperInvariant(),
                TransactionType = "StockOut",
                TransactionDate = now,
                Reason = "Xuất kho bán hàng",
                ReferenceNumber = sale.SaleCode,
                CreatedAt = now
            };

            foreach (var detail in sale.Details)
            {
                if (detail.Quantity <= 0)
                    throw new InvalidOperationException(
                        "Số lượng hóa đơn không hợp lệ.");

                // Trừ kho có điều kiện ngay trong SQL Server.
                var affected = await _context.Products
                    .Where(p =>
                        p.Id == detail.ProductId &&
                        p.CurrentStock >= detail.Quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.CurrentStock,
                            p => p.CurrentStock - detail.Quantity)
                        .SetProperty(p => p.UpdatedAt, now));

                if (affected != 1)
                    throw new InvalidOperationException(
                        "Không đủ tồn kho hoặc sản phẩm không tồn tại.");

                var costPrice = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.Id == detail.ProductId)
                    .Select(p => p.CostPrice)
                    .SingleAsync();

                inventoryTransaction.Details.Add(
                    new InventoryTransactionDetail
                    {
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity,
                        UnitPrice = costPrice,
                        Note = "Bán hàng: " + sale.SaleCode
                    });
            }

            _context.InventoryTransactions.Add(
                inventoryTransaction);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task<decimal?> GetProductPriceAsync(long productId)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.Id == productId && p.Status == "Active")
                .Select(p => (decimal?)p.SellingPrice)
                .FirstOrDefaultAsync();
        }

    }
}
