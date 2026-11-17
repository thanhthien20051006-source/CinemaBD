using CinemaBD.Application.Exceptions;
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

namespace CinemaBD.Tests.Auth;

public sealed class CustomerLockedMessageTests
{
    private const string LockedMessage = "Tài khoản đang bị khóa. Vui lòng liên hệ quản trị viên.";
    private const string GenericMessage = "Tài khoản hoặc mật khẩu không chính xác.";

    [Fact]
    public async Task LockedCustomer_WithWrongPassword_ReceivesGenericErrorAndAuditsFailure()
    {
        using var db = CreateDatabase();
        var setup = await CreateServiceAsync(db, locked: true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => setup.Service.LoginAsync("customer", "wrong password"));

        exception.Message.Should().Be(GenericMessage);
        setup.Audit.Events.Should().ContainSingle(item => item.EventType == "login_failed");
        (await setup.Lockout.GetAsync("KHACHHANG", "customer")).FailedAttempts.Should().Be(3);
    }

    [Fact]
    public async Task LockedCustomer_WithCorrectPassword_ReceivesLockedAccountErrorWithoutLogin()
    {
        using var db = CreateDatabase();
        var setup = await CreateServiceAsync(db, locked: true);

        var exception = await Assert.ThrowsAsync<CustomerAccountLockedException>(
            () => setup.Service.LoginAsync("customer", "CorrectPassword123!"));

        exception.Message.Should().Be(LockedMessage);
        setup.Audit.Events.Should().NotContain(item => item.EventType == "login_success");
        (await setup.Lockout.GetAsync("KHACHHANG", "customer")).IsLocked.Should().BeTrue();
        db.IdentitySessions.Should().BeEmpty();
    }

    [Fact]
    public async Task UnlockedCustomer_WithCorrectPassword_CanLoginNormally()
    {
        using var db = CreateDatabase();
        var setup = await CreateServiceAsync(db, locked: false);

        var result = await setup.Service.LoginAsync("customer", "CorrectPassword123!");

        result.Token.Should().Be("access-token");
        setup.Audit.Events.Should().Contain(item => item.EventType == "login_success");
    }

    private static AppDbContext CreateDatabase()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(AuthService Service, AccountLockoutService Lockout, InMemoryAuditWriter Audit)> CreateServiceAsync(
        AppDbContext db, bool locked)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:AccountLockout:MaxFailedAttempts"] = "3",
            ["Security:AccountLockout:LockoutMinutes"] = "15",
            ["Security:AccountLockout:WarningThreshold"] = "1"
        }).Build();
        var hasher = new Pbkdf2PasswordHasher();
        db.Customers.Add(new LegacyCustomer
        {
            MaKH = "CUSTOMER-1",
            HoTen = "Customer",
            TKhoan = "customer",
            MatKhau = hasher.Hash("CorrectPassword123!"),
            AccessFailedCount = locked ? 3 : 0,
            LockoutEndUtc = locked ? DateTime.UtcNow.AddMinutes(10) : null
        });
        await db.SaveChangesAsync();

        var audit = new InMemoryAuditWriter();
        var lockout = new AccountLockoutService(db, configuration);
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(service => service.CreateToken(
                It.IsAny<UserAccount>(), It.IsAny<Guid>(), It.IsAny<bool>()))
            .Returns("access-token");
        var service = new AuthService(db, hasher, tokenService.Object, new IdentitySessionService(db),
            configuration, audit, NullLogger<AuthService>.Instance, lockout);
        return (service, lockout, audit);
    }

    private sealed class InMemoryAuditWriter : ISecurityAuditWriter
    {
        public List<SecurityAuditEvent> Events { get; } = [];

        public Task WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
