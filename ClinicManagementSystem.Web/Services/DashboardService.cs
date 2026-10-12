
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
                        p.Status == "Active" &&
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
                .GroupBy(s => s.SaleDate.Date)
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
    }
}
