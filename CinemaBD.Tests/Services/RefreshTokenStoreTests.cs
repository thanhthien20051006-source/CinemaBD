using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using CinemaBD.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Tests.Services;

public sealed class RefreshTokenStoreTests
{
    [Fact]
    public async Task RotateAsync_UsesExpectedAccountAndTreatsDuplicateTokenAsReuse()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var sessionId = Guid.NewGuid();
        var tokenBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var hash = System.Security.Cryptography.SHA256.HashData(tokenBytes);
        var now = DateTime.UtcNow;
        db.IdentitySessions.Add(new IdentitySession { SessionId = sessionId, AccountType = "Admin", AccountId = "10", CreatedAtUtc = now, LastSeenAtUtc = now, ExpiresAtUtc = now.AddDays(2) });
        db.RefreshTokens.Add(new RefreshTokenRecord { RefreshTokenId = Guid.NewGuid(), SessionId = sessionId, TokenHash = hash, CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(1) });
        await db.SaveChangesAsync();
        var auditWriter = new SecurityAuditWriter(db);
        var store = new EfRefreshTokenStore(db, auditWriter);

        var wrongAccount = await store.RotateAsync(hash, new byte[32], Guid.NewGuid(), "KHACHHANG", "KH1", now, now.AddDays(1));
        wrongAccount.Outcome.Should().Be(RefreshTokenStoreOutcome.Invalid);
        (await db.RefreshTokens.SingleAsync()).ConsumedAtUtc.Should().BeNull();

        var rotated = await store.RotateAsync(hash, new byte[32], Guid.NewGuid(), "Admin", "10", now, now.AddDays(1));
        rotated.Outcome.Should().Be(RefreshTokenStoreOutcome.Rotated);

        var reused = await store.RotateAsync(hash, new byte[32], Guid.NewGuid(), "Admin", "10", now, now.AddDays(1));
        reused.Outcome.Should().Be(RefreshTokenStoreOutcome.Reused);
        (await db.IdentitySessions.SingleAsync()).RevokedAtUtc.Should().NotBeNull();
        var audit = await db.SecurityAudit.SingleAsync();
        audit.EventType.Should().Be("refresh_token_reuse");
        audit.Details.Should().Contain("detected");
        System.Text.Json.JsonSerializer.Serialize(audit).Should().NotContain(Convert.ToHexString(hash));
        audit.Details.Should().NotContain("token=");
    }
}
