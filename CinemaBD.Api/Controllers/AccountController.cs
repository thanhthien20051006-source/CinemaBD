using CinemaBD.Api.Contracts.Account;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly ICustomerProfileService _customerProfileService;
    private readonly ILoyaltyPointService _loyaltyPointService;

    public AccountController(ICustomerProfileService customerProfileService, ILoyaltyPointService loyaltyPointService)
    {
        _customerProfileService = customerProfileService;
        _loyaltyPointService = loyaltyPointService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng", null));

        var profile = await _customerProfileService.GetProfileAsync(userId, cancellationToken);
        if (profile == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy thông tin người dùng", null));

        var year = DateTime.UtcNow.Year;
        var totalSpent = await _customerProfileService.GetTotalSpendingAsync(userId, year, cancellationToken);
        var response = new CustomerProfileResponse(
            profile.Id,
            profile.Username,
            profile.FullName,
            profile.Email,
            profile.PhoneNumber,
            profile.BirthDate,
            totalSpent);

        return Ok(new ApiResponse<object>(true, "Lấy thông tin tài khoản thành công", response));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateCustomerProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng", null));

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new ApiResponse<object>(false, "Họ tên không được để trống", null));

        var updated = await _customerProfileService.UpdateProfileAsync(userId, new Domain.Entities.CustomerProfileUpdate
        {
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            BirthDate = request.BirthDate
        }, cancellationToken);

        if (updated == null)
            return NotFound(new ApiResponse<object>(false, "Không tìm thấy thông tin người dùng", null));

        var totalSpent = await _customerProfileService.GetTotalSpendingAsync(userId, DateTime.UtcNow.Year, cancellationToken);
        var response = new CustomerProfileResponse(
            updated.Id,
            updated.Username,
            updated.FullName,
            updated.Email,
            updated.PhoneNumber,
            updated.BirthDate,
            totalSpent);

        return Ok(new ApiResponse<object>(true, "Cập nhật thông tin cá nhân thành công", response));
    }

    [HttpGet("loyalty")]
    public async Task<IActionResult> GetLoyalty(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng", null));

        return Ok(new ApiResponse<object>(true, "OK", await _loyaltyPointService.GetOrCreateAsync(userId, cancellationToken)));
    }

    [HttpPost("loyalty/preview-redeem")]
    public async Task<IActionResult> PreviewRedeem([FromBody] LoyaltyRedeemRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng", null));

        var result = await _loyaltyPointService.PreviewRedeemAsync(userId, request.Points, request.Subtotal, cancellationToken);
        return Ok(new ApiResponse<object>(result.Success, result.Message, result));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được người dùng", null));

        var history = await _customerProfileService.GetHistoryAsync(userId, cancellationToken);
        var response = history.Select(x => new CustomerHistoryResponse(
            x.InvoiceId,
            x.MovieTitle,
            x.ShowDate,
            x.StartTime,
            x.TotalAmount,
            x.Status,
            x.PaymentDate,
            x.TicketCount,
            x.CheckedInCount,
            x.SeatIds));

        return Ok(new ApiResponse<object>(true, "Lấy lịch sử hóa đơn thành công", response));
    }
}


public sealed record LoyaltyRedeemRequest(int Points, decimal Subtotal);
