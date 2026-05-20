using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Api.Contracts.Movies;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/showtimes")]
public class AdminShowtimesController : ControllerBase
{
    private readonly IAdminShowtimeService _showtimeService;

    public AdminShowtimesController(IAdminShowtimeService showtimeService) => _showtimeService = showtimeService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? roomId, [FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        var data = await _showtimeService.GetAllAsync(roomId, date, cancellationToken);
        var response = data.Select(ToResponse);
        return Ok(new ApiResponse<object>(true, "Lay danh sach suat chieu thanh cong", response));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminShowtimeUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await _showtimeService.CreateAsync(request.MovieId, request.RoomId, request.ShowDate, request.StartTime, request.TicketPrice, cancellationToken);
        if (created == null)
            return BadRequest(new ApiResponse<object>(false, "Khong the tao suat chieu", null));

        return Ok(new ApiResponse<object>(true, "Tao suat chieu thanh cong", ToResponse(created)));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AdminShowtimeUpsertRequest request, CancellationToken cancellationToken)
    {
        var updated = await _showtimeService.UpdateAsync(id, request.MovieId, request.RoomId, request.ShowDate, request.StartTime, request.TicketPrice, request.Status, cancellationToken);
        if (updated == null)
            return NotFound(new ApiResponse<object>(false, "Khong tim thay suat chieu de cap nhat", null));

        return Ok(new ApiResponse<object>(true, "Cap nhat suat chieu thanh cong", ToResponse(updated)));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deleted = await _showtimeService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new ApiResponse<object>(false, "Khong tim thay suat chieu de xoa", null));

        return Ok(new ApiResponse<object>(true, "Da xoa hoac huy suat chieu thanh cong", null));
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, CancellationToken cancellationToken)
    {
        var cancelled = await _showtimeService.CancelAsync(id, cancellationToken);
        if (!cancelled)
            return NotFound(new ApiResponse<object>(false, "Khong tim thay suat chieu de huy", null));

        return Ok(new ApiResponse<object>(true, "Da huy suat chieu", null));
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] AdminShowtimeGenerateRequest request, CancellationToken cancellationToken)
    {
        var result = await _showtimeService.GenerateAsync(new ShowtimeGenerateRequest
        {
            MovieIds = request.MovieIds,
            RoomIds = request.RoomIds,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            StartTimes = request.StartTimes,
            TicketPrice = request.TicketPrice
        }, cancellationToken);

        return Ok(new ApiResponse<object>(true, $"Da tao {result.Created} suat, bo qua {result.Skipped} slot.", result));
    }

    [HttpPost("expire-passed")]
    public async Task<IActionResult> ExpirePassed(CancellationToken cancellationToken)
    {
        var count = await _showtimeService.ExpirePassedShowtimesAsync(cancellationToken);
        return Ok(new ApiResponse<object>(true, $"Da expire {count} suat chieu.", new { expired = count }));
    }

    private static ShowtimeResponse ToResponse(ShowtimeDetail s)
        => new(s.Id, s.ShowDate, s.StartTime, s.RoomId, s.RoomName, s.TicketPrice, s.TotalSeats, s.AvailableSeats, s.Status, s.HeldSeats, s.SoldSeats, s.CheckedInSeats, s.CanEdit, s.CanDelete);
}
