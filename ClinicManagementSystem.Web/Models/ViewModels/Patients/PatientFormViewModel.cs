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

        [DataType(DataType.Date)]
        public DateOnly? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? Occupation { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public string Status { get; set; } = "Active";
    }
}