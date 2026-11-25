using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.Models.ViewModels.Patients;

public sealed class PatientGroupListViewModel
{
    public List<PatientGroupRowViewModel> Groups { get; set; } = new();
}

public sealed class PatientGroupRowViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "Amount";
    public decimal DiscountValue { get; set; }
    public int MemberCount { get; set; }
}

public sealed class PatientGroupEditViewModel
{
    public long Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, RegularExpression("Amount|Percent")] public string DiscountType { get; set; } = "Amount";
    [Range(typeof(decimal), "0", "999999999999")] public decimal DiscountValue { get; set; }
    public List<long> PatientIds { get; set; } = new();
    public List<PatientGroupPatientOption> Patients { get; set; } = new();
}

public sealed class PatientGroupPatientOption
{
    public long Id { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int InvoiceCount { get; set; }
    public decimal Revenue { get; set; }
}
