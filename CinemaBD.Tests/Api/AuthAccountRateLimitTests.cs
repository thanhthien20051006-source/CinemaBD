using CinemaBD.Api.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace CinemaBD.Tests.Api;

public sealed class AuthAccountRateLimitTests
{
    [Fact]
    public async Task ConfiguredPerAccountThreshold_Returns429OnNextLoginRequest()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:RateLimits:AuthAccountPermitLimit"] = "1",
            ["Security:RateLimits:AuthWindowSeconds"] = "60"
        }).Build();
        using var limiter = new AuthAccountRateLimiter(config);
        var filter = new AuthAccountRateLimitFilter(limiter);

        var first = await Invoke(filter, "/api/auth/login", new { Username = "test-user" });
        var second = await Invoke(filter, "/api/auth/login", new { Username = "test-user" });

        first.Result.Should().BeNull();
        var rejected = second.Result.Should().BeOfType<ObjectResult>().Subject;
        rejected.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        second.HttpContext.Response.Headers.RetryAfter.ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshRequests_ArePartitionedByAccessTokenSubject()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:RateLimits:AuthAccountPermitLimit"] = "1",
            ["Security:RateLimits:AuthWindowSeconds"] = "60"
        }).Build();
        using var limiter = new AuthAccountRateLimiter(config);
        var filter = new AuthAccountRateLimitFilter(limiter);
        var accessToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "test", audience: "test",
            claims: [new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "admin:27")],
            expires: DateTime.UtcNow.AddMinutes(1));
        var rawAccessToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(accessToken);

        var first = await Invoke(filter, "/api/admin/auth/refresh", new { AccessToken = rawAccessToken, RefreshToken = "non-secret-placeholder" });
        var second = await Invoke(filter, "/api/admin/auth/refresh", new { AccessToken = rawAccessToken, RefreshToken = "other-placeholder" });

        first.Result.Should().BeNull();
        second.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<ActionExecutingContext> Invoke(IAsyncActionFilter filter, string path, object request)
    {
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        var action = new ActionContext(http, new RouteData(), new ControllerActionDescriptor());
        var context = new ActionExecutingContext(action, new List<IFilterMetadata>(), new Dictionary<string, object?> { ["request"] = request }, new object());
        var executed = new ActionExecutedContext(action, new List<IFilterMetadata>(), new object());
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(executed));
        return context;
    }
}
