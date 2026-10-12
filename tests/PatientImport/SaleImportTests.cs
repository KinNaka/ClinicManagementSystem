using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

static class SaleImportTests
{
    public static async Task RunAsync(string[] files)
    {
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        var database = "CodexSaleImportTest_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            $"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=true;MultipleActiveResultSets=true", sql => sql.CommandTimeout(180)).Options;
        await using var db = new ApplicationDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var streams = files.Select(File.OpenRead).ToArray();
        try
        {
            await db.Database.EnsureCreatedAsync();
            var downloads = Path.GetDirectoryName(files[0])!;
            using var customers = File.OpenRead(Path.Combine(downloads, "DSKH.xlsx"));
            var customerReader = new KiotVietCustomerReader(new VietnamAddressService(new TestEnvironment(Path.GetFullPath("ClinicManagementSystem.Web"))));
            db.Patients.AddRange(customerReader.Read(customers).Select(r => r.Patient));
            var category = new ProductCategory { Code = "TEST", Name = "Test", Status = "ACTIVE" };
            using var products = File.OpenRead(Path.Combine(downloads, "DanhSachSanPham_KV12102026-211844-050.xlsx"));
            foreach (var row in new KiotVietProductReader().Read(products, true, true))
            {
                row.Product.Category = category;
                db.Products.Add(row.Product);
            }
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var stock = await db.Products.ToDictionaryAsync(p => p.ProductCode, p => p.CurrentStock);
            var legacy = await db.Patients.SumAsync(p => p.LegacyTotalSales);
            var importer = new SaleImportService(db, new KiotVietSaleReader(), cache);
            var preview = await importer.PreviewAsync(streams.Select((s, i) => ((Stream)s, files[i])), "owner", false, true);
            foreach (var invoice in preview.Invoices.Where(i => i.Errors.Count > 0).Take(10))
                Console.WriteLine($"Source row {invoice.RowNumber}: {string.Join("; ", invoice.Errors)}");
            Check(preview.Invoices.Count == 14801 && preview.ErrorCount == 0, "Source invoice count or validation failed.");
            Check(preview.Invoices.Sum(i => i.Sale.Details.Count) == 76466, "Detail row count changed.");
            Check(preview.Invoices.Sum(i => i.Sale.TotalAmount) == 4141127842m, "Source totals did not reconcile.");
            Check(preview.MissingPatientCodes.Count == 62 && preview.MissingProductCodes.Count == 51 && preview.WalkInCount == 75, "Missing-reference counts changed.");
            Check(!preview.CanImport && !(await importer.ImportAsync(preview.Token!, "owner")).Success && !await db.Sales.AnyAsync(), "Strict reference policy wrote records.");
            Check(!(await importer.ImportAsync(preview.Token!, "other-owner")).Success, "Owner isolation failed.");
            // Use the same cached preview to exercise the explicitly enabled missing-reference policy.
            preview.CreateMissingRecords = true;
            Check(preview.CanImport, "Allowed preview was blocked.");
            Console.WriteLine("Validated all four files. Importing 14,801 invoices into isolated test database...");
            var result = await importer.ImportAsync(preview.Token!, "owner");
            Check(result.Success && result.Imported == 14801, "Import failed: " + result.Error);
            db.ChangeTracker.Clear();
            Check(await db.Sales.CountAsync() == 14801 && await db.SaleDetails.CountAsync() == 76466, "Stored counts incorrect.");
            Check(await db.Sales.SumAsync(s => s.TotalAmount) == preview.Total && await db.SaleDetails.SumAsync(d => d.Quantity) == preview.Invoices.Sum(i => i.Sale.Details.Sum(d => d.Quantity)), "Stored money or quantities changed.");
            Check(await db.Sales.AllAsync(s => s.Status == "Imported") && !await db.InventoryTransactions.AnyAsync(), "Historical import changed status or inventory.");
            Check(await db.Patients.SumAsync(p => p.LegacyTotalSales) == legacy && !await db.Sales.AnyAsync(s => s.Status == "Confirmed"), "Historical totals were counted twice.");
            foreach (var product in await db.Products.AsNoTracking().ToListAsync())
                Check(stock.TryGetValue(product.ProductCode, out var original) ? product.CurrentStock == original : product.CurrentStock == 0 && product.Status == "INACTIVE", "Stock or placeholder status changed.");
            Check(await db.Patients.CountAsync() == 4946 + 62 + 1, "Customer linking created extra customers.");
            var service = new SaleService(db);
            var first = await service.GetAllAsync(null, "Imported");
            var last = await service.GetAllAsync(null, "Imported", int.MaxValue);
            Check(first.Sales.Count == 50 && first.TotalCount == 14801 && last.Sales.Count == 1 && last.Page == 297, "Pagination failed.");
            var id = first.Sales[0].Id;
            var storedSale = await db.Sales.Include(s => s.Details).FirstAsync(s => s.Id == id);
            var history = await new PatientService(db).GetDetailsAsync(storedSale.PatientId);
            Check(history != null && history.PurchaseHistory.TotalCount > 0 && history.CurrentTotalSales == 0, "Linked customer history or totals incorrect.");
            var productToRename = await db.Products.FindAsync(storedSale.Details.First().ProductId);
            productToRename!.Name = "Renamed after import";
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var display = (await service.GetByIdAsync(id))!;
            Check(display.Details.All(d => d.ProductName != "Renamed after import") && display.TotalAmount == storedSale.TotalAmount, "Historical product snapshot changed with catalog.");
            try { await service.ConfirmAsync(id); throw new Exception("Archived invoice was confirmed."); } catch (InvalidOperationException) { }
            Check(!(await importer.ImportAsync(preview.Token!, "owner")).Success, "Confirmation replay accepted.");
            foreach (var stream in streams) stream.Position = 0;
            var repeat = await importer.PreviewAsync(streams.Select((s, i) => ((Stream)s, files[i])), "owner", true, true);
            var repeated = await importer.ImportAsync(repeat.Token!, "owner");
            Check(repeated.Success && repeated.Imported == 0 && repeated.Skipped == 14801 && await db.Sales.CountAsync() == 14801, "Reimport duplicated invoices.");
            Console.WriteLine("Passed: all four files, exact amounts and rows, customer/product links, strict missing-reference blocking, owner isolation, no stock/totals duplication, inactive placeholders, pagination, read-only history and repeat import.");
        }
        finally
        {
            foreach (var stream in streams) stream.Dispose();
            if (db.Database.GetDbConnection().Database != database || !database.StartsWith("CodexSaleImportTest_")) throw new Exception("Unexpected cleanup target.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
