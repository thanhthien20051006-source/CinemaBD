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
    private readonly IIdentitySessionService _identitySessionService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly CinemaBD.Api.Security.IdentityRequestResolver _identityRequestResolver;

    public AdminAuthController(IAdminAuthApiService adminAuthService, IRevokedTokenService revokedTokenService, IAccountPasswordService accountPasswordService, IIdentitySessionService identitySessionService = null!, IRefreshTokenService refreshTokenService = null!, CinemaBD.Api.Security.IdentityRequestResolver identityRequestResolver = null!)
    {
        _adminAuthService = adminAuthService;
        _revokedTokenService = revokedTokenService;
        _accountPasswordService = accountPasswordService;
        _identitySessionService = identitySessionService;
        _refreshTokenService = refreshTokenService;
        _identityRequestResolver = identityRequestResolver;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminAuthService.LoginAsync(request.Username, request.Password, request.DeviceName, Request.Headers.UserAgent.ToString(), cancellationToken);
        var response = new AdminAuthResponse(result.AdminId, result.Username, result.FullName, result.Role, result.Token, result.MustChangePassword, result.PasswordExpired, result.RefreshToken, result.SessionId);
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
        var sessionId = GetCurrentSessionId();
        var adminId = GetAdminId();
        if (sessionId is null || adminId is null || !await _identitySessionService.RevokeSessionAsync(sessionId.Value, "Admin", adminId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), "logout", cancellationToken))
            return Unauthorized(new ApiResponse<object>(false, "Không thể thu hồi phiên hiện tại.", null));
        return Ok(new ApiResponse<object>(true, "Đã đăng xuất admin thành công.", null));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] AdminRefreshRequest request, CancellationToken cancellationToken)
    {
        var verified = await _identityRequestResolver.ResolveAsync($"Bearer {request.AccessToken}", cancellationToken, allowExpired: true);
        if (verified is null || verified.AccountType != "Admin") return Unauthorized();
        var accountId = verified.AccountId;
        var result = await _refreshTokenService.RotateAsync(request.RefreshToken, "Admin", accountId, cancellationToken);
        if (result.SessionId != verified.SessionId || result.AccountType != "Admin" || result.AccountId != accountId) return Unauthorized();
        return Ok(new ApiResponse<object>(true, "Làm mới phiên thành công.", new { token = result.AccessToken, refreshToken = result.RefreshToken, sessionId = result.SessionId }));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        var sessionId = GetCurrentSessionId();
        if (adminId is null || sessionId is null) return Unauthorized();
        var sessions = await _identitySessionService.ListSessionsAsync("Admin", adminId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), sessionId.Value, cancellationToken);
        var response = sessions.Select(x => new CinemaBD.Api.Contracts.Auth.SessionResponse(x.SessionId, x.CreatedAtUtc, x.LastSeenAtUtc, x.ExpiresAtUtc, x.DeviceName, x.UserAgent, x.IsCurrent, x.IsRevoked));
        return Ok(new ApiResponse<object>(true, "Lấy danh sách phiên thành công.", response));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        if (adminId is null) return Unauthorized();
        var revoked = await _identitySessionService.RevokeSessionAsync(sessionId, "Admin", adminId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), "user_revoked", cancellationToken);
        return revoked ? Ok(new ApiResponse<object>(true, "Đã thu hồi phiên.", null)) : NotFound(new ApiResponse<object>(false, "Không tìm thấy phiên.", null));
    }

    private Guid? GetCurrentSessionId() => Guid.TryParse(User.FindFirst("sid")?.Value, out var sessionId) ? sessionId : null;

    private int? GetAdminId()
    {
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return subject is not null && subject.StartsWith("admin:", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(subject["admin:".Length..], out var adminId) && adminId > 0 ? adminId : null;
    }

    public sealed record AdminRefreshRequest(string AccessToken, string RefreshToken);
    public sealed record ChangeAdminPasswordRequest(string CurrentPassword, string NewPassword);
}

