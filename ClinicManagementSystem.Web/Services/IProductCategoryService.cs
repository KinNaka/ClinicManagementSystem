using ClinicManagementSystem.Web.Models.ViewModels.ProductCategories;

namespace ClinicManagementSystem.Web.Services
{
    public interface IProductCategoryService
    {
        Task<List<ProductCategoryListViewModel>> GetAllAsync();
        Task<ProductCategoryFormViewModel?> GetByIdAsync(long id);
        Task CreateAsync(ProductCategoryFormViewModel model);
        Task<bool> UpdateAsync(long id, ProductCategoryFormViewModel model);
        Task<bool> DeleteAsync(long id);
    }
}
