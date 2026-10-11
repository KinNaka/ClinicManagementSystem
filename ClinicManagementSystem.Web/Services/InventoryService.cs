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
            GetStockAsync(string? searchTerm)
        {
            var query = _context.Products
                .AsNoTracking()
                .AsQueryable();

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
                    .Where(p => productIds.Contains(p.Id))
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
                    .Where(p => productIds.Contains(p.Id))
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

        private async Task<string> GenerateCodeAsync(string prefix)
        {
            // Mã dựa trên thời điểm; unique index trong DB
            // vẫn là lớp bảo vệ cuối cùng.
            return $"{prefix}-{DateTime.Now:yyyyMMddHHmmssfff}";
        }
    }
}