using ClinicManagementSystem.Web.Models.Identity;
using ClinicManagementSystem.Web.Models.ViewModels.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var model = new List<UserListViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new UserListViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = string.Join(", ", roles),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateUserViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (model.Role is not ("Admin" or "Staff"))
            ModelState.AddModelError(nameof(model.Role), "Vai trò không hợp lệ.");

        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = model.FullName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            // Roll back the newly created account if role assignment fails.
            var deleteResult = await _userManager.DeleteAsync(user);
            foreach (var error in roleResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            if (!deleteResult.Succeeded)
                ModelState.AddModelError(string.Empty,
                    "Không thể hoàn tác tài khoản vừa tạo. Hãy kiểm tra tài khoản trong danh sách.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Đã tạo tài khoản nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string id, string role)
    {
        if (role is not ("Admin" or "Staff"))
            return BadRequest("Vai trò không hợp lệ.");

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count == 1 && currentRoles[0] == role)
            return RedirectToAction(nameof(Index));

        if (currentRoles.Contains("Admin") && role != "Admin" &&
            !await HasAnotherActiveAdminAsync(user.Id))
        {
            TempData["ErrorMessage"] = "Không thể hạ quyền Admin hoạt động cuối cùng.";
            return RedirectToAction(nameof(Index));
        }

        // Identity stores roles separately; rollback the previous roles on failure.
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            TempData["ErrorMessage"] = FormatErrors(removeResult);
            return RedirectToAction(nameof(Index));
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            var restoreResult = await _userManager.AddToRolesAsync(user, currentRoles);
            TempData["ErrorMessage"] = restoreResult.Succeeded
                ? FormatErrors(addResult)
                : "Đổi quyền thất bại và không thể khôi phục quyền cũ. Cần kiểm tra tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        // Refresh security stamp so existing login cookies are revalidated.
        await _userManager.UpdateSecurityStampAsync(user);
        TempData["SuccessMessage"] = "Đã cập nhật vai trò.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["ErrorMessage"] = "Không thể tự khóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (user.IsActive && roles.Contains("Admin") &&
            !await HasAnotherActiveAdminAsync(user.Id))
        {
            TempData["ErrorMessage"] = "Không thể khóa Admin hoạt động cuối cùng.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = FormatErrors(result);
            return RedirectToAction(nameof(Index));
        }

        // Invalidate old cookies after status changes.
        await _userManager.UpdateSecurityStampAsync(user);
        TempData["SuccessMessage"] = user.IsActive
            ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        if (user.Id == _userManager.GetUserId(User) || !roles.Contains("Staff") || roles.Contains("Admin"))
        {
            TempData["ErrorMessage"] = "Chỉ được xóa tài khoản Staff khác; không thể xóa tài khoản đang đăng nhập hoặc Admin.";
            return RedirectToAction(nameof(Index));
        }
        var result = await _userManager.DeleteAsync(user);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Đã xóa tài khoản Staff." : "Không thể xóa tài khoản. Vui lòng tải lại danh sách và thử lại.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasAnotherActiveAdminAsync(string excludedId)
    {
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        return admins.Any(u => u.Id != excludedId && u.IsActive);
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => e.Description));
}
