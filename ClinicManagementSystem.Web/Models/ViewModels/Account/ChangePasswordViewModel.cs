using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Web.Models.ViewModels.Account;
public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Nhập mật khẩu hiện tại."), DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = "";
    [Required(ErrorMessage = "Nhập mật khẩu mới."), DataType(DataType.Password)]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải từ 8 đến 128 ký tự.")]
    public string NewPassword { get; set; } = "";
    [Required(ErrorMessage = "Nhập lại mật khẩu mới."), DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = "";
}
