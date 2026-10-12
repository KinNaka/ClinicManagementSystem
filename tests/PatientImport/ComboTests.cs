using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Products;
using ClinicManagementSystem.Web.Services;
using Microsoft.EntityFrameworkCore;

static class ComboTests
{
    public static async Task RunAsync()
    {
        var database = "CodexComboTest_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            $"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=true;MultipleActiveResultSets=true").Options;
        await using var db = new ApplicationDbContext(options);
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        try
        {
            await db.Database.EnsureCreatedAsync();
            var category = new ProductCategory { Code = "TEST", Name = "Test", Status = "ACTIVE" };
            var patient = new Patient { PatientCode = "TEST", FullName = "Test" };
            db.AddRange(category, patient); await db.SaveChangesAsync();
            var rabies = new Product { ProductCode = "RABIES", Name = "Dại", CategoryId = category.Id, CurrentStock = 30, CostPrice = 10, Status = "ACTIVE" };
            var other = new Product { ProductCode = "OTHER", Name = "Other", CategoryId = category.Id, CurrentStock = 1, Status = "Active" };
            db.AddRange(rabies, other); await db.SaveChangesAsync();
            var productService = new ProductService(db);
            var form = new ProductFormViewModel { ProductCode = "COMBO", Name = "Combo dại", CategoryId = category.Id, IsCombo = true, SellingPrice = 100,
                Components = new() { new() { ComponentProductId = rabies.Id, Quantity = 5 } } };
            Check(await productService.CreateAsync(form), "Combo creation failed.");
            var combo = await db.Products.SingleAsync(p => p.ProductCode == "COMBO");
            Check(combo.CurrentStock == 0 && (await productService.GetByIdAsync(combo.Id))!.Components.Single().Quantity == 5, "Composition persistence failed.");
            Check((await productService.GetAllAsync(null, null, null)).Single(p => p.Id == combo.Id).CurrentStock == 6, "Derived combo availability failed.");
            var saleService = new SaleService(db);
            async Task<Sale> Draft(params (long Id, decimal Qty)[] lines)
            {
                var sale = new Sale { SaleCode = Guid.NewGuid().ToString("N"), PatientId = patient.Id, Details = lines.Select(l => new SaleDetail { ProductId = l.Id, Quantity = l.Qty, UnitPrice = 100, LineTotal = 100 * l.Qty }).ToList() };
                db.Sales.Add(sale); await db.SaveChangesAsync(); return sale;
            }
            var sold = await Draft((combo.Id, 2), (rabies.Id, 3));
            await saleService.ConfirmAsync(sold.Id); db.ChangeTracker.Clear();
            Check((await db.Products.SingleAsync(p => p.Id == rabies.Id)).CurrentStock == 17, "2 combos + 3 retail must consume 13 units.");
            Check(await db.InventoryTransactionDetails.Where(d => d.ProductId == rabies.Id).SumAsync(d => d.Quantity) == 13, "Component stock-out audit incorrect.");
            try { await saleService.ConfirmAsync(sold.Id); throw new Exception("Duplicate confirmation accepted."); } catch (InvalidOperationException) { }
            Check((await db.Products.SingleAsync(p => p.Id == rabies.Id)).CurrentStock == 17, "Repeated confirmation consumed stock.");
            var edit = (await productService.GetByIdAsync(combo.Id))!;
            edit.Components.Add(new() { ComponentProductId = other.Id, Quantity = 2 });
            Check(await productService.UpdateAsync(combo.Id, edit), "Replacing combo components failed.");
            db.ChangeTracker.Clear();
            var insufficient = await Draft((combo.Id, 1));
            try { await saleService.ConfirmAsync(insufficient.Id); throw new Exception("Insufficient component accepted."); } catch (InvalidOperationException) { }
            db.ChangeTracker.Clear();
            Check((await db.Sales.SingleAsync(s => s.Id == insufficient.Id)).Status == "Draft", "Failed confirmation changed invoice status.");
            Check((await db.Products.SingleAsync(p => p.Id == rabies.Id)).CurrentStock == 17 && (await db.Products.SingleAsync(p => p.Id == other.Id)).CurrentStock == 1, "Failed confirmation partially deducted inventory.");
            Check(await db.InventoryTransactions.CountAsync() == 1, "Failed confirmation left audit records.");
            var inventory = new InventoryService(db);
            Check(!await inventory.StockInAsync(new ClinicManagementSystem.Web.ViewModels.Inventory.StockInViewModel {
                Details = new() { new() { ProductId = combo.Id, Quantity = 1, UnitPrice = 0 } }
            }), "Combo accepted direct stock-in instead of component inventory.");
            var invalid = new ProductFormViewModel { IsCombo = true, Components = new() { new() { ComponentProductId = combo.Id, Quantity = 1 } } };
            try { await productService.ValidateComboAsync(invalid, 0); throw new Exception("Nested combo accepted."); } catch (InvalidOperationException) { }
            invalid.Components = new() { new() { ComponentProductId = rabies.Id, Quantity = 0 } };
            try { await productService.ValidateComboAsync(invalid, 0); throw new Exception("Zero component accepted."); } catch (InvalidOperationException) { }
            Console.WriteLine("Passed combo integration: create/edit persistence, derived stock, 2 combos consume 10 units plus retail 3, inventory audit, repeat confirmation protection, insufficient-stock atomic rollback, nested/zero component rejection.");
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != database || !database.StartsWith("CodexComboTest_")) throw new Exception("Unexpected cleanup target.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
