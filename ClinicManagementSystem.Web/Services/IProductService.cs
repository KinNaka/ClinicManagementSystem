
using ClinicManagementSystem.Web.Models.ViewModels.Products;

namespace ClinicManagementSystem.Web.Services;

public interface IProductService
{
    Task<List<ProductListViewModel>> GetAllAsync(
    string? searchTerm,
    long? categoryId,
    string? statusFilter, ListPaging? paging = null, string? sort = null);

    Task<ProductFormViewModel?> GetByIdAsync(long id);

    Task<bool> CreateAsync(ProductFormViewModel model);

    Task<bool> UpdateAsync(long id, ProductFormViewModel model, string? actorId = null, string? actorName = null);

    Task<bool> ToggleStatusAsync(long id, string? actorId = null, string? actorName = null);

    Task<bool> DeleteAsync(long id);
}
