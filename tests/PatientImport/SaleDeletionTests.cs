using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Services;
using Microsoft.EntityFrameworkCore;

static class SaleDeletionTests
{
    public static async Task RunAsync()
    {
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        var database = "CodexSaleDeletionTest_" + Guid.NewGuid().ToString("N");
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            $"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=true;MultipleActiveResultSets=true").Options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            var patient = new Patient { PatientCode = "DELETE", FullName = "Test", LegacyTotalSales = 500 };
            var category = new ProductCategory { Code = "DELETE", Name = "Test", Status = "ACTIVE" };
            var component = new Product { ProductCode = "PART", Name = "Part", Category = category, CurrentStock = 100, Status = "ACTIVE" };
            var combo = new Product { ProductCode = "COMBO", Name = "Combo", Category = category, IsCombo = true, Status = "ACTIVE" };
            combo.Components.Add(new ComboComponent { ComponentProduct = component, Quantity = 5 });
            var sale = new Sale { SaleCode = "DELETE-CONFIRMED", Patient = patient, TotalAmount = 1000, Status = "Draft", SaleDate = DateTime.UtcNow.AddHours(7), Details = new List<SaleDetail> {
                new() { Product = combo, Quantity = 2, UnitPrice = 450, LineTotal = 900 },
                new() { Product = component, Quantity = 3, UnitPrice = 33, LineTotal = 100 }
            } };
            db.Sales.Add(sale); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var service = new SaleService(db);
            await service.ConfirmAsync(sale.Id); db.ChangeTracker.Clear();
            Check((await db.Products.SingleAsync(p => p.Id == component.Id)).CurrentStock == 87, "Initial deduction failed.");
            Check((await new DashboardService(db).GetStaffRevenueAsync()).TodayRevenue == 1000, "Initial revenue wrong.");
            var config = await db.Set<ComboComponent>().SingleAsync(); config.Quantity = 2;
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            Check(await service.DeleteAsync(sale.Id, "Admin test"), "Deletion failed."); db.ChangeTracker.Clear();
            Check((await db.Products.SingleAsync(p => p.Id == component.Id)).CurrentStock == 100, "Deletion used changed combo recipe instead of original stock out.");
            Check(!await db.Sales.AnyAsync() && !await db.SaleDetails.AnyAsync(), "Sale/details not deleted.");
            Check((await new DashboardService(db).GetStaffRevenueAsync()).TodayRevenue == 0, "Revenue not removed.");
            Check(await db.InventoryTransactions.CountAsync() == 2 && await db.InventoryTransactionDetails.Where(d => d.InventoryTransaction.TransactionType == "StockIn").SumAsync(d => d.Quantity) == 13, "Stock reversal audit incorrect.");
            Check(!await service.DeleteAsync(sale.Id) && (await db.Products.SingleAsync(p => p.Id == component.Id)).CurrentStock == 100, "Repeated deletion returned stock twice.");
            foreach (var status in new[] { "Draft", "Imported" })
            {
                var other = new Sale { SaleCode = "DELETE-" + status, PatientId = patient.Id, Status = status, TotalAmount = 30,
                    Details = new List<SaleDetail> { new() { ProductId = component.Id, Quantity = 3, UnitPrice = 10, LineTotal = 30 } } };
                db.Sales.Add(other); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
                Check(await service.DeleteAsync(other.Id), "Draft/imported deletion failed."); db.ChangeTracker.Clear();
            }
            Check((await db.Products.SingleAsync(p => p.Id == component.Id)).CurrentStock == 100 && await db.InventoryTransactions.CountAsync() == 2 && (await db.Patients.SingleAsync()).LegacyTotalSales == 500, "Draft/history deletion changed stock or opening balance.");
            var corrupt = new Sale { SaleCode = "MISSING-AUDIT", PatientId = patient.Id, Status = "Confirmed", Details = new List<SaleDetail> { new() { ProductId = component.Id, Quantity = 1, UnitPrice = 10, LineTotal = 10 } } };
            db.Sales.Add(corrupt); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            try { await service.DeleteAsync(corrupt.Id); throw new Exception("Missing stock audit accepted."); } catch (InvalidOperationException) { }
            db.ChangeTracker.Clear();
            Check((await db.Sales.SingleAsync(s => s.Id == corrupt.Id)).Status == "Confirmed" && (await db.Products.SingleAsync(p => p.Id == component.Id)).CurrentStock == 100, "Failed deletion did not roll back.");
            Console.WriteLine("Passed: Admin deletion, exact original combo/normal stock reversal after recipe changes, revenue removal, audit retained, repeat deletion, drafts/history without restocking, preserved old customer total, failed deletion rollback.");
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != database || !database.StartsWith("CodexSaleDeletionTest_")) throw new Exception("Unexpected cleanup target.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
