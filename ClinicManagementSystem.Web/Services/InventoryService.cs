using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Entities;
using ClinicManagementSystem.Web.ViewModels.Inventory;

namespace ClinicManagementSystem.Web.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;

        public InventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryIndexViewModel>>
            GetStockAsync(string? searchTerm, bool lowStockOnly = false)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(p => !p.IsCombo && !p.IsService);

            if (lowStockOnly) query = query.Where(p => p.CurrentStock <= p.MinimumStock);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();

                query = query.Where(p =>
                    p.Name.Contains(keyword) ||
                    p.ProductCode.Contains(keyword));
            }

            return await query
                .OrderBy(p => p.Name)
                .Select(p => new InventoryIndexViewModel
                {
                    ProductCode = p.ProductCode,
                    ProductName = p.Name,
                    Unit = p.Unit,
                    CurrentStock = p.CurrentStock,
                    MinimumStock = p.MinimumStock,
                    CostPrice = p.CostPrice
                })
                .ToListAsync();
        }

        public async Task<bool> StockInAsync(
            StockInViewModel model)
        {
            if (model.Details.Count == 0)
                return false;

            if (model.Details.Any(d =>
                d.ProductId <= 0 ||
                d.Quantity <= 0 ||
                d.UnitPrice < 0))
                return false;

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var productIds = model.Details
                    .Select(d => d.ProductId)
                    .Distinct()
                    .ToList();

                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id) && !p.IsCombo && !p.IsService)
                    .ToListAsync();

                if (products.Count != productIds.Count)
                    return false;

                var transaction = new InventoryTransaction
                {
                    TransactionCode = await GenerateCodeAsync("IN"),
                    TransactionType = "StockIn",
                    TransactionDate = DateTime.Now,
                    Reason = model.Reason,
                    ReferenceNumber = model.ReferenceNumber,
                    CreatedAt = DateTime.Now
                };

                foreach (var detail in model.Details)
                {
                    var product = products
                        .First(p => p.Id == detail.ProductId);

                    product.CurrentStock += detail.Quantity;
                    product.CostPrice = detail.UnitPrice;
                    product.UpdatedAt = DateTime.Now;

                    transaction.Details.Add(
                        new InventoryTransactionDetail
                        {
                            ProductId = product.Id,
                            Quantity = detail.Quantity,
                            UnitPrice = detail.UnitPrice,
                            Note = detail.Note
                        });
                }

                _context.InventoryTransactions.Add(transaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return true;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> StockOutAsync(
            StockOutViewModel model)
        {
            if (model.Details.Count == 0)
                return false;

            if (model.Details.Any(d =>
                d.ProductId <= 0 || d.Quantity <= 0))
                return false;

            // Gộp các dòng trùng sản phẩm để kiểm tra đủ tồn.
            var requested = model.Details
                .GroupBy(d => d.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(d => d.Quantity));

            await using var dbTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var productIds = requested.Keys.ToList();

                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id) && !p.IsCombo && !p.IsService)
                    .ToListAsync();

                if (products.Count != productIds.Count)
                    return false;

                // Kiểm tra tất cả trước khi trừ bất kỳ sản phẩm nào.
                foreach (var product in products)
                {
                    if (product.CurrentStock <
                        requested[product.Id])
                    {
                        return false;
                    }
                }

                var transaction = new InventoryTransaction
                {
                    TransactionCode = await GenerateCodeAsync("OUT"),
                    TransactionType = "StockOut",
                    TransactionDate = DateTime.Now,
                    Reason = model.Reason,
                    CreatedAt = DateTime.Now
                };

                foreach (var detail in model.Details)
                {
                    var product = products
                        .First(p => p.Id == detail.ProductId);

                    product.CurrentStock -= detail.Quantity;
                    product.UpdatedAt = DateTime.Now;

                    transaction.Details.Add(
                        new InventoryTransactionDetail
                        {
                            ProductId = product.Id,
                            Quantity = detail.Quantity,
                            UnitPrice = product.CostPrice,
                            Note = detail.Note
                        });
                }

                _context.InventoryTransactions.Add(transaction);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return true;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<InventoryTransaction>>
            GetTransactionsAsync()
        {
            return await _context.InventoryTransactions
                .AsNoTracking()
                .Include(t => t.Details)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<ClinicManagementSystem.Web.Models.ViewModels.Inventory.StockAdjustmentViewModel> GetStockCountAsync()
        {
            return new() { Details = await _context.Products.AsNoTracking().Where(p => !p.IsCombo && !p.IsService)
                .OrderBy(p => p.Name).Select(p => new ClinicManagementSystem.Web.Models.ViewModels.Inventory.StockCountRow {
                    ProductId = p.Id, ProductCode = p.ProductCode, ProductName = p.Name, Unit = p.Unit, ExpectedStock = p.CurrentStock
                }).ToListAsync() };
        }

        public async Task<int> AdjustStockAsync(ClinicManagementSystem.Web.Models.ViewModels.Inventory.StockAdjustmentViewModel model, string? actor)
        {
            var rows = model.Details.Where(d => d.CountedStock.HasValue).ToList();
            if (rows.Count == 0) throw new InvalidOperationException("Nhập tồn thực tế cho ít nhất một sản phẩm. Để trống các sản phẩm chưa kiểm.");
            if (string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Length > 500 || rows.Any(d => d.ProductId <= 0 || d.CountedStock < 0 || d.CountedStock > 999999999 || d.CountedStock != decimal.Round(d.CountedStock!.Value, 3) || d.Note?.Length > 180) || rows.Select(d => d.ProductId).Distinct().Count() != rows.Count)
                throw new InvalidOperationException("Dữ liệu kiểm kho không hợp lệ; số lượng tối đa 3 chữ số thập phân và không trùng sản phẩm.");
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var ids = rows.Select(d => d.ProductId).ToList();
            var products = await _context.Products.AsNoTracking().Where(p => ids.Contains(p.Id) && !p.IsCombo && !p.IsService).ToDictionaryAsync(p => p.Id);
            if (products.Count != rows.Count) throw new InvalidOperationException("Sản phẩm không còn tồn tại hoặc không quản lý tồn kho.");
            var record = new InventoryTransaction { TransactionCode = "ADJ-" + Guid.NewGuid().ToString("N").ToUpperInvariant(),
                TransactionType = "Adjustment", TransactionDate = DateTime.Now, CreatedAt = DateTime.Now,
                Reason = model.Reason, CreatedBy = actor == null ? null : actor[..Math.Min(actor.Length, 100)] };
            foreach (var row in rows.OrderBy(d => d.ProductId))
            {
                var product = products[row.ProductId];
                if (product.CurrentStock != row.ExpectedStock) throw new InvalidOperationException("Tồn kho đã thay đổi trong lúc kiểm. Số liệu đã được làm mới; hãy kiểm tra lại chênh lệch trước khi lưu.");
                var actual = row.CountedStock!.Value;
                var difference = actual - product.CurrentStock;
                if (difference == 0) continue;
                var affected = await _context.Products.Where(p => p.Id == row.ProductId && p.CurrentStock == row.ExpectedStock && !p.IsCombo && !p.IsService)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.CurrentStock, actual).SetProperty(p => p.UpdatedAt, DateTime.Now));
                if (affected != 1) throw new InvalidOperationException("Tồn kho đã thay đổi; vui lòng kiểm tra lại trước khi cân kho.");
                record.Details.Add(new InventoryTransactionDetail { ProductId = row.ProductId, Quantity = difference, UnitPrice = product.CostPrice,
                    Note = $"Tồn trước: {product.CurrentStock}; thực tế: {actual}; lệch: {difference}. {row.Note}" });
            }
            if (record.Details.Count > 0) { _context.InventoryTransactions.Add(record); await _context.SaveChangesAsync(); }
            await transaction.CommitAsync();
            return record.Details.Count;
        }

        private async Task<string> GenerateCodeAsync(string prefix)
        {
            // Mã dựa trên thời điểm; unique index trong DB
            // vẫn là lớp bảo vệ cuối cùng.
            return $"{prefix}-{DateTime.Now:yyyyMMddHHmmssfff}";
        }
    }
}
