using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.ViewModels.Inventory
{
    public class StockInDetailViewModel
    {
        [Required]
        public long ProductId { get; set; }

        [Range(typeof(decimal), "0.001", "999999999", ParseLimitsInInvariantCulture = true)]
        public decimal Quantity { get; set; }

        [Range(typeof(decimal), "0", "999999999", ParseLimitsInInvariantCulture = true)]
        public decimal UnitPrice { get; set; }

        public string? Note { get; set; }
    }
}