using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController, Authorize(Policy = "AdminOnly"), Route("api/admin/cinemas")]
public class AdminCinemasController : ControllerBase
{
    private readonly IAdminCinemaService _service;
    public AdminCinemasController(IAdminCinemaService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(ct)));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item == null ? NotFound(new ApiResponse<object>(false, "Không tìm thấy rạp", null)) : Ok(new ApiResponse<object>(true, "OK", item));
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] CinemaRequest request, CancellationToken ct)
    {
        var item = await _service.UpsertAsync(request.Id, request.Name, request.Address, request.Phone, request.Status, ct);
        return Ok(new ApiResponse<object>(true, "Lưu rạp thành công", item));
    }

    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> Toggle(string id, CancellationToken ct)
    {
        var ok = await _service.ToggleStatusAsync(id, ct);
        var item = ok ? await _service.GetByIdAsync(id, ct) : null;
        return Ok(new ApiResponse<object>(ok, ok ? "Cập nhật trạng thái rạp thành công" : "Không tìm thấy rạp", item));
    }

    public record CinemaRequest(string? Id, string Name, string? Address, string? Phone, string? Status);
}
