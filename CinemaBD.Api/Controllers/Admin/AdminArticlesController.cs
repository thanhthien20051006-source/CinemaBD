using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;
[ApiController, Authorize(Policy = "AdminOnly"), Route("api/admin/articles")]
public class AdminArticlesController : ControllerBase
{
    private readonly IAdminArticleService _service;
    public AdminArticlesController(IAdminArticleService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(ct)));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var article = await _service.GetByIdAsync(id, ct);
        return article == null
            ? NotFound(new ApiResponse<object>(false, "Không tìm thấy bài viết.", null))
            : Ok(new ApiResponse<object>(true, "OK", article));
    }
    [HttpPost] public async Task<IActionResult> Create([FromBody] ArticleRequest request, CancellationToken ct) { try { return Ok(new ApiResponse<object>(true, "OK", await _service.CreateAsync(request.Title, request.Summary, request.Content, request.ImageUrl, ct))); } catch (InvalidOperationException ex) { return BadRequest(new ApiResponse<object>(false, ex.Message, null)); } }
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, [FromBody] ArticleRequest request, CancellationToken ct) { try { return Ok(new ApiResponse<object>(true, "OK", await _service.UpdateAsync(id, request.Title, request.Summary, request.Content, request.ImageUrl, ct))); } catch (InvalidOperationException ex) { return BadRequest(new ApiResponse<object>(false, ex.Message, null)); } }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        return deleted
            ? Ok(new ApiResponse<object>(true, "Xóa bài viết thành công", null))
            : NotFound(new ApiResponse<object>(false, "Không tìm thấy bài viết để xóa.", null));
    }
}

