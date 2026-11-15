using CinemaBD.Api.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace CinemaBD.Tests.Security;

public sealed class PasswordChangeRestrictionFilterTests
{
    [Theory]
    [InlineData("/api/auth/password/change", "POST", true)]
    [InlineData("/api/admin/auth/password/change", "POST", true)]
    [InlineData("/api/auth/login", "POST", false)]
    [InlineData("/api/bookings/checkout", "POST", false)]
    [InlineData("/api/auth/logout", "POST", false)]
    [InlineData("/api/admin/auth/logout", "POST", false)]
    [InlineData("/api/auth/password/change/extra", "POST", false)]
    [InlineData("/api/auth/password/change", "GET", false)]
    public async Task RestrictedToken_AllowsOnlyPasswordChangeEndpoints(string path, string method, bool expectedAllowed)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("password_change_only", "true")], "Bearer"));
        var actionContext = new ActionContext(context, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var filterContext = new AuthorizationFilterContext(actionContext, Array.Empty<IFilterMetadata>());
        var filter = new PasswordChangeRestrictionFilter();

        await filter.OnAuthorizationAsync(filterContext);

        if (expectedAllowed)
            filterContext.Result.Should().BeNull();
        else
            filterContext.Result.Should().BeOfType<ForbidResult>();
    }

    [Theory]
    [InlineData("/api/account/profile", "GET")]
    [InlineData("/api/account/history", "GET")]
    [InlineData("/api/bookings/hold-seats", "POST")]
    [InlineData("/api/bookings/checkout", "POST")]
    public async Task RestrictedToken_ForbidsCustomerEndpoints(string path, string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("password_change_only", "true")], "Bearer"));
        var actionContext = new ActionContext(context, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var filterContext = new AuthorizationFilterContext(actionContext, Array.Empty<IFilterMetadata>());

        await new PasswordChangeRestrictionFilter().OnAuthorizationAsync(filterContext);

        filterContext.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task NormalToken_IsNotRestricted()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/bookings/checkout";
        context.Request.Method = "POST";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("account_type", "customer")], "Bearer"));
        var actionContext = new ActionContext(context, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var filterContext = new AuthorizationFilterContext(actionContext, Array.Empty<IFilterMetadata>());

        await new PasswordChangeRestrictionFilter().OnAuthorizationAsync(filterContext);

        filterContext.Result.Should().BeNull();
    }
}
