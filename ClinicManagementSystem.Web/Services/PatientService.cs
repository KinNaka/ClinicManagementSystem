using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Patients;
using ClinicManagementSystem.Web.Models.ViewModels.Sales;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Web.Services
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _context;

        public PatientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PatientListViewModel>> GetAllAsync(
            string? searchTerm,
            string? status)
        {
            var query = _context.Patients.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();

                query = query.Where(p =>
                    p.PatientCode.Contains(keyword) ||
                    p.FullName.Contains(keyword) ||
                    (p.PhoneNumber != null &&
                     p.PhoneNumber.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            return await query
                .OrderBy(p => p.FullName)
                .Select(p => new PatientListViewModel
                {
                    Id = p.Id,
                    PatientCode = p.PatientCode,
                    FullName = p.FullName,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    PhoneNumber = p.PhoneNumber,
                    TotalSales = p.LegacyTotalSales + (_context.Sales
                        .Where(s => s.PatientId == p.Id && s.Status == "Confirmed")
                        .Sum(s => (decimal?)s.TotalAmount) ?? 0m),
                    Status = p.Status
                })
                .ToListAsync();
        }

        public async Task<PatientFormViewModel?> GetByIdAsync(long id)
        {
            return await _context.Patients
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new PatientFormViewModel
                {
                    PatientCode = p.PatientCode,
                    FullName = p.FullName,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    PhoneNumber = p.PhoneNumber,
                    Address = p.Address,
                    ProvinceCode = p.ProvinceCode,
                    WardCode = p.WardCode,
                    ProvinceName = p.ProvinceName,
                    WardName = p.WardName,
                    Occupation = p.Occupation,
                    Notes = p.Notes,
                    Status = p.Status
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PatientDetailsViewModel?> GetDetailsAsync(long id, int page = 1)
        {
            var patient = await GetByIdAsync(id);
            if (patient == null) return null;

            // Include drafts with their status; fetch only the requested ten invoices.
            var query = _context.Sales.AsNoTracking().Where(s => s.PatientId == id);
            var history = new PatientPurchaseHistoryViewModel(await query.CountAsync(), page);
            history.Sales = await query
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id)
                .Skip(history.Offset)
                .Take(PatientPurchaseHistoryViewModel.PageSize)
                .Select(s => new SaleListViewModel
                {
                    Id = s.Id,
                    SaleCode = s.SaleCode,
                    PatientId = s.PatientId,
                    PatientCode = patient.PatientCode,
                    PatientName = patient.FullName,
                    SaleDate = s.SaleDate,
                    TotalAmount = s.TotalAmount,
                    Status = s.Status
                })
                .ToListAsync();

            var legacyTotalSales = await _context.Patients.AsNoTracking()
                .Where(p => p.Id == id).Select(p => p.LegacyTotalSales).SingleAsync();
            var currentTotalSales = await query.Where(s => s.Status == "Confirmed")
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
            return new PatientDetailsViewModel {
                Id = id, Patient = patient, PurchaseHistory = history,
                LegacyTotalSales = legacyTotalSales, CurrentTotalSales = currentTotalSales
            };
        }

        public async Task<bool> ExistsCodeAsync(
            string patientCode,
            long? excludeId = null)
        {
            var code = patientCode.Trim();

            var query = _context.Patients
                .Where(p => p.PatientCode == code);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<(bool Success, string? Error)> CreateAsync(
            PatientFormViewModel model)
        {
            var code = model.PatientCode.Trim().ToUpperInvariant();

            if (await ExistsCodeAsync(code))
            {
                return (false, "Mã bệnh nhân đã tồn tại.");
            }

            if (!IsValidStatus(model.Status))
            {
                return (false, "Trạng thái bệnh nhân không hợp lệ.");
            }

            var patient = new Patient
            {
                PatientCode = code,
                FullName = model.FullName.Trim(),
                DateOfBirth = model.DateOfBirth,
                Gender = NormalizeOptional(model.Gender),
                PhoneNumber = NormalizeOptional(model.PhoneNumber),
                Address = NormalizeOptional(model.Address),
                ProvinceCode = model.ProvinceCode,
                WardCode = model.WardCode,
                ProvinceName = model.ProvinceName,
                WardName = model.WardName,
                Occupation = NormalizeOptional(model.Occupation),
                Notes = NormalizeOptional(model.Notes),
                Status = model.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Patients.Add(patient);

            try
            {
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateException)
            {
                // Xử lý cả trường hợp hai người tạo cùng mã đồng thời.
                return (false, "Không thể lưu hồ sơ. Vui lòng kiểm tra mã bệnh nhân.");
            }
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(
            long id,
            PatientFormViewModel model)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
            {
                return (false, "Không tìm thấy bệnh nhân.");
            }

            var code = model.PatientCode.Trim();

            if (await ExistsCodeAsync(code, id))
            {
                return (false, "Mã bệnh nhân đã được sử dụng.");
            }

            if (!IsValidStatus(model.Status))
            {
                return (false, "Trạng thái bệnh nhân không hợp lệ.");
            }

            patient.PatientCode = code;
            patient.FullName = model.FullName.Trim();
            patient.DateOfBirth = model.DateOfBirth;
            patient.Gender = NormalizeOptional(model.Gender);
            patient.PhoneNumber = NormalizeOptional(model.PhoneNumber);
            patient.Address = NormalizeOptional(model.Address);
            patient.ProvinceCode = model.ProvinceCode;
            patient.WardCode = model.WardCode;
            patient.ProvinceName = model.ProvinceName;
            patient.WardName = model.WardName;
            patient.Occupation = NormalizeOptional(model.Occupation);
            patient.Notes = NormalizeOptional(model.Notes);
            patient.Status = model.Status;
            patient.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateException)
            {
                return (false, "Không thể cập nhật hồ sơ. Vui lòng kiểm tra mã bệnh nhân.");
            }
        }

        public async Task<bool> SetStatusAsync(long id, string status)
        {
            if (!IsValidStatus(status))
                return false;

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
                return false;

            patient.Status = status;
            patient.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        private static bool IsValidStatus(string? status)
        {
            return status is "Active" or "Inactive";
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
        public async Task<string> GeneratePatientCodeAsync()
        {
            var codes = await _context.Patients.AsNoTracking()
                .Where(p => p.PatientCode.StartsWith("LT"))
                .Select(p => p.PatientCode)
                .ToListAsync();
            return PatientCodeGenerator.Next(codes);
        }
    }
}
