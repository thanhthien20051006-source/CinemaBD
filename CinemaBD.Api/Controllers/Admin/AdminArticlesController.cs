using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;
[ApiController, Authorize, Route("api/admin/articles")]
public class AdminArticlesController : ControllerBase
{
    private readonly IAdminArticleService _service;
    public AdminArticlesController(IAdminArticleService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(ct)));
    [HttpGet("{id:int}")] public async Task<IActionResult> GetById(int id, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetByIdAsync(id, ct)));
    [HttpPost] public async Task<IActionResult> Create([FromBody] ArticleRequest request, CancellationToken ct) { try { return Ok(new ApiResponse<object>(true, "OK", await _service.CreateAsync(request.Title, request.Summary, request.Content, request.ImageUrl, ct))); } catch (InvalidOperationException ex) { return BadRequest(new ApiResponse<object>(false, ex.Message, null)); } }
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, [FromBody] ArticleRequest request, CancellationToken ct) { try { return Ok(new ApiResponse<object>(true, "OK", await _service.UpdateAsync(id, request.Title, request.Summary, request.Content, request.ImageUrl, ct))); } catch (InvalidOperationException ex) { return BadRequest(new ApiResponse<object>(false, ex.Message, null)); } }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) => Ok(new ApiResponse<object>(await _service.DeleteAsync(id, ct), "OK", null));
}

