using ClinicManagementSystem.Web.Controllers;
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.ViewModels.Patients;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

static class QuickPatientTests
{
    public static async Task RunAsync()
    {
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        var database = "CodexQuickPatientTest_" + Guid.NewGuid().ToString("N");
        var connection = await DevelopmentLocalDbConnection.ResolveAsync($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=true");
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            var service = new PatientService(db);
            var addresses = new VietnamAddressService(new TestEnvironment(Path.GetFullPath("ClinicManagementSystem.Web")));
            PatientsController Controller() => new(service, addresses) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
            var name = (ViewResult)await Controller().QuickCreate("Nguyễn Văn An");
            Check(((PatientFormViewModel)name.Model!).FullName == "Nguyễn Văn An" && !await db.Patients.AnyAsync(), "Opening quick form wrote records or lost search name.");
            var phone = (ViewResult)await Controller().QuickCreate("0900111222");
            Check(((PatientFormViewModel)phone.Model!).PhoneNumber == "0900111222", "Phone search not prefilled.");
            var created = (ViewResult)await Controller().QuickCreate(new PatientFormViewModel { PatientCode = "CLIENT-CODE", FullName = "Nguyễn Văn An", DateOfBirth = new DateOnly(2003, 11, 27), PhoneNumber = "0900111222", Status = "Inactive" });
            var result = (QuickPatientCreatedViewModel)created.Model!;
            var saved = await db.Patients.SingleAsync();
            Check(created.ViewName == "QuickCreated" && result.Id == saved.Id && result.Text == "Nguyễn Văn An - 2003 - 0900111222" && saved.PatientCode == "LT000001" && saved.Status == "Active", "Quick create code/status/selection label incorrect.");
            var duplicateController = Controller();
            var duplicate = (ViewResult)await duplicateController.QuickCreate(new PatientFormViewModel { FullName = "Relative", PhoneNumber = "0900111222" });
            Check(duplicate.ViewName == "Create" && !duplicateController.ModelState.IsValid && await db.Patients.CountAsync() == 1, "Duplicate phone bypassed checks.");
            var relative = (ViewResult)await Controller().QuickCreate(new PatientFormViewModel { FullName = "Relative", PhoneNumber = "0900111222", SharedPhoneWithPatientId = saved.Id, SharedPhoneRelationship = "Con" });
            Check(relative.ViewName == "QuickCreated" && await db.Patients.CountAsync() == 2, "Confirmed family member rejected.");
            var futureController = Controller();
            var future = (ViewResult)await futureController.QuickCreate(new PatientFormViewModel { FullName = "Future", DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) });
            Check(future.ViewName == "Create" && !futureController.ModelState.IsValid && await db.Patients.CountAsync() == 2, "Future date bypassed validation.");
            Check(!await db.Sales.AnyAsync(), "Quick patient form created or modified invoices.");
            Console.WriteLine("Passed: embedded form prefills name/phone without writes, automatic code, active patient, selection label, duplicate checks, shared family phone, invalid dates and no invoice mutation.");
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != database || !database.StartsWith("CodexQuickPatientTest_")) throw new Exception("Unexpected cleanup target.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}
