using ClinicManagementSystem.Web.Controllers;
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Dashboard;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

static class RevenueDayTests
{
    // In-memory fixtures and SQL translation only: never opens a database.
    public static async Task RunAsync()
    {
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        foreach (var date in new[] { new DateOnly(2026, 10, 27), new DateOnly(2024, 2, 29), new DateOnly(2026, 12, 31) })
        {
            var start = date.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);
            var rows = new[] {
                new Sale { Id = 1, Status = "Confirmed", SaleDate = start, TotalAmount = 10.25m, CreatedAt = start.AddDays(-5) },
                new Sale { Id = 2, Status = "Confirmed", SaleDate = end.AddTicks(-1), TotalAmount = 20.50m },
                new Sale { Id = 3, Status = "Confirmed", SaleDate = start.AddTicks(-1), TotalAmount = 100m },
                new Sale { Id = 4, Status = "Confirmed", SaleDate = end, TotalAmount = 200m },
                new Sale { Id = 5, Status = "Draft", SaleDate = start, TotalAmount = 300m },
                new Sale { Id = 6, Status = "Imported", SaleDate = start, TotalAmount = 400m },
                new Sale { Id = 7, Status = "Confirmed", SaleDate = null, TotalAmount = 500m }
            }.AsQueryable();
            var selected = DashboardService.RevenueSalesForDay(rows, date).ToList();
            Check(selected.Select(s => s.Id).SequenceEqual(new long[] { 1, 2 }), "Full-day boundaries/status filter incorrect.");
            Check(selected.Sum(s => s.TotalAmount) == 30.75m, "Revenue lost decimal precision or used creation date.");
            Check(!DashboardService.RevenueSalesForDay(rows, date.AddDays(2)).Any(), "Empty day includes unrelated sales.");
        }
        var day = new DateOnly(2026, 10, 27);
        var many = Enumerable.Range(1, 23).Select(i => new Sale {
            Id = i, Status = "Confirmed", SaleDate = day.ToDateTime(TimeOnly.MinValue), TotalAmount = 1.25m
        }).AsQueryable();
        var all = DashboardService.RevenueSalesForDay(many, day).OrderBy(s => s.SaleDate).ThenBy(s => s.Id);
        var paging = new ListPaging(99);
        Check(paging.Slice(all).Count() == 3 && paging.Page == 3 && paging.TotalCount == 23 && all.Sum(s => s.TotalAmount) == 28.75m,
            "Day total or paging bounds incorrect.");
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=unused;Database=RevenueTranslationOnly;Integrated Security=true;TrustServerCertificate=true").Options);
        var sql = DashboardService.RevenueSalesForDay(db.Sales, day).OrderBy(s => s.SaleDate).ThenBy(s => s.Id)
            .Skip(10).Take(10).Select(s => new { s.Id, s.TotalAmount, PatientName = s.SourcePatientName ?? s.Patient.FullName }).ToQueryString();
        Check(sql.Contains("Confirmed") && sql.Contains("OFFSET") && sql.Contains("SaleDate"), "Daily report does not translate to SQL.");
        Check(typeof(DashboardController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()
            .Any(a => a.Roles == "Admin,Staff"), "Revenue role boundary changed.");
        var spy = new Spy();
        var controller = new DashboardController(spy);
        Check(await controller.Revenue("2026-10-27", 2) is ViewResult && spy.Date == day && spy.Paging?.Page == 2,
            "Selected date/page not forwarded.");
        foreach (var invalid in new[] { "2026-02-30", "wrong", "9999-12-31" })
            Check(await controller.Revenue(invalid) is BadRequestObjectResult && spy.Calls == 1, "Invalid date queried revenue.");
        Check(await controller.Revenue() is ViewResult && spy.Date == null, "Default date no longer resolves in service using Vietnam time.");
        Console.WriteLine("Passed: full-day/leap/year boundaries, Confirmed-only, null dates, decimal totals, empty days, pagination, SQL translation, date validation/forwarding and role declaration. No database opened.");
    }

    sealed class Spy : IDashboardService
    {
        public DateOnly? Date;
        public ListPaging? Paging;
        public int Calls;
        public Task<DashboardViewModel> GetDashboardAsync() => throw new Exception("Unexpected dashboard query.");
        public Task<StaffRevenueViewModel> GetStaffRevenueAsync(DateOnly? date = null, ListPaging? paging = null)
        {
            Calls++; Date = date; Paging = paging;
            return Task.FromResult(new StaffRevenueViewModel());
        }
    }
}
