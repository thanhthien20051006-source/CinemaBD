using System.Security.Claims;
using CinemaBD.Api.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CinemaBD.Tests.Security;

public sealed class AdminPermissionClaimAuthorizationFilterTests
{
    [Fact]
    public async Task AdminWithMatchingPermissionClaim_IsAllowed()
    {
        var context = CreateContext("AdminMovies", "permission", "phim");
        await new AdminPermissionClaimAuthorizationFilter().OnAuthorizationAsync(context);
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task AdminWithNoMatchingPermissionClaim_IsForbidden()
    {
        var context = CreateContext("AdminMovies", "permission", "voucher");
        await new AdminPermissionClaimAuthorizationFilter().OnAuthorizationAsync(context);
        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task MasterAdmin_IsAllowedWithoutGranularPermissionClaims()
    {
        var context = CreateContext("AdminMovies", "is_master", "true");
        await new AdminPermissionClaimAuthorizationFilter().OnAuthorizationAsync(context);
        context.Result.Should().BeNull();
    }

    private static AuthorizationFilterContext CreateContext(string controller, string claimType, string claimValue)
    {
        var claims = new[]
        {
            new Claim("account_type", "admin"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(claimType, claimValue)
        };
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")) };
        var action = new ControllerActionDescriptor { ControllerName = controller, ActionName = "Index" };
        var actionContext = new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(), action);
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }
}
