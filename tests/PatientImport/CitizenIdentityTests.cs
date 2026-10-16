using System.ComponentModel.DataAnnotations;
using ClinicManagementSystem.Web.Models.ViewModels.Patients;

internal static class CitizenIdentityTests
{
    public static void Run()
    {
        foreach (var (number, valid) in new (string?, bool)[] {
            (null, true), ("", true), ("001234567890", true),
            ("12345678901", false), ("1234567890123", false), ("00123456789A", false) })
        {
            var model = new PatientFormViewModel { PatientCode = "LT000001", FullName = "Test", CitizenIdNumber = number };
            var errors = new List<ValidationResult>();
            if (Validator.TryValidateObject(model, new ValidationContext(model), errors, true) != valid)
                throw new Exception("Citizen identity validation failed.");
        }
        Console.WriteLine("Citizen identity validation passed: optional fields, leading zeroes, length and digits.");
    }
}
