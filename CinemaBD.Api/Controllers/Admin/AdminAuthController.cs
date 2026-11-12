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

    public AdminAuthController(IAdminAuthApiService adminAuthService, IRevokedTokenService revokedTokenService)
    {
        _adminAuthService = adminAuthService;
        _revokedTokenService = revokedTokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminAuthService.LoginAsync(request.Username, request.Password, cancellationToken);
        var response = new AdminAuthResponse(result.AdminId, result.Username, result.FullName, result.Role, result.Token);
        return Ok(new ApiResponse<object>(true, "Đăng nhập admin thành công", response));
    }

    [Authorize(Policy = "AdminOnly")]
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
}

