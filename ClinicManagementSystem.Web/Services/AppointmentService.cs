using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Appointments;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Web.Services;

public sealed class AppointmentService(ApplicationDbContext db)
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

    public async Task<AppointmentFormViewModel?> GetFormAsync(long saleId)
    {
        var sale = await db.Sales.AsNoTracking().Include(s => s.Patient).SingleOrDefaultAsync(s => s.Id == saleId);
        if (sale == null) return null;
        var appointment = await db.FollowUpAppointments.AsNoTracking().SingleOrDefaultAsync(a => a.SaleId == saleId);
        return new AppointmentFormViewModel
        {
            SaleId = sale.Id, SaleCode = sale.SaleCode, PatientName = sale.Patient.FullName,
            PatientCode = sale.Patient.PatientCode, PhoneNumber = sale.Patient.PhoneNumber,
            Exists = appointment != null, Status = appointment?.Status ?? "Pending",
            Version = appointment == null ? "" : Convert.ToBase64String(appointment.RowVersion),
            MedicationDays = appointment?.MedicationDays ?? 7,
            MedicationStartDate = appointment?.MedicationStartDate ?? Today,
            AppointmentDate = appointment?.AppointmentDate ?? Today.AddDays(7),
            UseCustomDate = appointment?.UseCustomDate ?? false, Notes = appointment?.Notes
        };
    }

    public async Task SaveAsync(AppointmentFormViewModel model, string actor)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), errors, true))
            throw new InvalidOperationException(string.Join(" ", errors.Select(e => e.ErrorMessage)));
        var sale = await db.Sales.AsNoTracking().SingleOrDefaultAsync(s => s.Id == model.SaleId)
            ?? throw new InvalidOperationException("Hóa đơn không còn tồn tại.");
        if (sale.Status is not ("Draft" or "Confirmed" or "Imported"))
            throw new InvalidOperationException("Hóa đơn đang được xử lý. Vui lòng tải lại trang.");
        var appointment = await db.FollowUpAppointments.SingleOrDefaultAsync(a => a.SaleId == model.SaleId);
        var now = DateTime.UtcNow;
        if (appointment == null)
        {
            if (!string.IsNullOrEmpty(model.Version)) throw new InvalidOperationException("Lịch hẹn đã thay đổi. Vui lòng tải lại trang.");
            appointment = new FollowUpAppointment
            {
                SaleId = sale.Id, SaleCode = sale.SaleCode, PatientId = sale.PatientId,
                CreatedAtUtc = now, CreatedBy = actor
            };
            db.FollowUpAppointments.Add(appointment);
        }
        else
        {
            if (appointment.Status != "Pending") throw new InvalidOperationException("Chỉ sửa được lịch đang chờ tái khám.");
            SetVersion(appointment, model.Version);
        }
        appointment.MedicationDays = model.MedicationDays;
        appointment.MedicationStartDate = model.MedicationStartDate!.Value;
        appointment.AppointmentDate = model.CalculateDate();
        appointment.UseCustomDate = model.UseCustomDate;
        appointment.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        appointment.UpdatedAtUtc = now;
        appointment.UpdatedBy = actor;
        await SaveChangesAsync();
    }

    public async Task ChangeStatusAsync(long id, string status, string version, string actor)
    {
        if (status is not ("Attended" or "Cancelled")) throw new InvalidOperationException("Trạng thái không hợp lệ.");
        var appointment = await db.FollowUpAppointments.SingleOrDefaultAsync(a => a.Id == id)
            ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn.");
        if (appointment.Status == status) return;
        if (appointment.Status != "Pending") throw new InvalidOperationException("Lịch hẹn đã được xử lý. Vui lòng tải lại trang.");
        SetVersion(appointment, version);
        appointment.Status = status;
        appointment.UpdatedAtUtc = DateTime.UtcNow;
        appointment.UpdatedBy = actor;
        if (status == "Attended")
        {
            appointment.AttendedAtUtc = appointment.UpdatedAtUtc;
            appointment.AttendedBy = actor;
        }
        await SaveChangesAsync();
    }

    public async Task<AppointmentListViewModel> GetListAsync(AppointmentListViewModel model)
    {
        var query = db.FollowUpAppointments.AsNoTracking();
        if (model.FromDate.HasValue) query = query.Where(a => a.AppointmentDate >= model.FromDate.Value);
        if (model.ToDate.HasValue) query = query.Where(a => a.AppointmentDate <= model.ToDate.Value);
        if (!string.IsNullOrEmpty(model.Status)) query = query.Where(a => a.Status == model.Status);
        if (!string.IsNullOrWhiteSpace(model.SearchTerm))
        {
            var term = model.SearchTerm.Trim();
            query = query.Where(a => a.Patient.FullName.Contains(term) || a.Patient.PatientCode.Contains(term) ||
                (a.Patient.PhoneNumber != null && a.Patient.PhoneNumber.Contains(term)) || a.SaleCode.Contains(term));
        }
        model.Sort = model.Sort is "latest" or "name_asc" or "name_desc" ? model.Sort : "soonest";
        query = model.Sort switch
        {
            "latest" => query.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.Id),
            "name_asc" => query.OrderBy(a => a.Patient.FullName).ThenBy(a => a.AppointmentDate).ThenBy(a => a.Id),
            "name_desc" => query.OrderByDescending(a => a.Patient.FullName).ThenBy(a => a.AppointmentDate).ThenBy(a => a.Id),
            _ => query.OrderBy(a => a.AppointmentDate).ThenBy(a => a.Id)
        };
        model.TotalCount = await query.CountAsync();
        model.Page = Math.Clamp(model.Page, 1, model.TotalPages);
        model.Items = await query.Skip((model.Page - 1) * AppointmentListViewModel.PageSize).Take(AppointmentListViewModel.PageSize)
            .Select(a => new AppointmentRowViewModel
            {
                Id = a.Id, PatientId = a.PatientId, SaleId = a.SaleId, SaleCode = a.SaleCode,
                PatientCode = a.Patient.PatientCode, PatientName = a.Patient.FullName, PhoneNumber = a.Patient.PhoneNumber,
                AppointmentDate = a.AppointmentDate, MedicationDays = a.MedicationDays, Status = a.Status,
                Notes = a.Notes, AttendedAtUtc = a.AttendedAtUtc, AttendedBy = a.AttendedBy,
                Version = Convert.ToBase64String(a.RowVersion)
            }).ToListAsync();
        return model;
    }

    private void SetVersion(FollowUpAppointment appointment, string? version)
    {
        if (string.IsNullOrEmpty(version)) throw new InvalidOperationException("Lịch hẹn đã thay đổi. Vui lòng tải lại trang.");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(version); }
        catch (FormatException) { throw new InvalidOperationException("Lịch hẹn đã thay đổi. Vui lòng tải lại trang."); }
        if (bytes.Length != 8) throw new InvalidOperationException("Lịch hẹn đã thay đổi. Vui lòng tải lại trang.");
        db.Entry(appointment).Property(a => a.RowVersion).OriginalValue = bytes;
    }

    private async Task SaveChangesAsync()
    {
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("Lịch hẹn đã được người khác cập nhật. Vui lòng tải lại trang."); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 or 547 })
        {
            throw new InvalidOperationException("Hóa đơn hoặc lịch hẹn đã thay đổi. Vui lòng tải lại trang để kiểm tra lịch hiện có.");
        }
    }
}
