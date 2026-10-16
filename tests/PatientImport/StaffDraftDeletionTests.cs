using System.Reflection;
using System.Security.Claims;
using ClinicManagementSystem.Web.Controllers;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

internal static class StaffDraftDeletionTests
{
    public static async Task RunAsync()
    {
        var action = typeof(SalesController).GetMethod(nameof(SalesController.Delete))!;
        var roles = action.GetCustomAttribute<AuthorizeAttribute>()?.Roles?.Split(',');
        if (roles == null || !roles.Contains("Staff") || !roles.Contains("Admin") ||
            action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() == null)
            throw new Exception("Delete must authorize Admin/Staff and retain CSRF protection.");

        foreach (var role in new[] { "Staff", "Admin" })
        {
            var service = DispatchProxy.Create<ISaleService, DeletionProxy>();
            var proxy = (DeletionProxy)service;
            var http = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Name, "Test user"), new Claim(ClaimTypes.Role, role)
                }, "Test"))
            };
            var controller = new SalesController(service)
            {
                ControllerContext = new ControllerContext { HttpContext = http },
                TempData = new TempDataDictionary(http, new MemoryTempDataProvider())
            };
            if (await controller.Delete(42) is not RedirectToActionResult || proxy.DraftOnly != (role == "Staff"))
                throw new Exception("Role-based draft restriction was not passed to the service.");
            if (role == "Staff")
            {
                proxy.Deny = true;
                if (await controller.Delete(42) is not ForbidResult)
                    throw new Exception("Forbidden invoice deletion must return HTTP 403.");
            }
        }
        Console.WriteLine("Passed: Staff draft restriction, Admin access, forbidden response and CSRF protection.");
    }

    public class DeletionProxy : DispatchProxy
    {
        public bool DraftOnly { get; private set; }
        public bool Deny { get; set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name != nameof(ISaleService.DeleteAsync)) throw new NotSupportedException();
            DraftOnly = (bool)args![2]!;
            return Deny ? Task.FromException<bool>(new UnauthorizedAccessException()) : Task.FromResult(true);
        }
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
