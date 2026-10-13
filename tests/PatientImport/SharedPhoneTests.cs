using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Patients;
using ClinicManagementSystem.Web.Services;
using Microsoft.EntityFrameworkCore;

static class SharedPhoneTests
{
    public static async Task RunAsync()
    {
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        var database = "CodexSharedPhoneTest_" + Guid.NewGuid().ToString("N");
        var connection = await DevelopmentLocalDbConnection.ResolveAsync($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=true");
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            var parent = new Patient { PatientCode = "PARENT", FullName = "Parent", PhoneNumber = "0900 111 222", Status = "Active", LegacyTotalSales = 500 };
            var other = new Patient { PatientCode = "OTHER", FullName = "Other", PhoneNumber = "0911222333", Status = "Active" };
            db.Patients.AddRange(parent, other); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var service = new PatientService(db);
            Check((await service.FindByPhoneAsync("+84 900-111-222")).Single().Id == parent.Id, "Formatted/+84 phone did not match.");
            var child = new PatientFormViewModel { PatientCode = "CHILD", FullName = "Child", PhoneNumber = "0900111222", Status = "Active" };
            Check(!(await service.CreateAsync(child)).Success && await db.Patients.CountAsync() == 2, "Unconfirmed duplicate saved.");
            child.SharedPhoneWithPatientId = other.Id; child.SharedPhoneRelationship = "Con";
            Check(!(await service.CreateAsync(child)).Success, "Wrong phone owner accepted.");
            child.SharedPhoneWithPatientId = parent.Id; child.SharedPhoneRelationship = null;
            Check(!(await service.CreateAsync(child)).Success, "Missing relationship accepted.");
            child.SharedPhoneRelationship = "Con";
            Check((await service.CreateAsync(child)).Success, "Confirmed relative rejected."); db.ChangeTracker.Clear();
            var saved = await db.Patients.SingleAsync(p => p.PatientCode == "CHILD");
            Check(saved.SharedPhoneWithPatientId == parent.Id && saved.SharedPhoneRelationship == "Con", "Shared phone relationship not stored.");
            Check((await service.FindByPhoneAsync("0900111222")).Count == 2 && (await service.FindByPhoneAsync("0900111222", saved.Id)).Single().Id == parent.Id, "Multiple matches or self exclusion failed.");
            var edit = (await service.GetByIdAsync(saved.Id))!;
            Check(edit.SharedPhoneOwnerName == "Parent", "Owner display missing.");
            edit.FullName = "Child renamed";
            Check((await service.UpdateAsync(saved.Id, edit)).Success, "Editing relative rejected.");
            edit.SharedPhoneWithPatientId = saved.Id;
            Check(!(await service.UpdateAsync(saved.Id, edit)).Success, "Self relationship accepted.");
            edit.SharedPhoneWithPatientId = parent.Id; edit.PhoneNumber = "0988777666";
            Check(!(await service.UpdateAsync(saved.Id, edit)).Success, "Old confirmation allowed a different phone.");
            edit.SharedPhoneWithPatientId = null; edit.SharedPhoneRelationship = null;
            Check((await service.UpdateAsync(saved.Id, edit)).Success, "Clearing relation for a unique phone failed."); db.ChangeTracker.Clear();
            Check((await db.Patients.SingleAsync(p => p.Id == saved.Id)).SharedPhoneWithPatientId == null && (await db.Patients.SingleAsync(p => p.Id == parent.Id)).LegacyTotalSales == 500, "Owner account or total modified.");
            foreach (var code in new[] { "BLANK1", "BLANK2" })
                Check((await service.CreateAsync(new PatientFormViewModel { PatientCode = code, FullName = code })).Success, "Blank phone wrongly treated as duplicate.");
            Console.WriteLine("Passed: normalized phone/+84 lookup, duplicate confirmation, valid owner/relation, stored family profile, multiple matches/self exclusion, edit, changed-phone invalidation, blank phones and unchanged owner totals.");
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != database || !database.StartsWith("CodexSharedPhoneTest_")) throw new Exception("Unexpected cleanup target.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
