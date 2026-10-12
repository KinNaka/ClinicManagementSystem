using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Inventory;
using ClinicManagementSystem.Web.Services;
using Microsoft.EntityFrameworkCore;

static class StockCountTests
{
    public static async Task RunAsync()
    {
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        var database = "CodexStockCountTest_" + Guid.NewGuid().ToString("N");
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            $"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=true;MultipleActiveResultSets=true").Options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            var category = new ProductCategory { Code = "TEST", Name = "Test", Status = "ACTIVE" };
            var first = new Product { ProductCode = "A", Name = "First", Category = category, CurrentStock = 10, MinimumStock = 5, CostPrice = 12, Status = "ACTIVE" };
            var second = new Product { ProductCode = "B", Name = "Second", Category = category, CurrentStock = 3, MinimumStock = 5, CostPrice = 20, Status = "ACTIVE" };
            var combo = new Product { ProductCode = "C", Name = "Combo", Category = category, IsCombo = true, Status = "ACTIVE" };
            var serviceProduct = new Product { ProductCode = "D", Name = "Service", Category = category, IsService = true, Status = "ACTIVE" };
            db.Products.AddRange(first, second, combo, serviceProduct); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var service = new InventoryService(db);
            Check((await service.GetStockAsync(null, true)).Single().ProductCode == "B" && (await service.GetStockAsync("First", true)).Count == 0, "Low stock/search filter incorrect.");
            var form = await service.GetStockCountAsync();
            Check(form.Details.Count == 2 && form.Details.All(r => r.CountedStock == null), "Combo/service included or uncounted lines prefilled.");
            form.Details.Single(r => r.ProductId == first.Id).CountedStock = 8.125m;
            form.Details.Single(r => r.ProductId == second.Id).CountedStock = 7.5m;
            Check(await service.AdjustStockAsync(form, "Admin") == 2, "Adjustment count incorrect.");
            db.ChangeTracker.Clear();
            Check((await db.Products.SingleAsync(p => p.Id == first.Id)).CurrentStock == 8.125m && (await db.Products.SingleAsync(p => p.Id == second.Id)).CurrentStock == 7.5m, "Fractional adjustment incorrect.");
            var audit = await db.InventoryTransactions.Include(t => t.Details).SingleAsync();
            Check(audit.TransactionType == "Adjustment" && audit.CreatedBy == "Admin" && audit.Details.Single(d => d.ProductId == first.Id).Quantity == -1.875m && audit.Details.Single(d => d.ProductId == second.Id).Quantity == 4.5m, "Adjustment history/actor/direction incorrect.");
            Check((await db.Products.SingleAsync(p => p.Id == first.Id)).CostPrice == 12 && !await db.Sales.AnyAsync(), "Stock count changed price or revenue.");
            try { await service.AdjustStockAsync(form, "Admin"); throw new Exception("Stale form accepted."); } catch (InvalidOperationException) { }
            var stale = await service.GetStockCountAsync();
            stale.Details.Single(r => r.ProductId == first.Id).CountedStock = 9;
            stale.Details.Single(r => r.ProductId == second.Id).CountedStock = 8;
            stale.Details.Single(r => r.ProductId == second.Id).ExpectedStock = 0;
            try { await service.AdjustStockAsync(stale, "Admin"); throw new Exception("Partially stale form accepted."); } catch (InvalidOperationException) { }
            db.ChangeTracker.Clear();
            Check((await db.Products.SingleAsync(p => p.Id == first.Id)).CurrentStock == 8.125m && await db.InventoryTransactions.CountAsync() == 1, "Partial adjustment failed to roll back.");
            var same = await service.GetStockCountAsync();
            foreach (var row in same.Details) row.CountedStock = row.ExpectedStock;
            Check(await service.AdjustStockAsync(same, "Admin") == 0 && await db.InventoryTransactions.CountAsync() == 1, "Matching count generated adjustments.");
            var zero = await service.GetStockCountAsync(); zero.Details.Single(r => r.ProductId == second.Id).CountedStock = 0;
            Check(await service.AdjustStockAsync(zero, "Admin") == 1, "Zero count or blank skipping failed."); db.ChangeTracker.Clear();
            Check((await db.Products.SingleAsync(p => p.Id == first.Id)).CurrentStock == 8.125m && (await db.Products.SingleAsync(p => p.Id == second.Id)).CurrentStock == 0, "Blank counts overwrote stock.");
            foreach (var invalid in new[] {
                new StockCountRow { ProductId = first.Id, CountedStock = -1 },
                new StockCountRow { ProductId = first.Id, CountedStock = 1.0001m },
                new StockCountRow { ProductId = combo.Id, CountedStock = 1 }
            })
            {
                try { await service.AdjustStockAsync(new StockAdjustmentViewModel { Details = new() { invalid } }, "Admin"); throw new Exception("Invalid count accepted."); } catch (InvalidOperationException) { }
            }
            Console.WriteLine("Passed: low-stock/search filter, combo/service exclusion, blank vs zero counts, signed 3-decimal adjustments, history/actor, unchanged cost/revenue, stale count/replay rejection, atomic rollback, unchanged counts, invalid quantities.");
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != database || !database.StartsWith("CodexStockCountTest_")) throw new Exception("Unexpected cleanup target.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
