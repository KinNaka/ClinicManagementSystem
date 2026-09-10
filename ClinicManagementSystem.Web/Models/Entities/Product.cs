namespace ClinicManagementSystem.Web.Models.Entities
{
    public class Product
    {
        public long Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public long CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Strength { get; set; }
        public string? DosageForm { get; set; }
        public string? Unit { get; set; }
        public string? Manufacturer { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal CurrentStock { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Navigation property
        public ProductCategory? Category { get; set; }
    }
}
