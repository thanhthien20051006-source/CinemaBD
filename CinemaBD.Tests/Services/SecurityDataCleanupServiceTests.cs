using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Tests.Services;

public sealed class SecurityDataCleanupServiceTests
{
    [Fact]
    public async Task CleanupAsync_DeletesOnlyExpiredAuditSessionsAndTheirTokens_OnSqliteWithForeignKeys()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (connection)
        {
            await using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                await pragma.ExecuteNonQueryAsync();
            }
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var db = new AppDbContext(options);
            await using (db)
            {
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = """
                        CREATE TABLE "IdentitySessions" (
                            "SessionId" TEXT NOT NULL PRIMARY KEY, "AccountType" TEXT NOT NULL, "AccountId" TEXT NOT NULL,
                            "CreatedAtUtc" TEXT NOT NULL, "LastSeenAtUtc" TEXT NOT NULL, "ExpiresAtUtc" TEXT NOT NULL,
                            "RevokedAtUtc" TEXT NULL, "RevocationReason" TEXT NULL, "DeviceName" TEXT NULL, "UserAgent" TEXT NULL, "LastSeenIp" TEXT NULL);
                        CREATE TABLE "RefreshTokens" (
                            "RefreshTokenId" TEXT NOT NULL PRIMARY KEY, "SessionId" TEXT NOT NULL, "TokenHash" BLOB NOT NULL,
                            "CreatedAtUtc" TEXT NOT NULL, "ExpiresAtUtc" TEXT NOT NULL, "ConsumedAtUtc" TEXT NULL, "RevokedAtUtc" TEXT NULL,
                            "ReplacedByRefreshTokenId" TEXT NULL,
                            FOREIGN KEY ("SessionId") REFERENCES "IdentitySessions" ("SessionId") ON DELETE CASCADE,
                            FOREIGN KEY ("ReplacedByRefreshTokenId") REFERENCES "RefreshTokens" ("RefreshTokenId") ON DELETE NO ACTION);
                        CREATE TABLE "SecurityAudit" (
                            "AuditId" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "OccurredAtUtc" TEXT NOT NULL, "EventType" TEXT NOT NULL,
                            "AccountType" TEXT NULL, "AccountId" TEXT NULL, "SessionId" TEXT NULL, "SourceIp" TEXT NULL,
                            "CorrelationId" TEXT NULL, "Details" TEXT NULL);
                        CREATE TABLE "RevokedTokens" ("Jti" TEXT NOT NULL PRIMARY KEY, "ExpiresAtUtc" TEXT NOT NULL);
                        """;
                    await command.ExecuteNonQueryAsync();
                }
                var now = DateTime.UtcNow;
                var expiredId = Guid.NewGuid();
                var activeId = Guid.NewGuid();
                db.IdentitySessions.AddRange(
                    new IdentitySession { SessionId = expiredId, AccountType = "Admin", AccountId = "2", CreatedAtUtc = now.AddDays(-40), LastSeenAtUtc = now.AddDays(-40), ExpiresAtUtc = now.AddDays(-1) },
                    new IdentitySession { SessionId = activeId, AccountType = "Admin", AccountId = "2", CreatedAtUtc = now, LastSeenAtUtc = now, ExpiresAtUtc = now.AddDays(10) });
                db.RefreshTokens.AddRange(
                    new RefreshTokenRecord { RefreshTokenId = Guid.NewGuid(), SessionId = expiredId, TokenHash = new byte[32], CreatedAtUtc = now.AddDays(-40), ExpiresAtUtc = now.AddDays(1) },
                    new RefreshTokenRecord { RefreshTokenId = Guid.NewGuid(), SessionId = activeId, TokenHash = Enumerable.Repeat((byte)1, 32).ToArray(), CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(10) });
                db.SecurityAudit.AddRange(
                    new SecurityAuditRecord { OccurredAtUtc = now.AddDays(-400), EventType = "old", Details = "old" },
                    new SecurityAuditRecord { OccurredAtUtc = now.AddDays(-1), EventType = "recent", Details = "recent" });
                db.RevokedTokens.AddRange(
                    new RevokedToken { Jti = "expired", ExpiresAtUtc = now.AddSeconds(-1) },
                    new RevokedToken { Jti = "active", ExpiresAtUtc = now.AddDays(1) });
                await db.SaveChangesAsync();

                var result = await new SecurityDataCleanupService(db).CleanupAsync(now, TimeSpan.FromDays(365));

                result.AuditRecordsDeleted.Should().Be(1);
                result.SessionsDeleted.Should().Be(1);
                result.RefreshTokensDeleted.Should().Be(1);
                result.RevokedTokensDeleted.Should().Be(1);
                (await db.IdentitySessions.CountAsync()).Should().Be(1);
                (await db.RefreshTokens.CountAsync()).Should().Be(1);
                (await db.SecurityAudit.CountAsync()).Should().Be(1);
                (await db.RevokedTokens.CountAsync()).Should().Be(1);
            }
        }
    }
}
