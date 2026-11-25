using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Patients;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Web.Services;

public sealed class PatientGroupService(ApplicationDbContext db)
{
    public async Task<PatientGroupListViewModel> GetAllAsync()
    {
        var groups = await db.PatientGroups.AsNoTracking().OrderBy(g => g.Name)
            .Select(g => new PatientGroupRowViewModel { Id = g.Id, Name = g.Name, DiscountType = g.DiscountType, DiscountValue = g.DiscountValue, MemberCount = g.Members.Count })
            .ToListAsync();
        return new PatientGroupListViewModel { Groups = groups };
    }

    public async Task<PatientGroupEditViewModel> PrepareAsync(long? id = null)
    {
        PatientGroup? group = null;
        if (id.HasValue) group = await db.PatientGroups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == id);
        if (id.HasValue && group == null) throw new KeyNotFoundException();
        var yearStart = new DateTime(DateTime.Today.Year, 1, 1);
        var nextYear = yearStart.AddYears(1);
        var patients = await db.Patients.AsNoTracking()
            .Where(p => p.Status == "Active" || p.Status == "ACTIVE")
            .OrderBy(p => p.FullName)
            .Select(p => new PatientGroupPatientOption
            {
                Id = p.Id, PatientCode = p.PatientCode, FullName = p.FullName, PhoneNumber = p.PhoneNumber,
                InvoiceCount = p.Sales.Count(s => (s.Status == "Confirmed" || s.Status == "Imported") && s.SaleDate >= yearStart && s.SaleDate < nextYear),
                Revenue = p.Sales.Where(s => (s.Status == "Confirmed" || s.Status == "Imported") && s.SaleDate != null).Sum(s => (decimal?)s.TotalAmount) ?? 0
            }).ToListAsync();
        return new PatientGroupEditViewModel
        {
            Id = group?.Id ?? 0, Name = group?.Name ?? "", DiscountType = group?.DiscountType ?? "Amount",
            DiscountValue = group?.DiscountValue ?? 0, PatientIds = group?.Members.Select(m => m.PatientId).ToList() ?? new(), Patients = patients
        };
    }

    public async Task SaveAsync(PatientGroupEditViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) throw new InvalidOperationException("Vui lòng nhập tên nhóm khách hàng.");
        if (model.DiscountType is not ("Amount" or "Percent") || model.DiscountValue < 0 || model.DiscountValue > 999999999999m || (model.DiscountType == "Percent" && model.DiscountValue > 100))
            throw new InvalidOperationException("Mức ưu đãi không hợp lệ.");
        model.PatientIds ??= new();
        var patientIds = model.PatientIds.Distinct().ToList();
        if (patientIds.Count != model.PatientIds.Count) throw new InvalidOperationException("Danh sách khách hàng không hợp lệ.");
        var validIds = await db.Patients.Where(p => patientIds.Contains(p.Id) && (p.Status == "Active" || p.Status == "ACTIVE")).Select(p => p.Id).ToListAsync();
        if (validIds.Count != patientIds.Count) throw new InvalidOperationException("Có khách hàng không tồn tại hoặc không hoạt động.");
        if (await db.PatientGroups.AnyAsync(g => g.Name == model.Name.Trim() && g.Id != model.Id)) throw new InvalidOperationException("Tên nhóm khách hàng đã tồn tại.");
        var group = model.Id == 0 ? new PatientGroup { CreatedAt = DateTime.Now } : await db.PatientGroups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == model.Id) ?? throw new KeyNotFoundException();
        group.Name = model.Name.Trim(); group.DiscountType = model.DiscountType; group.DiscountValue = model.DiscountValue;
        if (model.Id == 0) db.PatientGroups.Add(group);
        else db.PatientGroupMembers.RemoveRange(group.Members);
        var membershipsInOtherGroups = await db.PatientGroupMembers.Where(m => patientIds.Contains(m.PatientId) && m.PatientGroupId != model.Id).ToListAsync();
        db.PatientGroupMembers.RemoveRange(membershipsInOtherGroups);
        group.Members = patientIds.Select(patientId => new PatientGroupMember { PatientId = patientId }).ToList();
        await db.SaveChangesAsync();
    }

    public Task DeleteAsync(long id) => db.PatientGroups.Where(g => g.Id == id).ExecuteDeleteAsync();
}
