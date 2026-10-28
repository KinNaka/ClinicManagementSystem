
namespace ClinicManagementSystem.Web.Models.ViewModels.Sales
{
    public class SaleListViewModel
    {
        public long Id { get; set; }

        public string SaleCode { get; set; } = string.Empty;

        public long PatientId { get; set; }

        public string PatientCode { get; set; } = string.Empty;

        public string PatientName { get; set; } = string.Empty;

        public DateTime? SaleDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? DisplayDate => Status == "Draft" ? CreatedAt : SaleDate;

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
