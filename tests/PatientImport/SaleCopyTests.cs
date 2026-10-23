using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Services;
using Microsoft.EntityFrameworkCore;

static class SaleCopyTests
{
    public static async Task RunAsync()
    {
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        var database = "CodexSaleCopyTest_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=true;MultipleActiveResultSets=true").Options;
        await using var db = new ApplicationDbContext(options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            var patient = new Patient { PatientCode = "COPY", FullName = "Current name", Status = "Active" };
            var product = new Product { ProductCode = "P", Name = "Current product", SellingPrice = 500, CurrentStock = 10, Status = "ACTIVE", Category = new ProductCategory { Code = "COPY", Name = "Test", Status = "ACTIVE" } };
            var source = new Sale { SaleCode = "OLD", Patient = patient, Status = "Imported", SaleDate = new DateTime(2023, 1, 1), Notes = "Original notes", SourcePatientName = "Historical name", ImportedGoodsTotal = 17m, ImportedDiscount = 1.11m, ImportedSurcharge = 2.22m, TotalAmount = 18.11m,
                Details = new List<SaleDetail> {
                    new() { Product = product, Quantity = 1m, UnitPrice = 10m, LineTotal = 10m, Notes = "First note", SourceProductName = "Historical product" },
                    new() { Product = product, Quantity = 1m, UnitPrice = 7m, LineTotal = 7m, Notes = "Second note" }
                } };
            db.Sales.Add(source); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var service = new SaleService(db);
            Check(await service.PrepareCopyAsync(long.MaxValue) == null, "Missing source accepted.");
            var form = (await service.PrepareCopyAsync(source.Id))!;
            Check(form.Id == 0 && form.Status == "Draft" && form.PatientId == patient.Id && form.Notes == source.Notes && form.TotalAmount == 18.11m && form.Discount == 1.11m && form.Surcharge == 2.22m, "Copy header incorrect.");
            Check(form.Details.Count == 2 && form.Details[0].Quantity == 1m && form.Details[0].UnitPrice == 10 && form.Details[0].Notes == "First note" && form.Patients.Single().Text.Contains("Historical name"), "Copied lines/names incorrect.");
            Check(await db.Sales.CountAsync() == 1 && !await db.InventoryTransactions.AnyAsync(), "Opening copy wrote data.");
            // Prices/totals remain server-owned; discount is now editable and validated.
            form.TotalAmount = 0; form.Discount = 999; form.Details[0].UnitPrice = 0; form.Details[0].LineTotal = 0;
            try { await service.CreateAsync(form); throw new Exception("Excessive discount accepted."); } catch (InvalidOperationException) { }
            form.Discount = 1.11m;
            var id = await service.CreateAsync(form);
            db.ChangeTracker.Clear();
            var copy = await db.Sales.Include(s => s.Details).SingleAsync(s => s.Id == id);
            Check(copy.Status == "Draft" && copy.SaleCode != source.SaleCode && copy.SaleDate == null && copy.TotalAmount == 18.11m && copy.ImportedAt == null, "Copy identity/status/total incorrect.");
            Check(copy.Details.OrderBy(d => d.Id).Select(d => (d.Quantity, d.UnitPrice, d.LineTotal, d.Notes)).SequenceEqual(source.Details.Select(d => (d.Quantity, d.UnitPrice, d.LineTotal, d.Notes))), "Copied money or notes changed.");
            Check((await db.Products.SingleAsync()).CurrentStock == 10 && !await db.InventoryTransactions.AnyAsync(), "Saving draft changed stock.");
            db.ChangeTracker.Clear();
            var edit = (await service.GetDraftAsync(id))!;
            Check(edit.Details.All(d => d.Quantity == 1), "Opening a draft multiplied its quantities.");
            edit.Notes = "Edited note";
            await service.UpdateDraftAsync(id, edit); db.ChangeTracker.Clear();
            Check((await db.Sales.SingleAsync(s => s.Id == id)).TotalAmount == 18.11m, "Editing draft repriced copied lines.");
            var fractional = (await service.PrepareCopyAsync(source.Id))!;
            fractional.Details[0].Quantity = 1.5m;
            try { await service.CreateAsync(fractional); throw new Exception("Fractional copied quantity accepted."); } catch (InvalidOperationException) { }
            var invalid = (await service.PrepareCopyAsync(source.Id))!;
            invalid.Details[0].SourceDetailId = long.MaxValue;
            try { await service.CreateAsync(invalid); throw new Exception("Forged source line accepted."); } catch (InvalidOperationException) { }
            var changed = (await service.PrepareCopyAsync(source.Id))!;
            changed.Details[0].Quantity = 2m;
            var changedId = await service.CreateAsync(changed); db.ChangeTracker.Clear();
            Check((await db.Sales.SingleAsync(s => s.Id == changedId)).TotalAmount == 28.11m, "Changed quantity did not use original price.");
            await service.ConfirmAsync(id); db.ChangeTracker.Clear();
            var revenueService = new DashboardService(db);
            var revenue = await revenueService.GetStaffRevenueAsync();
            db.Sales.AddRange(
                new Sale { SaleCode = "PAST-WEEK", PatientId = patient.Id, Status = "Confirmed", SaleDate = revenue.WeekStart.AddSeconds(-1), TotalAmount = 999 },
                new Sale { SaleCode = "FUTURE", PatientId = patient.Id, Status = "Confirmed", SaleDate = revenue.Today.AddDays(1), TotalAmount = 999 },
                new Sale { SaleCode = "IMPORTED-TODAY", PatientId = patient.Id, Status = "Imported", SaleDate = revenue.Today, TotalAmount = 999 });
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            revenue = await revenueService.GetStaffRevenueAsync();
            Check(revenue.TodayRevenue == 18.11m && revenue.WeekRevenue == 18.11m && revenue.WeekStart.DayOfWeek == DayOfWeek.Monday, "Staff revenue included past week, future, drafts or legacy invoices.");
            Check((await db.Products.SingleAsync()).CurrentStock == 8m && await db.InventoryTransactionDetails.SumAsync(d => d.Quantity) == 2m, "Confirmation did not deduct exactly once.");
            Check((await db.Sales.SingleAsync(s => s.Id == source.Id)).Status == "Imported" && (await db.Sales.SingleAsync(s => s.Id == source.Id)).TotalAmount == 18.11m, "Source invoice modified.");
            var discounted = new ClinicManagementSystem.Web.Models.ViewModels.Sales.SaleFormViewModel {
                PatientId = patient.Id, DiscountType = "Percent", Discount = 10, Surcharge = 50,
                Details = new() { new() { ProductId = product.Id, Quantity = 2 } }
            };
            await service.PrepareFormAsync(discounted);
            Check(discounted.Discount == 10 && discounted.Surcharge == 50, "Form refresh lost entered adjustments.");
            var discountedId = await service.CreateAsync(discounted); db.ChangeTracker.Clear();
            var priced = await db.Sales.SingleAsync(s => s.Id == discountedId);
            Check(priced.TotalAmount == 950 && priced.ImportedDiscount == 100 && priced.ImportedSurcharge == 50, "Percentage discount/surcharge not persisted correctly.");
            var pricedDraft = (await service.GetDraftAsync(discountedId))!;
            await service.UpdateDraftAsync(discountedId, pricedDraft); db.ChangeTracker.Clear();
            Check((await db.Sales.SingleAsync(s => s.Id == discountedId)).TotalAmount == 950, "Editing draft lost adjustments.");
            var inactive = await db.Products.SingleAsync(); inactive.Status = "INACTIVE"; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var inactiveId = await service.CreateAsync((await service.PrepareCopyAsync(source.Id))!);
            try { await service.ConfirmAsync(inactiveId); throw new Exception("Inactive product confirmed."); } catch (InvalidOperationException) { }
            Console.WriteLine("Passed: copy preview without writes, original customer/products/notes/prices, whole quantities and duplicate product lines, discount/surcharge, forged price/source rejection, draft edit preserves money, changed quantity, new identity, unchanged source, no stock before confirmation, inactive product stays draft.");
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != database || !database.StartsWith("CodexSaleCopyTest_")) throw new Exception("Unexpected cleanup target.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
