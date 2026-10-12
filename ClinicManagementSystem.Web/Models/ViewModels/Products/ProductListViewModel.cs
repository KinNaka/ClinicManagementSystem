
namespace ClinicManagementSystem.Web.Models.ViewModels.Products
{
    public class ProductListViewModel
    {
        public long Id { get; set; }
        public bool IsCombo { get; set; }
        public bool IsService { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public long? CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal CurrentStock { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
    }
}
