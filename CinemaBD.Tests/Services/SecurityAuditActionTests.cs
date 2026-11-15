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

    [Fact]
    public async Task Middleware_Audits403ForbiddenResponse_WithoutExposingTokensOrSecrets()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var httpAccessor = new HttpContextAccessor();
        var writer = new SecurityAuditWriter(db, httpAccessor);
        var middleware = new SecurityAuditMiddleware(_ => Task.CompletedTask, NullLogger<SecurityAuditMiddleware>.Instance);

        var http = new DefaultHttpContext();
        http.Request.Method = "GET";
        http.Request.Path = "/api/admin/restricted-endpoint";
        http.Request.QueryString = new QueryString("?secret_token=SuperSecretToken123&api_key=ConfidentialKey");
        http.Request.Headers.Authorization = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.SecretPayload.Signature";
        http.Request.Headers["X-Device-Name"] = "Admin Terminal";
        http.Request.Headers["User-Agent"] = "AuditTestAgent/1.0";
        http.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.15");
        http.Response.StatusCode = StatusCodes.Status403Forbidden;
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("account_type", "admin"),
            new Claim(ClaimTypes.NameIdentifier, "admin:99"),
            new Claim("sid", Guid.NewGuid().ToString())
        }, "Bearer"));
        httpAccessor.HttpContext = http;

        await middleware.InvokeAsync(http, writer);

        var records = await db.SecurityAudit.Where(x => x.EventType == "authorization_denied").ToListAsync();
        records.Should().HaveCount(1);
        var record = records.Single();
        record.EventType.Should().Be("authorization_denied");
        record.AccountType.Should().Be("admin");
        record.AccountId.Should().Be("99");
        record.SourceIp.Should().Be("10.0.0.15");

        // Verify that sensitive tokens and secrets are completely excluded
        record.Details.Should().NotBeNull();
        record.Details!.Should().Contain("GET /api/admin/restricted-endpoint denied.");
        record.Details.Should().NotContain("SuperSecretToken123");
        record.Details.Should().NotContain("ConfidentialKey");
        record.Details.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
    }

    [Fact]
    public async Task Middleware_WhenAlreadyAuditedByFilter_DoesNotDuplicateAuditRecord()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var httpAccessor = new HttpContextAccessor();
        var writer = new SecurityAuditWriter(db, httpAccessor);
        var filter = new SecurityAuditActionFilter(writer, NullLogger<SecurityAuditActionFilter>.Instance);
        var middleware = new SecurityAuditMiddleware(_ => Task.CompletedTask, NullLogger<SecurityAuditMiddleware>.Instance);

        var contextPair = CreateContext("GET", "/api/admin/reports", StatusCodes.Status403Forbidden, forbid: true);
        httpAccessor.HttpContext = contextPair.Context.HttpContext;

        // 1. Action filter executes and writes audit
        await filter.OnActionExecutionAsync(contextPair.Context, () => Task.FromResult(contextPair.Executed));

        // 2. Middleware executes on the same context
        await middleware.InvokeAsync(contextPair.Context.HttpContext, writer);

        // Deduplication must ensure only 1 authorization_denied record is written
        var count = await db.SecurityAudit.CountAsync(x => x.EventType == "authorization_denied");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Middleware_WhenStatusNotForbidden_DoesNotWriteAudit()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var httpAccessor = new HttpContextAccessor();
        var writer = new SecurityAuditWriter(db, httpAccessor);
        var middleware = new SecurityAuditMiddleware(_ => Task.CompletedTask, NullLogger<SecurityAuditMiddleware>.Instance);

        var http = new DefaultHttpContext();
        http.Request.Method = "GET";
        http.Request.Path = "/api/movies";
        http.Response.StatusCode = StatusCodes.Status200OK;
        httpAccessor.HttpContext = http;

        await middleware.InvokeAsync(http, writer);

        var count = await db.SecurityAudit.CountAsync();
        count.Should().Be(0);
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
