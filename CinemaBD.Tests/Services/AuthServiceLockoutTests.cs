using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CinemaBD.Tests.Services;

public sealed class AuthServiceLockoutTests
{
    [Fact]
    public async Task CustomerLogin_UnknownAndWrongPasswordReturnSameError()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var hasher = new Pbkdf2PasswordHasher();
        db.Customers.Add(new LegacyCustomer { MaKH = "KH1", HoTen = "Customer", TKhoan = "alice", MatKhau = hasher.Hash("CorrectPassword123!") });
        await db.SaveChangesAsync();
        var lockout = new AccountLockoutService(db, Configuration(2, 15));
        var audit = new InMemoryAuditWriter();
        var service = new AuthService(db, hasher, Mock.Of<ITokenService>(), new IdentitySessionService(db), Configuration(2, 15), audit, NullLogger<AuthService>.Instance, lockout);

        var missing = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync("nobody", "wrong-password"));
        var existing = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync("alice", "wrong-password"));

        missing.Message.Should().Be(existing.Message);
        (await lockout.GetAsync("KHACHHANG", "alice")).IsLocked.Should().BeFalse();
        (await lockout.GetAsync("KHACHHANG", "nobody")).FailedAttempts.Should().Be(0, "only existing accounts have durable lockout columns; unknown-account probes remain protected by account/IP rate limits");
        audit.Events.Should().Contain(item => item.EventType == "login_failed" && item.Result == "failure");
    }

    [Fact]
    public async Task AdminLogin_LocksAtThresholdAndReturnsSameGenericErrorWhileLocked()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = Configuration(2, 15);
        var hasher = new Pbkdf2PasswordHasher();
        db.Admins.Add(new LegacyAdmin { AdminID = 27, Username = "alice", Password = hasher.Hash("CorrectPassword123!"), IsActive = true });
        await db.SaveChangesAsync();
        var lockout = new AccountLockoutService(db, config);
        var audit = new InMemoryAuditWriter();
        var service = new AdminAuthApiService(db, hasher, Mock.Of<ITokenService>(), new IdentitySessionService(db), new TestAdminAuthorizationService(), audit, NullLogger<AdminAuthApiService>.Instance, lockout);

        var first = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync("alice", "wrong-password"));
        var second = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync("alice", "wrong-password"));
        var locked = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync("alice", "wrong-password"));

        first.Message.Should().Be(second.Message).And.Be(locked.Message);
        (await lockout.GetAsync("Admin", "alice")).IsLocked.Should().BeTrue();
        audit.Events.Should().Contain(item => item.EventType == "account_locked");
        audit.Events.Should().Contain(item => item.EventType == "login_failed");
    }

    [Fact]
    public async Task CustomerLogin_LocksAfterThresholdAndResetsAfterSuccessfulLogin()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = Configuration(2, 15);
        var hasher = new Pbkdf2PasswordHasher();
        db.Customers.Add(new LegacyCustomer { MaKH = "KH-LOCK", HoTen = "Customer", TKhoan = "locked-customer", MatKhau = hasher.Hash("CorrectPassword123!") });
        await db.SaveChangesAsync();
        var clock = new ManualTimeProvider(DateTimeOffset.Parse("2026-11-13T10:00:00Z"));
        var lockout = new AccountLockoutService(db, config, clock);
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(mock => mock.CreateToken(It.IsAny<CinemaBD.Domain.Entities.UserAccount>(), It.IsAny<Guid>(), It.IsAny<bool>())).Returns("access-test-token");
        var service = new AuthService(db, hasher, tokenService.Object, new IdentitySessionService(db), config,
            new InMemoryAuditWriter(), NullLogger<AuthService>.Instance, lockout);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync("locked-customer", "wrong-password"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync("locked-customer", "wrong-password"));
        (await lockout.GetAsync("KHACHHANG", "locked-customer")).IsLocked.Should().BeTrue();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync("locked-customer", "wrong-password"));
        clock.Advance(TimeSpan.FromMinutes(15));
        (await service.LoginAsync("locked-customer", "CorrectPassword123!")).Token.Should().Be("access-test-token");
        (await lockout.GetAsync("KHACHHANG", "locked-customer")).FailedAttempts.Should().Be(0);
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }

    private static IConfiguration Configuration(int maxAttempts, int lockoutMinutes)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:AccountLockout:MaxFailedAttempts"] = maxAttempts.ToString(),
            ["Security:AccountLockout:LockoutMinutes"] = lockoutMinutes.ToString(),
            ["Security:AccountLockout:WarningThreshold"] = "1"
        }).Build();

    private sealed class InMemoryAuditWriter : ISecurityAuditWriter
    {
        public List<SecurityAuditEvent> Events { get; } = [];
        public Task WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class TestAdminAuthorizationService : IAdminAuthorizationService
    {
        public Task<AdminAuthorizationAccess> GetAccessForAdminAsync(int adminId, CancellationToken cancellationToken = default)
            => Task.FromResult(new AdminAuthorizationAccess(1, true, Array.Empty<string>()));
    }
}
