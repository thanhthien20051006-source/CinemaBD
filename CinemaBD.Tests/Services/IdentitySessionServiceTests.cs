using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Tests.Services;

public sealed class IdentitySessionServiceTests
{
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private sealed class RecordingAuditWriter : ISecurityAuditWriter
    {
        public List<SecurityAuditEvent> Events { get; } = [];
        public Task WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    [Fact]
    public async Task CreateSessionAsync_PersistsCanonicalIdentityAndBoundedDeviceMetadata()
    {
        using var db = CreateDb();
        var audit = new RecordingAuditWriter();
        var service = new IdentitySessionService(db, audit);
        var longLabel = new string('d', 140);
        var longAgent = new string('u', 600);

        var id = await service.CreateSessionAsync("KHACHHANG", "KH-123", longLabel, longAgent);

        var session = await db.IdentitySessions.SingleAsync();
        session.SessionId.Should().Be(id);
        session.AccountType.Should().Be("KHACHHANG");
        session.AccountId.Should().Be("KH-123");
        session.DeviceName.Should().HaveLength(100);
        session.UserAgent.Should().HaveLength(512);
        session.RevokedAtUtc.Should().BeNull();
        session.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
        (await service.IsActiveAsync(id)).Should().BeTrue();
        audit.Events.Should().ContainSingle(item => item.EventType == "login_success" && item.Result == "success");
    }

    [Fact]
    public async Task IsActiveAsync_RejectsRevokedExpiredAndMissingSessions()
    {
        using var db = CreateDb();
        var now = DateTime.UtcNow;
        var revokedId = Guid.NewGuid();
        var expiredId = Guid.NewGuid();
        db.IdentitySessions.AddRange(
            new IdentitySession { SessionId = revokedId, AccountType = "Admin", AccountId = "1", CreatedAtUtc = now, LastSeenAtUtc = now, ExpiresAtUtc = now.AddDays(1), RevokedAtUtc = now },
            new IdentitySession { SessionId = expiredId, AccountType = "Admin", AccountId = "1", CreatedAtUtc = now.AddDays(-2), LastSeenAtUtc = now.AddDays(-2), ExpiresAtUtc = now.AddSeconds(-1) });
        await db.SaveChangesAsync();
        var service = new IdentitySessionService(db);

        (await service.IsActiveAsync(revokedId)).Should().BeFalse();
        (await service.IsActiveAsync(expiredId)).Should().BeFalse();
        (await service.IsActiveAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task IsActiveAsync_QueriesAgainAfterSessionRevocation()
    {
        using var db = CreateDb();
        var service = new IdentitySessionService(db);
        var id = await service.CreateSessionAsync("KHACHHANG", "KH-456");
        (await service.IsActiveAsync(id)).Should().BeTrue();

        var session = await db.IdentitySessions.SingleAsync(x => x.SessionId == id);
        session.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        (await service.IsActiveAsync(id)).Should().BeFalse();
    }

    [Fact]
    public async Task RevokeSessionAsync_DoesNotAllowCrossAccountRevocation()
    {
        using var db = CreateDb();
        var audit = new RecordingAuditWriter();
        var service = new IdentitySessionService(db, audit);
        var id = await service.CreateSessionAsync("KHACHHANG", "KH-1");

        (await service.RevokeSessionAsync(id, "KHACHHANG", "KH-2", "logout")).Should().BeFalse();
        (await service.IsActiveAsync(id)).Should().BeTrue();
        audit.Events.Should().ContainSingle(item => item.EventType == "login_success");
    }

    [Fact]
    public async Task CreateSessionAsync_LogsWarningAndAuditForUnknownDevice()
    {
        using var db = CreateDb();
        var audit = new RecordingAuditWriter();
        var logger = new RecordingLogger<IdentitySessionService>();
        var service = new IdentitySessionService(db, audit, logger);
        await service.CreateSessionAsync("Admin", "3", "Known laptop", "Agent A");

        await service.CreateSessionAsync("Admin", "3", "Unrecognized phone", "Agent B");

        var alert = audit.Events.Should().ContainSingle(item => item.EventType == "new_device_login").Which;
        alert.CorrelationId.Should().NotBeNullOrWhiteSpace();
        logger.Messages.Should().Contain(message => message.Contains("unfamiliar device", StringComparison.OrdinalIgnoreCase)
            && message.Contains("CorrelationId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ListSessionsAsync_ReturnsOnlyOwnerSessionsAndMarksCurrent()
    {
        using var db = CreateDb();
        var service = new IdentitySessionService(db);
        var current = await service.CreateSessionAsync("Admin", "1", "Laptop", "Agent A");
        await service.CreateSessionAsync("Admin", "2", "Other", "Agent B");
        var rows = await service.ListSessionsAsync("Admin", "1", current);

        rows.Should().ContainSingle();
        rows.Single().IsCurrent.Should().BeTrue();
        rows.Single().DeviceName.Should().Be("Laptop");
    }
}
