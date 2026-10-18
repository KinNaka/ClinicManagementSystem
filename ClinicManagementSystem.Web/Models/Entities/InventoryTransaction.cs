using ClinicManagementSystem.Web.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.Entities
{
    public class InventoryTransaction
    {
        public long Id { get; set; }
        public DateTime? PaidAt { get; set; }
        [MaxLength(100)] public string? PaidBy { get; set; }
        public long? PatientId { get; set; }
        public Patient? Patient { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string TransactionType { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; }
            = DateTime.Now;

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public ICollection<InventoryTransactionDetail> Details
        { get; set; } =
                new List<InventoryTransactionDetail>();
    }
}
