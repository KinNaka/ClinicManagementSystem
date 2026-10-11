
using ClinicManagementSystem.Web.Models.ViewModels.Sales;

namespace ClinicManagementSystem.Web.Services
{
    public interface ISaleService
    {
        Task<SaleIndexViewModel> GetAllAsync(
            string? searchTerm, string? statusFilter);

        Task<SaleDetailsViewModel?> GetByIdAsync(long id);

        Task<SaleFormViewModel> PrepareFormAsync(
            SaleFormViewModel model);

        Task<long> CreateAsync(SaleFormViewModel model);

        Task<SaleFormViewModel?> GetDraftAsync(long id);

        Task UpdateDraftAsync(
            long id, SaleFormViewModel model);

        Task ConfirmAsync(long id);
        Task<decimal?> GetProductPriceAsync(long productId);
    }
}
