using System.Text.Json;
using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("rap", "cinema", "phong")]
public class AdminRapController : AdminApiCrudController
{
    public AdminRapController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cinemas = await GetDataAsync<List<AdminCinemaViewModel>>("api/admin/cinemas", ct) ?? new();
        return View("~/Areas/Admin/Views/AdminRap/Index.cshtml", cinemas);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminCinemaViewModel model, CancellationToken ct)
    {
        await SendAsync(HttpMethod.Post, "api/admin/cinemas", model, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { success = false, message = "Missing ID" });
        using var response = await Http.SendAsync(CreateApiRequest(HttpMethod.Post, $"api/admin/cinemas/{Uri.EscapeDataString(id)}/toggle"), ct);
        if (!response.IsSuccessStatusCode) return Json(new { success = false, status = "Không đổi được" });
        var item = await GetDataAsync<AdminCinemaViewModel>($"api/admin/cinemas/{Uri.EscapeDataString(id)}", ct);
        return Json(new { success = true, status = item?.Status ?? "Đã đổi" });
    }
}

[AdminPermission("phong")]
public class AdminPhongController : AdminApiCrudController
{
    public AdminPhongController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(string? cinemaId, CancellationToken ct)
    {
        var cinemas = await GetDataAsync<List<AdminCinemaViewModel>>("api/admin/cinemas", ct) ?? new();
        var url = "api/admin/rooms" + (string.IsNullOrWhiteSpace(cinemaId) ? "" : $"?cinemaId={Uri.EscapeDataString(cinemaId)}");
        var rooms = await GetDataAsync<List<AdminRoomViewModel>>(url, ct) ?? new();
        ViewBag.Cinemas = cinemas;
        ViewBag.CinemaId = cinemaId;
        return View("~/Areas/Admin/Views/AdminPhong/Index.cshtml", rooms);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminRoomViewModel model, CancellationToken ct)
    {
        var body = new { model.Id, model.Name, model.SeatCount, model.Status };
        await SendAsync(HttpMethod.Post, "api/admin/rooms", body, ct);
        return RedirectToAction(nameof(Index), new { cinemaId = model.CinemaId });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { success = false, message = "Missing ID" });
        using var response = await Http.SendAsync(CreateApiRequest(HttpMethod.Post, $"api/admin/rooms/{Uri.EscapeDataString(id)}/toggle"), ct);
        if (!response.IsSuccessStatusCode) return Json(new { success = false, status = "Không đổi được" });
        var room = await GetDataAsync<AdminRoomViewModel>($"api/admin/rooms/{Uri.EscapeDataString(id)}", ct);
        return Json(new { success = true, status = room?.Status ?? "Đã đổi" });
    }
}

[AdminPermission("ghe")]
public class AdminGheController : AdminApiCrudController
{
    public AdminGheController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(string? maPhong, string? search, CancellationToken ct)
    {
        var rooms = await GetDataAsync<List<AdminRoomViewModel>>("api/admin/rooms", ct) ?? new();
        var url = "api/admin/seats";
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(maPhong)) query.Add($"roomId={Uri.EscapeDataString(maPhong)}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (query.Count > 0) url += "?" + string.Join("&", query);
        var seats = await GetDataAsync<List<AdminSeatViewModel>>(url, ct) ?? new();
        return View("~/Areas/Admin/Views/AdminGhe/Index.cshtml", new AdminSeatPageViewModel { RoomId = maPhong, Search = search, Rooms = rooms, Seats = seats });
    }

    public async Task<IActionResult> SoDo(string? maPhong, CancellationToken ct)
    {
        var rooms = await GetDataAsync<List<AdminRoomViewModel>>("api/admin/rooms", ct) ?? new();
        maPhong ??= rooms.FirstOrDefault()?.Id;
        var seats = string.IsNullOrWhiteSpace(maPhong)
            ? new List<AdminSeatViewModel>()
            : await GetDataAsync<List<AdminSeatViewModel>>($"api/admin/seats/map/{Uri.EscapeDataString(maPhong)}", ct) ?? new();
        return View("~/Areas/Admin/Views/AdminGhe/SoDo.cshtml", new AdminSeatPageViewModel { RoomId = maPhong, Rooms = rooms, Seats = seats });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTrangThai(string id, string? maPhong, CancellationToken ct)
    {
        var url = $"api/admin/seats/{Uri.EscapeDataString(id)}/toggle" + (string.IsNullOrWhiteSpace(maPhong) ? "" : $"?roomId={Uri.EscapeDataString(maPhong)}");
        await SendAsync(HttpMethod.Post, url, null, ct);
        return RedirectToAction(nameof(Index), new { maPhong });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleTrangThaiAjax(string maGhe, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(maGhe)) return Json(new { success = false, message = "Mã ghế không hợp lệ" });
        var roomId = Request.Form["maPhong"].ToString();
        var url = $"api/admin/seats/{Uri.EscapeDataString(maGhe)}/toggle" + (string.IsNullOrWhiteSpace(roomId) ? "" : $"?roomId={Uri.EscapeDataString(roomId)}");
        using var response = await Http.SendAsync(CreateApiRequest(HttpMethod.Post, url), ct);
        if (!response.IsSuccessStatusCode) return Json(new { success = false, message = "Không đổi được trạng thái" });
        var text = await response.Content.ReadAsStringAsync(ct);
        string? status = null;
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.String) status = data.GetString();
        }
        catch { }
        return Json(new { success = true, trangThai = status ?? "Đã đổi" });
    }
}


