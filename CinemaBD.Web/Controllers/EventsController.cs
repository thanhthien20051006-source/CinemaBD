using CinemaBD.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Controllers;

[Route("su-kien")]
public class EventsController : Controller
{
    private readonly CinemaApiClient _apiClient;

    public EventsController(CinemaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var model = await _apiClient.GetEventsAsync(page, 9, ct);
        return View(model);
    }
}
