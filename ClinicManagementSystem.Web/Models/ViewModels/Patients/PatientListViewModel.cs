namespace ClinicManagementSystem.Web.Models.ViewModels.Patients
{
    public class PatientListViewModel
    {
        public long Id { get; set; }

        public string PatientCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? PhoneNumber { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}