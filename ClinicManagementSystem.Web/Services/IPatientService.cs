using ClinicManagementSystem.Web.Models.ViewModels.Patients;

namespace ClinicManagementSystem.Web.Services
{
    public interface IPatientService
    {
        Task<string> GeneratePatientCodeAsync();
        Task<List<PhoneMatchViewModel>> FindByPhoneAsync(string? phoneNumber, long? excludeId = null);
        Task<List<PatientListViewModel>> GetAllAsync(
            string? searchTerm,
            string? status, ListPaging? paging = null);

        Task<PatientFormViewModel?> GetByIdAsync(long id);

        Task<PatientDetailsViewModel?> GetDetailsAsync(long id, int page = 1);

        Task<bool> ExistsCodeAsync(
            string patientCode,
            long? excludeId = null);

        Task<(bool Success, string? Error)> CreateAsync(
            PatientFormViewModel model);

        Task<(bool Success, string? Error)> UpdateAsync(
            long id,
            PatientFormViewModel model);

        Task<bool> SetStatusAsync(long id, string status);
    }
}
