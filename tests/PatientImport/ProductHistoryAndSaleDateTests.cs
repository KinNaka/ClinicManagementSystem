using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Sales;
using ClinicManagementSystem.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ClinicManagementSystem.Web.Controllers;

static class ProductHistoryAndSaleDateTests
{
    public static async Task RunAsync()
    {
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        var authorization = typeof(ProductsController).GetMethod("History")!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single();
        Check(authorization.Roles == "Admin", "Product history must be restricted to Admin.");
        var connection = await DevelopmentLocalDbConnection.ResolveAsync(@"Server=(localdb)\mssqllocaldb;Database=CodexHistoryDate_" + Guid.NewGuid().ToString("N") + ";Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true");
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
        try {
            await db.Database.EnsureCreatedAsync();
            var category = new ProductCategory { Code = "TEST", Name = "Test", Status = "Active" };
            var patient = new Patient { PatientCode = "TEST", FullName = "Test", Status = "Active" };
            db.AddRange(category, patient); await db.SaveChangesAsync();
            var product = new Product { ProductCode = "P1", Name = "Sản phẩm", CategoryId = category.Id, CurrentStock = 20, SellingPrice = 100, Status = "Active" };
            db.Add(product); await db.SaveChangesAsync();
            var products = new ProductService(db);
            var form = (await products.GetByIdAsync(product.Id))!;
            form.SellingPrice = 150;
            Check(await products.UpdateAsync(product.Id, form, "admin-1", "Admin 1"), "Price update failed.");
            db.ChangeTracker.Clear();
            var history = await db.ProductEditHistories.SingleAsync();
            Check(history.FieldName == "SellingPrice" && decimal.Parse(history.OldValue!, System.Globalization.CultureInfo.InvariantCulture) == 100 && decimal.Parse(history.NewValue!, System.Globalization.CultureInfo.InvariantCulture) == 150 && history.ActorId == "admin-1", "Price history or identity incorrect.");
            form = (await products.GetByIdAsync(product.Id))!;
            Check(await products.UpdateAsync(product.Id, form, "admin-2", "Admin 2"), "No-op save failed.");
            Check(await db.ProductEditHistories.CountAsync() == 1, "No-op edit wrote history.");
            form.SellingPrice = 175;
            Check(await products.UpdateAsync(product.Id, form, "admin-2", "Admin 2"), "Second price update failed.");
            Check(await db.ProductEditHistories.AnyAsync(h => h.FieldName == "SellingPrice" && h.ActorId == "admin-2"), "Second admin price history missing.");
            var sales = new SaleService(db);
            var draftForm = new SaleFormViewModel { PatientId = patient.Id, Details = new() { new() { ProductId = product.Id, Quantity = 1 } } };
            var createdBefore = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time");
            var id = await sales.CreateAsync(draftForm);
            db.ChangeTracker.Clear();
            var draft = await db.Sales.SingleAsync(s => s.Id == id);
            Check(draft.SaleDate == null && draft.Status == "Draft", "Draft must not have sale date.");
            Check(draft.CreatedAt >= createdBefore, "New draft creation time must use Vietnam time.");
            var draftDate = createdBefore.AddDays(-2);
            draft.CreatedAt = draftDate; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            Check((await sales.GetByIdAsync(id))!.DisplayDate == draftDate, "Old draft must display its stored creation time.");
            Check((await sales.GetAllAsync(null, "Draft")).Sales.Single().DisplayDate == draftDate, "Draft list must display creation time.");
            Check(!(await sales.GetAllAsync(null, "Draft", period: "today")).Sales.Any(), "Old draft must be excluded from today's filter.");
            var edit = (await sales.GetDraftAsync(id))!;
            await sales.UpdateDraftAsync(id, edit); db.ChangeTracker.Clear();
            Check((await sales.GetByIdAsync(id))!.DisplayDate == draftDate, "Editing must preserve draft creation time.");
            var before = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time");
            await sales.ConfirmAsync(id); db.ChangeTracker.Clear();
            var confirmed = await db.Sales.SingleAsync(s => s.Id == id);
            Check(confirmed.SaleDate >= before && confirmed.Status == "Confirmed", "Confirmation must set today's sale date.");
            Check((await sales.GetByIdAsync(id))!.DisplayDate == confirmed.SaleDate, "Confirmed invoice must display confirmation time.");
            Check((await sales.GetAllAsync(null, "Confirmed", period: "today")).Sales.Single().DisplayDate == confirmed.SaleDate, "Confirmed list/filter must use confirmation time.");
            Check(confirmed.CreatedAt == draftDate, "Confirmation must preserve original creation time.");
            var movement = await db.InventoryTransactions.SingleAsync();
            Check(movement.TransactionDate == confirmed.SaleDate, "Stock and invoice dates must match.");
            try { await sales.ConfirmAsync(id); throw new Exception("Repeated confirmation accepted."); } catch (InvalidOperationException) { }
            Check((await db.Sales.AsNoTracking().SingleAsync(s => s.Id == id)).SaleDate == confirmed.SaleDate, "Repeated confirmation changed date.");
            var picker = await sales.PrepareFormAsync(draftForm);
            Check(picker.Products.Single(p => p.Value == product.Id.ToString()).Text.Contains("Tồn: 19"), "Picker stock must reflect confirmed sale.");
            // Failed stock validation must roll back the sale date and confirmation together.
            var failed = new Sale { SaleCode = "INSUFFICIENT", PatientId = patient.Id, Details = new List<SaleDetail> { new() { ProductId = product.Id, Quantity = 99, UnitPrice = 175, LineTotal = 17325 } } };
            db.Add(failed); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            try { await sales.ConfirmAsync(failed.Id); throw new Exception("Insufficient stock accepted."); } catch (InvalidOperationException) { }
            var retained = await db.Sales.AsNoTracking().SingleAsync(s => s.Id == failed.Id);
            Check(retained.Status == "Draft" && retained.SaleDate == null, "Failed confirmation persisted sale date.");
            Check((await sales.GetByIdAsync(failed.Id))!.DisplayDate == retained.CreatedAt, "Failed confirmation must retain draft display time.");
            Console.WriteLine("PASS: admin access, two-admin audit, no-op edit, undated draft, confirmation date, repeat/failed confirmation, current stock labels.");
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}

