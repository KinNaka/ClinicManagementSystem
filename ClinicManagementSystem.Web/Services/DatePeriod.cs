namespace ClinicManagementSystem.Web.Services;

public static class DatePeriod
{
    public static (DateTime? Start, DateTime? End) Bounds(string? period, DateTime? today = null)
    {
        var date = (today ?? TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time")).Date;
        var month = new DateTime(date.Year, date.Month, 1);
        var year = new DateTime(date.Year, 1, 1);
        return period switch {
            "today" => (date, date.AddDays(1)),
            "yesterday" => (date.AddDays(-1), date),
            "month" => (month, month.AddMonths(1)),
            "lastMonth" => (month.AddMonths(-1), month),
            "year" => (year, year.AddYears(1)),
            "lastYear" => (year.AddYears(-1), year),
            _ => (null, null)
        };
    }
}
