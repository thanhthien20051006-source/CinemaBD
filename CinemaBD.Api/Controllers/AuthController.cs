using CinemaBD.Api.Contracts.Auth;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace CinemaBD.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await _authService.LoginAsync(request.Username, request.Password, cancellationToken);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var userId = jwt.Subject;
        var username = jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName)?.Value ?? request.Username;
        var fullName = jwt.Claims.FirstOrDefault(x => x.Type == "full_name")?.Value ?? string.Empty;
        var response = new AuthResponse(userId, username, fullName, token);
        return Ok(new ApiResponse<object>(true, "Đăng nhập thành công", response));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var token = await _authService.RegisterAsync(request.FullName, request.Username, request.Password, request.Email, request.PhoneNumber, cancellationToken);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var userId = jwt.Subject;
        var username = jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName)?.Value ?? request.Username;
        var fullName = jwt.Claims.FirstOrDefault(x => x.Type == "full_name")?.Value ?? request.FullName;
        var response = new AuthResponse(userId, username, fullName, token);
        return Ok(new ApiResponse<object>(true, "Đăng ký thành công", response));
    }
}

