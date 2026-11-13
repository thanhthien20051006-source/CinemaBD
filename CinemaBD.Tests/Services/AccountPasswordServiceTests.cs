using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CinemaBD.Tests.Services;

public sealed class AccountPasswordServiceTests
{
    [Fact]
    public async Task ChangeCustomerPassword_ChangesHashClearsRotationFlagsAndRevokesSession()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
        var hasher = new Pbkdf2PasswordHasher();
        db.Customers.Add(new LegacyCustomer
        {
            MaKH = "KH-test",
            HoTen = "Test Customer",
            TKhoan = "test-customer",
            MatKhau = hasher.Hash("OldPassword123!"),
            MustChangePassword = true,
            PasswordExpiresAt = DateTime.UtcNow.AddDays(-1)
        });
        db.IdentitySessions.AddRange(
            new IdentitySession { SessionId = Guid.NewGuid(), AccountType = "KHACHHANG", AccountId = "KH-test", CreatedAtUtc = DateTime.UtcNow, LastSeenAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddDays(1) },
            new IdentitySession { SessionId = Guid.NewGuid(), AccountType = "KHACHHANG", AccountId = "KH-other", CreatedAtUtc = DateTime.UtcNow, LastSeenAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddDays(1) });
        await db.SaveChangesAsync();
        var revokedTokens = new Mock<IRevokedTokenService>();
        var service = new AccountPasswordService(db, hasher, revokedTokens.Object, new IdentitySessionService(db));
        var expiry = DateTime.UtcNow.AddMinutes(5);

        await service.ChangeCustomerPasswordAsync("KH-test", "OldPassword123!", "NewPassword123!", "customer-jti", expiry);

        var customer = await db.Customers.SingleAsync();
        hasher.Verify("NewPassword123!", customer.MatKhau).Should().BeTrue();
        customer.MustChangePassword.Should().BeFalse();
        customer.PasswordExpiresAt.Should().BeNull();
        var sessions = await db.IdentitySessions.OrderBy(x => x.AccountId).ToListAsync();
        sessions.Should().ContainSingle(x => x.AccountId == "KH-test" && x.RevokedAtUtc != null);
        sessions.Should().ContainSingle(x => x.AccountId == "KH-other" && x.RevokedAtUtc == null);
        revokedTokens.Verify(x => x.RevokeAsync("customer-jti", expiry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeAdminPassword_ChangesHashClearsRotationFlagsAndRevokesSession()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
        var hasher = new Pbkdf2PasswordHasher();
        db.Admins.Add(new LegacyAdmin
        {
            AdminID = 27,
            Username = "test-admin",
            Password = hasher.Hash("OldPassword123!"),
            FullName = "Test Admin",
            IsActive = true,
            MaCV = 1,
            MustChangePassword = true,
            PasswordExpiresAt = DateTime.UtcNow.AddDays(-1)
        });
        db.IdentitySessions.AddRange(
            new IdentitySession { SessionId = Guid.NewGuid(), AccountType = "Admin", AccountId = "27", CreatedAtUtc = DateTime.UtcNow, LastSeenAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddDays(1) },
            new IdentitySession { SessionId = Guid.NewGuid(), AccountType = "Admin", AccountId = "28", CreatedAtUtc = DateTime.UtcNow, LastSeenAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddDays(1) });
        await db.SaveChangesAsync();
        var revokedTokens = new Mock<IRevokedTokenService>();
        var service = new AccountPasswordService(db, hasher, revokedTokens.Object, new IdentitySessionService(db));
        var expiry = DateTime.UtcNow.AddMinutes(5);

        await service.ChangeAdminPasswordAsync(27, "OldPassword123!", "NewPassword123!", "admin-jti", expiry);

        var admin = await db.Admins.SingleAsync();
        hasher.Verify("NewPassword123!", admin.Password).Should().BeTrue();
        admin.MustChangePassword.Should().BeFalse();
        admin.PasswordExpiresAt.Should().BeNull();
        var sessions = await db.IdentitySessions.OrderBy(x => x.AccountId).ToListAsync();
        sessions.Should().ContainSingle(x => x.AccountId == "27" && x.RevokedAtUtc != null);
        sessions.Should().ContainSingle(x => x.AccountId == "28" && x.RevokedAtUtc == null);
        revokedTokens.Verify(x => x.RevokeAsync("admin-jti", expiry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_DoesNotUpdateCredentialsOrRevoke()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
        var hasher = new Pbkdf2PasswordHasher();
        var originalHash = hasher.Hash("OldPassword123!");
        db.Customers.Add(new LegacyCustomer { MaKH = "KH-test", HoTen = "Test", TKhoan = "test", MatKhau = originalHash });
        await db.SaveChangesAsync();
        var revokedTokens = new Mock<IRevokedTokenService>();
        var service = new AccountPasswordService(db, hasher, revokedTokens.Object, new IdentitySessionService(db));

        var act = () => service.ChangeCustomerPasswordAsync("KH-test", "Incorrect123!", "NewPassword123!", "customer-jti", DateTime.UtcNow.AddMinutes(5));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Mật khẩu hiện tại không chính xác.");
        (await db.Customers.SingleAsync()).MatKhau.Should().Be(originalHash);
        revokedTokens.Verify(x => x.RevokeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
