using System.Security.Claims;
using ClinicManagementSystem.Web.Controllers;
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static class AccountSecurityTests
{
    public static async Task RunAsync()
    {
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        var database = "CodexAccountSecurityTest_" + Guid.NewGuid().ToString("N");
        var services = new ServiceCollection(); services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={database};Trusted_Connection=true"));
        services.AddIdentityCore<ApplicationUser>(o => o.Password.RequiredLength = 8).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        try
        {
            await db.Database.EnsureCreatedAsync();
            await roles.CreateAsync(new IdentityRole("Admin")); await roles.CreateAsync(new IdentityRole("Staff"));
            var admin = new ApplicationUser { UserName = "admin@test.local", Email = "admin@test.local" };
            var staff = new ApplicationUser { UserName = "staff@test.local", Email = "staff@test.local" };
            Check((await users.CreateAsync(admin, "Original1!")).Succeeded && (await users.CreateAsync(staff, "Original1!")).Succeeded, "Could not create test users.");
            await users.AddToRoleAsync(admin, "Admin"); await users.AddToRoleAsync(staff, "Staff");
            var stamp = admin.SecurityStamp;
            Check(!(await users.ChangePasswordAsync(admin, "Wrong1!", "Replacement2!")).Succeeded && await users.CheckPasswordAsync(admin, "Original1!"), "Wrong old password changed password.");
            Check((await users.ChangePasswordAsync(admin, "Original1!", "Replacement2!")).Succeeded && await users.CheckPasswordAsync(admin, "Replacement2!") && !await users.CheckPasswordAsync(admin, "Original1!") && admin.SecurityStamp != stamp, "Password or security stamp not changed.");
            var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, admin.Id), new Claim(ClaimTypes.Role, "Admin") }, "Test")) };
            var controller = new UserManagementController(users) { ControllerContext = new ControllerContext { HttpContext = context }, TempData = new TempDataDictionary(context, new TempProvider()) };
            await controller.Delete(admin.Id); Check(await users.FindByIdAsync(admin.Id) != null, "Own/Admin account deleted.");
            await controller.Delete(staff.Id); Check(await users.FindByIdAsync(staff.Id) == null && await users.FindByIdAsync(admin.Id) != null, "Staff deletion failed or deleted Admin.");
            Check(await controller.Delete(staff.Id) is NotFoundResult, "Repeated delete incorrect.");
            var both = new ApplicationUser { UserName = "both@test.local" }; await users.CreateAsync(both, "Original1!"); await users.AddToRoleAsync(both, "Staff"); await users.AddToRoleAsync(both, "Admin");
            await controller.Delete(both.Id); Check(await users.FindByIdAsync(both.Id) != null, "Mixed Admin/Staff account deleted.");
            Console.WriteLine("Passed: wrong current password rejected, new password and stamp applied, own/Admin accounts protected, other Staff deleted, mixed roles protected, repeated deletion handled.");
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != database || !database.StartsWith("CodexAccountSecurityTest_")) throw new Exception("Unexpected cleanup target.");
            await db.Database.EnsureDeletedAsync();
        }
    }
    private sealed class TempProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
