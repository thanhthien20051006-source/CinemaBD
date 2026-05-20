using CinemaBD.Web.Models;
using CinemaBD.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Controllers;

public class HomeController : Controller
{
    private readonly CinemaApiClient _apiClient;

    public HomeController(CinemaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(int showingPage = 1, int upcomingPage = 1, CancellationToken cancellationToken = default)
    {
        var model = await _apiClient.GetHomeDataAsync(showingPage, upcomingPage, 4, cancellationToken);
        return View(model);
    }

    [HttpGet("movies/{id}")]
    public async Task<IActionResult> Details(string id, DateTime? date, CancellationToken cancellationToken)
    {
        var selectedDate = date?.Date ?? DateTime.Today;
        var movie = await _apiClient.GetMovieByIdAsync(id, cancellationToken);
        if (movie == null)
            return NotFound();

        var showtimes = await _apiClient.GetShowtimesAsync(id, selectedDate, cancellationToken);
        var reviews = await _apiClient.GetReviewsAsync(id, cancellationToken);
        var token = HttpContext.Session.GetString("UserToken");
        var eligibility = string.IsNullOrWhiteSpace(token) ? null : await _apiClient.GetReviewEligibilityAsync(token, id, cancellationToken);
        return View(new MovieDetailsPageViewModel
        {
            Movie = movie,
            SelectedDate = selectedDate,
            Showtimes = showtimes,
            Reviews = reviews,
            ReviewEligibility = eligibility,
            ReviewMessage = TempData["ReviewMessage"] as string
        });
    }

    [HttpPost("movies/{id}/reviews")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReview(string id, string content, int rating, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["ReviewMessage"] = "Bạn cần đăng nhập để gửi đánh giá.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["ReviewMessage"] = "Nội dung đánh giá không được rỗng.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _apiClient.CreateReviewAsync(token, id, content, rating, cancellationToken);
        TempData["ReviewMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("showtimes/{showtimeId}/seats")]
    public async Task<IActionResult> Seats(string showtimeId, CancellationToken cancellationToken)
    {
        var seats = await _apiClient.GetSeatsAsync(showtimeId, cancellationToken);
        return View(new SeatSelectionPageViewModel
        {
            ShowtimeId = showtimeId,
            Seats = seats
        });
    }
}

