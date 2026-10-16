using System.Security.Claims;
using ClinicManagementSystem.Web.Controllers;
using ClinicManagementSystem.Web.Models.ViewModels.Appointments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

internal static class AppointmentViewTests
{
    public static async Task RunAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(AppointmentsController).Assembly.FullName,
            ContentRootPath = Path.GetFullPath("ClinicManagementSystem.Web")
        });
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(AppointmentsController).Assembly);
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        await using var app = builder.Build();
        app.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");
        using var scope = app.Services.CreateScope();

        async Task<string> Render(string viewName, object model, string query)
        {
            var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            http.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "Razor test"));
            http.Request.Scheme = "http";
            http.Request.Host = new HostString("localhost");
            http.Request.Path = "/Appointments/" + viewName;
            http.Request.QueryString = new QueryString(query);
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Name, "Test staff"), new Claim(ClaimTypes.Role, "Staff")
            }, "Test"));
            var route = new RouteData();
            route.Values["controller"] = "Appointments";
            route.Values["action"] = viewName;
            var action = new ActionContext(http, route, new ActionDescriptor());
            var engine = scope.ServiceProvider.GetRequiredService<ICompositeViewEngine>();
            var view = engine.GetView(null, "~/Views/Appointments/" + viewName + ".cshtml", true);
            if (!view.Success) throw new Exception("Appointment Razor view not found.");
            using var writer = new StringWriter();
            var data = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
            var temp = new TempDataDictionary(http, scope.ServiceProvider.GetRequiredService<ITempDataProvider>());
            await view.View.RenderAsync(new ViewContext(action, view.View, data, temp, writer, new HtmlHelperOptions()));
            return writer.ToString();
        }

        var edit = await Render("Edit", new AppointmentFormViewModel
        {
            SaleId = 12, SaleCode = "HD012", PatientName = "Test patient", PatientCode = "LT001",
            MedicationStartDate = new DateOnly(2026, 10, 16), AppointmentDate = new DateOnly(2026, 10, 23)
        }, "?saleId=12");
        if (!edit.Contains("name=\"__RequestVerificationToken\"") || !edit.Contains("name=\"MedicationDays\"") ||
            !edit.Contains("value=\"2026-10-16\"") || !edit.Contains("appointment-form.js"))
            throw new Exception("Appointment entry form did not render dates, CSRF token or calculation script.");
        var index = await Render("Index", new AppointmentListViewModel
        {
            FromDate = new DateOnly(2026, 10, 16), ToDate = new DateOnly(2026, 10, 16),
            TotalCount = 21, Page = 1, Status = "Pending",
            Items = [new AppointmentRowViewModel { Id = 1, PatientId = 5, SaleId = 12, SaleCode = "HD012",
                PatientName = "Test patient", AppointmentDate = new DateOnly(2026, 10, 16),
                Status = "Pending", Version = Convert.ToBase64String(new byte[8]) }]
        }, "?fromDate=2026-10-16&toDate=2026-10-16&status=Pending&searchTerm=Test&sort=soonest");
        if (!index.Contains("name=\"status\" value=\"Attended\"") || !index.Contains("name=\"version\"") ||
            !index.Contains("page=2") || !index.Contains("searchTerm=Test") || !index.Contains("20 h"))
            throw new Exception("Appointment list did not render attendance actions, 20-record paging or retained filters.");
        Console.WriteLine("Appointment Razor views passed: entry form, date fields, CSRF tokens, status actions and filtered pagination.");
    }
}
