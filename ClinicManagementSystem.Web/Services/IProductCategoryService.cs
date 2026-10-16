using ClinicManagementSystem.Web.Models.ViewModels.ProductCategories;

namespace ClinicManagementSystem.Web.Services
{
    public interface IProductCategoryService
    {
        Task<List<ProductCategoryListViewModel>> GetAllAsync(ListPaging? paging = null, string? sort = null);
        Task<ProductCategoryFormViewModel?> GetByIdAsync(long id);
        Task <bool> CreateAsync(ProductCategoryFormViewModel model);
        Task<bool> UpdateAsync(long id, ProductCategoryFormViewModel model);
        Task<(bool Success, string? Error)> DeleteAsync(long id);
        Task<bool> ToggleStatusAsync(long id);
    }
}
