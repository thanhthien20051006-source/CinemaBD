using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Api.Contracts.Movies;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/showtimes")]
public class AdminShowtimesController : ControllerBase
{
    private readonly IAdminShowtimeService _showtimeService;

    public AdminShowtimesController(IAdminShowtimeService showtimeService)
    {
        _showtimeService = showtimeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? roomId, [FromQuery] DateTime? date, CancellationToken cancellationToken)
    {
        var data = await _showtimeService.GetAllAsync(roomId, date, cancellationToken);
        var response = data.Select(s => new ShowtimeResponse(s.Id, s.ShowDate, s.StartTime, s.RoomId, s.RoomName, s.TicketPrice, s.TotalSeats, s.AvailableSeats, s.Status));
        return Ok(new ApiResponse<object>(true, "Lấy danh sách suất chiếu thành công", response));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminShowtimeUpsertRequest request, CancellationToken cancellationToken)
    {
        var created = await _showtimeService.CreateAsync(request.MovieId, request.RoomId, request.ShowDate, request.StartTime, request.TicketPrice, cancellationToken);
        if (created == null)
            return BadRequest(new ApiResponse<object>(false, "Không thể tạo suất chiếu", null));

        var response = new ShowtimeResponse(created.Id, created.ShowDate, created.StartTime, created.RoomId, created.RoomName, created.TicketPrice, created.TotalSeats, created.AvailableSeats, created.Status);
        return Ok(new ApiResponse<object>(true, "Tạo suất chiếu thành công", response));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AdminShowtimeUpsertRequest request, CancellationToken cancellationToken)
    {
        var updated = await _showtimeService.UpdateAsync(id, request.MovieId, request.RoomId, request.ShowDate, request.StartTime, request.TicketPrice, cancellationToken);
        if (updated == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy suất chiếu để cập nhật", null));

        var response = new ShowtimeResponse(updated.Id, updated.ShowDate, updated.StartTime, updated.RoomId, updated.RoomName, updated.TicketPrice, updated.TotalSeats, updated.AvailableSeats, updated.Status);
        return Ok(new ApiResponse<object>(true, "Cập nhật suất chiếu thành công", response));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deleted = await _showtimeService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy suất chiếu để xóa", null));

        return Ok(new ApiResponse<object>(true, "Xóa suất chiếu thành công", null));
    }

    /// <summary>
    /// Trigger thủ công: đánh dấu Expired cho tất cả suất chiếu đã qua giờ.
    /// Thường không cần gọi vì background service tự chạy mỗi phút.
    /// </summary>
    [HttpPost("expire-passed")]
    public async Task<IActionResult> ExpirePassed(CancellationToken cancellationToken)
    {
        var count = await _showtimeService.ExpirePassedShowtimesAsync(cancellationToken);
        return Ok(new ApiResponse<object>(true, $"Đã expire {count} suất chiếu.", new { expired = count }));
    }
}

