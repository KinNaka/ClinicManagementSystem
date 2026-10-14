using ClinicManagementSystem.Web.Models.ViewModels.Sales;
using ClinicManagementSystem.Web.Services;

internal static class SalePricingTests
{
    public static void Run()
    {
        void Check(bool value) { if (!value) throw new Exception("Sale pricing mismatch"); }
        SaleFormViewModel Model(string type, decimal discount, decimal surcharge = 0) => new() { DiscountType = type, Discount = discount, Surcharge = surcharge };
        Check(SalePricing.Calculate(200000, Model("Amount", 30000, 50000)).Total == 220000);
        Check(SalePricing.Calculate(200000, Model("Percent", 15, 50000)).Total == 220000);
        Check(SalePricing.Calculate(199.99m, Model("Percent", 12.5m)).Discount == 25);
        Check(SalePricing.Calculate(100, Model("Percent", 100, 50)).Total == 50);
        foreach (var model in new[] { Model("Amount", 101), Model("Percent", 101), Model("Amount", -1), Model("Amount", 0, -1), Model("invalid", 0) })
        {
            try { SalePricing.Calculate(100, model); }
            catch (InvalidOperationException) { continue; }
            throw new Exception("Invalid sale pricing accepted");
        }
        Console.WriteLine("Passed: server invoice amount/percent discount, surcharge, rounding and invalid adjustments.");
    }
}
