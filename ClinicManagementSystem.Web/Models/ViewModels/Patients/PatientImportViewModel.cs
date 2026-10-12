using ClinicManagementSystem.Web.Models.Entities;

namespace ClinicManagementSystem.Web.Models.ViewModels.Patients;

public sealed class PatientImportViewModel
{
    public string FileName { get; set; } = "";
    public string? Token { get; set; }
    public List<PatientImportRow> Rows { get; set; } = new();
    public int NewCount => Rows.Count(r => r.CanImport);
    public int ExistingCount => Rows.Count(r => r.Exists);
    public int ErrorCount => Rows.Count(r => r.Errors.Count > 0);
    public decimal TotalLegacySales => Rows.Where(r => r.CanImport).Sum(r => r.Patient.LegacyTotalSales);
}

public sealed class PatientImportRow
{
    public int RowNumber { get; set; }
    public Patient Patient { get; set; } = new();
    public bool Exists { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool CanImport => !Exists && Errors.Count == 0;
}
