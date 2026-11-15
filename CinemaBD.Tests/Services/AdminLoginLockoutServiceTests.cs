using CinemaBD.Application.Exceptions;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CinemaBD.Tests.Services;

public sealed class AdminLoginLockoutServiceTests
{
    [Fact]
    public async Task LoginAsync_TemporaryLockThrowsSpecificExceptionWithoutExtendingLockOrIncrementingCount()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var lockoutEnd = DateTime.UtcNow.AddMinutes(12);
        fixture.Db.Admins.Add(new LegacyAdmin
        {
            AdminID = 37,
            Username = "locked_admin",
            Password = new Pbkdf2PasswordHasher().Hash("CorrectPassword123!"),
            AccessFailedCount = 5,
            LockoutEndUtc = lockoutEnd,
            IsActive = true
        });
        await fixture.Db.SaveChangesAsync();
        var originalCount = (await fixture.Db.Admins.AsNoTracking().SingleAsync()).AccessFailedCount;
        var originalEnd = (await fixture.Db.Admins.AsNoTracking().SingleAsync()).LockoutEndUtc;
        var service = new AdminAuthApiService(
            fixture.Db,
            new Pbkdf2PasswordHasher(),
            Mock.Of<ITokenService>(),
            Mock.Of<IIdentitySessionService>(),
            Mock.Of<IAdminAuthorizationService>(),
            lockoutService: new AccountLockoutService(fixture.Db, Configuration()));

        var exception = await Assert.ThrowsAsync<AccountLockedException>(() => service.LoginAsync("locked_admin", "password"));

        exception.LockoutEndUtc.Should().Be(originalEnd);
        var after = await fixture.Db.Admins.AsNoTracking().SingleAsync();
        after.AccessFailedCount.Should().Be(originalCount);
        after.LockoutEndUtc.Should().Be(originalEnd);
    }

    [Fact]
    public async Task LoginAsync_ManualLockThrowsSpecificExceptionWithMaxValue()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.Admins.Add(new LegacyAdmin
        {
            AdminID = 38,
            Username = "manual_lock",
            Password = "not-needed",
            AccessFailedCount = 5,
            LockoutEndUtc = DateTime.MaxValue,
            IsActive = true
        });
        await fixture.Db.SaveChangesAsync();
        var service = new AdminAuthApiService(
            fixture.Db,
            new Pbkdf2PasswordHasher(),
            Mock.Of<ITokenService>(),
            Mock.Of<IIdentitySessionService>(),
            Mock.Of<IAdminAuthorizationService>(),
            lockoutService: new AccountLockoutService(fixture.Db, Configuration()));

        var exception = await Assert.ThrowsAsync<AccountLockedException>(() => service.LoginAsync("manual_lock", "password"));

        exception.LockoutEndUtc.Should().Be(DateTime.MaxValue);
    }

    [Fact]
    public async Task LoginAsync_ExpiredLockIsResetAndDoesNotThrowLockException()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.Admins.Add(new LegacyAdmin
        {
            AdminID = 39,
            Username = "expired_lock",
            Password = "not-needed",
            AccessFailedCount = 4,
            LockoutEndUtc = DateTime.UtcNow.AddMinutes(-2),
            IsActive = true
        });
        await fixture.Db.SaveChangesAsync();
        var service = new AdminAuthApiService(
            fixture.Db,
            new Pbkdf2PasswordHasher(),
            Mock.Of<ITokenService>(),
            Mock.Of<IIdentitySessionService>(),
            Mock.Of<IAdminAuthorizationService>(),
            lockoutService: new AccountLockoutService(fixture.Db, Configuration()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync("expired_lock", "wrong-password"));

        exception.Should().NotBeOfType<AccountLockedException>();
        exception.Message.Should().Be("Tài khoản hoặc mật khẩu không chính xác.");
        var after = await fixture.Db.Admins.AsNoTracking().SingleAsync();
        after.AccessFailedCount.Should().Be(1);
        after.LockoutEndUtc.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_UnknownUsernameKeepsGenericErrorAndDoesNotRevealLockout()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = new AdminAuthApiService(
            fixture.Db,
            new Pbkdf2PasswordHasher(),
            Mock.Of<ITokenService>(),
            Mock.Of<IIdentitySessionService>(),
            Mock.Of<IAdminAuthorizationService>(),
            lockoutService: new AccountLockoutService(fixture.Db, Configuration()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync("missing_admin", "wrong-password"));

        exception.Should().NotBeOfType<AccountLockedException>();
        exception.Message.Should().Be("Tài khoản hoặc mật khẩu không chính xác.");
    }

    private static IConfiguration Configuration()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:AccountLockout:MaxFailedAttempts"] = "5",
            ["Security:AccountLockout:LockoutMinutes"] = "15"
        }).Build();

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext Db { get; }

        private SqliteFixture(SqliteConnection connection, AppDbContext db)
        {
            _connection = connection;
            Db = db;
        }

        public static async Task<SqliteFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            await using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                await pragma.ExecuteNonQueryAsync();
            }
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var db = new AppDbContext(options);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE "Admin" ("AdminID" INTEGER NOT NULL PRIMARY KEY, "Username" TEXT NOT NULL, "Password" TEXT NOT NULL, "AccessFailedCount" INTEGER NULL, "LockoutEndUtc" TEXT NULL, "IsActive" INTEGER NOT NULL DEFAULT 1, "FullName" TEXT NULL, "Email" TEXT NULL, "Phone" TEXT NULL, "Role" TEXT NULL, "CreatedAt" TEXT NULL, "MaCV" INTEGER NULL, "MustChangePassword" INTEGER NOT NULL DEFAULT 0, "PasswordExpiresAt" TEXT NULL);
                CREATE TABLE "KHACHHANG" ("MaKH" TEXT NOT NULL PRIMARY KEY, "HoTen" TEXT NOT NULL, "TKhoan" TEXT NOT NULL, "MatKhau" TEXT NOT NULL, "AccessFailedCount" INTEGER NULL, "LockoutEndUtc" TEXT NULL, "Email" TEXT NULL, "SDT" TEXT NULL, "NgaySinh" TEXT NULL, "MustChangePassword" INTEGER NOT NULL DEFAULT 0, "PasswordExpiresAt" TEXT NULL);
                """;
            await command.ExecuteNonQueryAsync();
            return new SqliteFixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
