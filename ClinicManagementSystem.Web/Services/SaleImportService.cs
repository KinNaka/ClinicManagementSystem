using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClinicManagementSystem.Web.Services;

public sealed class SaleImportService(ApplicationDbContext db, KiotVietSaleReader reader, IMemoryCache cache)
{
    public const string WalkInCode = "KV_WALKIN";
    private sealed class Pending(string owner, SaleImportViewModel model)
    {
        public string Owner { get; } = owner;
        public SaleImportViewModel Model { get; } = model;
        public SemaphoreSlim Lock { get; } = new(1, 1);
        public bool Completed { get; set; }
    }
    public async Task<SaleImportViewModel> PreviewAsync(IEnumerable<(Stream Stream, string Name)> files, string owner, bool createMissing, bool walkIn)
    {
        var model = new SaleImportViewModel { CreateMissingRecords = createMissing, UseWalkInCustomer = walkIn };
        foreach (var file in files)
        {
            model.Files.Add(Path.GetFileName(file.Name));
            model.Invoices.AddRange(reader.Read(file.Stream, file.Name));
        }
        if (model.Invoices.Count > 20000) throw new InvalidDataException("Tối đa 20.000 hóa đơn mỗi đợt nhập.");
        foreach (var group in model.Invoices.GroupBy(i => i.Sale.SaleCode, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            foreach (var invoice in group) invoice.Errors.Add("Mã hóa đơn trùng giữa các file. Chỉ chọn một bản của hóa đơn này.");
        await ResolveAsync(model);
        model.Token = Guid.NewGuid().ToString("N");
        cache.Set("sale-import:" + model.Token, new Pending(owner, model), TimeSpan.FromMinutes(30));
        return model;
    }
    private async Task ResolveAsync(SaleImportViewModel model)
    {
        var saleCodes = (await db.Sales.AsNoTracking().Select(s => s.SaleCode).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var patientCodes = (await db.Patients.AsNoTracking().Select(p => p.PatientCode).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var products = await db.Products.AsNoTracking().Select(p => new { p.Id, p.ProductCode }).ToListAsync();
        var productCodes = products.Select(p => p.ProductCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ambiguous = products.GroupBy(p => p.ProductCode, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var invoice in model.Invoices)
        {
            invoice.Exists = saleCodes.Contains(invoice.Sale.SaleCode);
            if (!invoice.Exists && invoice.Sale.Details.Any(d => ambiguous.Contains(d.SourceProductCode!)))
                invoice.Errors.Add("Mã hàng bị trùng trong hệ thống; cần xử lý trước khi liên kết hóa đơn.");
        }
        var fresh = model.Invoices.Where(i => !i.Exists).ToList();
        model.MissingPatientCodes = fresh.Select(i => i.PatientCode).Where(c => c.Length > 0 && !patientCodes.Contains(c)).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList();
        model.MissingProductCodes = fresh.SelectMany(i => i.Sale.Details).Select(d => d.SourceProductCode!).Where(c => !productCodes.Contains(c)).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList();
    }
    public async Task<(bool Success, int Imported, int Skipped, string? Error)> ImportAsync(string token, string owner)
    {
        if (!cache.TryGetValue<Pending>("sale-import:" + token, out var pending) || pending == null || pending.Owner != owner)
            return (false, 0, 0, "Bản xem trước đã hết hạn. Vui lòng tải lại file.");
        await pending.Lock.WaitAsync();
        try
        {
            if (pending.Completed) return (false, 0, 0, "Đợt nhập đã được lưu.");
            var model = pending.Model;
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            await ResolveAsync(model);
            if (model.ErrorCount > 0 || (!model.CreateMissingRecords && (model.MissingPatientCodes.Count > 0 || model.MissingProductCodes.Count > 0)) || (!model.UseWalkInCustomer && model.WalkInCount > 0))
                return (false, 0, 0, "Còn lỗi hoặc mã KH/mã hàng chưa liên kết. Vui lòng tải lại file và kiểm tra lựa chọn.");
            var patients = (await db.Patients.ToListAsync()).ToDictionary(p => p.PatientCode, StringComparer.OrdinalIgnoreCase);
            var productGroups = (await db.Products.ToListAsync()).GroupBy(p => p.ProductCode, StringComparer.OrdinalIgnoreCase);
            var products = productGroups.Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.Single(), StringComparer.OrdinalIgnoreCase);
            var invoices = model.Invoices.Where(i => !i.Exists).ToList();
            ProductCategory? historicalCategory = null;
            if (model.MissingProductCodes.Count > 0)
            {
                historicalCategory = await db.ProductCategories.FirstOrDefaultAsync(c => c.Code == "KV-HISTORY");
                if (historicalCategory == null)
                {
                    historicalCategory = new ProductCategory { Code = "KV-HISTORY", Name = "Lịch sử KiotViet (thiếu danh mục)", Status = "ACTIVE", CreateAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
                    db.ProductCategories.Add(historicalCategory);
                }
            }
            foreach (var invoice in invoices)
            {
                var code = invoice.PatientCode.Length == 0 ? WalkInCode : invoice.PatientCode;
                if (!patients.TryGetValue(code, out var patient))
                {
                    patient = new Patient { PatientCode = code, FullName = invoice.PatientCode.Length == 0 ? "Khách lẻ KiotViet" : string.IsNullOrWhiteSpace(invoice.PatientName) ? code : invoice.PatientName,
                        Status = "Active", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                        Notes = "Tạo từ hóa đơn KiotViet để liên kết lịch sử; chưa nhập tổng bán lũy kế." };
                    if (!string.IsNullOrWhiteSpace(invoice.Phone) && invoice.Phone.Length <= 20 && new PhoneAttribute().IsValid(invoice.Phone)) patient.PhoneNumber = invoice.Phone;
                    if (!string.IsNullOrWhiteSpace(invoice.Address) && invoice.Address.Length <= 300) patient.Address = invoice.Address;
                    patients.Add(code, patient); db.Patients.Add(patient);
                }
                var sale = invoice.Sale;
                sale.Patient = patient;
                sale.ImportedAt = DateTime.UtcNow;
                foreach (var detail in sale.Details)
                {
                    if (!products.TryGetValue(detail.SourceProductCode!, out var product))
                    {
                        product = new Product { ProductCode = detail.SourceProductCode!, Name = detail.SourceProductName!, Unit = detail.SourceUnit,
                            Category = historicalCategory!, SellingPrice = detail.UnitPrice, CurrentStock = 0, Status = "INACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                            KiotVietInfo = "Tạo từ hóa đơn cũ do thiếu danh mục; tồn kho 0, ngừng kinh doanh. Cần kiểm tra loại hàng, giá và tồn kho trước khi kích hoạt." };
                        products.Add(product.ProductCode, product); db.Products.Add(product);
                    }
                    detail.Product = product;
                }
                db.Sales.Add(sale);
            }
            try { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(); db.ChangeTracker.Clear(); cache.Remove("sale-import:" + token);
                return (false, 0, 0, "Không thể lưu đợt nhập; chưa nhập hóa đơn nào. Vui lòng tải lại file để đối chiếu dữ liệu/mã trùng.");
            }
            pending.Completed = true; cache.Remove("sale-import:" + token);
            return (true, invoices.Count, model.ExistingCount, null);
        }
        finally { pending.Lock.Release(); }
    }
}
