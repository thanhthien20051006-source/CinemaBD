using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaBD.Api.Controllers.Admin;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
internal sealed class AdminPermissionAttribute : TypeFilterAttribute
{
    public AdminPermissionAttribute(string permission) : base(typeof(AdminPermissionFilter))
    {
        Arguments = [permission];
    }
}

internal sealed class AdminPermissionFilter(IAdminAuthorizationService authorizationService, string permission) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var subject = context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (subject is null || !subject.StartsWith("admin:", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(subject["admin:".Length..], out var adminId) || adminId <= 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        try
        {
            var access = await authorizationService.GetAccessForAdminAsync(adminId, context.HttpContext.RequestAborted);
            if (!access.IsMaster && !access.Permissions.Any(value => CinemaBD.Domain.Entities.AdminPermissionMatcher.Matches(value, [permission])))
                context.Result = new ForbidResult();
        }
        catch (InvalidOperationException)
        {
            context.Result = new ForbidResult();
        }
    }
}

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Authorize(Policy = "NotPasswordChangeOnly")]
[AdminPermission("phanquyen")]
[Route("api/admin/accounts")]
public sealed class AdminAccountsController(
    IAdminAccountService service,
    IAdminAuthorizationService authorizationService) : ControllerBase
{
    public static bool CanCreateAccount(bool isMaster, bool mfaVerified) => isMaster && mfaVerified;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminAccountCreateRequest request, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("mfa_verified", "true")) return Forbid();

        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (subject is null || !subject.StartsWith("admin:", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(subject["admin:".Length..], out var adminId) || adminId <= 0)
            return Forbid();

        bool isMaster;
        try
        {
            var access = await authorizationService.GetAccessForAdminAsync(adminId, cancellationToken);
            isMaster = access.IsMaster;
            if (!CanCreateAccount(isMaster, mfaVerified: true)) return Forbid();
        }
        catch (InvalidOperationException)
        {
            return Forbid();
        }

        try
        {
            var created = await service.CreateAsync(request.Username, request.FullName, request.RoleId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                new ApiResponse<object>(true, "Tạo tài khoản quản trị thành công.", new
                {
                    AdminId = created.AdminId,
                    TemporaryPassword = created.TemporaryPassword
                }));
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new ApiResponse<object>(false, "Không thể tạo tài khoản quản trị.", null));
        }
    }
}
