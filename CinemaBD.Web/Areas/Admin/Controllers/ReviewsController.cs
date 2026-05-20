using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using CinemaBD.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Areas.Admin.Controllers;

[AdminPermission("phanhoi", "review", "danhgia")]
public class ReviewsController : BaseAdminController
{
    private readonly CinemaApiClient _apiClient;

    public ReviewsController(CinemaApiClient apiClient) => _apiClient = apiClient;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken") ?? HttpContext.Session.GetString("AdminToken") ?? string.Empty;
        var reviews = await _apiClient.GetAdminReviewsAsync(token, cancellationToken);
        var model = reviews.Select(x => new AdminReviewViewModel
        {
            Id = x.Id,
            MovieId = x.MovieId,
            CustomerId = x.CustomerId,
            Content = x.Content,
            Rating = x.Rating,
            IsHidden = x.IsHidden,
            CreatedAt = x.CreatedAt,
            MovieTitle = x.MovieTitle,
            CustomerName = x.CustomerName
        }).ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleHidden(int id, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken") ?? HttpContext.Session.GetString("AdminToken") ?? string.Empty;
        await _apiClient.ToggleAdminReviewHiddenAsync(token, id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("UserToken") ?? HttpContext.Session.GetString("AdminToken") ?? string.Empty;
        await _apiClient.DeleteAdminReviewAsync(token, id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
