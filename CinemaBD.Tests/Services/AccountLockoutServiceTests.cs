using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CinemaBD.Tests.Services;

public sealed class AccountLockoutServiceTests
{
    [Fact]
    public async Task RecordFailureAsync_LocksAtConfiguredThresholdAndUnlocksAfterDuration()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.Parse("2026-11-13T10:00:00Z"));
        var service = new AccountLockoutService(Configuration(maxAttempts: 3, lockoutMinutes: 15), clock);

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
    public async Task RecordFailureAsync_TracksCustomerAndAdminCountersIndependently()
    {
        var service = new AccountLockoutService(Configuration(maxAttempts: 2, lockoutMinutes: 15));
        await service.RecordFailureAsync("Admin", "alice", "27");
        var customer = await service.RecordFailureAsync("KHACHHANG", "alice", "KH-1");

        customer.IsLocked.Should().BeFalse();
        (await service.GetAsync("Admin", "alice")).FailedAttempts.Should().Be(1);
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
}
