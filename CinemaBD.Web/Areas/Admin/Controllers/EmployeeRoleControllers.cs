using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

public abstract class AdminApiCrudController : BaseAdminController
{
    protected readonly HttpClient Http;
    protected AdminApiCrudController(HttpClient http, IConfiguration configuration)
    {
        Http = http;
        if (Http.BaseAddress == null)
        {
            Http.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5188/");
        }
    }

    protected HttpRequestMessage CreateApiRequest(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        var token = HttpContext.Session.GetString("AdminToken");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null) request.Content = JsonContent.Create(body);
        return request;
    }

    protected async Task<T?> GetDataAsync<T>(string url, CancellationToken ct)
    {
        using var response = await Http.SendAsync(CreateApiRequest(HttpMethod.Get, url), ct);
        if (!response.IsSuccessStatusCode) return default;
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(cancellationToken: ct);
        if (payload?.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return default;
        return JsonSerializer.Deserialize<T>(payload!.Data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    protected async Task<bool> SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var response = await Http.SendAsync(CreateApiRequest(method, url, body), ct);
        return response.IsSuccessStatusCode;
    }
}

    [AdminPermission("nhanvien")]
    public class NhanVienController : AdminApiCrudController
    {
        public NhanVienController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }
        public async Task<IActionResult> Index(string? search, CancellationToken ct)
        {
            var rows = await GetDataAsync<List<EmployeeFormViewModel>>("api/admin/employees", ct) ?? new();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                rows = rows.Where(x => 
                    x.FullName.Contains(s, StringComparison.OrdinalIgnoreCase) || 
                    (x.Email?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.PhoneNumber?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            }
            ViewBag.Roles = await GetDataAsync<List<RoleFormViewModel>>("api/admin/roles", ct) ?? new();
            return View("~/Areas/Admin/Views/NhanVien/Index.cshtml", rows);
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var model = await GetDataAsync<EmployeeFormViewModel>($"api/admin/employees/{id}", ct);
            if (model == null) return NotFound();
            return Json(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAjax(EmployeeFormViewModel model, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(model.FullName)) return Json(new { success = false, message = "Họ tên là bắt buộc." });
            var body = new { model.FullName, model.BirthDate, model.PhoneNumber, model.Email, model.StartDate, model.RoleId, model.IsActive };
            var ok = model.Id > 0 ? await SendAsync(HttpMethod.Put, $"api/admin/employees/{model.Id}", body, ct) : await SendAsync(HttpMethod.Post, "api/admin/employees", body, ct);
            return Json(new { success = ok, message = ok ? "Lưu nhân viên thành công" : "Không lưu được nhân viên" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id, CancellationToken ct)
        {
            var ok = await SendAsync(HttpMethod.Delete, $"api/admin/employees/{id}", null, ct);
            return Json(new { success = ok, message = ok ? "Đã xóa nhân viên" : "Không xóa được nhân viên" });
        }
    }

    [AdminPermission("role", "chucvu")]
    public class RoleController : AdminApiCrudController
    {
        public RoleController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var rows = await GetDataAsync<List<RoleFormViewModel>>("api/admin/roles", ct) ?? new();
            return View("~/Areas/Admin/Views/Role/Index.cshtml", rows);
        }

        [HttpGet]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var model = await GetDataAsync<RoleFormViewModel>($"api/admin/roles/{id}", ct);
            if (model == null) return NotFound();
            return Json(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAjax(RoleFormViewModel model, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(model.Name)) return Json(new { success = false, message = "Tên chức vụ là bắt buộc." });
            var body = new { model.Name, model.IsMaster, model.IsActive };
            var ok = model.Id > 0 ? await SendAsync(HttpMethod.Put, $"api/admin/roles/{model.Id}", body, ct) : await SendAsync(HttpMethod.Post, "api/admin/roles", body, ct);
            return Json(new { success = ok, message = ok ? "Lưu chức vụ thành công" : "Không lưu được chức vụ" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id, CancellationToken ct)
        {
            var ok = await SendAsync(HttpMethod.Delete, $"api/admin/roles/{id}", null, ct);
            return Json(new { success = ok, message = ok ? "Đã xóa chức vụ" : "Không xóa được chức vụ" });
        }
    }

[AdminPermission("phanquyen")]
public class RolePermissionController : AdminApiCrudController
{
    public RolePermissionController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(int? roleId, CancellationToken ct)
    {
        var roles = await GetDataAsync<List<RoleFormViewModel>>("api/admin/roles", ct) ?? new();
        var selectedRoleId = roleId ?? roles.FirstOrDefault()?.Id;
        var allPermissions = await GetDataAsync<List<PermissionViewModel>>("api/admin/roles/permissions", ct) ?? new();
        var assigned = selectedRoleId.HasValue
            ? await GetDataAsync<List<PermissionViewModel>>($"api/admin/roles/{selectedRoleId.Value}/permissions", ct) ?? new()
            : new List<PermissionViewModel>();

        return View("~/Areas/Admin/Views/RolePermission/Index.cshtml", new RolePermissionPageViewModel
        {
            SelectedRoleId = selectedRoleId,
            Roles = roles,
            AllPermissions = allPermissions,
            AssignedPermissions = assigned
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int roleId, int permissionId, CancellationToken ct)
    {
        var ok = await SendAsync(HttpMethod.Post, "api/admin/roles/permissions/assign", new { RoleId = roleId, PermissionId = permissionId }, ct);
        TempData["SuccessMessage"] = ok ? "Gán quyền thành công" : "Không gán được quyền";
        return RedirectToAction(nameof(Index), new { roleId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int roleId, int permissionId, CancellationToken ct)
    {
        var ok = await SendAsync(HttpMethod.Delete, $"api/admin/roles/{roleId}/permissions/{permissionId}", null, ct);
        TempData["SuccessMessage"] = ok ? "Gỡ quyền thành công" : "Không gỡ được quyền";
        return RedirectToAction(nameof(Index), new { roleId });
    }
}


