using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Services;
using Microsoft.EntityFrameworkCore;

static class SaleAdjustmentTests
{
    public static async Task RunAsync()
    {
        var database = "CodexSaleAdjustmentTest_" + Guid.NewGuid().ToString("N");
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=true").Options);
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        try
        {
            await db.Database.EnsureCreatedAsync();
            var product = new Product { ProductCode = "P", Name = "Test", SellingPrice = 100, CurrentStock = 10, Status = "ACTIVE", Category = new ProductCategory { Code = "C", Name = "Test" } };
            var sale = new Sale { SaleCode = "ADJUST", Patient = new Patient { PatientCode = "P", FullName = "Test", Status = "Active" },
                ImportedGoodsTotal = 200, ImportedDiscount = 20, ImportedSurcharge = 5, TotalAmount = 185,
                Details = new List<SaleDetail> { new() { Product = product, Quantity = 2, UnitPrice = 100, LineTotal = 200 } } };
            db.Add(sale); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var service = new SaleService(db);
            var form = (await service.GetDraftAsync(sale.Id))!;
            Check(form.DiscountType == "Amount" && form.Discount == 20 && form.Surcharge == 5, "Opening edit lost saved adjustments.");
            form.Discount = 30; form.Surcharge = 50;
            await service.UpdateDraftAsync(sale.Id, form); db.ChangeTracker.Clear();
            var saved = await db.Sales.AsNoTracking().SingleAsync();
            Check(saved.TotalAmount == 220 && saved.ImportedDiscount == 30 && saved.ImportedSurcharge == 50, "Amount adjustments were not saved.");
            form = (await service.GetDraftAsync(sale.Id))!;
            form.DiscountType = "Percent"; form.Discount = 25; form.Surcharge = 10;
            await service.UpdateDraftAsync(sale.Id, form); db.ChangeTracker.Clear();
            saved = await db.Sales.AsNoTracking().SingleAsync();
            Check(saved.TotalAmount == 160 && saved.ImportedDiscount == 50 && saved.ImportedSurcharge == 10, "Percentage adjustment total incorrect.");
            foreach (var invalid in new[] { ("Percent", 101m, 10m), ("Amount", 201m, 10m), ("Amount", 0m, -1m) })
            {
                form = (await service.GetDraftAsync(sale.Id))!;
                form.DiscountType = invalid.Item1; form.Discount = invalid.Item2; form.Surcharge = invalid.Item3;
                try { await service.UpdateDraftAsync(sale.Id, form); throw new Exception("Invalid adjustments accepted."); }
                catch (InvalidOperationException) { }
                await service.PrepareFormAsync(form);
                Check(form.Discount == invalid.Item2 && form.Surcharge == invalid.Item3, "Validation refresh lost entered values.");
                db.ChangeTracker.Clear();
                Check((await db.Sales.AsNoTracking().SingleAsync()).TotalAmount == 160, "Failed validation changed saved total.");
            }
            form = (await service.GetDraftAsync(sale.Id))!;
            form.Discount = 0; form.Surcharge = 0;
            await service.UpdateDraftAsync(sale.Id, form); db.ChangeTracker.Clear();
            saved = await db.Sales.AsNoTracking().SingleAsync();
            Check(saved.TotalAmount == 200 && saved.ImportedDiscount == 0 && saved.ImportedSurcharge == 0, "Cannot clear adjustments.");
            Check((await db.Products.AsNoTracking().SingleAsync()).CurrentStock == 10 && !await db.InventoryTransactions.AnyAsync(), "Editing adjustments affected stock.");
            Console.WriteLine("Passed invoice edit adjustments: reload, amount, percentage, invalid input rollback, form retention, clear to zero, unchanged inventory.");
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != database || !database.StartsWith("CodexSaleAdjustmentTest_")) throw new Exception("Unexpected cleanup target.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
