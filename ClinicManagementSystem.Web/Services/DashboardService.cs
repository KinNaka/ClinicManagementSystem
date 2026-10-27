
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Web.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            // Ngày hiện tại theo múi giờ Việt Nam.
            var vietnamTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    "SE Asia Standard Time");

            var now = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow, vietnamTimeZone);

            var today = now.Date;
            var tomorrow = today.AddDays(1);
            var sevenDaysAgo = today.AddDays(-6);

            // SaleDate được giả định lưu theo giờ Việt Nam.
            // Nếu lưu UTC, cần chuyển các mốc ngày sang UTC.

            var model = new DashboardViewModel
            {
                TotalPatients = await _context.Patients
                    .AsNoTracking()
                    .CountAsync(),

                TotalProducts = await _context.Products
                    .AsNoTracking()
                    .CountAsync(),

                LowStockProducts = await _context.Products
                    .AsNoTracking()
                    .CountAsync(p =>
                        (p.Status == "Active" || p.Status == "ACTIVE") &&
                        !p.IsCombo && !p.IsService && p.CurrentStock <= p.MinimumStock),

                TodaySalesCount = await _context.Sales
                    .AsNoTracking()
                    .CountAsync(s =>
                        s.Status == "Confirmed" &&
                        s.SaleDate >= today &&
                        s.SaleDate < tomorrow),

                TodayRevenue = await _context.Sales
                    .AsNoTracking()
                    .Where(s =>
                        s.Status == "Confirmed" &&
                        s.SaleDate >= today &&
                        s.SaleDate < tomorrow)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0
            };

            var revenueData = await _context.Sales
                .AsNoTracking()
                .Where(s =>
                    s.Status == "Confirmed" &&
                    s.SaleDate >= sevenDaysAgo &&
                    s.SaleDate < tomorrow)
                .GroupBy(s => s.SaleDate!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(s => s.TotalAmount)
                })
                .ToListAsync();

            model.RevenueLast7Days =
                Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var date = sevenDaysAgo.AddDays(i);

                        return new DailyRevenueViewModel
                        {
                            Date = date,
                            Revenue = revenueData
                                .Where(r => r.Date == date)
                                .Sum(r => r.Revenue)
                        };
                    })
                    .ToList();

            return model;
        }

        public async Task<StaffRevenueViewModel> GetStaffRevenueAsync(DateOnly? date = null, ListPaging? paging = null)
        {
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).Date;
            var tomorrow = today.AddDays(1);
            var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            var daily = await _context.Sales.AsNoTracking()
                .Where(s => s.Status == "Confirmed" && s.SaleDate >= weekStart && s.SaleDate < tomorrow)
                .GroupBy(s => s.SaleDate!.Value.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(s => s.TotalAmount) })
                .ToListAsync();
            var selectedDate = date ?? DateOnly.FromDateTime(today);
            var sales = RevenueSalesForDay(_context.Sales.AsNoTracking(), selectedDate);
            var selectedRevenue = await sales.SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
            paging ??= new ListPaging();
            var page = await paging.ApplyAsync(sales.OrderBy(s => s.SaleDate).ThenBy(s => s.Id));
            return new StaffRevenueViewModel {
                SelectedDate = selectedDate, SelectedRevenue = selectedRevenue, SelectedSalesCount = paging.TotalCount,
                Sales = await page.Select(s => new RevenueSaleViewModel {
                    Id = s.Id, SaleCode = s.SaleCode, SaleDate = s.SaleDate!.Value,
                    PatientName = s.SourcePatientName ?? s.Patient.FullName, TotalAmount = s.TotalAmount
                }).ToListAsync(),
                Today = today, WeekStart = weekStart,
                TodayRevenue = daily.Where(d => d.Date == today).Sum(d => d.Revenue),
                WeekRevenue = daily.Sum(d => d.Revenue)
            };
        }
        // SaleDate is the Vietnam-local confirmation time, not CreatedAt or UTC.
        public static IQueryable<ClinicManagementSystem.Web.Models.Entities.Sale> RevenueSalesForDay(
            IQueryable<ClinicManagementSystem.Web.Models.Entities.Sale> sales, DateOnly date)
        {
            if (date == DateOnly.MaxValue) throw new ArgumentOutOfRangeException(nameof(date));
            var start = date.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);
            return sales.Where(s => s.Status == "Confirmed" && s.SaleDate >= start && s.SaleDate < end);
        }
    }
}
