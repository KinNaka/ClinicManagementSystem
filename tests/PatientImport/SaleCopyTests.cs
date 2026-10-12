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
            var source = new Sale { SaleCode = "OLD", Patient = patient, Status = "Imported", SaleDate = new DateTime(2023, 1, 1), Notes = "Original notes", SourcePatientName = "Historical name", ImportedGoodsTotal = 10.33m, ImportedDiscount = 1.11m, ImportedSurcharge = 2.22m, TotalAmount = 11.44m,
                Details = new List<SaleDetail> {
                    new() { Product = product, Quantity = .333m, UnitPrice = 10m, LineTotal = 3.33m, Notes = "First note", SourceProductName = "Historical product" },
                    new() { Product = product, Quantity = 1m, UnitPrice = 7m, LineTotal = 7m, Notes = "Second note" }
                } };
            db.Sales.Add(source); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var service = new SaleService(db);
            Check(await service.PrepareCopyAsync(long.MaxValue) == null, "Missing source accepted.");
            var form = (await service.PrepareCopyAsync(source.Id))!;
            Check(form.Id == 0 && form.Status == "Draft" && form.PatientId == patient.Id && form.Notes == source.Notes && form.TotalAmount == 11.44m && form.Discount == 1.11m && form.Surcharge == 2.22m, "Copy header incorrect.");
            Check(form.Details.Count == 2 && form.Details[0].Quantity == .333m && form.Details[0].UnitPrice == 10 && form.Details[0].Notes == "First note" && form.Patients.Single().Text.Contains("Historical name"), "Copied lines/names incorrect.");
            Check(await db.Sales.CountAsync() == 1 && !await db.InventoryTransactions.AnyAsync(), "Opening copy wrote data.");
            // Posted prices and totals must never override the source stored by the server.
            form.TotalAmount = 0; form.Discount = 999; form.Details[0].UnitPrice = 0; form.Details[0].LineTotal = 0;
            var id = await service.CreateAsync(form);
            db.ChangeTracker.Clear();
            var copy = await db.Sales.Include(s => s.Details).SingleAsync(s => s.Id == id);
            Check(copy.Status == "Draft" && copy.SaleCode != source.SaleCode && copy.SaleDate > source.SaleDate && copy.TotalAmount == 11.44m && copy.ImportedAt == null, "Copy identity/status/total incorrect.");
            Check(copy.Details.OrderBy(d => d.Id).Select(d => (d.Quantity, d.UnitPrice, d.LineTotal, d.Notes)).SequenceEqual(source.Details.Select(d => (d.Quantity, d.UnitPrice, d.LineTotal, d.Notes))), "Copied money or notes changed.");
            Check((await db.Products.SingleAsync()).CurrentStock == 10 && !await db.InventoryTransactions.AnyAsync(), "Saving draft changed stock.");
            db.ChangeTracker.Clear();
            var edit = (await service.GetDraftAsync(id))!;
            edit.Notes = "Edited note";
            await service.UpdateDraftAsync(id, edit); db.ChangeTracker.Clear();
            Check((await db.Sales.SingleAsync(s => s.Id == id)).TotalAmount == 11.44m, "Editing draft repriced copied lines.");
            var invalid = (await service.PrepareCopyAsync(source.Id))!;
            invalid.Details[0].SourceDetailId = long.MaxValue;
            try { await service.CreateAsync(invalid); throw new Exception("Forged source line accepted."); } catch (InvalidOperationException) { }
            var changed = (await service.PrepareCopyAsync(source.Id))!;
            changed.Details[0].Quantity = .5m;
            var changedId = await service.CreateAsync(changed); db.ChangeTracker.Clear();
            Check((await db.Sales.SingleAsync(s => s.Id == changedId)).TotalAmount == 13.11m, "Changed quantity did not use original price.");
            await service.ConfirmAsync(id); db.ChangeTracker.Clear();
            Check((await db.Products.SingleAsync()).CurrentStock == 8.667m && await db.InventoryTransactionDetails.SumAsync(d => d.Quantity) == 1.333m, "Confirmation did not deduct exactly once.");
            Check((await db.Sales.SingleAsync(s => s.Id == source.Id)).Status == "Imported" && (await db.Sales.SingleAsync(s => s.Id == source.Id)).TotalAmount == 11.44m, "Source invoice modified.");
            var inactive = await db.Products.SingleAsync(); inactive.Status = "INACTIVE"; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var inactiveId = await service.CreateAsync((await service.PrepareCopyAsync(source.Id))!);
            try { await service.ConfirmAsync(inactiveId); throw new Exception("Inactive product confirmed."); } catch (InvalidOperationException) { }
            Console.WriteLine("Passed: copy preview without writes, original customer/products/notes/prices, fractions and duplicate product lines, discount/surcharge, forged price/source rejection, draft edit preserves money, changed quantity, new identity, unchanged source, no stock before confirmation, inactive product stays draft.");
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != database || !database.StartsWith("CodexSaleCopyTest_")) throw new Exception("Unexpected cleanup target.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
