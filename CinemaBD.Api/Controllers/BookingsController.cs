using CinemaBD.Api.Contracts.Booking;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ISeatService _seatService;

    public BookingsController(IBookingService bookingService, ISeatService seatService)
    {
        _bookingService = bookingService;
        _seatService = seatService;
    }

    [HttpGet("showtimes/{showtimeId}/seats")]
    public async Task<IActionResult> GetSeats(string showtimeId, CancellationToken cancellationToken)
    {
        var seats = await _seatService.GetSeatsByShowtimeAsync(showtimeId, cancellationToken);
        var response = seats.Select(s => new SeatResponse(s.Id, s.Row, s.Column, s.SeatType, s.IsBooked, s.Status, s.Price));
        return Ok(new ApiResponse<object>(true, "Lấy sơ đồ ghế thành công", response));
    }

    [Authorize]
    [HttpPost("hold-seats")]
    public async Task<IActionResult> HoldSeats([FromBody] SeatHoldRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng", null));

        var result = await _bookingService.HoldSeatsAsync(userId, request.ShowtimeId, request.Seats, cancellationToken);
        return Ok(new ApiResponse<object>(result.Success, result.Message, result));
    }

    [Authorize]
    [HttpPost("release-seats")]
    public async Task<IActionResult> ReleaseSeats([FromBody] SeatHoldRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng", null));

        var result = await _bookingService.ReleaseSeatsAsync(userId, request.ShowtimeId, request.Seats, cancellationToken);
        return Ok(new ApiResponse<object>(result.Success, result.Message, result));
    }

    [Authorize]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng", null));

        var returnUrl = Request.Headers.TryGetValue("X-Return-Url", out var headerReturnUrl) ? headerReturnUrl.ToString() : null;
        var result = await _bookingService.CreateCheckoutAsync(userId, request.ShowtimeId, request.Seats, request.Combos, request.TotalAmount, returnUrl, cancellationToken);
        var response = new CheckoutResponse(result.TransactionRef, result.PaymentUrl, result.TotalAmount);
        return Ok(new ApiResponse<object>(true, "Tạo phiên thanh toán thành công", response));
    }

    [Authorize]
    [HttpPost("refund-request")]
    public async Task<IActionResult> CreateRefundRequest([FromBody] RefundRequestBody request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng", null));

        var result = await _bookingService.CreateRefundRequestAsync(userId, request.TransactionRef, request.TicketId, request.Reason, cancellationToken);
        return Ok(new ApiResponse<object>(result.Success, result.Message, result));
    }

    [HttpGet("invoice/{txnRef}")]
    public async Task<IActionResult> GetInvoice(string txnRef, CancellationToken cancellationToken)
    {
        var data = await _bookingService.GetInvoiceAsync(txnRef, cancellationToken);
        if (data == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy hóa đơn", null));

        var response = new InvoiceResponse(
            data.TransactionRef,
            data.PaymentId,
            data.PaymentStatus,
            data.TotalAmount,
            data.MovieId,
            data.MovieTitle,
            data.MoviePosterUrl,
            data.ShowDate,
            data.StartTime,
            data.RoomName,
            data.Seats,
            data.Tickets.Select(t => new InvoiceTicketResponse(
                t.TicketId,
                t.SeatId,
                t.Price,
                t.Status,
                t.IsCheckedIn,
                t.CheckedInAt)).ToList(),
            data.Combos);

        return Ok(new ApiResponse<object>(true, "Lấy hóa đơn thành công", response));
    }

    public sealed record SeatHoldRequest(string ShowtimeId, List<string> Seats);
    public sealed record RefundRequestBody(string TransactionRef, string? TicketId, string Reason);
}


