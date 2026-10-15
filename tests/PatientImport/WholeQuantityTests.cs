using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ClinicManagementSystem.Web.Models.Validation;
using ClinicManagementSystem.Web.Models.ViewModels.Sales;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

internal static class WholeQuantityTests
{
    public static async Task RunAsync()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "vi-VN", "en-US", "de-DE" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                foreach (var (raw, expected) in new[] { ("1", 1m), ("1.000", 1m), ("1000", 1000m), ("12.000", 12m) })
                {
                    var context = Context(raw);
                    await new QuantityModelBinder().BindModelAsync(context);
                    if (!context.Result.IsModelSet || (decimal)context.Result.Model! != expected)
                        throw new Exception($"Quantity {raw} changed under {culture}.");
                }
                foreach (var raw in new[] { "1,000", "1.000,00", "1,5", "NaN", "1e3" })
                {
                    var context = Context(raw);
                    await new QuantityModelBinder().BindModelAsync(context);
                    if (context.ModelState.IsValid) throw new Exception($"Ambiguous quantity {raw} accepted.");
                }
                // Verify every editable quantity/stock field carries both validation and the invariant binder.
                foreach (var type in typeof(SaleDetailFormViewModel).Assembly.GetTypes().Where(t => t.Namespace?.Contains("ViewModels") == true))
                foreach (var property in type.GetProperties().Where(p => p.GetCustomAttributes(typeof(WholeQuantityAttribute), true).Length > 0))
                {
                    if (property.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ModelBinderAttribute), true).Length == 0)
                        throw new Exception($"Missing quantity binder: {type.Name}.{property.Name}");
                    var validator = property.GetCustomAttributes(typeof(WholeQuantityAttribute), true).Cast<WholeQuantityAttribute>().Single();
                    if (!validator.IsValid(1.000m) || validator.IsValid(1.5m)) throw new Exception("Fractional quantity validation failed.");
                }
                var results = new List<ValidationResult>();
                var fractional = new SaleDetailFormViewModel { ProductId = 1, SourceDetailId = 123, Quantity = 1.5m };
                if (Validator.TryValidateObject(fractional, new ValidationContext(fractional), results, true))
                    throw new Exception("Copied invoice accepted fractional quantity.");
            }
        }
        finally { CultureInfo.CurrentCulture = previous; }
        Console.WriteLine("Passed: invariant quantity binding (1.000 stays 1), whole quantity validation, ambiguous separators rejected under vi-VN/en-US/de-DE.");
    }

    private static DefaultModelBindingContext Context(string raw) => new()
    {
        ModelName = "Details[0].Quantity",
        ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(decimal)),
        ModelState = new ModelStateDictionary(),
        ValueProvider = new FormValueProvider(BindingSource.Form,
            new Microsoft.AspNetCore.Http.FormCollection(new Dictionary<string, StringValues> { ["Details[0].Quantity"] = raw }), CultureInfo.CurrentCulture)
    };
}
