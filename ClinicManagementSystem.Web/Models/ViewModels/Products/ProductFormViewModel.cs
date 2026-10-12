using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicManagementSystem.Web.Models.ViewModels.Products;

public class ProductFormViewModel
{
    public long Id { get; set; }
    public bool IsCombo { get; set; }
    public bool IsService { get; set; }
    public string? KiotVietInfo { get; set; }
    public List<ComboComponentFormViewModel> Components { get; set; } = new();
    public List<SelectListItem> ComponentOptions { get; set; } = new();

    [Required(ErrorMessage = "Vui lòng nhập mã sản phẩm.")]
    [StringLength(50, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Mã sản phẩm")]
    public string ProductCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(200, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public long? CategoryId { get; set; }

    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Hàm lượng")]
    public string? Strength { get; set; }

    [StringLength(100, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Dạng bào chế")]
    public string? DosageForm { get; set; }

    [StringLength(50, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Đơn vị tính")]
    public string? Unit { get; set; }

    [StringLength(200, ErrorMessage = "{0} tối đa {1} ký tự.")]
    [Display(Name = "Nhà sản xuất")]
    public string? Manufacturer { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "{0} phải từ {1} đến {2}.")]
    [Display(Name = "Giá vốn")]
    public decimal CostPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "{0} phải từ {1} đến {2}.")]
    [Display(Name = "Giá bán")]
    public decimal SellingPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "{0} phải từ {1} đến {2}.")]
    [Display(Name = "Tồn tối thiểu")]
    public decimal MinimumStock { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "{0} phải từ {1} đến {2}.")]
    [Display(Name = "Tồn kho")]
    public decimal CurrentStock { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}.")]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "ACTIVE";

    public List<SelectListItem> Categories { get; set; } = new();
}

public class ComboComponentFormViewModel
{
    [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn sản phẩm thành phần.")]
    public long ComponentProductId { get; set; }
    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Số lượng thành phần phải từ {1} đến {2}.")]
    public decimal Quantity { get; set; } = 1;
}
