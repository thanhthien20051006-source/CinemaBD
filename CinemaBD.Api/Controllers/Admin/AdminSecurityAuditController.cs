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
[Route("api/admin/security/audit")]
public sealed class AdminSecurityAuditController(
    ISecurityAuditQueryService queryService,
    IAdminAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] SecurityAuditQueryRequest request, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("mfa_verified", "true")) return Forbid();
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (subject is null || !subject.StartsWith("admin:", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(subject["admin:".Length..], out var adminId) || adminId <= 0) return Forbid();
        try
        {
            var access = await authorizationService.GetAccessForAdminAsync(adminId, cancellationToken);
            if (!access.IsMaster && !access.Permissions.Any(permission => CinemaBD.Domain.Entities.AdminPermissionMatcher.Matches(permission, ["phanquyen"])))
                return Forbid();
        }
        catch (InvalidOperationException)
        {
            return Forbid();
        }

        try
        {
            var result = await queryService.QueryAsync(new SecurityAuditQuery(request.AccountType, request.AccountId, request.SessionId,
                request.FromUtc, request.ToUtc, request.EventType, request.Page, request.PageSize), cancellationToken);
            var response = new SecurityAuditPageResponse(result.Items.Select(item => new SecurityAuditItemResponse(item.AuditId, item.OccurredAtUtc,
                item.EventType, item.AccountType, item.AccountId, item.SessionId, item.CorrelationId, item.Details)).ToArray(), result.Page, result.PageSize, result.TotalCount);
            return Ok(new ApiResponse<object>(true, "Audit events loaded.", response));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ApiResponse<object>(false, exception.Message, null));
        }
    }
}
