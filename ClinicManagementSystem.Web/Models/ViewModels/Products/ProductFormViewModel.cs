using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicManagementSystem.Web.Models.ViewModels.Products;

public class ProductFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Product code is required.")]
    [StringLength(50)]
    [Display(Name = "Product Code")]
    public string ProductCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(200)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public long? CategoryId { get; set; }

    [StringLength(100)]
    public string? Strength { get; set; }

    [StringLength(100)]
    [Display(Name = "Dosage Form")]
    public string? DosageForm { get; set; }

    [StringLength(50)]
    public string? Unit { get; set; }

    [StringLength(200)]
    public string? Manufacturer { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    [Display(Name = "Cost Price")]
    public decimal CostPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    [Display(Name = "Selling Price")]
    public decimal SellingPrice { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    [Display(Name = "Minimum Stock")]
    public decimal MinimumStock { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    [Display(Name = "Current Stock")]
    public decimal CurrentStock { get; set; }

    [Required]
    public string Status { get; set; } = "ACTIVE";

    public List<SelectListItem> Categories { get; set; } = new();
}