using System.Globalization;
using System.Text.RegularExpressions;

namespace ClinicManagementSystem.Web.Services;

public static class PatientCodeGenerator
{
    public static string Next(IEnumerable<string> existingCodes)
    {
        long maximum = 0;
        foreach (var code in existingCodes)
        {
            if (Regex.IsMatch(code, @"^LT\d{6,}$", RegexOptions.IgnoreCase) &&
                long.TryParse(code.AsSpan(2), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                maximum = Math.Max(maximum, number);
        }
        return "LT" + checked(maximum + 1).ToString("D6", CultureInfo.InvariantCulture);
    }
}
