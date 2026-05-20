using CinemaBD.Web.Core;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("suatchieu", "lichchieu")]
public class AdminSuatChieuController : AdminApiCrudController
{
    public AdminSuatChieuController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public IActionResult Index(string? maPhong, DateTime? ngay)
    {
        return RedirectToAction("Index", "Showtimes", new { area = "Admin", roomId = maPhong, date = ngay?.ToString("yyyy-MM-dd") });
    }

    public IActionResult CheckSeats(string id)
    {
        return RedirectToAction("SoDo", "AdminGhe", new { area = "Admin" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateShowtime(string maPhim, string maPhong, DateTime ngayChieu, string gioBatDau, decimal giaVe, string? trangThai, CancellationToken ct)
    {
        var result = await CreateOrUpdateAsync(null, maPhim, maPhong, ngayChieu, gioBatDau, giaVe, trangThai, ct);
        TempData[result.Success ? "SuccessMessage" : "Error"] = result.Message;
        return RedirectToAction("Index", "Showtimes", new { area = "Admin" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateShowtimeAjax(string maPhim, string maPhong, DateTime ngayChieu, string gioBatDau, decimal giaVe, string? trangThai, CancellationToken ct)
    {
        var result = await CreateOrUpdateAsync(null, maPhim, maPhong, ngayChieu, gioBatDau, giaVe, trangThai, ct);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShowtimeAjax(string maSuatChieu, string maPhim, string maPhong, DateTime ngayChieu, string gioBatDau, decimal giaVe, string? trangThai, CancellationToken ct)
    {
        var result = await CreateOrUpdateAsync(maSuatChieu, maPhim, maPhong, ngayChieu, gioBatDau, giaVe, trangThai, ct);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteShowtimeAjax(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return Json(new { success = false, message = "Khong xac dinh duoc suat chieu can xoa." });
        var ok = await SendAsync(HttpMethod.Delete, $"api/admin/showtimes/{Uri.EscapeDataString(id)}", null, ct);
        return Json(new { success = ok, message = ok ? "Da xoa/huy suat chieu." : "Khong xoa duoc suat chieu." });
    }

    [HttpPost]
    public async Task<IActionResult> GenerateShowtimesAjax(string movieIds, string roomIds, DateTime fromDate, DateTime toDate, string startTimes, decimal ticketPrice, CancellationToken ct)
    {
        var body = new
        {
            MovieIds = SplitCsv(movieIds),
            RoomIds = SplitCsv(roomIds),
            FromDate = fromDate.Date,
            ToDate = toDate.Date,
            StartTimes = SplitCsv(startTimes),
            TicketPrice = ticketPrice
        };

        var ok = await SendAsync(HttpMethod.Post, "api/admin/showtimes/generate", body, ct);
        return Json(new { success = ok, message = ok ? "Da sinh lich chieu tu dong." : "Khong sinh duoc lich chieu." });
    }

    private async Task<(bool Success, string Message)> CreateOrUpdateAsync(string? id, string maPhim, string maPhong, DateTime ngayChieu, string gioBatDau, decimal giaVe, string? trangThai, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(maPhim) || string.IsNullOrWhiteSpace(maPhong))
            return (false, "Vui long chon phim va phong chieu.");
        if (!TimeSpan.TryParse(gioBatDau, out _))
            return (false, "Gio bat dau khong hop le.");

        var body = new { MovieId = maPhim, RoomId = maPhong, ShowDate = ngayChieu.Date, StartTime = gioBatDau, TicketPrice = giaVe, Status = trangThai };
        try
        {
            var ok = string.IsNullOrWhiteSpace(id)
                ? await SendAsync(HttpMethod.Post, "api/admin/showtimes", body, ct)
                : await SendAsync(HttpMethod.Put, $"api/admin/showtimes/{Uri.EscapeDataString(id)}", body, ct);
            return (ok, ok ? (string.IsNullOrWhiteSpace(id) ? "Tao suat chieu thanh cong." : "Cap nhat suat chieu thanh cong.") : "Khong luu duoc suat chieu.");
        }
        catch (Exception ex)
        {
            return (false, "Loi: " + ex.Message);
        }
    }

    private static string[] SplitCsv(string value)
        => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
