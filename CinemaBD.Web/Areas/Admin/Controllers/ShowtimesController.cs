using System.Text;
using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using CinemaBD.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("suatchieu", "lichchieu")]
public class ShowtimesController : AdminApiCrudController
{
    private readonly CinemaApiClient _apiClient;

    public ShowtimesController(HttpClient http, IConfiguration configuration, CinemaApiClient apiClient) : base(http, configuration)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(string? roomId, string? movieId, string? status, DateTime? date, DateTime? toDate, CancellationToken ct)
    {
        var model = await BuildPageModelAsync(roomId, movieId, status, date, toDate, ct);
        return View(model);
    }

    public async Task<IActionResult> Seats(string id, DateTime? date, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return RedirectToAction(nameof(Index));

        var targetDate = date ?? DateTime.Today;
        var page = await BuildPageModelAsync(null, null, null, targetDate, targetDate, ct);
        var showtime = page.Showtimes.FirstOrDefault(x => x.Id == id)
            ?? (await BuildPageModelAsync(null, null, null, DateTime.Today.AddDays(-31), DateTime.Today.AddDays(31), ct)).Showtimes.FirstOrDefault(x => x.Id == id);

        var seats = await _apiClient.GetSeatsAsync(id, ct);
        var model = new AdminShowtimeSeatMapViewModel
        {
            ShowtimeId = id,
            MovieTitle = showtime?.MovieTitle ?? showtime?.MovieId ?? id,
            RoomName = showtime?.RoomName ?? showtime?.RoomId ?? string.Empty,
            ShowDate = showtime?.ShowDate ?? targetDate,
            StartTime = showtime?.StartTime ?? string.Empty,
            Status = showtime?.Status ?? string.Empty,
            TotalSeats = seats.Count,
            AvailableSeats = seats.Count(x => IsSeatStatus(x, "Available") || string.IsNullOrWhiteSpace(x.Status)),
            HeldSeats = seats.Count(x => IsSeatStatus(x, "Held")),
            SoldSeats = seats.Count(x => IsSeatStatus(x, "Sold")),
            CheckedInSeats = seats.Count(x => IsSeatStatus(x, "CheckedIn")),
            Seats = seats.OrderBy(x => x.Row).ThenBy(x => x.Column).ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> ExportCsv(string? roomId, string? movieId, string? status, DateTime? date, DateTime? toDate, CancellationToken ct)
    {
        var model = await BuildPageModelAsync(roomId, movieId, status, date, toDate, ct);
        var csv = new StringBuilder();
        csv.AppendLine("Ma suat,Ngay,Gio,Phim,Phong,Gia ve,Tong ghe,Ghe trong,Dang giu,Da ban,Check-in,Trang thai");

        foreach (var s in model.Showtimes)
        {
            csv.AppendLine(string.Join(',', new[]
            {
                Csv(s.Id),
                Csv(s.ShowDate.ToString("dd/MM/yyyy")),
                Csv(s.StartTime),
                Csv(s.MovieTitle ?? s.MovieId),
                Csv(s.RoomName),
                Csv(s.TicketPrice.ToString("0")),
                Csv(s.TotalSeats.ToString()),
                Csv(s.AvailableSeats.ToString()),
                Csv(s.HeldSeats.ToString()),
                Csv(s.SoldSeats.ToString()),
                Csv(s.CheckedInSeats.ToString()),
                Csv(s.Status ?? string.Empty)
            }));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        var fileName = $"lich-chieu-{model.SelectedDate:yyyyMMdd}-{model.ToDate:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private async Task<AdminShowtimePageViewModel> BuildPageModelAsync(string? roomId, string? movieId, string? status, DateTime? date, DateTime? toDate, CancellationToken ct)
    {
        var fromDate = (date ?? DateTime.Today).Date;
        var endDate = (toDate ?? fromDate).Date;
        if (endDate < fromDate) endDate = fromDate;
        if ((endDate - fromDate).TotalDays > 31) endDate = fromDate.AddDays(31);

        var rooms = await GetDataAsync<List<AdminRoomViewModel>>("api/admin/rooms", ct) ?? new();
        var movies = await GetDataAsync<List<AdminMovieOptionViewModel>>("api/admin/movies", ct) ?? new();
        var showtimes = new List<AdminShowtimeViewModel>();

        for (var day = fromDate; day <= endDate; day = day.AddDays(1))
        {
            var url = $"api/admin/showtimes?date={day:yyyy-MM-dd}";
            if (!string.IsNullOrWhiteSpace(roomId)) url += $"&roomId={Uri.EscapeDataString(roomId)}";
            var rows = await GetDataAsync<List<AdminShowtimeViewModel>>(url, ct) ?? new();
            showtimes.AddRange(rows);
        }

        foreach (var item in showtimes)
            item.MovieTitle = movies.FirstOrDefault(x => x.Id == item.MovieId)?.Title ?? item.MovieId;

        if (!string.IsNullOrWhiteSpace(movieId))
            showtimes = showtimes.Where(x => x.MovieId == movieId).ToList();
        if (!string.IsNullOrWhiteSpace(status))
            showtimes = showtimes.Where(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();

        return new AdminShowtimePageViewModel
        {
            RoomId = roomId,
            MovieId = movieId,
            Status = status,
            SelectedDate = fromDate,
            ToDate = endDate,
            Rooms = rooms.OrderBy(x => x.Name).ToList(),
            Movies = movies.OrderBy(x => x.Title).ToList(),
            Showtimes = showtimes.OrderBy(x => x.ShowDate).ThenBy(x => x.RoomName).ThenBy(x => x.StartTime).ToList()
        };
    }

    private static bool IsSeatStatus(SeatViewModel seat, string status)
        => string.Equals(seat.Status, status, StringComparison.OrdinalIgnoreCase)
           || (status == "Available" && !seat.IsBooked && string.IsNullOrWhiteSpace(seat.Status));

    private static string Csv(string value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
