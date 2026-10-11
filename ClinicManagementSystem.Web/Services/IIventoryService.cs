using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Entities;
using ClinicManagementSystem.Web.ViewModels.Inventory;

namespace ClinicManagementSystem.Web.Services
{
    public interface IInventoryService
    {
        Task<List<InventoryIndexViewModel>> GetStockAsync(
            string? searchTerm);

        Task<bool> StockInAsync(
            StockInViewModel model);

        Task<bool> StockOutAsync(
            StockOutViewModel model);

        Task<List<InventoryTransaction>> GetTransactionsAsync();
    }
}