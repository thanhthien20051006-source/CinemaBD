using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Tests.Services;

public sealed class IdentitySessionServiceTests
{
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    [Fact]
    public async Task CreateSessionAsync_PersistsCanonicalIdentityAndActiveSession()
    {
        using var db = CreateDb();
        var service = new IdentitySessionService(db);

        var id = await service.CreateSessionAsync("KHACHHANG", "KH-123");

        var session = await db.IdentitySessions.SingleAsync();
        session.SessionId.Should().Be(id);
        session.AccountType.Should().Be("KHACHHANG");
        session.AccountId.Should().Be("KH-123");
        session.RevokedAtUtc.Should().BeNull();
        session.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
        (await service.IsActiveAsync(id)).Should().BeTrue();
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
}
