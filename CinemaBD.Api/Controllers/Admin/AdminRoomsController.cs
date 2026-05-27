using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;
[ApiController, Authorize(Policy = "AdminOnly"), Route("api/admin/rooms")]
public class AdminRoomsController : ControllerBase
{
    private readonly IAdminRoomService _service;
    public AdminRoomsController(IAdminRoomService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get([FromQuery] string? cinemaId, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(cinemaId, ct)));
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var room = await _service.GetByIdAsync(id, ct);
        return room == null
            ? NotFound(new ApiResponse<object>(false, "Không tìm thấy phòng.", null))
            : Ok(new ApiResponse<object>(true, "OK", room));
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] RoomRequest request, CancellationToken ct)
    {
        try
        {
            var room = await _service.UpsertAsync(request.Id, request.Name, request.SeatCount, request.Status, ct);
            return Ok(new ApiResponse<object>(true, "Lưu phòng thành công", room));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object>(false, ex.Message, null));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>(false, ex.Message, null));
        }
    }

    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> Toggle(string id, CancellationToken ct)
    {
        var ok = await _service.ToggleStatusAsync(id, ct);
        if (!ok)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy phòng.", null));

        var room = await _service.GetByIdAsync(id, ct);
        return Ok(new ApiResponse<object>(true, "Cập nhật trạng thái phòng thành công", room));
    }

    public record RoomRequest(string? Id, string Name, int SeatCount, string? Status);
}

