using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CinemaBD.Tests.Security;

public sealed class RevokedTokenServiceTests
{
    [Fact]
    public async Task IsRevokedAsync_WhenTokenIsRevokedAndNotExpired_ReturnsTrue()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new RevokedTokenService(db);

        await service.RevokeAsync("test_jti_1", DateTime.UtcNow.AddMinutes(10));

        var isRevoked = await service.IsRevokedAsync("test_jti_1");
        isRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task IsRevokedAsync_WhenTokenExpired_ReturnsFalse()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new RevokedTokenService(db);

        db.RevokedTokens.Add(new RevokedToken
        {
            Jti = "test_jti_expired",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();

        var isRevoked = await service.IsRevokedAsync("test_jti_expired");
        isRevoked.Should().BeFalse("Expired revoked tokens are no longer considered actively revoked");
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_PurgesOnlyExpiredTokens()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new RevokedTokenService(db);

        db.RevokedTokens.AddRange(
            new RevokedToken { Jti = "expired_1", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10) },
            new RevokedToken { Jti = "expired_2", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1) },
            new RevokedToken { Jti = "active_1", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15) }
        );
        await db.SaveChangesAsync();

        var cleanedCount = await service.CleanupExpiredTokensAsync();
        cleanedCount.Should().Be(2);

        var remaining = await db.RevokedTokens.ToListAsync();
        remaining.Should().HaveCount(1);
        remaining[0].Jti.Should().Be("active_1");
    }
}
