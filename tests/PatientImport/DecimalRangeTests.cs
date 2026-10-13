using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ClinicManagementSystem.Web.Models.ViewModels.Sales;

internal static class DecimalRangeTests
{
    public static void Run()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "vi-VN", "en-US", "de-DE" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                foreach (var type in typeof(SaleDetailFormViewModel).Assembly.GetTypes())
                foreach (var property in type.GetProperties())
                foreach (var range in property.GetCustomAttributes(typeof(RangeAttribute), true).Cast<RangeAttribute>())
                {
                    if (range.OperandType != typeof(decimal)) continue;
                    var minimum = decimal.Parse(range.Minimum.ToString()!, CultureInfo.InvariantCulture);
                    var maximum = decimal.Parse(range.Maximum.ToString()!, CultureInfo.InvariantCulture);
                    if (!range.IsValid(minimum) || !range.IsValid(maximum)
                        || range.IsValid(minimum - 0.001m) || range.IsValid(maximum + 0.001m))
                        throw new Exception($"Decimal range failed for {type.Name}.{property.Name} under {culture}.");
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
        Console.WriteLine("Passed: decimal range boundaries under vi-VN, en-US and de-DE.");
    }
}
