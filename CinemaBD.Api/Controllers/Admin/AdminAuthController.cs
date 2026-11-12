using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthApiService _adminAuthService;
    private readonly IRevokedTokenService _revokedTokenService;
    private readonly IAccountPasswordService _accountPasswordService;

    public AdminAuthController(IAdminAuthApiService adminAuthService, IRevokedTokenService revokedTokenService, IAccountPasswordService accountPasswordService)
    {
        _adminAuthService = adminAuthService;
        _revokedTokenService = revokedTokenService;
        _accountPasswordService = accountPasswordService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminAuthService.LoginAsync(request.Username, request.Password, cancellationToken);
        var response = new AdminAuthResponse(result.AdminId, result.Username, result.FullName, result.Role, result.Token, result.MustChangePassword, result.PasswordExpired);
        return Ok(new ApiResponse<object>(true, "Đăng nhập admin thành công", response));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeAdminPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(User.FindFirst("account_type")?.Value, "admin", StringComparison.OrdinalIgnoreCase))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object>(false, "Thông tin đổi mật khẩu không hợp lệ.", null));

        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (subject is null || !subject.StartsWith("admin:", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(subject["admin:".Length..], out var adminId) || adminId <= 0)
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được tài khoản quản trị.", null));

        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var expiry = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (string.IsNullOrWhiteSpace(jti) || !long.TryParse(expiry, out var expirySeconds))
            return Unauthorized(new ApiResponse<object>(false, "Phiên hiện tại không hợp lệ.", null));

        await _accountPasswordService.ChangeAdminPasswordAsync(adminId, request.CurrentPassword, request.NewPassword, jti, DateTimeOffset.FromUnixTimeSeconds(expirySeconds).UtcDateTime, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.", null));
    }

    [Authorize(Policy = "AdminOnly")]
    [Authorize(Policy = "NotPasswordChangeOnly")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var expiresAt = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (string.IsNullOrWhiteSpace(jti) || !long.TryParse(expiresAt, out var expirySeconds))
            return Unauthorized(new ApiResponse<object>(false, "Không thể thu hồi phiên hiện tại.", null));

        await _revokedTokenService.RevokeAsync(jti, DateTimeOffset.FromUnixTimeSeconds(expirySeconds).UtcDateTime, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Đã đăng xuất admin thành công.", null));
    }

    public sealed record ChangeAdminPasswordRequest(string CurrentPassword, string NewPassword);
}

