using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;

    public ArticlesController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 9, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var articles = await _articleService.GetPagedAsync(page, pageSize, ct);
        var total = await _articleService.GetTotalCountAsync(ct);

        return Ok(new ApiResponse<object>(true, "OK", new
        {
            Items = articles,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize))
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var article = await _articleService.GetByIdAsync(id, ct);
        return article == null
            ? NotFound(new ApiResponse<object>(false, "Không tìm thấy bài viết.", null))
            : Ok(new ApiResponse<object>(true, "OK", article));
    }
}
