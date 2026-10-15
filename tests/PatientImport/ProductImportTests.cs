using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.IO.Compression;
using System.Xml.Linq;

static class ProductImportTests
{
    public static async Task RunAsync(string fileName)
    {
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        var reader = new KiotVietProductReader();
        using (var invalidFile = Fixture(new[] { "CODE", "CODE", "BAD" }))
        {
            var invalidRows = reader.Read(invalidFile, true, true);
            Check(invalidRows.Count == 3 && invalidRows[0].Errors.Count > 0 && invalidRows[1].Errors.Count > 0 && invalidRows[2].Errors.Count > 0, "Duplicate codes and invalid prices were accepted.");
        }
        using var source = File.OpenRead(fileName);
        var rows = reader.Read(source, true, true);
        if (rows.Any(r => r.SourceStock != decimal.Truncate(r.SourceStock)))
        {
            Check(rows.Where(r => r.SourceStock != decimal.Truncate(r.SourceStock))
                .All(r => r.Errors.Any(e => e.Contains("số nguyên"))), "Fractional source stock must block import without rounding.");
            Console.WriteLine("Passed: legacy file with fractional stock is rejected; database import checks require a file with whole stock quantities.");
            return;
        }
        Check(rows.Count == 467 && rows.All(r => r.Errors.Count == 0), "Provided product file did not parse cleanly.");
        Check(rows.Count(r => r.Product.IsService) == 41 && rows.Count(r => r.Product.IsCombo) == 15, "Product type mapping incorrect.");
        Check(rows.Count(r => r.Product.Unit == null) == 111, "Blank units changed.");
        Check(rows.Single(r => r.Product.ProductCode == "mockhoa").Product.CurrentStock == 0 && rows.Single(r => r.Product.ProductCode == "mockhoa").Product.KiotVietInfo!.Contains("-1"), "Negative-stock policy lost original value.");
        source.Position = 0;
        Check(reader.Read(source, true, false).Single(r => r.Product.ProductCode == "mockhoa").Product.CurrentStock == -1, "Keep-negative-stock option failed.");
        source.Position = 0;
        Check(reader.Read(source, false, false).All(r => r.Product.CurrentStock == 0), "Skip-stock option failed.");
        Check(rows.Where(r => r.Product.IsCombo).All(r => r.Product.Status == "INACTIVE" && r.Product.CurrentStock == 0), "Unconfigured combos must not be sellable.");

        var database = "CodexProductImportTest_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(
            $"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=true;MultipleActiveResultSets=true").Options;
        await using var db = new ApplicationDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        try
        {
            await db.Database.EnsureCreatedAsync();
            var category = new ProductCategory { Code = "EXISTING", Name = rows[0].CategoryName, Status = "ACTIVE" };
            db.ProductCategories.Add(category); await db.SaveChangesAsync();
            var existing = new Product { ProductCode = rows[0].Product.ProductCode, Name = "Keep existing", CategoryId = category.Id, CurrentStock = 42, SellingPrice = 7, Status = "ACTIVE" };
            db.Products.Add(existing); await db.SaveChangesAsync();
            var importer = new ProductImportService(db, reader, cache);
            source.Position = 0;
            var preview = await importer.PreviewAsync(source, Path.GetFileName(fileName), "owner", true, true);
            Check(preview.NewCount == 466 && preview.ExistingCount == 1 && preview.ErrorCount == 0, "Preview duplicate handling incorrect.");
            Check(await db.Products.CountAsync() == 1 && await db.ProductCategories.CountAsync() == 1, "Preview wrote products or categories.");
            using (var badFile = Fixture(new[] { "BAD" }))
            {
                var bad = await importer.PreviewAsync(badFile, "invalid.xlsx", "owner", true, true);
                Check(!(await importer.ImportAsync(bad.Token!, "owner")).Success && await db.Products.CountAsync() == 1, "Invalid file partially imported records.");
            }
            Check(!(await importer.ImportAsync(preview.Token!, "different-owner")).Success, "Preview owner isolation failed.");
            var result = await importer.ImportAsync(preview.Token!, "owner");
            Check(result.Success && result.Imported == 466 && result.Skipped == 1, "Product import failed.");
            db.ChangeTracker.Clear();
            var retained = await db.Products.SingleAsync(p => p.Id == existing.Id);
            Check(retained.CurrentStock == 42 && retained.SellingPrice == 7 && retained.Name == "Keep existing", "Existing product was overwritten.");
            Check(await db.Products.CountAsync() == 467 && await db.ProductCategories.CountAsync() == rows.Select(r => r.CategoryName).Distinct(StringComparer.OrdinalIgnoreCase).Count(), "Products/groups were duplicated or omitted.");
            var whole = rows.First(r => !r.Exists && !r.Product.IsCombo && !r.Product.IsService);
            Check((await db.Products.SingleAsync(p => p.ProductCode == whole.Product.ProductCode)).CurrentStock == whole.Product.CurrentStock, "Whole source stock changed during import.");
            Check(!(await importer.ImportAsync(preview.Token!, "owner")).Success, "Confirmation replay accepted.");
            source.Position = 0;
            var repeat = await importer.PreviewAsync(source, "repeat.xlsx", "owner", true, false);
            var repeated = await importer.ImportAsync(repeat.Token!, "owner");
            Check(repeated.Success && repeated.Imported == 0 && repeated.Skipped == 467 && await db.Products.CountAsync() == 467, "Repeat import duplicated products or stock.");

            var patient = new Patient { PatientCode = "TEST", FullName = "Test" };
            db.Patients.Add(patient); await db.SaveChangesAsync();
            var service = await db.Products.FirstAsync(p => p.IsService);
            var sale = new Sale { SaleCode = "SERVICE", PatientId = patient.Id, Details = new List<SaleDetail> { new() { ProductId = service.Id, Quantity = 1, UnitPrice = 10, LineTotal = 10 } } };
            db.Sales.Add(sale); await db.SaveChangesAsync();
            await new SaleService(db).ConfirmAsync(sale.Id);
            db.ChangeTracker.Clear();
            Check((await db.Sales.SingleAsync(s => s.Id == sale.Id)).Status == "Confirmed" && await db.InventoryTransactions.CountAsync() == 0, "Service-only sale affected inventory.");
            var combo = await db.Products.FirstAsync(p => p.IsCombo);
            var productService = new ProductService(db);
            var form = (await productService.GetByIdAsync(combo.Id))!;
            form.Status = "ACTIVE";
            try { await productService.UpdateAsync(combo.Id, form); throw new Exception("Empty combo activated."); } catch (InvalidOperationException) { }
            form.Components.Add(new() { ComponentProductId = retained.Id, Quantity = 5 });
            Check(await productService.UpdateAsync(combo.Id, form), "Imported combo could not be configured/activated.");
            db.ChangeTracker.Clear();
            var comboSale = new Sale { SaleCode = "CONFIGURED-COMBO", PatientId = patient.Id, Details = new List<SaleDetail> { new() { ProductId = combo.Id, Quantity = 1, UnitPrice = 10, LineTotal = 10 } } };
            db.Sales.Add(comboSale); await db.SaveChangesAsync(); await new SaleService(db).ConfirmAsync(comboSale.Id);
            db.ChangeTracker.Clear();
            Check((await db.Products.SingleAsync(p => p.Id == retained.Id)).CurrentStock == 37, "Imported combo did not deduct 5 component units.");
            Check((await db.Products.SingleAsync(p => p.Id == combo.Id)).KiotVietInfo != null, "Editing imported combo lost source metadata.");
            Console.WriteLine("Passed: 467-row source validation, services/combos, missing units, both negative-stock policies, skip stock, preview has no writes, groups, duplicates/replay, no overwrites, 3-decimal stock, service sales without inventory, configure imported combo and consume 5 units.");
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != database || !database.StartsWith("CodexProductImportTest_")) throw new Exception("Unexpected cleanup target.");
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static MemoryStream Fixture(string[] codes)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var rows = new List<string[]> { new[] { "Loại hàng", "Nhóm hàng(3 Cấp)", "Mã hàng", "Tên hàng", "Giá bán", "Giá vốn", "Tồn kho" } };
        rows.AddRange(codes.Select(c => new[] { "Hàng hóa", "Group", c, "Name", c == "BAD" ? "-10" : "100", "20", "3" }));
        var file = new MemoryStream();
        using (var zip = new ZipArchive(file, ZipArchiveMode.Create, true))
        {
            void Write(string path, XDocument xml) { using var output = zip.CreateEntry(path).Open(); xml.Save(output); }
            Write("xl/workbook.xml", new XDocument(new XElement(ns + "workbook", new XElement(ns + "sheets", new XElement(ns + "sheet", new XAttribute(rel + "id", "rId1"))))));
            Write("xl/_rels/workbook.xml.rels", new XDocument(new XElement("Relationships", new XElement("Relationship", new XAttribute("Id", "rId1"), new XAttribute("Target", "worksheets/sheet1.xml")))));
            Write("xl/worksheets/sheet1.xml", new XDocument(new XElement(ns + "worksheet", new XElement(ns + "sheetData", rows.Select((row, i) =>
                new XElement(ns + "row", new XAttribute("r", i + 1), row.Select((value, j) => new XElement(ns + "c", new XAttribute("r", (char)('A' + j) + (i + 1).ToString()), new XAttribute("t", "inlineStr"), new XElement(ns + "is", new XElement(ns + "t", value))))))))));
        }
        file.Position = 0;
        return file;
    }
}
