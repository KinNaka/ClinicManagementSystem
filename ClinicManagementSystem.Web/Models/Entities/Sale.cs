
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

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public Patient Patient { get; set; } = null!;

        public ICollection<SaleDetail> Details { get; set; }
            = new List<SaleDetail>();
    }
}
