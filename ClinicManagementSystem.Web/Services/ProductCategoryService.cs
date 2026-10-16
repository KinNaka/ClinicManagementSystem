using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.ProductCategories;
using Microsoft.EntityFrameworkCore;
namespace ClinicManagementSystem.Web.Services
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly ApplicationDbContext _context;
        public ProductCategoryService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<bool> ToggleStatusAsync(long id)
        {
            var category = await _context.ProductCategories
                .FirstOrDefaultAsync(x => x.Id == id);

            if (category == null)
            {
                return false;
            }

            category.Status = category.Status == "ACTIVE"
                ? "INACTIVE"
                : "ACTIVE";

            category.UpdateAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<List<ProductCategoryListViewModel>> GetAllAsync(ListPaging? paging = null)
        {
            var query = _context.ProductCategories.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id).AsQueryable();
            if (paging != null) query = await paging.ApplyAsync(query);
            return await query
                .Select(x => new ProductCategoryListViewModel{ 
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Status = x.Status
                })
                .ToListAsync();
        }

        public async Task<ProductCategoryFormViewModel?> GetByIdAsync(long id)
        {
            return await _context.ProductCategories.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new ProductCategoryFormViewModel
                {
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    Status = x.Status
                })
                .FirstOrDefaultAsync();
        }
        public async Task<bool> CreateAsync(ProductCategoryFormViewModel model)
        {
            var code = model.Code.Trim();

            var exists = await _context.ProductCategories
                .AnyAsync(x => x.Code == code);

            if (exists)
            {
                return false;
            }

            var category = new ProductCategory
            {
                Code = code,
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                Status = model.Status.Trim(),
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };

            _context.ProductCategories.Add(category);

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> UpdateAsync(long id, ProductCategoryFormViewModel model)
        {
            var category = await _context.ProductCategories.FirstOrDefaultAsync(x => x.Id == id);
            if (category == null)
            {
                return false;
            }
            category.Code = model.Code.Trim();
            category.Name = model.Name.Trim();
            category.Description = model.Description?.Trim();
            category.Status = model.Status.Trim();
            category.UpdateAt = DateTime.UtcNow;
            _context.ProductCategories.Update(category);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<(bool Success, string? Error)> DeleteAsync(long id)
        {
            var category = await _context.ProductCategories
                .Include(x => x.Products)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (category == null)
            {
                return (false, "Không tìm thấy danh mục sản phẩm.");
            }

            if (category.Products.Any())
            {
                return (
                    false,
                    "Không thể xóa danh mục đang được sử dụng bởi sản phẩm."
                );
            }

            _context.ProductCategories.Remove(category);

            await _context.SaveChangesAsync();

            return (true, null);
        }


    }
}
