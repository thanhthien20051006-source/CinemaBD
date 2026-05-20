using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthCoreService _auth;

    public AuthApiController(IAuthCoreService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var data = await _auth.LoginAsync(request, cancellationToken);
        if (data is null)
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Sai tài khoản hoặc mật khẩu" });

        return Ok(new ApiResponse<AuthResponse> { Success = true, Message = "OK", Data = data });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var data = await _auth.RegisterAsync(request, cancellationToken);
        if (data is null)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Tài khoản đã tồn tại" });

        return Ok(new ApiResponse<AuthResponse> { Success = true, Message = "OK", Data = data });
    }
}

