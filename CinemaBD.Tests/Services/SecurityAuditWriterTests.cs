using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Tests.Services;

public sealed class SecurityAuditWriterTests
{
    [Fact]
    public async Task WriteAsync_PersistsSafeActorContextWithoutCredentialMaterial()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var http = new DefaultHttpContext();
        http.Request.Method = "POST";
        http.Request.Path = "/api/auth/login";
        http.Request.Headers["X-Device-Name"] = "Android phone";
        http.Request.Headers["User-Agent"] = "CinemaBD-Mobile/1.0";
        http.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.0.2.12");
        var writer = new SecurityAuditWriter(db, new HttpContextAccessor { HttpContext = http });

        await writer.WriteAsync(new SecurityAuditEvent(
            "login_success",
            "KHACHHANG",
            "KH-001",
            Guid.NewGuid(),
            Result: "success",
            Details: "Login succeeded.",
            CorrelationId: "trace-safe-001"));

        var stored = await db.SecurityAudit.SingleAsync();
        stored.EventType.Should().Be("login_success");
        stored.AccountType.Should().Be("KHACHHANG");
        stored.AccountId.Should().Be("KH-001");
        stored.SourceIp.Should().Be("192.0.2.12");
        stored.Details.Should().Contain("CinemaBD-Mobile/1.0");
        stored.Details.Should().Contain("success");
        stored.OccurredAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        System.Text.Json.JsonSerializer.Serialize(stored).Should().NotContain("password");
        System.Text.Json.JsonSerializer.Serialize(stored).Should().NotContain("access-token-value");
        System.Text.Json.JsonSerializer.Serialize(stored).Should().NotContain("refresh-token-value");
        System.Text.Json.JsonSerializer.Serialize(stored).Should().NotContain("credential-hash-value");
    }

    [Theory]
    [InlineData("password=super-secret-value")]
    [InlineData("Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.signature")]
    [InlineData("credential hash 0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    public async Task WriteAsync_RejectsSensitiveValuesBeforePersistence(string details)
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var writer = new SecurityAuditWriter(db);
        await writer.WriteAsync(new SecurityAuditEvent("login_failed", Result: "failure", Details: details));

        var record = await db.SecurityAudit.SingleAsync();
        record.EventType.Should().Be("login_failed");
        record.Details.Should().NotContain(details);
        record.Details.Should().NotContain("hidden-value");
        record.Details.Should().Contain("Details omitted");
    }

    [Fact]
    public async Task WriteAsync_OmitsSensitiveFreeFormDetailsButKeepsNormalizedEvent()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var writer = new SecurityAuditWriter(db);
        await writer.WriteAsync(new SecurityAuditEvent("login_failed", Result: "failure", Details: "password=super-secret-value"));

        var record = await db.SecurityAudit.SingleAsync();
        record.EventType.Should().Be("login_failed");
        record.Details.Should().NotContain("super-secret-value");
        record.Details.Should().Contain("Details omitted");
    }
}
