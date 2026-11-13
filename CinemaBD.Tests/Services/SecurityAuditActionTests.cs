using CinemaBD.Api.Security;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace CinemaBD.Tests.Services;

public sealed class SecurityAuditActionTests
{
    [Fact]
    public async Task ActionFilter_AuditsPasswordChangeAndForbidResult()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var httpAccessor = new HttpContextAccessor();
        var writer = new SecurityAuditWriter(db, httpAccessor);
        var filter = new SecurityAuditActionFilter(writer, NullLogger<SecurityAuditActionFilter>.Instance);

        var passwordContext = CreateContext("POST", "/api/auth/password/change", StatusCodes.Status200OK);
        httpAccessor.HttpContext = passwordContext.Context.HttpContext;
        await filter.OnActionExecutionAsync(passwordContext.Context, () => Task.FromResult(passwordContext.Executed));
        var deniedContext = CreateContext("GET", "/api/admin/movies", StatusCodes.Status403Forbidden, forbid: true);
        httpAccessor.HttpContext = deniedContext.Context.HttpContext;
        await filter.OnActionExecutionAsync(deniedContext.Context, () => Task.FromResult(deniedContext.Executed));

        var records = await db.SecurityAudit.OrderBy(record => record.EventType).ToListAsync();
        records.Any(record => record.EventType == "password_change" && record.Details is not null && record.Details.Contains("success", StringComparison.Ordinal)).Should().BeTrue();
        records.Any(record => record.EventType == "authorization_denied").Should().BeTrue();
        records.All(record => record.Details is null || !record.Details.Contains("secret", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    private static (ActionExecutingContext Context, ActionExecutedContext Executed) CreateContext(string method, string path, int status, bool forbid = false)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("account_type", "customer"),
                new Claim(ClaimTypes.NameIdentifier, "KH-TEST"),
                new Claim("sid", Guid.NewGuid().ToString())
            }, "Bearer"))
        };
        http.Request.Method = method;
        http.Request.Path = path;
        http.Request.Headers["X-Device-Name"] = "Test device";
        http.Request.Headers["User-Agent"] = "unit-test-agent";
        http.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.0.2.99");
        http.Response.StatusCode = status;
        var action = new ActionContext(http, new RouteData(), new ControllerActionDescriptor { RouteValues = new Dictionary<string, string?> { ["controller"] = "Auth" } });
        var filters = new List<IFilterMetadata>();
        var context = new ActionExecutingContext(action, filters, new Dictionary<string, object?>(), new object());
        var executed = new ActionExecutedContext(action, filters, new object())
        {
            Result = forbid ? new ForbidResult() : new StatusCodeResult(status)
        };
        return (context, executed);
    }
}
