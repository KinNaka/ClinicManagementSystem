using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Web.Models.ViewModels.ProductCategoríe
{
    public class ProductcategoryFormViewModel
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";

    }
}
