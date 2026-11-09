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

    public AuthController(IAuthService authService, IRevokedTokenService revokedTokenService)
    {
        _authService = authService;
        _revokedTokenService = revokedTokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await _authService.LoginAsync(request.Username, request.Password, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Đăng nhập thành công", CreateResponse(token, request.Username, string.Empty)));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var token = await _authService.RegisterAsync(request.FullName, request.Username, request.Password, request.Email, request.PhoneNumber, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Đăng ký thành công", CreateResponse(token, request.Username, request.FullName)));
    }

    [AllowAnonymous]
    [HttpPost("google")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var token = await _authService.GoogleLoginAsync(request.IdToken, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Đăng nhập Google thành công", CreateResponse(token, string.Empty, string.Empty)));
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

    private static AuthResponse CreateResponse(string token, string fallbackUsername, string fallbackFullName)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var userId = jwt.Subject;
        var username = jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName)?.Value ?? fallbackUsername;
        var fullName = jwt.Claims.FirstOrDefault(x => x.Type == "full_name")?.Value ?? fallbackFullName;
        return new AuthResponse(userId, username, fullName, token);
    }
}
