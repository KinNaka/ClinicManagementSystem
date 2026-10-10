
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
        string? statusFilter)
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

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new ProductListViewModel
            {
                Id = x.Id,
                ProductCode = x.ProductCode,
                Name = x.Name,
                CategoryName = x.Category.Name,
                CostPrice = x.CostPrice,
                SellingPrice = x.SellingPrice,
                CurrentStock = x.CurrentStock,
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
                Id = x.Id,
                ProductCode = x.ProductCode,
                Name = x.Name,
                CategoryId = x.CategoryId,
                Strength = x.Strength,
                DosageForm = x.DosageForm,
                Unit = x.Unit,
                Manufacturer = x.Manufacturer,
                CostPrice = x.CostPrice,
                SellingPrice = x.SellingPrice,
                MinimumStock = x.MinimumStock,
                CurrentStock = x.CurrentStock,
                Status = x.Status
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CreateAsync(ProductFormViewModel model)
    {
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
            ProductCode = code,
            Name = model.Name.Trim(),
            CategoryId = model.CategoryId!.Value,
            Strength = model.Strength?.Trim(),
            DosageForm = model.DosageForm?.Trim(),
            Unit = model.Unit?.Trim(),
            Manufacturer = model.Manufacturer?.Trim(),
            CostPrice = model.CostPrice,
            SellingPrice = model.SellingPrice,
            MinimumStock = model.MinimumStock,
            CurrentStock = model.CurrentStock,
            Status = model.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateAsync(
        long id,
        ProductFormViewModel model)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
            return false;

        var code = model.ProductCode.Trim();

        var duplicateCode = await _context.Products
            .AnyAsync(x => x.Id != id && x.ProductCode == code);

        if (duplicateCode)
            return false;

        var categoryExists = await _context.ProductCategories
            .AnyAsync(x => x.Id == model.CategoryId);

        if (!categoryExists)
            return false;

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

        return true;
    }

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
