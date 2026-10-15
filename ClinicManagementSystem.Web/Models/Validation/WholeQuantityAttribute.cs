using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.Models.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class WholeQuantityAttribute : ValidationAttribute
{
    public WholeQuantityAttribute() : base("Số lượng phải là số nguyên.") { }
    public override bool IsValid(object? value) => value == null || value is decimal number && number == decimal.Truncate(number);
}
