using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using ClinicManagementSystem.Web.Services;
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

var root = Path.GetFullPath("ClinicManagementSystem.Web");
var reader = new KiotVietCustomerReader(new VietnamAddressService(new TestEnvironment(root)));
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
MemoryStream Workbook(string[][] rows, bool date1904 = false)
{
    XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    var stream = new MemoryStream();
    using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
    {
        void Write(string path, XDocument xml) { using var output = zip.CreateEntry(path).Open(); xml.Save(output); }
        Write("xl/workbook.xml", new XDocument(new XElement(ns + "workbook",
            new XElement(ns + "workbookPr", new XAttribute("date1904", date1904 ? "1" : "0")),
            new XElement(ns + "sheets", new XElement(ns + "sheet", new XAttribute("name", "Customers"), new XAttribute(rel + "id", "rId1"))))));
        Write("xl/_rels/workbook.xml.rels", new XDocument(new XElement("Relationships",
            new XElement("Relationship", new XAttribute("Id", "rId1"), new XAttribute("Target", "worksheets/sheet1.xml")))));
        Write("xl/worksheets/sheet1.xml", new XDocument(new XElement(ns + "worksheet", new XElement(ns + "sheetData",
            rows.Select((values, i) => new XElement(ns + "row", new XAttribute("r", i + 1),
                values.Select((value, j) => new XElement(ns + "c", new XAttribute("r", (char)('A' + j) + (i + 1).ToString()),
                    new XAttribute("t", "inlineStr"), new XElement(ns + "is", new XElement(ns + "t", value))))))))));
    }
    stream.Position = 0;
    return stream;
}
var headers = new[] { "Mã khách hàng", "Tên khách hàng", "Điện thoại", "Tổng bán", "Ngày sinh", "Khu vực giao hàng", "Phường/Xã", "Trạng thái", "Ghi chú" };
using (var file = Workbook(new[] { headers, new[] { "kh000001", "Nguyễn An", "0900123456", "123456.50", "01/02/1990", "Thành phố Hà Nội", "Phường Ba Đình", "1", "Ghi chú" } }))
{
    var row = reader.Read(file).Single();
    Check(row.Errors.Count == 0, string.Join(";", row.Errors));
    Check(row.Patient.PatientCode == "kh000001" && row.Patient.PhoneNumber == "0900123456", "Identifiers or phone zeroes were changed.");
    Check(row.Patient.LegacyTotalSales == 123456.50m && row.Patient.DateOfBirth == new DateOnly(1990, 2, 1), "Money or date parsing failed.");
    Check(row.Patient.ProvinceCode == 1 && row.Patient.WardCode == 4, "New address mapping failed.");
}
using (var file = Workbook(new[] { headers,
    new[] { "KH001", "An", "", "10", "", "Tỉnh cũ", "Xã cũ", "0", "" },
    new[] { "kh001", "Bình", "", "20", "", "", "", "1", "" } }))
{
    var rows = reader.Read(file);
    Check(rows.All(r => r.Errors.Count == 1 && !r.CanImport), "Duplicate codes must block both rows.");
    Check(rows[0].Patient.Address == "Xã cũ, Tỉnh cũ" && rows[0].Patient.ProvinceCode == null && rows[0].Patient.Status == "Inactive", "Legacy address/status lost.");
}
using (var file = Workbook(new[] { headers, new[] { "KH002", "An", "", "-1", "31/02/2020", "", "", "1", "" } }))
    Check(reader.Read(file).Single().Errors.Count == 2, "Invalid amount/date accepted.");
using (var file = Workbook(new[] { headers, new[] { "KH004", "An", "0900123456 / 0912345678", "10", "01/01/2959", "", "", "1", "Original note" } }))
{
    var row = reader.Read(file).Single();
    Check(row.Errors.Count == 0 && row.Patient.PhoneNumber == "0900123456", "Multiple phone numbers were not parsed.");
    Check(row.Patient.DateOfBirth == null && row.Patient.Notes!.Contains("01/01/2959") && row.Patient.Notes.Contains("0900123456 / 0912345678") && row.Patient.Notes.Contains("Original note"), "Original imported data was lost.");
}
var birthday = new DateTime(1990, 2, 1);
foreach (var date1904 in new[] { false, true })
{
    using var file = Workbook(new[] { headers, new[] { "KH003", "An", "", "100", (birthday.ToOADate() - (date1904 ? 1462 : 0)).ToString(CultureInfo.InvariantCulture), "", "", "1", "" } }, date1904);
    Check(reader.Read(file).Single().Patient.DateOfBirth == DateOnly.FromDateTime(birthday), "Excel date epoch is incorrect.");
}
try
{
    using var file = Workbook(new[] { new[] { "Tên khách hàng" }, new[] { "An" } });
    reader.Read(file); throw new Exception("Missing headers accepted.");
}
catch (InvalidDataException) { }
Check(PatientCodeGenerator.Next(new[] { "KH006962", "BN000999" }) == "LT000001", "Legacy codes changed the new sequence.");
Check(PatientCodeGenerator.Next(new[] { "LT000002", "LT000010", "LT999999", "KH999999" }) == "LT1000000", "Numeric sequence/collision boundary failed.");
Check(PatientCodeGenerator.Next(new[] { "lt000123", "LT_BAD" }) == "LT000124", "Case-insensitive sequence failed.");
Console.WriteLine("Passed: code preservation, phone zeroes, old sales, address mapping/fallback, duplicate codes, invalid values, two date epochs, required headers and LT sequence.");
if (args.FirstOrDefault() == "--combo") await ComboTests.RunAsync();
if (args.FirstOrDefault() == "--copy") await SaleCopyTests.RunAsync();
if (args.FirstOrDefault() == "--roles") await DashboardRoleTests.RunAsync();
if (args.FirstOrDefault() == "--products") await ProductImportTests.RunAsync(args[1]);
if (args.FirstOrDefault() == "--sales")
{
    var invoiceHeaders = new[] { "Mã hóa đơn", "Thời gian", "Mã khách hàng", "Tên khách hàng", "Tổng tiền hàng", "Giảm giá hóa đơn", "Thu khác", "Mã hàng", "Tên hàng", "Số lượng", "Giá bán", "Thành tiền" };
    var line = new[] { "HD-TEST", "12/10/2026", "KH01{DEL}", "Test", "20", "1", "2", "P01", "Product", "1", "10", "10" };
    var saleReader = new KiotVietSaleReader();
    using (var file = Workbook(new[] { invoiceHeaders, line, line }))
    {
        var invoice = saleReader.Read(file, "test.xlsx").Single();
        Check(invoice.Errors.Count == 0 && invoice.Sale.Details.Count == 2 && invoice.Sale.TotalAmount == 21 && invoice.PatientCode == "KH01{DEL}", "Repeated product lines, DEL codes or discounts lost.");
    }
    var conflicting = (string[])line.Clone(); conflicting[2] = "OTHER";
    using (var file = Workbook(new[] { invoiceHeaders, line, conflicting }))
        Check(saleReader.Read(file, "test.xlsx").Single().Errors.Count > 0, "Conflicting invoice headers accepted.");
    using (var file = Workbook(new[] { invoiceHeaders, line }))
        Check(saleReader.Read(file, "test.xlsx").Single().Errors.Count > 0, "Unbalanced invoice accepted.");
    var invalid = (string[])line.Clone(); invalid[9] = "0";
    using (var file = Workbook(new[] { invoiceHeaders, invalid, line }))
        Check(saleReader.Read(file, "test.xlsx").Single().Errors.Count > 0, "Zero quantity accepted.");
    await SaleImportTests.RunAsync(args.Skip(1).ToArray());
}
if (args.Length > 0 && args[0] is not ("--integration" or "--localdb" or "--combo" or "--products" or "--sales" or "--copy" or "--roles"))
{
    using var file = File.OpenRead(args[0]);
    var rows = reader.Read(file);
    Console.WriteLine($"File check: {rows.Count} customers; {rows.Count(r => r.Errors.Count > 0)} invalid; {rows.Count(r => r.Warnings.Count > 0)} with warnings; old sales {rows.Sum(r => r.Patient.LegacyTotalSales):0.00}.");
    foreach (var row in rows.Where(r => r.Errors.Count > 0).Take(20)) Console.WriteLine($"Excel row {row.RowNumber}: {string.Join("; ", row.Errors)}");
    Check(rows.Count == 4946 && rows.All(r => r.Errors.Count == 0), "Provided KiotViet file contains unexpected or invalid rows.");
    Check(rows.Sum(r => r.Patient.LegacyTotalSales) == 4046552137m, "Provided workbook total did not reconcile.");
}
if (args.FirstOrDefault() == "--localdb")
{
    var original = @"Server=(localdb)\mssqllocaldb;Database=ClinicManagementSystem;Trusted_Connection=true;MultipleActiveResultSets=true";
    var savedPath = Environment.GetEnvironmentVariable("PATH");
    string resolvedString;
    try
    {
        // Prove the active-engine path works even when the CLI cannot be found.
        Environment.SetEnvironmentVariable("PATH", "");
        resolvedString = await DevelopmentLocalDbConnection.ResolveAsync(original);
    }
    finally { Environment.SetEnvironmentVariable("PATH", savedPath); }
    var resolved = new SqlConnectionStringBuilder(resolvedString);
    Check(resolved.DataSource.StartsWith(@"np:\\.\pipe\LOCALDB#") && resolved.InitialCatalog == "ClinicManagementSystem" && resolved.IntegratedSecurity, "LocalDB resolver changed database or authentication.");
    await using var connection = new SqlConnection(resolved.ConnectionString);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT DB_NAME()";
    Check((string?)await command.ExecuteScalarAsync() == "ClinicManagementSystem", "Resolved connection opened a different database.");
    var remote = "Server=example.test;Database=Example;Integrated Security=true";
    Check(await DevelopmentLocalDbConnection.ResolveAsync(remote) == remote, "Non-LocalDB connection was altered.");
    Console.WriteLine("Passed: dynamic LocalDB pipe opens the existing database; original Windows authentication and database retained; non-LocalDB configuration unchanged.");
}
if (args.FirstOrDefault() == "--integration")
{
    // Every write is confined to a fresh test database, never the application's database.
    var database = "CodexPatientImportTest_" + Guid.NewGuid().ToString("N");
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=true;MultipleActiveResultSets=true").Options;
    await using var context = new ApplicationDbContext(options);
    using var cache = new MemoryCache(new MemoryCacheOptions());
    try
    {
        await context.Database.EnsureCreatedAsync();
        var existing = new Patient { PatientCode = "LT000010", FullName = "Existing", LegacyTotalSales = 20m };
        context.Patients.Add(existing);
        await context.SaveChangesAsync();
        var importer = new PatientImportService(context, reader, cache);
        var rows = new[] { headers,
            new[] { "LT000010", "Must not overwrite", "", "999", "", "", "", "1", "" },
            new[] { "kh006962", "Imported", "0900123456", "15", "", "", "", "1", "" } };
        using var file = Workbook(rows);
        var preview = await importer.PreviewAsync(file, "customers.xlsx", "owner");
        Check(preview.NewCount == 1 && preview.ExistingCount == 1 && preview.TotalLegacySales == 15m, "Duplicate preview is incorrect.");
        Check(await context.Patients.CountAsync() == 1, "Preview wrote patient data.");
        Check(!(await importer.ImportAsync(preview.Token!, "someone-else")).Success, "Preview owner isolation failed.");
        var result = await importer.ImportAsync(preview.Token!, "owner");
        Check(result.Success && result.Imported == 1 && result.Skipped == 1, "Import counts are incorrect.");
        Check(!(await importer.ImportAsync(preview.Token!, "owner")).Success, "Confirmation replay was accepted.");
        file.Position = 0;
        var repeat = await importer.PreviewAsync(file, "customers.xlsx", "owner");
        var repeated = await importer.ImportAsync(repeat.Token!, "owner");
        Check(repeated.Success && repeated.Imported == 0 && repeated.Skipped == 2, "Repeat import duplicated customers.");
        context.ChangeTracker.Clear();
        var original = await context.Patients.SingleAsync(p => p.PatientCode == "LT000010");
        Check(original.FullName == "Existing" && original.LegacyTotalSales == 20m, "Existing profile was overwritten.");
        var imported = await context.Patients.SingleAsync(p => p.PatientCode == "kh006962");
        Check(imported.LegacyTotalSales == 15m && imported.LegacyImportedAt != null && imported.PatientCode == "kh006962", "Imported code/total changed.");
        context.Sales.AddRange(
            new Sale { SaleCode = "Confirmed1", PatientId = imported.Id, Status = "Confirmed", TotalAmount = 7m },
            new Sale { SaleCode = "Draft1", PatientId = imported.Id, Status = "Draft", TotalAmount = 100m });
        await context.SaveChangesAsync();
        var service = new PatientService(context);
        Check((await service.GetAllAsync("kh006962", null)).Single().TotalSales == 22m, "Legacy + confirmed sales includes drafts or omits money.");
        var details = await service.GetDetailsAsync(imported.Id);
        Check(details!.TotalSales == 22m && details.LegacyTotalSales == 15m && details.CurrentTotalSales == 7m, "Details totals are incorrect.");
        var edit = await service.GetByIdAsync(imported.Id);
        edit!.FullName = "Edited";
        Check((await service.UpdateAsync(imported.Id, edit)).Success, "Imported customer edit failed.");
        context.ChangeTracker.Clear();
        imported = await context.Patients.SingleAsync(p => p.Id == imported.Id);
        Check(imported.PatientCode == "kh006962" && imported.LegacyTotalSales == 15m, "Editing changed the original code or old sales.");
        Check(await service.GeneratePatientCodeAsync() == "LT000011", "New codes continued the KiotViet sequence.");
        using var invalid = Workbook(new[] { headers, new[] { "KH_BAD", "An", "", "-1", "", "", "", "1", "" } });
        var badPreview = await importer.PreviewAsync(invalid, "bad.xlsx", "owner");
        Check(!(await importer.ImportAsync(badPreview.Token!, "owner")).Success && await context.Patients.CountAsync() == 2, "Invalid preview partially imported records.");
        Console.WriteLine("Passed LocalDB integration: preview has no writes, ownership, duplicate/repeat import, unchanged original profiles, replay protection, exact totals excluding drafts, edits retain code and historical total, new LT sequence, invalid-file atomicity.");
    }
    finally
    {
        var actualDatabase = context.Database.GetDbConnection().Database;
        if (actualDatabase != database || !database.StartsWith("CodexPatientImportTest_"))
            throw new Exception("Refusing to clean up an unexpected database.");
        await context.Database.EnsureDeletedAsync();
    }
}

sealed class TestEnvironment(string root) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "PatientImportTests";
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = root;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
