using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Controllers.Api;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/account")]
public class AccountApiController : ControllerBase
{
    private readonly IAuthCoreService _auth;

    public AccountApiController(IAuthCoreService auth)
    {
        _auth = auth;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var token = GetBearerToken()!;

        var data = await _auth.GetProfileAsync(token, cancellationToken);
        if (data is null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Không tìm thấy user" });

        return Ok(new ApiResponse<UserProfileViewModel> { Success = true, Message = "OK", Data = data });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("history")]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var token = GetBearerToken()!;

        var data = await _auth.GetHistoryAsync(token, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<InvoiceHistoryItem>> { Success = true, Message = "OK", Data = data });
    }

    private string? GetBearerToken() => Request.Headers.Authorization.FirstOrDefault() is { } authorization
        && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : null;
}

