using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Controllers.Api;

[ApiController]
[Route("api/bookings")]
public class BookingsApiController : ControllerBase
{
    private readonly IBookingCoreService _booking;

    public BookingsApiController(IBookingCoreService booking)
    {
        _booking = booking;
    }

    [HttpGet("showtimes/{showtimeId}/seats")]
    public async Task<IActionResult> GetSeats(string showtimeId, CancellationToken cancellationToken)
    {
        var data = await _booking.GetSeatsAsync(showtimeId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<SeatViewModel>> { Success = true, Message = "OK", Data = data });
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Thiếu thông tin user" });

        var data = await _booking.CheckoutAsync(userId, request, cancellationToken);
        if (data is null)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Checkout thất bại" });

        return Ok(new ApiResponse<CheckoutResponse> { Success = true, Message = "OK", Data = data });
    }

    [HttpGet("invoice/{txnRef}")]
    public async Task<IActionResult> Invoice(string txnRef, CancellationToken cancellationToken)
    {
        var data = await _booking.GetInvoiceAsync(txnRef, cancellationToken);
        if (data is null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Không tìm thấy hóa đơn" });

        return Ok(new ApiResponse<InvoiceViewModel> { Success = true, Message = "OK", Data = data });
    }
}

