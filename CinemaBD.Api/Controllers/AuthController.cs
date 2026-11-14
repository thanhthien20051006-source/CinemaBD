using CinemaBD.Api.Contracts.Auth;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using CinemaBD.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IRevokedTokenService _revokedTokenService;
    private readonly IAccountPasswordService _accountPasswordService;
    private readonly IIdentitySessionService _identitySessionService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IdentityRequestResolver _identityRequestResolver;

    public AuthController(IAuthService authService, IRevokedTokenService revokedTokenService, IAccountPasswordService accountPasswordService, IIdentitySessionService identitySessionService = null!, IRefreshTokenService refreshTokenService = null!, IdentityRequestResolver identityRequestResolver = null!)
    {
        _authService = authService;
        _revokedTokenService = revokedTokenService;
        _accountPasswordService = accountPasswordService;
        _identitySessionService = identitySessionService;
        _refreshTokenService = refreshTokenService;
        _identityRequestResolver = identityRequestResolver;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _authService.LoginAsync(request.Username, request.Password, request.DeviceName, userAgent, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Đăng nhập thành công", CreateResponse(result.Token, request.Username, string.Empty, result.MustChangePassword, result.PasswordExpired, result.RefreshToken, result.SessionId)));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _authService.RegisterAsync(request.FullName, request.Username, request.Password, request.Email, request.PhoneNumber, request.DeviceName, userAgent, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Đăng ký thành công", CreateResponse(result.Token, request.Username, request.FullName, mustChangePassword: false, passwordExpired: false, result.RefreshToken, result.SessionId)));
    }

    [AllowAnonymous]
    [HttpPost("google")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.GoogleLoginAsync(request.IdToken, request.DeviceName, Request.Headers.UserAgent.ToString(), cancellationToken);
        var restricted = new JwtSecurityTokenHandler().ReadJwtToken(result.Token).Claims.Any(x => x.Type == "password_change_only" && x.Value == "true");
        return Ok(new ApiResponse<object>(true, "Đăng nhập Google thành công", CreateResponse(result.Token, string.Empty, string.Empty, restricted, restricted, result.RefreshToken, result.SessionId)));
    }

    [Authorize]
    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!HasAccountType("customer"))
            return Forbid();
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object>(false, "Thông tin đổi mật khẩu không hợp lệ.", null));

        var customerId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(customerId))
            return Unauthorized(new ApiResponse<object>(false, "Không xác định được tài khoản.", null));

        if (!IsCustomerSubject(customerId))
            return Forbid();

        var (jti, expiresAtUtc) = GetCurrentTokenIdentity();
        await _accountPasswordService.ChangeCustomerPasswordAsync(customerId, request.CurrentPassword, request.NewPassword, jti, expiresAtUtc, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.", null));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var expiresAt = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (string.IsNullOrWhiteSpace(jti) || !long.TryParse(expiresAt, out var expirySeconds))
            return Unauthorized(new ApiResponse<object>(false, "Không thể thu hồi phiên hiện tại.", null));

        await _revokedTokenService.RevokeAsync(jti, DateTimeOffset.FromUnixTimeSeconds(expirySeconds).UtcDateTime, cancellationToken);
        var sessionId = GetCurrentSessionId();
        if (sessionId is null || !await _identitySessionService.RevokeSessionAsync(sessionId.Value, "KHACHHANG", GetAccountId(), "logout", cancellationToken))
            return Unauthorized(new ApiResponse<object>(false, "Không thể thu hồi phiên hiện tại.", null));
        return Ok(new ApiResponse<object>(true, "Đã đăng xuất.", null));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized(new ApiResponse<object>(false, "Refresh token không hợp lệ.", null));
        var verified = await _identityRequestResolver.ResolveAsync($"Bearer {request.AccessToken}", cancellationToken, allowExpired: true);
        if (verified is null || verified.AccountType != "KHACHHANG")
            return Unauthorized(new ApiResponse<object>(false, "Phiên không hợp lệ.", null));
        var accountId = verified.AccountId;
        var sessionId = verified.SessionId;
        try
        {
            var result = await _refreshTokenService.RotateAsync(request.RefreshToken, "KHACHHANG", accountId, cancellationToken);
            if (result.SessionId != sessionId || result.AccountType != "KHACHHANG" || result.AccountId != accountId)
                return Unauthorized(new ApiResponse<object>(false, "Refresh token không thuộc phiên.", null));
            return Ok(new ApiResponse<object>(true, "Làm mới phiên thành công.", new { token = result.AccessToken, refreshToken = result.RefreshToken, sessionId = result.SessionId }));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ApiResponse<object>(false, "Refresh token không hợp lệ hoặc đã hết hạn.", null));
        }
    }

    [AllowAnonymous]
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var identity = await ParseIdentityFromAccessToken(Request.Headers.Authorization.ToString());
        if (identity is null) return Unauthorized();
        var sessions = await _identitySessionService.ListSessionsAsync("KHACHHANG", identity.Value.AccountId, identity.Value.SessionId, cancellationToken);
        var response = sessions.Select(x => new SessionResponse(x.SessionId, x.CreatedAtUtc, x.LastSeenAtUtc, x.ExpiresAtUtc, x.DeviceName, x.UserAgent, x.IsCurrent, x.IsRevoked));
        return Ok(new ApiResponse<object>(true, "Lấy danh sách phiên thành công.", response));
    }

    [AllowAnonymous]
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var identity = await ParseIdentityFromAccessToken(Request.Headers.Authorization.ToString());
        if (identity is null) return Unauthorized();
        var revoked = await _identitySessionService.RevokeSessionAsync(sessionId, "KHACHHANG", identity.Value.AccountId, "user_revoked", cancellationToken);
        return revoked ? Ok(new ApiResponse<object>(true, "Đã thu hồi phiên.", null)) : NotFound(new ApiResponse<object>(false, "Không tìm thấy phiên.", null));
    }

    private string GetAccountId() => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty;

    private async Task<(string AccountId, Guid SessionId)?> ParseIdentityFromAccessToken(string authorization)
    {
        var verified = await _identityRequestResolver.ResolveAsync(authorization, HttpContext.RequestAborted);
        return verified?.AccountType == "KHACHHANG" ? (verified.AccountId, verified.SessionId) : null;
    }

    private Guid? GetCurrentSessionId()
        => Guid.TryParse(User.FindFirst("sid")?.Value, out var sessionId) ? sessionId : null;

    private string? GetCanonicalAccountType()
        => string.Equals(User.FindFirst("account_type")?.Value, "customer", StringComparison.OrdinalIgnoreCase) ? "KHACHHANG" : null;

    public sealed record RefreshRequest(string AccessToken, string RefreshToken);

    private bool HasAccountType(string accountType)
        => string.Equals(User.FindFirst("account_type")?.Value, accountType, StringComparison.OrdinalIgnoreCase);

    private static bool IsCustomerSubject(string subject)
        => !subject.StartsWith("admin:", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(subject);

    private (string? Jti, DateTime? ExpiresAtUtc) GetCurrentTokenIdentity()
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var expiresAt = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        return long.TryParse(expiresAt, out var expirySeconds)
            ? (jti, DateTimeOffset.FromUnixTimeSeconds(expirySeconds).UtcDateTime)
            : (jti, null);
    }

    private static AuthResponse CreateResponse(string token, string fallbackUsername, string fallbackFullName, bool mustChangePassword, bool passwordExpired, string? refreshToken = null, Guid? sessionId = null)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var userId = jwt.Subject;
        var username = jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName)?.Value ?? fallbackUsername;
        var fullName = jwt.Claims.FirstOrDefault(x => x.Type == "full_name")?.Value ?? fallbackFullName;
        return new AuthResponse(userId, username, fullName, token, mustChangePassword, passwordExpired, refreshToken, sessionId);
    }

    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
