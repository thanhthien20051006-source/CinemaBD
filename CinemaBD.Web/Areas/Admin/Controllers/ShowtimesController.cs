using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("suatchieu", "lichchieu")]
public class ShowtimesController : AdminApiCrudController
{
    public ShowtimesController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(string? roomId, string? movieId, string? status, DateTime? date, DateTime? toDate, CancellationToken ct)
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

        return View(new AdminShowtimePageViewModel
        {
            RoomId = roomId,
            MovieId = movieId,
            Status = status,
            SelectedDate = fromDate,
            ToDate = endDate,
            Rooms = rooms.OrderBy(x => x.Name).ToList(),
            Movies = movies.OrderBy(x => x.Title).ToList(),
            Showtimes = showtimes.OrderBy(x => x.ShowDate).ThenBy(x => x.RoomName).ThenBy(x => x.StartTime).ToList()
        });
    }
}
