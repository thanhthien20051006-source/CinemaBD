using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;
public record ComboRequest(string Id, string Name, decimal Price, string? Description, string? ImageUrl);
public record GenreRequest(string Name, string? Description);
public record ArticleRequest(string Title, string? Summary, string? Content, string? ImageUrl);
public record EventRequest(string Title, string? Description, string? ImageUrl, DateTime? StartDate, DateTime? EndDate);
[ApiController, Authorize, Route("api/admin/genres")]
public class AdminGenresController : ControllerBase
{
    private readonly IAdminGenreService _service;
    public AdminGenresController(IAdminGenreService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(ct)));
    [HttpGet("{id:int}")] public async Task<IActionResult> GetById(int id, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetByIdAsync(id, ct)));
    [HttpPost] public async Task<IActionResult> Save([FromBody] GenreRequest request, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.UpsertAsync(0, request.Name, request.Description, ct)));
    [HttpPut("{id:int}")] public async Task<IActionResult> Save(int id, [FromBody] GenreRequest request, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.UpsertAsync(id, request.Name, request.Description, ct)));
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) => Ok(new ApiResponse<object>(await _service.DeleteAsync(id, ct), "OK", null));
}

