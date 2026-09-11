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

        public async Task<List<ProductCategoryListViewModel>> GetAllAsync()
        {
            return await _context.ProductCategories
                .AsNoTracking()
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
            return await _context.ProductCategories
                .AsNoTracking()
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
        public async Task CreateAsync(ProductCategoryFormViewModel model)
        {
            var category = new ProductCategory
            {
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                Status = model.Status,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };
            _context.ProductCategories.Add(category);
            await _context.SaveChangesAsync();
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
        public async Task<bool> DeleteAsync(long id)
        {
            var category = await _context.ProductCategories
                .FirstOrDefaultAsync(x => x.Id == id);

            if (category == null)
                return false;

            _context.ProductCategories.Remove(category);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
