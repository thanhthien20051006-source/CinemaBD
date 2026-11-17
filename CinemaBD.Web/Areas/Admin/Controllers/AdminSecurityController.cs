using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("phanquyen")]
public sealed class AdminSecurityController : BaseAdminController
{
    private const int AuditPageSize = 50;
    private const string AuditEndpoint = "api/admin/security/audit";
    private readonly HttpClient _http;

    public AdminSecurityController(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        if (_http.BaseAddress == null)
        {
            _http.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5188/");
        }
    }

    public async Task<IActionResult> Index(string eventType = "login_failed", int page = 1, CancellationToken cancellationToken = default)
    {
        eventType = eventType is "login_failed" or "account_locked" ? eventType : "login_failed";
        page = Math.Max(1, page);
        var query = $"{AuditEndpoint}?eventType={Uri.EscapeDataString(eventType)}&page={page}&pageSize={AuditPageSize}";

        try
        {
            using var response = await _http.SendAsync(CreateRequest(HttpMethod.Get, query), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể tải nhật ký bảo mật.";
                return View(new SecurityAuditPageViewModel(eventType, page, AuditPageSize, 0, Array.Empty<SecurityAuditItemViewModel>()));
            }

            var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(cancellationToken: cancellationToken);
            if (apiResult?.Success != true || apiResult.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                TempData["Error"] = "Không thể tải nhật ký bảo mật.";
                return View(new SecurityAuditPageViewModel(eventType, page, AuditPageSize, 0, Array.Empty<SecurityAuditItemViewModel>()));
            }

            var auditPage = JsonSerializer.Deserialize<SecurityAuditPageViewModel>(apiResult.Data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (auditPage == null)
            {
                TempData["Error"] = "Không thể tải nhật ký bảo mật.";
                return View(new SecurityAuditPageViewModel(eventType, page, AuditPageSize, 0, Array.Empty<SecurityAuditItemViewModel>()));
            }

            return View(auditPage with { EventTypeFilter = eventType });
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Không thể kết nối đến dịch vụ bảo mật.";
            return View(new SecurityAuditPageViewModel(eventType, page, AuditPageSize, 0, Array.Empty<SecurityAuditItemViewModel>()));
        }
        catch (JsonException)
        {
            TempData["Error"] = "Không thể đọc nhật ký bảo mật.";
            return View(new SecurityAuditPageViewModel(eventType, page, AuditPageSize, 0, Array.Empty<SecurityAuditItemViewModel>()));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockAccount(string accountType, string accountId, bool locked, CancellationToken cancellationToken)
    {
        if (!IsValidAccount(accountType, accountId))
        {
            TempData["Error"] = "Loại tài khoản hoặc mã tài khoản không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var success = await PostAsync("api/admin/security/accounts/lock", new { AccountType = accountType, AccountId = accountId.Trim(), Locked = locked }, cancellationToken);
        TempData[success ? "SuccessMessage" : "Error"] = success
            ? (locked ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.")
            : "Không thể cập nhật trạng thái tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSessions(string accountType, string accountId, CancellationToken cancellationToken)
    {
        if (!IsValidAccount(accountType, accountId))
        {
            TempData["Error"] = "Loại tài khoản hoặc mã tài khoản không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var success = await PostAsync("api/admin/security/accounts/sessions/revoke-all", new { AccountType = accountType, AccountId = accountId.Trim() }, cancellationToken);
        TempData[success ? "SuccessMessage" : "Error"] = success
            ? "Đã thu hồi các phiên của tài khoản."
            : "Không thể thu hồi phiên tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        var token = HttpContext.Session.GetString("AdminToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body != null) request.Content = JsonContent.Create(body);
        return request;
    }

    private async Task<bool> PostAsync(string url, object body, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.SendAsync(CreateRequest(HttpMethod.Post, url, body), cancellationToken);
            if (!response.IsSuccessStatusCode) return false;
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(cancellationToken: cancellationToken);
            return result?.Success == true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsValidAccount(string accountType, string accountId)
    {
        return (accountType is "Admin" or "KHACHHANG") && !string.IsNullOrWhiteSpace(accountId);
    }
}

public sealed record SecurityAuditPageViewModel(
    string EventTypeFilter,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<SecurityAuditItemViewModel> Items);

public sealed record SecurityAuditItemViewModel(
    DateTime OccurredAtUtc,
    string EventType,
    string? AccountType,
    string? AccountId,
    string? CorrelationId);
