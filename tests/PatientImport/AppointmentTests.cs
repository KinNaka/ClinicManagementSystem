using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Reflection;
using ClinicManagementSystem.Web.Controllers;
using ClinicManagementSystem.Web.Data;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Appointments;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

internal static class AppointmentTests
{
    private static void Check(bool valid, string message) { if (!valid) throw new Exception(message); }

    public static async Task RunAsync()
    {
        var form = new AppointmentFormViewModel { MedicationStartDate = new DateOnly(2024, 2, 27), MedicationDays = 3 };
        Check(form.CalculateDate() == new DateOnly(2024, 3, 1), "Leap-day date calculation failed.");
        form.MedicationStartDate = new DateOnly(2026, 12, 29);
        form.MedicationDays = 7;
        Check(form.CalculateDate() == new DateOnly(2027, 1, 5), "Year boundary calculation failed.");
        form.UseCustomDate = true;
        form.AppointmentDate = new DateOnly(2027, 1, 8);
        Check(form.CalculateDate() == form.AppointmentDate, "Custom date was ignored.");
        bool Valid() => Validator.TryValidateObject(form, new ValidationContext(form), new List<ValidationResult>(), true);
        Check(Valid(), "Valid form rejected.");
        form.AppointmentDate = null;
        Check(!Valid(), "Custom date is required.");
        form.AppointmentDate = new DateOnly(2026, 1, 1);
        Check(!Valid(), "Appointment before start date accepted.");
        form.UseCustomDate = false;
        form.MedicationDays = 0;
        Check(!Valid(), "Zero medication days accepted.");
        form.MedicationDays = 3651;
        Check(!Valid(), "Excessive medication days accepted.");

        var capture = new CaptureQueries();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(new OfflineConnection()).AddInterceptors(capture).Options);
        var entity = db.Model.FindEntityType(typeof(FollowUpAppointment))!;
        Check(entity.GetIndexes().Any(i => i.IsUnique && i.Properties.SingleOrDefault()?.Name == "SaleId"), "One appointment per invoice is not enforced.");
        Check(entity.FindProperty("RowVersion")!.IsConcurrencyToken, "Appointment concurrency protection is missing.");
        Check(entity.GetForeignKeys().Single(f => f.Properties[0].Name == "SaleId").DeleteBehavior == DeleteBehavior.SetNull, "Invoice deletion must retain appointment history.");
        var service = new AppointmentService(db);
        var result = await service.GetListAsync(new AppointmentListViewModel
        {
            FromDate = new DateOnly(2026, 10, 16), ToDate = new DateOnly(2026, 10, 16),
            SearchTerm = "Test patient", Status = "Pending", Page = 2
        });
        Check(result.TotalCount == 41 && result.TotalPages == 3 && result.Page == 2, "Appointments must paginate by 20 records, not calendar days.");
        var sql = capture.Sql.Last();
        Check(sql.Contains("OFFSET") && sql.Contains("FETCH NEXT") && sql.Contains("ORDER BY") && sql.Contains("AppointmentDate"), "Appointment paging/sorting did not execute in SQL.");
        Check(sql.Contains(">=") && sql.Contains("<=") && sql.Contains("PhoneNumber") && sql.Contains("Status"), "Date/search/status filters are missing from SQL.");
        Check(capture.PagingValues.SequenceEqual(new[] { 20, 20 }), "The second page must skip and take exactly 20 appointments.");
        await service.GetListAsync(new AppointmentListViewModel { Page = int.MaxValue, Sort = "latest" });
        Check(capture.Sql.Last().Contains("DESC"), "Reverse date sorting failed.");
        Check(capture.PagingValues.SequenceEqual(new[] { 40, 20 }), "Out-of-range page was not clamped.");

        var controller = new AppointmentsController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var index = (AppointmentListViewModel)((ViewResult)await controller.Index(null, null, null, null, null)).Model!;
        Check(index.FromDate == AppointmentService.Today, "First visit must start today.");
        controller.HttpContext.Request.QueryString = new QueryString("?toDate=2026-10-15&status=Pending");
        index = (AppointmentListViewModel)((ViewResult)await controller.Index(null, new DateOnly(2026, 10, 15), null, "Pending", null)).Model!;
        Check(index.FromDate == null, "Overdue filter must include past dates.");
        controller.HttpContext.Request.QueryString = new QueryString("?fromDate=&toDate=");
        index = (AppointmentListViewModel)((ViewResult)await controller.Index(null, null, null, null, null)).Model!;
        Check(index.FromDate == null && index.ToDate == null, "Explicit empty dates must show all dates.");
        var queries = capture.Sql.Count;
        await controller.Index(new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 10), null, null, null);
        Check(!controller.ModelState.IsValid && queries == capture.Sql.Count, "Invalid date ranges should not run a query.");
        Check(typeof(AppointmentsController).GetCustomAttribute<AuthorizeAttribute>()?.Roles == "Admin,Staff", "Appointment permissions are missing.");
        foreach (var method in new[] { "ChangeStatus", "Edit" })
            Check(typeof(AppointmentsController).GetMethods().Any(m => m.Name == method && m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() != null), "Appointment mutation lacks CSRF protection.");
        capture.Appointment = new FollowUpAppointment
        {
            Id = 1, PatientId = 5, SaleId = 12, SaleCode = "HD012", MedicationDays = 7,
            MedicationStartDate = new DateOnly(2026, 10, 16), AppointmentDate = new DateOnly(2026, 10, 23),
            Status = "Pending", RowVersion = new byte[8], CreatedBy = "Clerk", UpdatedBy = "Clerk"
        };
        await service.ChangeStatusAsync(1, "Attended", Convert.ToBase64String(new byte[8]), "Staff test");
        var tracked = db.ChangeTracker.Entries<FollowUpAppointment>().Single().Entity;
        Check(tracked.Status == "Attended" && tracked.AttendedBy == "Staff test" && tracked.AttendedAtUtc.HasValue,
            "Attendance must save status, time and confirming employee together.");
        Check(capture.Sql.Last().Contains("UPDATE [FollowUpAppointments]") && capture.Sql.Last().Contains("[RowVersion] ="),
            "Status update must persist through EF with an optimistic concurrency check.");
        db.ChangeTracker.Clear();
        capture.RejectUpdate = true;
        try { await service.ChangeStatusAsync(1, "Cancelled", Convert.ToBase64String(new byte[8]), "Other staff"); throw new Exception("Stale status write was accepted."); }
        catch (InvalidOperationException ex) { Check(ex.Message.Contains("cập nhật"), "Stale update should ask the employee to reload."); }
        db.ChangeTracker.Clear();
        capture.RejectUpdate = false;
        capture.Appointment.Status = "Attended";
        try { await service.ChangeStatusAsync(1, "Cancelled", Convert.ToBase64String(new byte[8]), "Other staff"); throw new Exception("Completed appointment was cancelled."); }
        catch (InvalidOperationException) { }
        Console.WriteLine("Appointment checks passed: date calculation/validation, 20-record SQL paging, filters, defaults, concurrency schema, invoice history retention and authorization.");
    }

    // Intercept real EF SQL, with no network access or writes to any database.
    private sealed class CaptureQueries : DbCommandInterceptor
    {
        public List<string> Sql { get; } = [];
        public int[] PagingValues { get; private set; } = [];
        public FollowUpAppointment? Appointment { get; set; }
        public bool RejectUpdate { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            var table = new DataTable();
            if (command.CommandText.Contains("UPDATE [FollowUpAppointments]"))
            {
                table.Columns.Add("RowVersion", typeof(byte[]));
                if (!RejectUpdate) table.Rows.Add(new object[] { new byte[] { 0, 0, 0, 0, 0, 0, 0, 2 } });
            }
            else if (Appointment != null && !command.CommandText.Contains("COUNT(*)") && !command.CommandText.Contains("OFFSET"))
            {
                var select = command.CommandText[..command.CommandText.IndexOf("FROM", StringComparison.Ordinal)];
                var names = System.Text.RegularExpressions.Regex.Matches(select, @"\[\w+\]\.\[(?<name>\w+)\]")
                    .Select(m => m.Groups["name"].Value).ToArray();
                foreach (var name in names)
                {
                    var type = typeof(FollowUpAppointment).GetProperty(name)!.PropertyType;
                    table.Columns.Add(name, Nullable.GetUnderlyingType(type) ?? type);
                }
                table.Rows.Add(names.Select(name => typeof(FollowUpAppointment).GetProperty(name)!.GetValue(Appointment) ?? DBNull.Value).ToArray());
            }
            else if (command.CommandText.Contains("COUNT(*)")) { table.Columns.Add("Count", typeof(int)); table.Rows.Add(41); }
            else
            {
                var paging = System.Text.RegularExpressions.Regex.Match(command.CommandText,
                    @"OFFSET\s+(?<skip>@\w+)\s+ROWS\s+FETCH NEXT\s+(?<take>@\w+)");
                PagingValues = paging.Success
                    ? new[] { Convert.ToInt32(command.Parameters[paging.Groups["skip"].Value].Value),
                        Convert.ToInt32(command.Parameters[paging.Groups["take"].Value].Value) }
                    : [];
            }
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }

    private sealed class OfflineConnection : DbConnection
    {
        private ConnectionState state;
        [System.Diagnostics.CodeAnalysis.AllowNull] public override string ConnectionString { get; set; } = "";
        public override string Database => "OfflineAppointments";
        public override string DataSource => "Offline";
        public override string ServerVersion => "16.0";
        public override ConnectionState State => state;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public override void Close() => state = ConnectionState.Closed;
        public override void Open() => state = ConnectionState.Open;
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new Microsoft.Data.SqlClient.SqlCommand();
    }
}
