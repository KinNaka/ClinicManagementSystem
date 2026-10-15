using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.ViewModels.Inventory
{
    public class StockInDetailViewModel
    {
        [Required]
        public long ProductId { get; set; }

        [Range(typeof(decimal), "1", "999999999", ParseLimitsInInvariantCulture = true)]
        [Microsoft.AspNetCore.Mvc.ModelBinder(typeof(ClinicManagementSystem.Web.Models.Validation.QuantityModelBinder))]
        [ClinicManagementSystem.Web.Models.Validation.WholeQuantity]
        public decimal Quantity { get; set; }

        [Range(typeof(decimal), "0", "999999999", ParseLimitsInInvariantCulture = true)]
        public decimal UnitPrice { get; set; }

        public string? Note { get; set; }
    }
}