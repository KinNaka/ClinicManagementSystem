
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Products;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Web.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }


    public async Task<List<ProductListViewModel>> GetAllAsync(
        string? searchTerm,
        long? categoryId,
        string? statusFilter, ListPaging? paging = null, string? sort = null)
    {
        var query = _context.Products
            .AsNoTracking()
            .AsQueryable();

        // Search by product code or name
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var keyword = searchTerm.Trim();

            query = query.Where(x =>
                x.ProductCode.Contains(keyword) ||
                x.Name.Contains(keyword));
        }

        // Filter by category
        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        // Filter by status
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(x => x.Status == statusFilter);
        }

        query = ListSorting.Apply(query, sort, x => x.CreatedAt, x => x.Name, x => x.Id);
        if (paging != null) query = await paging.ApplyAsync(query);
        return await query
            .Select(x => new ProductListViewModel
            {
                Id = x.Id, IsCombo = x.IsCombo, IsService = x.IsService,
                ProductCode = x.ProductCode,
                Name = x.Name,
                CategoryName = x.Category.Name,
                CostPrice = x.CostPrice,
                SellingPrice = x.SellingPrice,
                CurrentStock = x.IsCombo ? (x.Components.Min(c => (decimal?)decimal.Floor(c.ComponentProduct.CurrentStock / c.Quantity)) ?? 0) : x.CurrentStock,
                Unit = x.Unit,
                Status = x.Status
            })
            .ToListAsync();
    }


    public async Task<ProductFormViewModel?> GetByIdAsync(long id)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ProductFormViewModel
            {
                Id = x.Id, IsCombo = x.IsCombo, IsService = x.IsService,
                ProductCode = x.ProductCode,
                Name = x.Name,
                CategoryId = x.CategoryId,
                KiotVietInfo = x.KiotVietInfo, Strength = x.Strength,
                DosageForm = x.DosageForm,
                Unit = x.Unit,
                Manufacturer = x.Manufacturer,
                CostPrice = x.CostPrice,
                SellingPrice = x.SellingPrice,
                MinimumStock = x.MinimumStock,
                CurrentStock = x.CurrentStock,
                Status = x.Status,
                Components = x.Components.Select(c => new ComboComponentFormViewModel { ComponentProductId = c.ComponentProductId, Quantity = c.Quantity }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CreateAsync(ProductFormViewModel model)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await ValidateComboAsync(model, 0);
        var code = model.ProductCode.Trim();

        var exists = await _context.Products
            .AnyAsync(x => x.ProductCode == code);

        if (exists)
            return false;

        var categoryExists = await _context.ProductCategories
            .AnyAsync(x => x.Id == model.CategoryId);

        if (!categoryExists)
            return false;

        var product = new Product
        {
            ProductCode = code, IsCombo = model.IsCombo, IsService = model.IsService, Components = BuildComponents(model),
            Name = model.Name.Trim(),
            CategoryId = model.CategoryId!.Value,
            Strength = model.Strength?.Trim(),
            DosageForm = model.DosageForm?.Trim(),
            Unit = model.Unit?.Trim(),
            Manufacturer = model.Manufacturer?.Trim(),
            CostPrice = model.CostPrice,
            SellingPrice = model.SellingPrice,
            MinimumStock = model.MinimumStock,
            CurrentStock = model.IsCombo || model.IsService ? 0 : model.CurrentStock,
            Status = model.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    public async Task<bool> UpdateAsync(
        long id,
        ProductFormViewModel model)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
            return false;

        await ValidateComboAsync(model, id);
        var code = model.ProductCode.Trim();

        var duplicateCode = await _context.Products
            .AnyAsync(x => x.Id != id && x.ProductCode == code);

        if (duplicateCode)
            return false;

        var categoryExists = await _context.ProductCategories
            .AnyAsync(x => x.Id == model.CategoryId);

        if (!categoryExists)
            return false;

        product.IsCombo = model.IsCombo; product.IsService = model.IsService;
        if (model.IsCombo || model.IsService) model.CurrentStock = 0;
        var previous = await _context.Set<ComboComponent>().Where(c => c.ComboProductId == id).ToListAsync();
        _context.RemoveRange(previous);
        await _context.SaveChangesAsync();
        product.Components = BuildComponents(model);
        product.ProductCode = code;
        product.Name = model.Name.Trim();
        product.CategoryId = model.CategoryId!.Value;
        product.Strength = model.Strength?.Trim();
        product.DosageForm = model.DosageForm?.Trim();
        product.Unit = model.Unit?.Trim();
        product.Manufacturer = model.Manufacturer?.Trim();
        product.CostPrice = model.CostPrice;
        product.SellingPrice = model.SellingPrice;
        product.MinimumStock = model.MinimumStock;
        product.CurrentStock = model.CurrentStock;
        product.Status = model.Status;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    public async Task ValidateComboAsync(ProductFormViewModel model, long id)
    {
        if (model.MinimumStock < 0 || model.CurrentStock < 0 || model.MinimumStock != decimal.Truncate(model.MinimumStock) || model.CurrentStock != decimal.Truncate(model.CurrentStock))
            throw new InvalidOperationException("Tồn kho và tồn tối thiểu phải là số nguyên không âm.");
        if (model.IsCombo && model.IsService) throw new InvalidOperationException("Hàng không thể vừa là combo vừa là dịch vụ.");
        if (model.IsService && id > 0 && (await _context.Set<ComboComponent>().AnyAsync(c => c.ComponentProductId == id) || await _context.Products.AnyAsync(p => p.Id == id && p.CurrentStock != 0))) throw new InvalidOperationException("Hàng còn tồn kho hoặc đang là thành phần combo, không thể chuyển thành dịch vụ.");
        if (!model.IsCombo) return;
        if (model.Components.Count == 0 || model.Components.Count > 100)
            throw new InvalidOperationException("Combo phải có từ 1 đến 100 thành phần.");
        if (model.Components.Any(c => c.ComponentProductId == id || c.Quantity <= 0 || c.Quantity > 999999999 || decimal.Truncate(c.Quantity) != c.Quantity))
            throw new InvalidOperationException("Thành phần không được là chính combo; số lượng phải là số nguyên dương.");
        var ids = model.Components.Select(c => c.ComponentProductId).ToList();
        if (ids.Distinct().Count() != ids.Count)
            throw new InvalidOperationException("Không được chọn trùng thành phần trong combo.");
        if (await _context.Set<ComboComponent>().AnyAsync(c => c.ComponentProductId == id))
            throw new InvalidOperationException("Hàng đang là thành phần của combo khác, không thể đổi thành combo.");
        if (await _context.Products.CountAsync(p => ids.Contains(p.Id) && !p.IsCombo && !p.IsService && (p.Status == "Active" || p.Status == "ACTIVE")) != ids.Count)
            throw new InvalidOperationException("Thành phần phải là hàng đang hoạt động; không hỗ trợ combo chứa combo khác.");
        if (id > 0 && await _context.Products.AnyAsync(p => p.Id == id && !p.IsCombo && !p.IsService && p.CurrentStock != 0))
            throw new InvalidOperationException("Hàng còn tồn kho, cần xử lý tồn kho trước khi chuyển thành combo.");
    }

    private static List<ComboComponent> BuildComponents(ProductFormViewModel model) => model.IsCombo
        ? model.Components.Select(c => new ComboComponent { ComponentProductId = c.ComponentProductId, Quantity = c.Quantity }).ToList()
        : new();
    public async Task<bool> DeleteAsync(long id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
            return false;

        _context.Products.Remove(product);

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // Không xóa được vì có dữ liệu liên quan
            _context.Entry(product).State = EntityState.Unchanged;
            return false;
        }
    }
}
