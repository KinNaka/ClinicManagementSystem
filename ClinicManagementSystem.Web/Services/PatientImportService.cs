using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.ViewModels.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClinicManagementSystem.Web.Services;

public sealed class PatientImportService(ApplicationDbContext context, KiotVietCustomerReader reader, IMemoryCache cache)
{
    public PatientImportViewModel? GetPreview(string token, string owner) => cache.TryGetValue<PendingImport>("patient-import:" + token, out var pending) && pending != null && pending.Owner == owner && !pending.Completed ? pending.Model : null;
    private sealed class PendingImport(string owner, PatientImportViewModel model)
    {
        public string Owner { get; } = owner;
        public PatientImportViewModel Model { get; } = model;
        public SemaphoreSlim Lock { get; } = new(1, 1);
        public bool Completed { get; set; }
    }

    public async Task<PatientImportViewModel> PreviewAsync(Stream stream, string fileName, string owner)
    {
        var model = new PatientImportViewModel { FileName = Path.GetFileName(fileName), Rows = reader.Read(stream) };
        await MarkExistingAsync(model);
        model.Token = Guid.NewGuid().ToString("N");
        cache.Set("patient-import:" + model.Token, new PendingImport(owner, model), TimeSpan.FromMinutes(20));
        return model;
    }

    private async Task MarkExistingAsync(PatientImportViewModel model)
    {
        var codes = (await context.Patients.AsNoTracking().Select(p => p.PatientCode).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in model.Rows) row.Exists = codes.Contains(row.Patient.PatientCode);
    }

    public async Task<(bool Success, int Imported, int Skipped, string? Error)> ImportAsync(string token, string owner)
    {
        if (!cache.TryGetValue<PendingImport>("patient-import:" + token, out var pending) || pending == null || pending.Owner != owner)
            return (false, 0, 0, "Bản xem trước đã hết hạn. Vui lòng tải lại file.");
        await pending.Lock.WaitAsync();
        try
        {
            if (pending.Completed) return (false, 0, 0, "File này đã được nhập. Vui lòng kiểm tra danh sách khách hàng.");
            var model = pending.Model;
            if (model.ErrorCount > 0) return (false, 0, 0, "File còn dòng lỗi. Vui lòng sửa file và xem trước lại.");
            // Recheck codes at commit time; another user may have imported since preview.
            await MarkExistingAsync(model);
            var patients = model.Rows.Where(r => r.CanImport).Select(r => r.Patient).ToList();
            foreach (var patient in patients)
            {
                patient.CreatedAt = patient.UpdatedAt = DateTime.UtcNow;
                patient.LegacyImportedAt = DateTime.UtcNow;
            }
            await using var transaction = await context.Database.BeginTransactionAsync();
            context.Patients.AddRange(patients);
            try
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                context.ChangeTracker.Clear();
                return (false, 0, 0, "Không thể lưu file; chưa nhập khách hàng nào. Có thể mã KH vừa được tạo bởi người khác. Vui lòng tải lại file để kiểm tra.");
            }
            pending.Completed = true;
            cache.Remove("patient-import:" + token);
            return (true, patients.Count, model.ExistingCount, null);
        }
        finally { pending.Lock.Release(); }
    }
}
