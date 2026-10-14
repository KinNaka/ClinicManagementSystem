using ClinicManagementSystem.Web.Models.ViewModels.Sales;

namespace ClinicManagementSystem.Web.Services;

public static class SalePricing
{
    public static (decimal Discount, decimal Total) Calculate(decimal goods, SaleFormViewModel model)
    {
        if (model.DiscountType is not ("Amount" or "Percent"))
            throw new InvalidOperationException("Vui lòng chọn kiểu giảm giá hợp lệ.");
        if (model.Discount < 0 || model.Surcharge < 0 || model.Discount > 999999999999m || model.Surcharge > 999999999999m)
            throw new InvalidOperationException("Giảm giá và thu khác phải từ 0 đến 999.999.999.999.");
        if (model.DiscountType == "Percent" && model.Discount > 100)
            throw new InvalidOperationException("Giảm giá phần trăm phải từ 0 đến 100.");
        var discount = decimal.Round(model.DiscountType == "Percent" ? goods * model.Discount / 100 : model.Discount, 2, MidpointRounding.AwayFromZero);
        if (discount > goods) throw new InvalidOperationException("Giảm giá không được vượt tiền hàng.");
        return (discount, goods - discount + decimal.Round(model.Surcharge, 2, MidpointRounding.AwayFromZero));
    }
}
