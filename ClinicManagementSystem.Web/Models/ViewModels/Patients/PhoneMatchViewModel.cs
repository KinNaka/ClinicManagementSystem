namespace ClinicManagementSystem.Web.Models.ViewModels.Patients;
public sealed class PhoneMatchViewModel
{
    public long Id { get; set; }
    public string PatientCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = "";
}
