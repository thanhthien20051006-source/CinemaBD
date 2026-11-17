using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("phanquyen")]
public sealed class AdminUserCreateController(HttpClient http, IConfiguration configuration) : BaseAdminController
{
    private readonly HttpClient _http = ConfigureClient(http, configuration);

    private static HttpClient ConfigureClient(HttpClient client, IConfiguration configuration)
    {
        client.BaseAddress ??= new Uri(configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5188/");
        return client;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var roles = await GetRolesAsync(cancellationToken);
        ViewBag.Roles = roles.Where(x => x.IsActive && !x.IsMaster).ToList();
        return View("~/Areas/Admin/Views/AdminUserCreate/Index.cshtml");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string username, string fullName, int roleId, CancellationToken cancellationToken)
    {
        ViewBag.Roles = (await GetRolesAsync(cancellationToken)).Where(x => x.IsActive && !x.IsMaster).ToList();
        username = username?.Trim() ?? string.Empty;
        fullName = fullName?.Trim() ?? string.Empty;
        if (!CinemaBD.Infrastructure.Services.AdminAccountService.IsValidUsername(username) || fullName.Length is < 1 or > 100 || roleId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng kiểm tra lại thông tin đã nhập.");
            return View("~/Areas/Admin/Views/AdminUserCreate/Index.cshtml");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/admin/accounts")
        {
            Content = JsonContent.Create(new { Username = username, FullName = fullName, RoleId = roleId })
        };
        var token = HttpContext.Session.GetString("AdminToken");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản quản trị. Vui lòng kiểm tra thông tin hoặc quyền truy cập.");
                return View("~/Areas/Admin/Views/AdminUserCreate/Index.cshtml");
            }

            using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
            var data = payload?.RootElement.TryGetProperty("data", out var apiData) == true
                ? apiData : default;
            if (data.ValueKind != JsonValueKind.Object
                || !data.TryGetProperty("temporaryPassword", out var temporaryPassword)
                || temporaryPassword.ValueKind != JsonValueKind.String
                || !data.TryGetProperty("adminId", out var adminId))
            {
                ModelState.AddModelError(string.Empty, "Không thể xác nhận kết quả tạo tài khoản.");
                return View("~/Areas/Admin/Views/AdminUserCreate/Index.cshtml");
            }

            ViewBag.CreatedAdminId = adminId.GetInt32();
            ViewBag.TemporaryPassword = temporaryPassword.GetString();
            return View("~/Areas/Admin/Views/AdminUserCreate/Index.cshtml");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không kết nối được dịch vụ tạo tài khoản.");
            return View("~/Areas/Admin/Views/AdminUserCreate/Index.cshtml");
        }
    }

    private async Task<List<RoleFormViewModel>> GetRolesAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/admin/roles");
        var token = HttpContext.Session.GetString("AdminToken");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return [];
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(cancellationToken: cancellationToken);
            if (payload is not { Success: true } || payload.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return [];
            return JsonSerializer.Deserialize<List<RoleFormViewModel>>(payload.Data.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    private sealed record ApiResponse<T>(bool Success, string Message, T Data);
}
