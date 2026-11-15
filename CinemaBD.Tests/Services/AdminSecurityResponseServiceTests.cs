using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Tests.Services;

public sealed class AdminSecurityResponseServiceTests
{
    private sealed class RecordingAuditWriter : ISecurityAuditWriter
    {
        public List<SecurityAuditEvent> Events { get; } = [];
        public Task WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ResponseActions_RevokeSessions_LockUnlock_AndRequirePasswordChange()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Customers.Add(new LegacyCustomer { MaKH = "KH1", HoTen = "Customer", TKhoan = "customer", MatKhau = "hash" });
        await db.SaveChangesAsync();
        var audit = new RecordingAuditWriter();
        var sessions = new IdentitySessionService(db, audit);
        var lockout = new AccountLockoutService(db, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var service = new AdminSecurityResponseService(db, sessions, new LockoutAdapter(lockout), audit);
        var sessionId = await sessions.CreateSessionAsync("KHACHHANG", "KH1");

        (await service.SetAccountLockAsync("KHACHHANG", "KH1", true)).Should().BeTrue();
        (await lockout.GetAsync("KHACHHANG", "customer")).IsLocked.Should().BeTrue();
        (await sessions.IsActiveAsync(sessionId)).Should().BeFalse();
        (await service.SetAccountLockAsync("KHACHHANG", "KH1", false)).Should().BeTrue();
        (await lockout.GetAsync("KHACHHANG", "customer")).FailedAttempts.Should().Be(0);

        var secondSession = await sessions.CreateSessionAsync("KHACHHANG", "KH1");
        (await service.ForcePasswordChangeAsync("KHACHHANG", "KH1")).Should().BeTrue();
        (await db.Customers.SingleAsync()).MustChangePassword.Should().BeTrue();
        (await sessions.IsActiveAsync(secondSession)).Should().BeFalse();
        audit.Events.Should().Contain(x => x.EventType == "admin_account_locked");
        audit.Events.Should().Contain(x => x.EventType == "admin_account_unlocked");
        audit.Events.Should().Contain(x => x.EventType == "admin_password_change_required");
    }

    private sealed class LockoutAdapter(AccountLockoutService service) : IAdministrativeAccountLockoutService
    {
        public Task<bool> SetAdministrativeLockAsync(string accountType, string accountId, bool locked, CancellationToken cancellationToken = default)
            => service.SetAdministrativeLockAsync(accountType, accountId, locked, cancellationToken);
    }

    [Fact]
    public async Task LockAccount_RefusesLastActiveMaster()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Roles.Add(new LegacyRole { MaCV = 1, TenChucVu = "Master", IsMaster = true, IsActive = true });
        db.Admins.Add(new LegacyAdmin { AdminID = 1, Username = "master", Password = "hash", IsActive = true, MaCV = 1 });
        await db.SaveChangesAsync();
        var audit = new RecordingAuditWriter();
        var service = new AdminSecurityResponseService(db, new IdentitySessionService(db), new LockoutAdapter(new AccountLockoutService(db, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build())), audit);

        var action = () => service.SetAccountLockAsync("Admin", "1", true);

        await action.Should().ThrowAsync<InvalidOperationException>();
        (await db.Admins.SingleAsync()).LockoutEndUtc.Should().BeNull();
        audit.Events.Should().NotContain(x => x.EventType == "admin_account_locked");
    }
}
