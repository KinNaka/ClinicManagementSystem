using ClinicManagementSystem.Web.Models.Entities;
namespace ClinicManagementSystem.Web.Models.ViewModels.Products;

public sealed class ProductImportViewModel
{
    public string FileName { get; set; } = "";
    public string? Token { get; set; }
    public bool ImportStock { get; set; } = true;
    public bool ResetNegativeStock { get; set; } = true;
    public List<ProductImportRow> Rows { get; set; } = new();
    public int NewCount => Rows.Count(r => !r.Exists && r.Errors.Count == 0);
    public int ExistingCount => Rows.Count(r => r.Exists);
    public int ErrorCount => Rows.Count(r => r.Errors.Count > 0);
    public int NewCategoryCount { get; set; }
}
public sealed class ProductImportRow
{
    public int RowNumber { get; set; }
    public Product Product { get; set; } = new();
    public string CategoryName { get; set; } = "";
    public string TypeName { get; set; } = "";
    public decimal SourceStock { get; set; }
    public bool Exists { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
