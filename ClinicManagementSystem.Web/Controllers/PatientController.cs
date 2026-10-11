using ClinicManagementSystem.Web.Models.ViewModels.Patients;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Web.Controllers
{
    public class PatientsController : Controller
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? status)
        {
            var model = new PatientIndexViewModel
            {
                SearchTerm = searchTerm,
                Status = status,
                Patients = await _patientService.GetAllAsync(
                    searchTerm, status)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PatientFormViewModel
            {
                PatientCode = await _patientService.GeneratePatientCodeAsync(),
                Status = "Active"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PatientFormViewModel model)
        {
            ValidatePatient(model);

            if (!ModelState.IsValid)
                return View(model);

            var result = await _patientService.CreateAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Error ?? "Không thể tạo hồ sơ.");

                return View(model);
            }

            TempData["SuccessMessage"] = "Đã tạo hồ sơ bệnh nhân.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var patient = await _patientService.GetByIdAsync(id);

            if (patient == null)
                return NotFound();

            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            long id,
            PatientFormViewModel model)
        {
            ValidatePatient(model);

            if (!ModelState.IsValid)
                return View(model);

            var result = await _patientService.UpdateAsync(id, model);

            if (!result.Success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Error ?? "Không thể cập nhật hồ sơ.");

                return View(model);
            }

            TempData["SuccessMessage"] = "Đã cập nhật hồ sơ bệnh nhân.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(long id, int page = 1)
        {
            var patient = await _patientService.GetDetailsAsync(id, page);

            if (patient == null)
                return NotFound();

            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(
            long id,
            string status)
        {
            if (status is not ("Active" or "Inactive"))
            {
                TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var success = await _patientService.SetStatusAsync(id, status);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success
                    ? "Đã cập nhật trạng thái bệnh nhân."
                    : "Không tìm thấy bệnh nhân.";

            return RedirectToAction(nameof(Index));
        }

        private void ValidatePatient(PatientFormViewModel model)
        {
            if (model.DateOfBirth.HasValue &&
                model.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "Ngày sinh không được ở tương lai.");
            }

            if (model.Gender is not null &&
                model.Gender is not ("Male" or "Female" or "Other"))
            {
                ModelState.AddModelError(
                    nameof(model.Gender),
                    "Giới tính không hợp lệ.");
            }

            if (model.Status is not ("Active" or "Inactive"))
            {
                ModelState.AddModelError(
                    nameof(model.Status),
                    "Trạng thái không hợp lệ.");
            }
        }
    }
}
