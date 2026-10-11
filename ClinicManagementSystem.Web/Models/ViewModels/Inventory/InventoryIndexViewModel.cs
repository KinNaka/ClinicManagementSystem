namespace ClinicManagementSystem.Web.ViewModels.Inventory
{
    public class InventoryIndexViewModel
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? Unit { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal CostPrice { get; set; }

        public bool IsLowStock =>
            CurrentStock <= MinimumStock;
    }
}