using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.Models.ViewModels.Appointments;

public sealed class AppointmentFormViewModel : IValidatableObject
{
    public long SaleId { get; set; }
    public string SaleCode { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string PatientCode { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Version { get; set; }
    public bool Exists { get; set; }
    [Display(Name = "Số ngày thuốc")]
    [Range(1, 3650, ErrorMessage = "Số ngày thuốc phải từ 1 đến 3650.")]
    public int MedicationDays { get; set; } = 7;
    [Display(Name = "Ngày bắt đầu uống thuốc"), Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu."), DataType(DataType.Date)]
    public DateOnly? MedicationStartDate { get; set; }
    [Display(Name = "Tự chọn ngày tái khám")]
    public bool UseCustomDate { get; set; }
    [Display(Name = "Ngày tái khám"), DataType(DataType.Date)]
    public DateOnly? AppointmentDate { get; set; }
    [Display(Name = "Ghi chú"), StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    public string? Notes { get; set; }

    public DateOnly CalculateDate()
    {
        if (UseCustomDate) return AppointmentDate ?? throw new InvalidOperationException("Vui lòng chọn ngày tái khám.");
        try { return MedicationStartDate!.Value.AddDays(MedicationDays); }
        catch (ArgumentOutOfRangeException) { throw new InvalidOperationException("Ngày tái khám vượt phạm vi cho phép."); }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UseCustomDate && !AppointmentDate.HasValue)
            yield return new ValidationResult("Vui lòng chọn ngày tái khám.", [nameof(AppointmentDate)]);
        if (UseCustomDate && AppointmentDate.HasValue && MedicationStartDate.HasValue && AppointmentDate < MedicationStartDate)
            yield return new ValidationResult("Ngày tái khám không được trước ngày bắt đầu uống thuốc.", [nameof(AppointmentDate)]);
    }
}

public sealed class AppointmentListViewModel
{
    public const int PageSize = 20;
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string Sort { get; set; } = "soonest";
    public int Page { get; set; } = 1;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public List<AppointmentRowViewModel> Items { get; set; } = [];
}

public sealed class AppointmentRowViewModel
{
    public long Id { get; set; }
    public long PatientId { get; set; }
    public long? SaleId { get; set; }
    public string SaleCode { get; set; } = "";
    public string PatientCode { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public int MedicationDays { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime? AttendedAtUtc { get; set; }
    public string? AttendedBy { get; set; }
    public string Version { get; set; } = "";
}
