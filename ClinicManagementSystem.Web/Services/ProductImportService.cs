using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClinicManagementSystem.Web.Services;

public sealed class ProductImportService(ApplicationDbContext db, KiotVietProductReader reader, IMemoryCache cache)
{
    private sealed class Pending(string owner, ProductImportViewModel model)
    {
        public string Owner { get; } = owner;
        public ProductImportViewModel Model { get; } = model;
        public SemaphoreSlim Lock { get; } = new(1, 1);
        public bool Completed { get; set; }
    }
    public async Task<ProductImportViewModel> PreviewAsync(Stream stream, string fileName, string owner, bool importStock, bool resetNegativeStock)
    {
        var model = new ProductImportViewModel { FileName = Path.GetFileName(fileName), ImportStock = importStock, ResetNegativeStock = resetNegativeStock,
            Rows = reader.Read(stream, importStock, resetNegativeStock), Token = Guid.NewGuid().ToString("N") };
        await MarkExistingAsync(model);
        var categories = await db.ProductCategories.AsNoTracking().ToListAsync();
        foreach (var row in model.Rows.Where(r => !r.Exists))
        {
            var matches = categories.Where(c => string.Equals(c.Name.Trim(), row.CategoryName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count > 1) row.Errors.Add("Có nhiều nhóm cùng tên trong hệ thống; cần xử lý nhóm trùng trước khi nhập.");
            else if (matches.Count == 1 && matches[0].Status is not ("Active" or "ACTIVE")) row.Errors.Add("Nhóm hàng đang ngừng hoạt động; cần kích hoạt trước khi nhập.");
        }
        var names = categories.Select(c => c.Name.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        model.NewCategoryCount = model.Rows.Where(r => !r.Exists && r.Errors.Count == 0).Select(r => r.CategoryName).Distinct(StringComparer.OrdinalIgnoreCase).Count(name => !names.Contains(name));
        cache.Set("product-import:" + model.Token, new Pending(owner, model), TimeSpan.FromMinutes(20));
        return model;
    }
    private async Task MarkExistingAsync(ProductImportViewModel model)
    {
        var codes = (await db.Products.AsNoTracking().Select(p => p.ProductCode).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in model.Rows) row.Exists = codes.Contains(row.Product.ProductCode);
    }
    public async Task<(bool Success, int Imported, int Skipped, string? Error)> ImportAsync(string token, string owner)
    {
        if (!cache.TryGetValue<Pending>("product-import:" + token, out var pending) || pending == null || pending.Owner != owner)
            return (false, 0, 0, "Bản xem trước đã hết hạn. Vui lòng tải lại file.");
        await pending.Lock.WaitAsync();
        try
        {
            if (pending.Completed) return (false, 0, 0, "File đã được nhập.");
            var model = pending.Model;
            if (model.ErrorCount > 0) return (false, 0, 0, "File còn dòng lỗi. Vui lòng sửa và tải lại.");
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await MarkExistingAsync(model);
            var categories = await db.ProductCategories.ToListAsync();
            var rows = model.Rows.Where(r => !r.Exists).ToList();
            foreach (var row in rows)
            {
                var matches = categories.Where(c => string.Equals(c.Name.Trim(), row.CategoryName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count > 1 || (matches.Count == 1 && matches[0].Status is not ("Active" or "ACTIVE")))
                    return (false, 0, 0, "Nhóm hàng đã thay đổi sau khi xem trước. Vui lòng tải lại file.");
                var category = matches.SingleOrDefault();
                if (category == null)
                {
                    category = new ProductCategory { Code = "KV-G-" + Guid.NewGuid().ToString("N").ToUpperInvariant(), Name = row.CategoryName, Status = "ACTIVE", CreateAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
                    categories.Add(category); db.ProductCategories.Add(category);
                }
                var product = row.Product;
                product.Category = category;
                product.CreatedAt = product.UpdatedAt = DateTime.UtcNow;
                db.Products.Add(product);
            }
            try { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(); db.ChangeTracker.Clear();
                return (false, 0, 0, "Không thể lưu file; chưa nhập hàng nào. Vui lòng tải lại file để kiểm tra dữ liệu và mã hàng.");
            }
            pending.Completed = true; cache.Remove("product-import:" + token);
            return (true, rows.Count, model.ExistingCount, null);
        }
        finally { pending.Lock.Release(); }
    }
}
