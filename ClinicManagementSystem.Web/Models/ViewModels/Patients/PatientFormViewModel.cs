using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.Models.ViewModels.Patients
{
    public class PatientFormViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã bệnh nhân.")]
        [StringLength(30, ErrorMessage = "Mã bệnh nhân tối đa 30 ký tự.")]
        [RegularExpression(
            @"^[A-Za-z0-9_-]+$",
            ErrorMessage = "Mã chỉ được chứa chữ, số, dấu gạch ngang và gạch dưới.")]
        public string PatientCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(200, ErrorMessage = "Họ tên tối đa 200 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [Microsoft.AspNetCore.Mvc.ModelBinder(BinderType = typeof(ClinicManagementSystem.Web.Models.Binding.DayMonthYearDateBinder))]
        public DateOnly? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        public long? SharedPhoneWithPatientId { get; set; }
        [StringLength(50)]
        public string? SharedPhoneRelationship { get; set; }
        public string? SharedPhoneOwnerName { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        public int? ProvinceCode { get; set; }
        public int? WardCode { get; set; }
        [StringLength(100)]
        public string? ProvinceName { get; set; }
        [StringLength(100)]
        public string? WardName { get; set; }

        [StringLength(100)]
        public string? Occupation { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public string Status { get; set; } = "Active";
    }
}
