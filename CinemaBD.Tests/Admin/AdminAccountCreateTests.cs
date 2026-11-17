using CinemaBD.Api.Controllers.Admin;
using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Tests.Admin;

public sealed class AdminAccountCreateTests
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
    public void GenerateTemporaryPassword_HasLengthAndAllCharacterClasses()
    {
        var password = AdminAccountService.GenerateTemporaryPassword();

        password.Should().HaveLength(16);
        password.Should().ContainAny("ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(x => x.ToString()));
        password.Should().ContainAny("abcdefghijklmnopqrstuvwxyz".Select(x => x.ToString()));
        password.Should().ContainAny("0123456789".Select(x => x.ToString()));
        password.Should().ContainAny("!@#$%^&*()-_=+".Select(x => x.ToString()));
    }

    [Theory]
    [InlineData("adm_123", true)]
    [InlineData("ab", false)]
    [InlineData("admin-name", false)]
    [InlineData("name with space", false)]
    [InlineData("abcdefghijklmnopqrstuvwxyz1234567", false)]
    public void ValidateUsername_EnforcesAllowedFormat(string username, bool expected)
    {
        AdminAccountService.IsValidUsername(username).Should().Be(expected);
    }

    [Fact]
    public void CreateAuthorization_RejectsCallerWhoIsNotMaster()
    {
        AdminAccountsController.CanCreateAccount(isMaster: false, mfaVerified: true).Should().BeFalse();
        AdminAccountsController.CanCreateAccount(isMaster: true, mfaVerified: false).Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_RejectsMasterRole()
    {
        await using var db = CreateDatabase();
        db.Roles.Add(new LegacyRole { MaCV = 1, TenChucVu = "Role", IsMaster = true, IsActive = true });
        await db.SaveChangesAsync();
        var audit = new RecordingAuditWriter();
        var service = new AdminAccountService(db, new Pbkdf2PasswordHasher(), audit);

        var action = () => service.CreateAsync("demo_admin", "Demo Admin", 1);

        await action.Should().ThrowAsync<InvalidOperationException>();
        (await db.Admins.CountAsync()).Should().Be(0);
        audit.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateUsernameWithoutCaseSensitivity()
    {
        await using var db = CreateDatabase();
        db.Roles.Add(new LegacyRole { MaCV = 2, TenChucVu = "Role", IsMaster = false, IsActive = true });
        db.Admins.Add(new LegacyAdmin { AdminID = 1, Username = "Demo_Admin", Password = "existing-hash", IsActive = true });
        await db.SaveChangesAsync();
        var service = new AdminAccountService(db, new Pbkdf2PasswordHasher(), new RecordingAuditWriter());

        var action = () => service.CreateAsync("demo_admin", "Demo Admin", 2);

        await action.Should().ThrowAsync<InvalidOperationException>();
        (await db.Admins.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ReturnsOneTimePasswordAndStoresOnlyItsHash()
    {
        await using var db = CreateDatabase();
        db.Roles.Add(new LegacyRole { MaCV = 2, TenChucVu = "Role", IsMaster = false, IsActive = true });
        await db.SaveChangesAsync();
        var audit = new RecordingAuditWriter();
        var hasher = new Pbkdf2PasswordHasher();
        var service = new AdminAccountService(db, hasher, audit);

        var result = await service.CreateAsync("demo_admin", "Demo Admin", 2);
        var saved = await db.Admins.SingleAsync();

        result.AdminId.Should().Be(saved.AdminID);
        result.TemporaryPassword.Should().HaveLength(16);
        saved.Password.Should().NotBe(result.TemporaryPassword);
        hasher.Verify(result.TemporaryPassword, saved.Password).Should().BeTrue();
        saved.MustChangePassword.Should().BeTrue();
        audit.Events.Should().ContainSingle(x => x.EventType == "admin_account_created"
            && x.ActorType == "Admin"
            && x.ActorId == saved.AdminID.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private static AppDbContext CreateDatabase()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
