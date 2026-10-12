
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
            string? searchTerm, string? statusFilter, int page = 1)
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
                    s.Patient.FullName.Contains(keyword) || (s.SourcePatientName != null && s.SourcePatientName.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(s =>
                    s.Status == statusFilter);
            }

            var totalCount = await query.CountAsync();
            page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(totalCount / (double)SaleIndexViewModel.PageSize)));
            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .Skip((page - 1) * SaleIndexViewModel.PageSize)
                .Take(SaleIndexViewModel.PageSize)
                .Select(s => new SaleListViewModel
                {
                    Id = s.Id,
                    SaleCode = s.SaleCode,
                    PatientId = s.PatientId,
                    PatientCode = s.SourcePatientCode ?? s.Patient.PatientCode,
                    PatientName = s.SourcePatientName ?? s.Patient.FullName,
                    SaleDate = s.SaleDate,
                    TotalAmount = s.TotalAmount,
                    Status = s.Status
                })
                .ToListAsync();

            return new SaleIndexViewModel
            {
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                Page = page, TotalCount = totalCount,
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
                    PatientId = s.PatientId,
                    PatientPhone = s.Patient.PhoneNumber,
                    SaleCode = s.SaleCode,
                    PatientCode = s.SourcePatientCode ?? s.Patient.PatientCode,
                    PatientName = s.SourcePatientName ?? s.Patient.FullName,
                    SaleDate = s.SaleDate,
                    Status = s.Status,
                    Notes = s.Notes,
                    TotalAmount = s.TotalAmount,
                    ImportedGoodsTotal = s.ImportedGoodsTotal, ImportedDiscount = s.ImportedDiscount, ImportedSurcharge = s.ImportedSurcharge, ImportedFileName = s.ImportedFileName,
                    Details = s.Details.Select(d =>
                        new SaleDetailDisplayViewModel
                        {
                            ProductCode = d.SourceProductCode ?? d.Product.ProductCode,
                            Notes = d.Notes,
                            ProductName = d.SourceProductName ?? d.Product.Name,
                            Unit = d.SourceUnit ?? d.Product.Unit,
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice,
                            LineTotal = d.LineTotal
                        }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<SaleFormViewModel?> PrepareCopyAsync(long id)
        {
            var source = await _context.Sales.AsNoTracking().Include(s => s.Details).FirstOrDefaultAsync(s => s.Id == id);
            if (source == null) return null;
            return await PrepareFormAsync(new SaleFormViewModel {
                CopyFromId = source.Id, PatientId = source.PatientId, Notes = source.Notes,
                TotalAmount = source.TotalAmount,
                Details = source.Details.OrderBy(d => d.Id).Select(d => new SaleDetailFormViewModel {
                    SourceDetailId = d.Id, ProductId = d.ProductId, Quantity = d.Quantity, Notes = d.Notes
                }).ToList()
            });
        }

        public async Task<SaleFormViewModel> PrepareFormAsync(
            SaleFormViewModel model)
        {
            var sourceId = model.Id > 0 ? model.Id : model.CopyFromId;
            var source = sourceId.HasValue ? await _context.Sales.AsNoTracking().Include(s => s.Details).ThenInclude(d => d.Product).FirstOrDefaultAsync(s => s.Id == sourceId) : null;
            model.CopyFromCode = model.CopyFromId.HasValue ? source?.SaleCode : null;
            model.Discount = source?.ImportedDiscount ?? 0;
            model.Surcharge = source?.ImportedSurcharge ?? 0;
            foreach (var item in model.Details)
            {
                var original = source?.Details.FirstOrDefault(d => d.Id == item.SourceDetailId && d.ProductId == item.ProductId);
                if (original != null)
                {
                    item.UnitPrice = original.UnitPrice;
                    item.PreservedQuantity = original.Quantity;
                    item.PreservedLineTotal = original.LineTotal;
                    item.ProductName = original.SourceProductName ?? original.Product.Name;
                    item.Unit = original.SourceUnit ?? original.Product.Unit;
                    item.LineTotal = item.Quantity == original.Quantity ? original.LineTotal : decimal.Round(item.Quantity * original.UnitPrice, 2, MidpointRounding.AwayFromZero);
                }
                else { item.SourceDetailId = 0; item.PreservedQuantity = 0; item.PreservedLineTotal = 0; }
            }
            model.Patients = await _context.Patients
                .AsNoTracking()
                .Where(p => p.Id == model.PatientId || (p.Status == "Active" || p.Status == "ACTIVE"))
                .OrderBy(p => p.FullName)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.PatientCode + " - " + p.FullName
                })
                .ToListAsync();

            if (source?.PatientId == model.PatientId && !string.IsNullOrWhiteSpace(source.SourcePatientName))
            {
                var selectedPatient = model.Patients.FirstOrDefault(p => p.Value == model.PatientId.ToString());
                if (selectedPatient != null) selectedPatient.Text = (source.SourcePatientCode ?? "") + " - " + source.SourcePatientName;
            }
            var selectedIds = model.Details.Select(d => d.ProductId).ToList();
            model.Products = await _context.Products
                .AsNoTracking()
                .Where(p => selectedIds.Contains(p.Id) || (p.Status == "Active" || p.Status == "ACTIVE"))
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
            SaleFormViewModel model, Sale? source = null)
        {
            if (model.Details == null ||
                model.Details.Count == 0)
                throw new InvalidOperationException(
                    "Hóa đơn phải có ít nhất một sản phẩm.");

            if (model.Details.Any(d =>
                d.ProductId <= 0 || d.Quantity <= 0 || d.Quantity > 999999999 ||
                d.Quantity != decimal.Round(d.Quantity, d.SourceDetailId > 0 ? 3 : 0)))
                throw new InvalidOperationException(
                    "Sản phẩm không hợp lệ hoặc số lượng không phải số nguyên từ 1 đến 999999999.");

            var productIds = model.Details
                .Select(d => d.ProductId)
                .Distinct()
                .ToList();

            if (model.Details.Where(d => d.SourceDetailId > 0).GroupBy(d => d.SourceDetailId).Any(g => g.Count() > 1) ||
                model.Details.GroupBy(d => d.ProductId).Any(g => g.Count() > 1 && g.Any(d => d.SourceDetailId <= 0)))
                throw new InvalidOperationException(
                    "Không được thêm trùng sản phẩm.");

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            if (products.Count != productIds.Count)
                throw new InvalidOperationException(
                    "Có sản phẩm không tồn tại hoặc ngừng hoạt động.");

            var details = new List<SaleDetail>();

            foreach (var item in model.Details)
            {
                var product = products[item.ProductId];
                var original = source?.Details.FirstOrDefault(d => d.Id == item.SourceDetailId && d.ProductId == item.ProductId);
                if (original == null && product.Status is not ("Active" or "ACTIVE"))
                    throw new InvalidOperationException("Sản phẩm chọn mới đã ngừng hoạt động.");
                if (item.SourceDetailId > 0 && original == null)
                    throw new InvalidOperationException("Dòng sản phẩm sao chép không khớp hóa đơn gốc. Vui lòng tải lại trang.");
                var price = original?.UnitPrice ?? product.SellingPrice;

                if (product.SellingPrice < 0)
                    throw new InvalidOperationException(
                        "Giá bán sản phẩm không hợp lệ.");

                decimal lineTotal;

                try
                {
                    lineTotal = original != null && item.Quantity == original.Quantity ? original.LineTotal :
                        decimal.Round(item.Quantity * price, 2, MidpointRounding.AwayFromZero);
                }
                catch (OverflowException)
                {
                    throw new InvalidOperationException(
                        "Giá trị hóa đơn vượt giới hạn.");
                }

                details.Add(new SaleDetail
                {
                    ProductId = product.Id,
                    Notes = item.Notes,
                    Quantity = item.Quantity,
                    UnitPrice = price,
                    LineTotal = lineTotal,
                    SourceProductCode = original?.SourceProductCode,
                    SourceProductName = original?.SourceProductName,
                    SourceUnit = original?.SourceUnit,
                    KiotVietInfo = original?.KiotVietInfo
                });
            }

            return details;
        }

        private async Task ValidatePatientAsync(long patientId, bool allowInactive = false)
        {
            var exists = await _context.Patients
                .AnyAsync(p => p.Id == patientId &&
                               (allowInactive || p.Status == "Active" || p.Status == "ACTIVE"));

            if (!exists)
                throw new InvalidOperationException(
                    "Vui lòng chọn bệnh nhân đang hoạt động.");
        }

        public async Task<long> CreateAsync(
            SaleFormViewModel model)
        {
            var source = model.CopyFromId.HasValue ? await _context.Sales.AsNoTracking().Include(s => s.Details).FirstOrDefaultAsync(s => s.Id == model.CopyFromId) : null;
            if (model.CopyFromId.HasValue && source == null) throw new InvalidOperationException("Hóa đơn gốc không còn tồn tại.");
            await ValidatePatientAsync(model.PatientId, source?.PatientId == model.PatientId);
            var details = await BuildDetailsAsync(model, source);
            var total = details.Sum(d => d.LineTotal) - (source?.ImportedDiscount ?? 0) + (source?.ImportedSurcharge ?? 0);
            if (total < 0) throw new InvalidOperationException("Tổng tiền sau giảm giá không được âm.");

            var sale = new Sale
            {
                SaleCode = "SALE-" +
                    Guid.NewGuid().ToString("N").ToUpperInvariant(),
                PatientId = model.PatientId,
                SaleDate = DateTime.Now,
                Status = "Draft",
                Notes = model.Notes,
                TotalAmount = total,
                ImportedGoodsTotal = details.Sum(d => d.LineTotal),
                ImportedDiscount = source?.ImportedDiscount ?? 0,
                ImportedSurcharge = source?.ImportedSurcharge ?? 0,
                SourcePatientName = source?.PatientId == model.PatientId ? source.SourcePatientName : null,
                SourcePatientCode = source?.PatientId == model.PatientId ? source.SourcePatientCode : null,
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
                            SourceDetailId = d.Id,
                            ProductId = d.ProductId,
                            Notes = d.Notes,
                            ProductName = d.SourceProductName ?? d.Product.Name,
                            Unit = d.SourceUnit ?? d.Product.Unit,
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice,
                            LineTotal = d.LineTotal
                        }).ToList()
                });
        }

        public async Task UpdateDraftAsync(
            long id, SaleFormViewModel model)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var sale = await _context.Sales
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null || sale.Status != "Draft")
                throw new InvalidOperationException(
                    "Chỉ được sửa hóa đơn nháp.");
            await ValidatePatientAsync(model.PatientId, sale.PatientId == model.PatientId);
            var details = await BuildDetailsAsync(model, sale);
            var total = details.Sum(d => d.LineTotal) - sale.ImportedDiscount + sale.ImportedSurcharge;
            if (total < 0) throw new InvalidOperationException("Tổng tiền sau giảm giá không được âm.");

            // Chặn việc xác nhận đồng thời trong lúc sửa.
            var updated = await _context.Sales
                .Where(s => s.Id == id && s.Status == "Draft")
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.UpdatedAt, DateTime.Now));

            if (updated != 1)
                throw new InvalidOperationException(
                    "Hóa đơn đã thay đổi trạng thái.");

            _context.SaleDetails.RemoveRange(sale.Details);

            if (sale.PatientId != model.PatientId) { sale.SourcePatientCode = null; sale.SourcePatientName = null; }
            sale.PatientId = model.PatientId;
            sale.Notes = model.Notes;
            sale.TotalAmount = total;
            sale.ImportedGoodsTotal = details.Sum(d => d.LineTotal);
            sale.UpdatedAt = DateTime.Now;
            sale.Details = details;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task ConfirmAsync(long id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

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

            var soldIds = sale.Details.Select(d => d.ProductId).Distinct().ToList();
            var soldProducts = await _context.Products.AsNoTracking().Include(p => p.Components)
                .Where(p => soldIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
            var required = new Dictionary<long, decimal>();
            foreach (var detail in sale.Details)
            {
                if (detail.Quantity <= 0)
                    throw new InvalidOperationException(
                        "Số lượng hóa đơn không hợp lệ.");
                if (!soldProducts.TryGetValue(detail.ProductId, out var sold) || sold.Status is not ("Active" or "ACTIVE"))
                    throw new InvalidOperationException("Hàng bán không tồn tại hoặc đã ngừng hoạt động.");
                if (sold.IsService) continue;
                if (sold.IsCombo && sold.Components.Count == 0)
                    throw new InvalidOperationException($"Combo {sold.Name} chưa có thành phần.");
                if (sold.IsCombo)
                {
                    foreach (var component in sold.Components)
                    {
                        if (component.Quantity <= 0) throw new InvalidOperationException("Cấu hình thành phần combo không hợp lệ.");
                        required[component.ComponentProductId] = required.GetValueOrDefault(component.ComponentProductId) + detail.Quantity * component.Quantity;
                    }
                }
                else required[detail.ProductId] = required.GetValueOrDefault(detail.ProductId) + detail.Quantity;
            }

            foreach (var item in required.OrderBy(r => r.Key))
            {

                // Trừ kho có điều kiện ngay trong SQL Server.
                var affected = await _context.Products
                    .Where(p =>
                        p.Id == item.Key && !p.IsCombo && !p.IsService &&
                        (p.Status == "Active" || p.Status == "ACTIVE") &&
                        p.CurrentStock >= item.Value)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.CurrentStock,
                            p => p.CurrentStock - item.Value)
                        .SetProperty(p => p.UpdatedAt, now));

                if (affected != 1)
                    throw new InvalidOperationException(
                        "Không đủ tồn kho hoặc sản phẩm không tồn tại.");

                var costPrice = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.Id == item.Key)
                    .Select(p => p.CostPrice)
                    .SingleAsync();

                inventoryTransaction.Details.Add(
                    new InventoryTransactionDetail
                    {
                        ProductId = item.Key,
                        Quantity = item.Value,
                        UnitPrice = costPrice,
                        Note = "Bán hàng: " + sale.SaleCode
                    });
            }

            if (inventoryTransaction.Details.Count > 0) _context.InventoryTransactions.Add(inventoryTransaction);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task<decimal?> GetProductPriceAsync(long productId)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.Id == productId && (p.Status == "Active" || p.Status == "ACTIVE"))
                .Select(p => (decimal?)p.SellingPrice)
                .FirstOrDefaultAsync();
        }

    }
}
