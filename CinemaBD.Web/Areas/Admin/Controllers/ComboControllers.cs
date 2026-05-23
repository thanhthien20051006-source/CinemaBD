using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("dichvu", "combo")]
public class AdminDichVuController : AdminApiCrudController
{
    public AdminDichVuController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var combos = await GetDataAsync<List<AdminComboEditViewModel>>("api/admin/combos", ct) ?? new();
        return View("~/Areas/Admin/Views/AdminDichVu/Index.cshtml", combos.OrderBy(x => x.Name).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Form(string? id, CancellationToken ct)
    {
        var model = string.IsNullOrWhiteSpace(id)
            ? new AdminComboEditViewModel()
            : await GetDataAsync<AdminComboEditViewModel>($"api/admin/combos/{Uri.EscapeDataString(id)}", ct) ?? new AdminComboEditViewModel();
        return PartialView("~/Areas/Admin/Views/AdminDichVu/_DichVuForm.cshtml", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminComboEditViewModel model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) return BadRequest("Tên dịch vụ không được để trống");
        if (model.Price < 0) return BadRequest("Giá dịch vụ không được âm");
        var body = new { model.Id, model.Name, model.Price, model.Description, model.ImageUrl };
        var existing = await GetDataAsync<AdminComboEditViewModel>($"api/admin/combos/{Uri.EscapeDataString(model.Id)}", ct);
        var ok = existing == null
            ? await SendAsync(HttpMethod.Post, "api/admin/combos", body, ct)
            : await SendAsync(HttpMethod.Put, $"api/admin/combos/{Uri.EscapeDataString(model.Id)}", body, ct);
        if (!ok) return BadRequest("Không lưu được dịch vụ");
        var combos = await GetDataAsync<List<AdminComboEditViewModel>>("api/admin/combos", ct) ?? new();
        return PartialView("~/Areas/Admin/Views/AdminDichVu/_DichVuTable.cshtml", combos.OrderBy(x => x.Name).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest("Thiếu mã combo");
        await SendAsync(HttpMethod.Delete, $"api/admin/combos/{Uri.EscapeDataString(id)}", null, ct);
        var combos = await GetDataAsync<List<AdminComboEditViewModel>>("api/admin/combos", ct) ?? new();
        return PartialView("~/Areas/Admin/Views/AdminDichVu/_DichVuTable.cshtml", combos.OrderBy(x => x.Name).ToList());
    }
}


