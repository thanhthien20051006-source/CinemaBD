using CinemaBD.Api.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace CinemaBD.Tests.Api;

public sealed class AuthRateLimitTests
{
    [Fact]
    public async Task IpLimiter_Returns429AfterConfiguredPermitLimit()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:RateLimits:AuthIpPermitLimit"] = "2",
            ["Security:RateLimits:AuthWindowSeconds"] = "60"
        }).Build();
        using var filter = new AuthRateLimitFilter(config);

        var first = await InvokeFilter(filter, "/api/auth/login", "alice");
        var second = await InvokeFilter(filter, "/api/auth/refresh", "bob");
        var third = await InvokeFilter(filter, "/api/admin/auth/login", "charlie");

        first.Result.Should().BeNull();
        second.Result.Should().BeNull();
        third.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<ActionExecutingContext> InvokeFilter(IAsyncActionFilter filter, string path, string username)
    {
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        http.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.0.2.25");
        var action = new ActionContext(http, new RouteData(), new ControllerActionDescriptor());
        var context = new ActionExecutingContext(action, new List<IFilterMetadata>(), new Dictionary<string, object?>
        {
            ["request"] = new LoginTestRequest(username)
        }, controller: new object());

        var executed = new ActionExecutedContext(action, new List<IFilterMetadata>(), controller: new object());
        await filter.OnActionExecutionAsync(context, () => Task.FromResult(executed));
        return context;
    }

    private sealed record LoginTestRequest(string Username);
}
