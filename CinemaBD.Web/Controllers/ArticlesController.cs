using CinemaBD.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Controllers;

[Route("goc-dien-anh")]
public class ArticlesController : Controller
{
    private readonly CinemaApiClient _apiClient;

    public ArticlesController(CinemaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var model = await _apiClient.GetArticlesAsync(page, 9, ct);
        return View(model);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var model = await _apiClient.GetArticleDetailsAsync(id, ct);
        return model == null ? NotFound() : View(model);
    }
}
