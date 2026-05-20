using CinemaBD.Web.Core;
using CinemaBD.Web.Domain;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("phim")]
public class MoviesController : AdminApiCrudController
{
    public MoviesController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

    public async Task<IActionResult> Index(string? search, CancellationToken ct)
    {
        var movies = await GetDataAsync<List<Movie>>("api/admin/movies", ct) ?? new();
        foreach (var movie in movies)
            movie.PosterUrl = NormalizePosterUrl(movie.PosterUrl);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            movies = movies.Where(x => 
                Contains(x.Id, s) || Contains(x.Title, s) || Contains(x.Genre, s) || 
                Contains(x.Director, s) || Contains(x.Country, s)).ToList();
        }
        return View(movies.OrderByDescending(x => x.ReleaseDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();
        var movie = await GetDataAsync<Movie>($"api/admin/movies/{Uri.EscapeDataString(id)}", ct);
        if (movie == null) return NotFound();
        movie.PosterUrl = NormalizePosterUrl(movie.PosterUrl);
        return Json(movie);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAjax(Movie model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Title)) return Json(new { success = false, message = "Tiêu đề phim là bắt buộc." });
        
        var body = new
        {
            model.Title,
            model.Genre,
            DurationMinutes = model.DurationMinutes,
            model.Director,
            model.Cast,
            model.Country,
            model.AgeRestriction,
            model.Description,
            model.PosterUrl,
            model.TrailerUrl,
            model.ReleaseDate,
            model.EndDate,
            model.Status
        };

        var ok = string.IsNullOrWhiteSpace(model.Id)
            ? await SendAsync(HttpMethod.Post, "api/admin/movies", body, ct)
            : await SendAsync(HttpMethod.Put, $"api/admin/movies/{Uri.EscapeDataString(model.Id)}", body, ct);

        return Json(new { success = ok, message = ok ? "Lưu phim thành công." : "Không lưu được phim." });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAjax(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return Json(new { success = false, message = "Thiếu mã phim." });
        var ok = await SendAsync(HttpMethod.Delete, $"api/admin/movies/{Uri.EscapeDataString(id)}", null, ct);
        return Json(new { success = ok, message = ok ? "Đã xóa phim." : "Không xóa được phim." });
    }

    private string NormalizePosterUrl(string? posterUrl)
    {
        if (string.IsNullOrWhiteSpace(posterUrl))
            return string.Empty;

        if (Uri.TryCreate(posterUrl, UriKind.Absolute, out _))
            return posterUrl;

        var fileName = Path.GetFileName(posterUrl.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        return Url.Content($"~/legacy/Content/img/Posters/{fileName}");
    }

    private static bool Contains(string? source, string value) => source?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;
}

[AdminPermission("phim")]
public class AdminPhimController : MoviesController
{
    public AdminPhimController(HttpClient http, IConfiguration configuration) : base(http, configuration) { }
}


