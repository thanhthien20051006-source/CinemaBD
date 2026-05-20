using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthApiService _adminAuthService;

    public AdminAuthController(IAdminAuthApiService adminAuthService)
    {
        _adminAuthService = adminAuthService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _adminAuthService.LoginAsync(request.Username, request.Password, cancellationToken);
        var response = new AdminAuthResponse(result.AdminId, result.Username, result.FullName, result.Role, result.Token);
        return Ok(new ApiResponse<object>(true, "Đăng nhập admin thành công", response));
    }
}

