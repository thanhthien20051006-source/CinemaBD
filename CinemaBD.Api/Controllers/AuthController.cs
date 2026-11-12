using CinemaBD.Api.Contracts.Auth;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
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

    public AuthController(IAuthService authService, IRevokedTokenService revokedTokenService, IAccountPasswordService accountPasswordService)
    {
        _authService = authService;
        _revokedTokenService = revokedTokenService;
        _accountPasswordService = accountPasswordService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Username, request.Password, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Đăng nhập thành công", CreateResponse(result.Token, request.Username, string.Empty, result.MustChangePassword, result.PasswordExpired)));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var token = await _authService.RegisterAsync(request.FullName, request.Username, request.Password, request.Email, request.PhoneNumber, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Đăng ký thành công", CreateResponse(token, request.Username, request.FullName, mustChangePassword: false, passwordExpired: false)));
    }

    [AllowAnonymous]
    [HttpPost("google")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var token = await _authService.GoogleLoginAsync(request.IdToken, cancellationToken);
        var restricted = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Any(x => x.Type == "password_change_only" && x.Value == "true");
        return Ok(new ApiResponse<object>(true, "Đăng nhập Google thành công", CreateResponse(token, string.Empty, string.Empty, restricted, restricted)));
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
        return Ok(new ApiResponse<object>(true, "Đã đăng xuất.", null));
    }

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

    private static AuthResponse CreateResponse(string token, string fallbackUsername, string fallbackFullName, bool mustChangePassword, bool passwordExpired)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var userId = jwt.Subject;
        var username = jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName)?.Value ?? fallbackUsername;
        var fullName = jwt.Claims.FirstOrDefault(x => x.Type == "full_name")?.Value ?? fallbackFullName;
        return new AuthResponse(userId, username, fullName, token, mustChangePassword, passwordExpired);
    }

    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
