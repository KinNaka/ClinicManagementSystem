namespace ClinicManagementSystem.Web.Models.ViewModels.Patients
{
    public class PatientIndexViewModel
    {
        public List<PatientListViewModel> Patients { get; set; } = new();

        public string? SearchTerm { get; set; }

        public string? Status { get; set; }
    }
}