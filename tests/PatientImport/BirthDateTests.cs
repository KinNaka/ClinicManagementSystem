using System.Globalization;
using ClinicManagementSystem.Web.Models.Binding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

static class BirthDateTests
{
    public static async Task RunAsync()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            foreach (var test in new[] { ("01/02/1990", (DateOnly?)new DateOnly(1990, 2, 1), true),
                ("27112003", (DateOnly?)new DateOnly(2003, 11, 27), true), ("31022003", (DateOnly?)null, false),
                ("29/02/2000", (DateOnly?)new DateOnly(2000, 2, 29), true), ("", (DateOnly?)null, true),
                ("31/04/1990", (DateOnly?)null, false), ("29/02/2001", (DateOnly?)null, false),
                ("1990-02-01", (DateOnly?)null, false), ("02/25/1990", (DateOnly?)null, false) })
            {
                var context = DefaultModelBindingContext.CreateBindingContext(new ActionContext { HttpContext = new DefaultHttpContext() },
                    new QueryStringValueProvider(BindingSource.Query, new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { ["DateOfBirth"] = test.Item1 }), CultureInfo.InvariantCulture),
                    new EmptyModelMetadataProvider().GetMetadataForType(typeof(DateOnly?)), null, "DateOfBirth");
                context.ModelName = "DateOfBirth";
                await new DayMonthYearDateBinder().BindModelAsync(context);
                if ((context.ModelState.ErrorCount == 0) != test.Item3 || (test.Item3 && (!context.Result.IsModelSet || (DateOnly?)context.Result.Model != test.Item2)))
                    throw new Exception("Birthdate parsing failed: " + test.Item1);
            }
            Console.WriteLine("Passed: dd/MM/yyyy under US culture, day/month ambiguity, leap years, invalid dates and optional blank value.");
        }
        finally { CultureInfo.CurrentCulture = saved; }
    }
}
