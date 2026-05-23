using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CinemaBD.Web.Areas.Admin.Controllers;

public class DashboardController : BaseAdminController
{
    private readonly HttpClient _http;

    public DashboardController(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _http.BaseAddress ??= new Uri(configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5188/");
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summary = await GetDashboardAsync(cancellationToken) ?? new AdminDashboardApiViewModel();
        ViewBag.MovieCount = summary.TotalMovies;
        ViewBag.ShowtimeCount = summary.TotalShowtimes;
        ViewBag.UserCount = summary.TotalCustomers;
        ViewBag.BookingCount = summary.TotalAdmins;
        ViewBag.TotalRevenue = summary.TotalPaidRevenue;
        ViewBag.AdminName = HttpContext.Session.GetString("AdminFullName") ?? "Admin";
        return View();
    }

    private async Task<AdminDashboardApiViewModel?> GetDashboardAsync(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/admin/dashboard");
        var token = HttpContext.Session.GetString("AdminToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>(cancellationToken: ct);
        if (payload?.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return null;

        return JsonSerializer.Deserialize<AdminDashboardApiViewModel>(
            payload!.Data.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
