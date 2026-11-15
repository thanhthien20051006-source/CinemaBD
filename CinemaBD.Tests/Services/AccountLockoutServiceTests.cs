using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CinemaBD.Tests.Services;

public sealed class AccountLockoutServiceTests
{
    [Fact]
    public async Task RecordFailureAsync_LocksAtConfiguredThresholdAndUnlocksAfterDuration()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.Admins.Add(new LegacyAdmin { AdminID = 27, Username = "alice", Password = "hash", IsActive = true });
        await fixture.Db.SaveChangesAsync();
        var clock = new ManualTimeProvider(DateTimeOffset.Parse("2026-11-13T10:00:00Z"));
        var service = new AccountLockoutService(fixture.Db, Configuration(maxAttempts: 3, lockoutMinutes: 15), clock);

        var first = await service.RecordFailureAsync("Admin", "alice", "27");
        var second = await service.RecordFailureAsync("Admin", "alice", "27");
        var third = await service.RecordFailureAsync("Admin", "alice", "27");

        first.IsLocked.Should().BeFalse();
        second.IsLocked.Should().BeFalse();
        third.IsLocked.Should().BeTrue();
        third.LockoutEndUtc.Should().Be(clock.GetUtcNow().UtcDateTime.AddMinutes(15));
        (await service.RecordFailureAsync("Admin", "alice", "27")).FailedAttempts.Should().Be(3);

        clock.Advance(TimeSpan.FromMinutes(15));
        var expired = await service.GetAsync("Admin", "alice");
        expired.IsLocked.Should().BeFalse();
        expired.LockoutEndUtc.Should().NotBeNull();
        (await service.UnlockAsync("Admin", "alice")).Should().NotBeNull();
        (await service.GetAsync("Admin", "alice")).FailedAttempts.Should().Be(0);
        var accountUnlock = await service.RecordFailureAsync("Admin", "alice", "27");
        accountUnlock.FailedAttempts.Should().Be(1);
        accountUnlock.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task LockoutState_IsDurableAcrossServiceRecreation_ForAdminAndCustomer()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.Admins.Add(new LegacyAdmin { AdminID = 27, Username = "alice", Password = "hash", IsActive = true });
        fixture.Db.Customers.Add(new LegacyCustomer { MaKH = "KH-1", HoTen = "Customer", TKhoan = "alice", MatKhau = "hash" });
        await fixture.Db.SaveChangesAsync();
        var config = Configuration(maxAttempts: 2, lockoutMinutes: 15);
        var service = new AccountLockoutService(fixture.Db, config);

        (await service.RecordFailureAsync("Admin", "alice", "27")).FailedAttempts.Should().Be(1);
        (await service.RecordFailureAsync("Admin", "alice", "27")).IsLocked.Should().BeTrue();
        (await service.RecordFailureAsync("KHACHHANG", "alice", "KH-1")).FailedAttempts.Should().Be(1);

        fixture.Db.ChangeTracker.Clear();
        var restartedService = new AccountLockoutService(fixture.Db, config);
        (await restartedService.GetAsync("Admin", "alice")).IsLocked.Should().BeTrue();
        (await restartedService.GetAsync("KHACHHANG", "alice")).FailedAttempts.Should().Be(1);
        (await fixture.Db.Admins.AsNoTracking().SingleAsync()).AccessFailedCount.Should().Be(2);
        (await fixture.Db.Customers.AsNoTracking().SingleAsync()).AccessFailedCount.Should().Be(1);
    }

    [Fact]
    public async Task SetAdministrativeLockAsync_LocksAndUnlocksCustomerAndAdmin()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.Admins.Add(new LegacyAdmin { AdminID = 27, Username = "alice", Password = "hash", IsActive = true });
        fixture.Db.Customers.Add(new LegacyCustomer { MaKH = "KH-1", HoTen = "Customer", TKhoan = "customer", MatKhau = "hash" });
        await fixture.Db.SaveChangesAsync();
        var service = new AccountLockoutService(fixture.Db, Configuration(3, 15));

        (await service.SetAdministrativeLockAsync("Admin", "27", true)).Should().BeTrue();
        (await service.GetAsync("Admin", "alice")).IsLocked.Should().BeTrue();
        (await service.SetAdministrativeLockAsync("Admin", "27", false)).Should().BeTrue();
        (await service.GetAsync("Admin", "alice")).FailedAttempts.Should().Be(0);
        (await service.SetAdministrativeLockAsync("KHACHHANG", "KH-1", true)).Should().BeTrue();
        (await service.GetAsync("KHACHHANG", "customer")).IsLocked.Should().BeTrue();
    }

    private static IConfiguration Configuration(int maxAttempts, int lockoutMinutes)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:AccountLockout:MaxFailedAttempts"] = maxAttempts.ToString(),
            ["Security:AccountLockout:LockoutMinutes"] = lockoutMinutes.ToString()
        }).Build();

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }

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
