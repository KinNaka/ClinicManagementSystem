
namespace ClinicManagementSystem.Web.Models.ViewModels.Sales
{
    public class SaleDetailsViewModel
    {
        public long Id { get; set; }

        public string SaleCode { get; set; } = string.Empty;

        public long PatientId { get; set; }

        public string PatientCode { get; set; } = string.Empty;

        public string PatientName { get; set; } = string.Empty;
        public string? PatientPhone { get; set; }

        public DateTime SaleDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal ImportedGoodsTotal { get; set; }
        public decimal ImportedDiscount { get; set; }
        public decimal ImportedSurcharge { get; set; }
        public string? ImportedFileName { get; set; }

        public List<SaleDetailDisplayViewModel> Details
        { get; set; } = new();
    }

    public class SaleDetailDisplayViewModel
    {
        public string? Notes { get; set; }

        public string ProductCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string? Unit { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}
