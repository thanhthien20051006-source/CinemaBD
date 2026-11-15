using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace CinemaBD.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Authorize(Policy = "NotPasswordChangeOnly")]
[Route("api/admin/security/accounts")]
public sealed class AdminSecurityResponseController(
    IAdminSecurityResponseService service,
    IAdminAuthorizationService authorizationService) : ControllerBase
{
    [HttpPost("sessions/revoke-all")]
    public async Task<IActionResult> RevokeAllSessions([FromBody] AdminSecurityAccountRequest request, CancellationToken cancellationToken)
    {
        if (!await HasSecurityPermissionAsync(cancellationToken)) return Forbid();
        return await ExecuteAsync(() => service.RevokeAllSessionsAsync(request.AccountType, request.AccountId, cancellationToken), "Sessions revoked.");
    }

    [HttpPost("lock")]
    public async Task<IActionResult> SetLock([FromBody] AdminSecurityLockRequest request, CancellationToken cancellationToken)
    {
        if (!await HasSecurityPermissionAsync(cancellationToken)) return Forbid();
        return await ExecuteAsync(() => service.SetAccountLockAsync(request.AccountType, request.AccountId, request.Locked, cancellationToken), request.Locked ? "Account locked." : "Account unlocked.");
    }

    [HttpPost("require-password-change")]
    public async Task<IActionResult> RequirePasswordChange([FromBody] AdminSecurityAccountRequest request, CancellationToken cancellationToken)
    {
        if (!await HasSecurityPermissionAsync(cancellationToken)) return Forbid();
        return await ExecuteAsync(() => service.ForcePasswordChangeAsync(request.AccountType, request.AccountId, cancellationToken), "Password change required; sessions revoked.");
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task<bool>> action, string message)
    {
        try
        {
            var succeeded = await action();
            if (!succeeded) return NotFound(new ApiResponse<object>(false, "Tài khoản không tồn tại hoặc loại tài khoản không hợp lệ.", null));
            return Ok(new ApiResponse<object>(true, message, null));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ApiResponse<object>(false, exception.Message, null));
        }
    }

    private async Task<bool> HasSecurityPermissionAsync(CancellationToken cancellationToken)
    {
        if (!User.HasClaim("mfa_verified", "true")) return false;
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (subject is null || !subject.StartsWith("admin:", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(subject["admin:".Length..], out var adminId) || adminId <= 0) return false;
        try
        {
            var access = await authorizationService.GetAccessForAdminAsync(adminId, cancellationToken);
            return access.IsMaster || access.Permissions.Any(permission => CinemaBD.Domain.Entities.AdminPermissionMatcher.Matches(permission, ["phanquyen"]));
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
