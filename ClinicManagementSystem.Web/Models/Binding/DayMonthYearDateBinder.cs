using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ClinicManagementSystem.Web.Models.Binding;

public sealed class DayMonthYearDateBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        var text = value.FirstValue?.Trim();
        if (string.IsNullOrEmpty(text)) context.Result = ModelBindingResult.Success(null);
        else if (DateOnly.TryParseExact(text, new[] { "dd/MM/yyyy", "ddMMyyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            context.Result = ModelBindingResult.Success(date);
        else context.ModelState.TryAddModelError(context.ModelName, "Ngày sinh không hợp lệ. Vui lòng nhập dd/mm/yyyy, ví dụ 25/12/1990.");
        return Task.CompletedTask;
    }
}
