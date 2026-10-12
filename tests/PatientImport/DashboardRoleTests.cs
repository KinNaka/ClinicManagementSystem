using System.Security.Claims;
using ClinicManagementSystem.Web.Controllers;
using ClinicManagementSystem.Web.Models.ViewModels.Dashboard;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

static class DashboardRoleTests
{
    public static async Task RunAsync()
    {
        foreach (var type in new[] { typeof(ProductsController), typeof(ProductCategoriesController) })
            foreach (var action in type.GetMethods().Where(m => m.Name is "Create" or "Edit" or "Delete" or "ToggleStatus"))
                if (!action.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Any(a => a.Roles == "Admin"))
                    throw new Exception("Product/category creation endpoint missing Admin restriction.");
        if (!typeof(SalesController).GetMethod("Delete")!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Any(a => a.Roles == "Admin"))
            throw new Exception("Invoice deletion missing Admin restriction.");
        if (!typeof(ProductImportsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Any(a => a.Roles == "Admin"))
            throw new Exception("Product import missing Admin restriction.");
        var service = new Spy();
        foreach (var action in typeof(InventoryController).GetMethods().Where(m => m.Name == "StockCount"))
            if (!action.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Any(a => a.Roles == "Admin"))
                throw new Exception("Stock adjustment missing Admin restriction.");
        DashboardController Controller(string role) => new(service) { ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test")) }
        } };
        var staff = Controller("Staff");
        if (await staff.Index() is not RedirectToActionResult { ActionName: "Revenue" } || service.FullCalls != 0) throw new Exception("Staff accessed full dashboard data.");
        if (await staff.Revenue() is not ViewResult { Model: StaffRevenueViewModel } || service.FullCalls != 0 || service.StaffCalls != 1) throw new Exception("Staff revenue used full dashboard.");
        if (await Controller("Admin").Index() is not ViewResult { Model: DashboardViewModel } || service.FullCalls != 1) throw new Exception("Admin dashboard restricted.");
        Console.WriteLine("Passed: Staff direct dashboard redirects without querying totals; staff view receives only day/week model; Admin retains full dashboard.");
    }
    private sealed class Spy : IDashboardService
    {
        public int FullCalls, StaffCalls;
        public Task<DashboardViewModel> GetDashboardAsync() { FullCalls++; return Task.FromResult(new DashboardViewModel()); }
        public Task<StaffRevenueViewModel> GetStaffRevenueAsync() { StaffCalls++; return Task.FromResult(new StaffRevenueViewModel()); }
    }
}
