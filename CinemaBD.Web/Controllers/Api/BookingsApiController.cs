using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        var token = GetBearerToken()!;
        var data = await _booking.CheckoutAsync(token, request, cancellationToken);
        if (data is null)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Checkout thất bại" });

        return Ok(new ApiResponse<CheckoutResponse> { Success = true, Message = "OK", Data = data });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("invoice/{txnRef}")]
    public async Task<IActionResult> Invoice(string txnRef, CancellationToken cancellationToken)
    {
        var data = await _booking.GetInvoiceAsync(GetBearerToken()!, txnRef, cancellationToken);
        if (data is null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Không tìm thấy hóa đơn" });

        return Ok(new ApiResponse<InvoiceViewModel> { Success = true, Message = "OK", Data = data });
    }

    private string? GetBearerToken() => Request.Headers.Authorization.FirstOrDefault() is { } authorization
        && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : null;
}

