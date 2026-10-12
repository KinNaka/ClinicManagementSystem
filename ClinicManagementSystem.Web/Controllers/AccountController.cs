
using ClinicManagementSystem.Web.Models.Identity;
using ClinicManagementSystem.Web.Models.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email hoặc mật khẩu không chính xác.");

                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tài khoản tạm thời bị khóa do đăng nhập sai nhiều lần.");
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email hoặc mật khẩu không chính xác.");
            }

            return View(model);
        }

        [Authorize(Roles = "Admin"), HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [Authorize(Roles = "Admin"), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsActive) return Challenge();
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Code switch {
                        "PasswordMismatch" => "Mật khẩu hiện tại không đúng.",
                        "PasswordTooShort" => "Mật khẩu mới chưa đủ độ dài yêu cầu.",
                        "PasswordRequiresDigit" => "Mật khẩu mới phải có chữ số.",
                        "PasswordRequiresLower" => "Mật khẩu mới phải có chữ thường.",
                        "PasswordRequiresUpper" => "Mật khẩu mới phải có chữ hoa.",
                        "PasswordRequiresNonAlphanumeric" => "Mật khẩu mới phải có ký tự đặc biệt.",
                        _ => "Không thể đổi mật khẩu. Vui lòng kiểm tra dữ liệu và thử lại."
                    });
                return View(model);
            }
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Đã đổi mật khẩu thành công.";
            return RedirectToAction(nameof(ChangePassword));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
