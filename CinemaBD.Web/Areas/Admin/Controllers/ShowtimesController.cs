using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("suatchieu", "lichchieu")]
public class ShowtimesController : AdminApiCrudController
{
    public ShowtimesController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(string? roomId, string? movieId, string? status, DateTime? date, CancellationToken ct)
    {
        var selectedDate = (date ?? DateTime.Today).Date;
        var rooms = await GetDataAsync<List<AdminRoomViewModel>>("api/admin/rooms", ct) ?? new();
        var movies = await GetDataAsync<List<AdminMovieOptionViewModel>>("api/admin/movies", ct) ?? new();
        var url = $"api/admin/showtimes?date={selectedDate:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(roomId)) url += $"&roomId={Uri.EscapeDataString(roomId)}";
        var showtimes = await GetDataAsync<List<AdminShowtimeViewModel>>(url, ct) ?? new();

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
            SelectedDate = selectedDate,
            Rooms = rooms.OrderBy(x => x.Name).ToList(),
            Movies = movies.OrderBy(x => x.Title).ToList(),
            Showtimes = showtimes.OrderBy(x => x.RoomName).ThenBy(x => x.StartTime).ToList()
        });
    }
}

