using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ClinicManagementSystem.Web.Models.Validation;

// HTML number inputs submit invariant decimals; never interpret '.' as a thousands separator.
public sealed class QuantityModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var result = context.ValueProvider.GetValue(context.ModelName);
        if (result == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, result);
        var raw = result.FirstValue;
        if (string.IsNullOrWhiteSpace(raw) && context.ModelType == typeof(decimal?))
            context.Result = ModelBindingResult.Success(null);
        else if (decimal.TryParse(raw, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                     CultureInfo.InvariantCulture, out var value))
            context.Result = ModelBindingResult.Success(value);
        else
            context.ModelState.TryAddModelError(context.ModelName, "Số lượng không hợp lệ; nhập số không có dấu phân cách hàng nghìn.");
        return Task.CompletedTask;
    }
}
