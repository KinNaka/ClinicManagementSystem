using ClinicManagementSystem.Web.Models.ViewModels.Patients;
using ClinicManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class PatientGroupsController(PatientGroupService groups) : Controller
{
    public async Task<IActionResult> Index() => View(await groups.GetAllAsync());
    public async Task<IActionResult> Create() => View("Edit", await groups.PrepareAsync());
    public async Task<IActionResult> Edit(long id)
    {
        try { return View(await groups.PrepareAsync(id)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PatientGroupEditViewModel model)
    {
        try
        {
            if (!ModelState.IsValid) { model.Patients = (await groups.PrepareAsync(model.Id == 0 ? null : model.Id)).Patients; return View("Edit", model); }
            await groups.SaveAsync(model);
            TempData["SuccessMessage"] = "Đã lưu nhóm khách hàng.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            try { model.Patients = (await groups.PrepareAsync(model.Id == 0 ? null : model.Id)).Patients; }
            catch (KeyNotFoundException) { return NotFound(); }
            return View("Edit", model);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id) { await groups.DeleteAsync(id); TempData["SuccessMessage"] = "Đã xóa nhóm khách hàng."; return RedirectToAction(nameof(Index)); }
}
