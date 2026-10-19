using CinemaBD.Web.Core;
using CinemaBD.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Web.Controllers.Api;

[ApiController]
[Route("api/account")]
public class AccountApiController : ControllerBase
{
    private readonly IAuthCoreService _auth;

    public AccountApiController(IAuthCoreService auth)
    {
        _auth = auth;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var token = GetBearerToken();
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Thiếu thông tin user" });

        var data = await _auth.GetProfileAsync(token, cancellationToken);
        if (data is null)
            return NotFound(new ApiResponse<object> { Success = false, Message = "Không tìm thấy user" });

        return Ok(new ApiResponse<UserProfileViewModel> { Success = true, Message = "OK", Data = data });
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var token = GetBearerToken();
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "Thiếu thông tin user" });

        var data = await _auth.GetHistoryAsync(token, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<InvoiceHistoryItem>> { Success = true, Message = "OK", Data = data });
    }

    private string? GetBearerToken()
    {
        var authorization = Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization["Bearer ".Length..].Trim();

        return Request.Headers["X-User-Id"].FirstOrDefault();
    }
}

