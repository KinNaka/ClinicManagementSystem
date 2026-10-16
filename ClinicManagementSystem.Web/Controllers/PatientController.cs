using ClinicManagementSystem.Web.Models.ViewModels.Patients;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Web.Controllers
{
    public class PatientsController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly VietnamAddressService _addresses;

        public PatientsController(IPatientService patientService, VietnamAddressService addresses)
        {
            _patientService = patientService;
            _addresses = addresses;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? status, int page = 1)
        {
            var paging = new ListPaging(page);
            ViewData["Paging"] = paging;
            var model = new PatientIndexViewModel
            {
                SearchTerm = searchTerm,
                Status = status,
                Patients = await _patientService.GetAllAsync(
                    searchTerm, status, paging)
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
                if (await _patientService.ExistsCodeAsync(model.PatientCode))
                {
                    model.PatientCode = await _patientService.GeneratePatientCodeAsync();
                    ModelState.Remove(nameof(model.PatientCode));
                }
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

        [HttpGet]
        public async Task<IActionResult> QuickCreate(string? searchTerm)
        {
            ViewData["QuickCreate"] = true;
            var term = searchTerm?.Trim() ?? "";
            var model = new PatientFormViewModel { PatientCode = await _patientService.GeneratePatientCodeAsync(), Status = "Active" };
            if (term.Length <= 200 && term.Any(char.IsLetter) && !term.Any(char.IsDigit)) model.FullName = term;
            else if (term.Length <= 20 && System.Text.RegularExpressions.Regex.IsMatch(term, @"^[+\d ().-]{8,20}$")) model.PhoneNumber = term;
            return View("Create", model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCreate(PatientFormViewModel model)
        {
            ViewData["QuickCreate"] = true;
            model.PatientCode = await _patientService.GeneratePatientCodeAsync();
            model.Status = "Active";
            ModelState.Remove(nameof(model.PatientCode));
            ModelState.Remove(nameof(model.Status));
            ValidatePatient(model);
            if (!ModelState.IsValid) return View("Create", model);
            var result = await _patientService.CreateAsync(model);
            if (!result.Success) { ModelState.AddModelError("", result.Error ?? "Không thể tạo bệnh nhân."); return View("Create", model); }
            var created = (await _patientService.GetAllAsync(model.PatientCode, null)).Single(p => p.PatientCode == model.PatientCode);
            return View("QuickCreated", new QuickPatientCreatedViewModel {
                Id = created.Id,
                Text = created.FullName + " - " + (created.DateOfBirth?.Year.ToString() ?? "Chưa có năm sinh") + " - " +
                    (string.IsNullOrWhiteSpace(created.PhoneNumber) ? "Chưa có SĐT" : created.PhoneNumber)
            });
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

        [HttpGet]
        public IActionResult AddressProvinces() => Json(_addresses.Provinces.Select(p => new { code = p.Code, name = p.Name }));

        [HttpGet]
        public async Task<IActionResult> PhoneMatches(string? phoneNumber, long? excludeId)
        {
            if (phoneNumber?.Length > 40) return BadRequest();
            var matches = await _patientService.FindByPhoneAsync(phoneNumber, excludeId);
            return Json(matches.Select(p => new { p.Id, p.PatientCode, p.FullName, p.PhoneNumber, p.Status,
                detailsUrl = Url.Action(nameof(Details), new { id = p.Id }) }));
        }

        [HttpGet]
        public IActionResult AddressWards(int provinceCode) => Json(_addresses.FindProvince(provinceCode)?.Wards ?? new());

        private void ValidatePatient(PatientFormViewModel model)
        {
            model.ProvinceName = null;
            model.WardName = null;
            var province = _addresses.FindProvince(model.ProvinceCode);
            if (model.ProvinceCode.HasValue && province == null)
                ModelState.AddModelError(nameof(model.ProvinceCode), "Vui lòng chọn tỉnh/thành phố hợp lệ.");
            var ward = province?.Wards.FirstOrDefault(w => w.Code == model.WardCode);
            if (model.WardCode.HasValue && ward == null)
                ModelState.AddModelError(nameof(model.WardCode), "Xã/phường không thuộc tỉnh/thành phố đã chọn.");
            if (province != null && ward == null)
                ModelState.AddModelError(nameof(model.WardCode), "Vui lòng chọn xã/phường.");
            model.ProvinceName = province?.Name;
            model.WardName = ward?.Name;
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
