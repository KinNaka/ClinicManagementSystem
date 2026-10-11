using ClinicManagementSystem.Web.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.Entities
{
    public class InventoryTransactionDetail
    {
        public long Id { get; set; }

        public long InventoryTransactionId { get; set; }

        public long ProductId { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        [MaxLength(300)]
        public string? Note { get; set; }

        public InventoryTransaction InventoryTransaction
        { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}