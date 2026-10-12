
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.Models.Entities
{
    public class Sale
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string SaleCode { get; set; } = string.Empty;

        public long PatientId { get; set; }

        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Draft";

        public decimal TotalAmount { get; set; }
        public decimal ImportedGoodsTotal { get; set; }
        public decimal ImportedDiscount { get; set; }
        public decimal ImportedSurcharge { get; set; }
        [MaxLength(200)] public string? SourcePatientName { get; set; }
        [MaxLength(30)] public string? SourcePatientCode { get; set; }
        [MaxLength(255)] public string? ImportedFileName { get; set; }
        public DateTime? ImportedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public Patient Patient { get; set; } = null!;

        public ICollection<SaleDetail> Details { get; set; }
            = new List<SaleDetail>();
    }
}
