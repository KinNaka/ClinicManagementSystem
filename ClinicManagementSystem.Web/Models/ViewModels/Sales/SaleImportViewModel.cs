using ClinicManagementSystem.Web.Models.Entities;

namespace ClinicManagementSystem.Web.Models.ViewModels.Sales;

public sealed class SaleImportViewModel
{
    public string? Token { get; set; }
    public List<string> Files { get; set; } = new();
    public bool CreateMissingRecords { get; set; }
    public bool UseWalkInCustomer { get; set; } = true;
    public List<SaleImportInvoice> Invoices { get; set; } = new();
    public List<string> MissingPatientCodes { get; set; } = new();
    public List<string> MissingProductCodes { get; set; } = new();
    public int ExistingCount => Invoices.Count(i => i.Exists);
    public int ErrorCount => Invoices.Count(i => i.Errors.Count > 0);
    public int NewCount => Invoices.Count(i => !i.Exists && i.Errors.Count == 0);
    public decimal Total => Invoices.Where(i => !i.Exists).Sum(i => i.Sale.TotalAmount);
    public int WalkInCount => Invoices.Count(i => !i.Exists && i.PatientCode.Length == 0);
    public bool CanImport => NewCount > 0 && ErrorCount == 0 &&
        (CreateMissingRecords || (MissingPatientCodes.Count == 0 && MissingProductCodes.Count == 0)) && (UseWalkInCustomer || WalkInCount == 0);
}
public sealed class SaleImportInvoice
{
    public Sale Sale { get; set; } = new();
    public string FileName { get; set; } = "";
    public int RowNumber { get; set; }
    public string PatientCode { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool Exists { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
