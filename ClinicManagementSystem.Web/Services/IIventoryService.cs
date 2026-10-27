using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Entities;
using ClinicManagementSystem.Web.ViewModels.Inventory;

namespace ClinicManagementSystem.Web.Services
{
    public interface IInventoryService
    {
        Task<List<InventoryIndexViewModel>> GetStockAsync(
            string? searchTerm, bool lowStockOnly = false, ListPaging? paging = null, string? sort = null);

        Task<bool> StockInAsync(
            StockInViewModel model);

        Task<bool> StockOutAsync(
            StockOutViewModel model);

        Task<List<InventoryTransaction>> GetTransactionsAsync(ListPaging? paging = null, string? sort = null);
        Task<ClinicManagementSystem.Web.Models.ViewModels.Inventory.StockAdjustmentViewModel> GetStockCountAsync(ListPaging? paging = null, string? searchTerm = null, string? sort = null, bool archived = false);
        Task<int> AdjustStockAsync(ClinicManagementSystem.Web.Models.ViewModels.Inventory.StockAdjustmentViewModel model, string? actor);
    }
}
