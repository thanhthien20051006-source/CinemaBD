using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;
[ApiController, Authorize, Route("api/admin/seats")]
public class AdminSeatsController : ControllerBase
{
    private readonly IAdminSeatService _service;
    public AdminSeatsController(IAdminSeatService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get([FromQuery] string? roomId, [FromQuery] string? search, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetAllAsync(roomId, search, ct)));
    [HttpGet("map/{roomId}")] public async Task<IActionResult> Map(string roomId, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.GetSeatMapAsync(roomId, ct)));
    [HttpPost("{id}/toggle")] public async Task<IActionResult> Toggle(string id, [FromQuery] string? roomId, CancellationToken ct) => Ok(new ApiResponse<object>(true, "OK", await _service.ToggleStatusAsync(id, roomId, ct)));
}

