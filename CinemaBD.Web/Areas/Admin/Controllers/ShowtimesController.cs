using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("suatchieu", "lichchieu")]
public class ShowtimesController : AdminApiCrudController
{
    public ShowtimesController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(string? roomId, DateTime? date, CancellationToken ct)
    {
        var selectedDate = (date ?? DateTime.Today).Date;
        var rooms = await GetDataAsync<List<AdminRoomViewModel>>("api/admin/rooms", ct) ?? new();
        var movies = await GetDataAsync<List<AdminMovieOptionViewModel>>("api/admin/movies", ct) ?? new();
        var url = $"api/admin/showtimes?date={selectedDate:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(roomId)) url += $"&roomId={Uri.EscapeDataString(roomId)}";
        var showtimes = await GetDataAsync<List<AdminShowtimeViewModel>>(url, ct) ?? new();

        foreach (var item in showtimes)
            item.MovieTitle = movies.FirstOrDefault(x => x.Id == item.MovieId)?.Title ?? item.MovieId;

        return View(new AdminShowtimePageViewModel
        {
            RoomId = roomId,
            SelectedDate = selectedDate,
            Rooms = rooms.OrderBy(x => x.Name).ToList(),
            Movies = movies.OrderBy(x => x.Title).ToList(),
            Showtimes = showtimes.OrderBy(x => x.RoomName).ThenBy(x => x.StartTime).ToList()
        });
    }
}

