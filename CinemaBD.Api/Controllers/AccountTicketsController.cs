using CinemaBD.Api.Contracts.Account;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Authorize(Policy = "NotPasswordChangeOnly")]
[Route("api/account")]
public sealed class AccountTicketsController : ControllerBase
{
    private readonly ICustomerProfileService _customerProfileService;
    private readonly IQrCodeSecurityService _qrCodeSecurityService;

    public AccountTicketsController(
        ICustomerProfileService customerProfileService,
        IQrCodeSecurityService qrCodeSecurityService)
    {
        _customerProfileService = customerProfileService;
        _qrCodeSecurityService = qrCodeSecurityService;
    }

    [Authorize(Policy = "NotPasswordChangeOnly")]
    [HttpGet("tickets")]
    public async Task<IActionResult> GetMyTickets(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng.", null));

        var tickets = await _customerProfileService.GetTicketsAsync(userId, cancellationToken);
        var response = tickets.Select(ticket =>
        {
            var payload = _qrCodeSecurityService.SignTicket(
                ticket.TransactionRef,
                ticket.PaymentId,
                ticket.TicketId,
                userId,
                ticket.ShowtimeId);
            if (!_qrCodeSecurityService.TryVerifyTicket(payload, out var verifiedQr) || verifiedQr == null)
                throw new InvalidOperationException("Không thể tạo mã QR hợp lệ cho vé.");
            var issuedAt = new DateTimeOffset(verifiedQr.IssuedAtUtc, TimeSpan.Zero);
            var signature = payload.Split('|').LastOrDefault() ?? string.Empty;

            return new CustomerTicketResponse(
                ticket.TicketId,
                ticket.TransactionRef,
                ticket.MovieTitle,
                ticket.MoviePosterUrl,
                ticket.ShowDate,
                ticket.StartTime,
                ticket.RoomName,
                ticket.SeatId,
                ticket.Price,
                ticket.Status,
                ticket.CheckedInAt,
                payload,
                signature,
                issuedAt.UtcDateTime);
        }).ToList();

        return Ok(new ApiResponse<object>(true, "Lấy danh sách vé thành công.", response));
    }
}
