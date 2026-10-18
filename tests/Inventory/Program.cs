using ClinicManagementSystem.Web.Services;
var cases = new[] {
    ("today", new DateTime(2026, 1, 1), new DateTime(2026, 1, 2)),
    ("yesterday", new DateTime(2025, 12, 31), new DateTime(2026, 1, 1)),
    ("month", new DateTime(2026, 1, 1), new DateTime(2026, 2, 1)),
    ("lastMonth", new DateTime(2025, 12, 1), new DateTime(2026, 1, 1)),
    ("year", new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)),
    ("lastYear", new DateTime(2025, 1, 1), new DateTime(2026, 1, 1))
};
foreach (var (period, expectedStart, expectedEnd) in cases) {
    var (start, end) = DatePeriod.Bounds(period, new DateTime(2026, 1, 1, 23, 59, 59));
    if (start != expectedStart || end != expectedEnd) throw new Exception($"Invalid bounds for {period}");
    if (!(expectedEnd.AddTicks(-1) >= start && expectedEnd.AddTicks(-1) < end)) throw new Exception("Last instant excluded");
    if (expectedEnd < end) throw new Exception("Next period included");
}
var leap = DatePeriod.Bounds("lastMonth", new DateTime(2024, 3, 10));
if (leap.Start != new DateTime(2024, 2, 1) || leap.End != new DateTime(2024, 3, 1)) throw new Exception("Leap month failed");
if (DatePeriod.Bounds("invalid") != (null, null)) throw new Exception("Invalid filter must mean all dates");
Console.WriteLine("PASS: six periods, year rollover, leap month, exclusive end and unknown period.");
