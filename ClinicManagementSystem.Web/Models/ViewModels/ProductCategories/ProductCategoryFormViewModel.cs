using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Web.Models.ViewModels.ProductCategories
{
    public class ProductCategoryFormViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập {0}.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "{0} phải từ {2} đến {1} ký tự.")]
        [Display(Name = "Mã danh mục")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập {0}.")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "{0} phải từ {2} đến {1} ký tự.")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả tối đa {1} ký tự.")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; } = string.Empty;

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";

    }
}
