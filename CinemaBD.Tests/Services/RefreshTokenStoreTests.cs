using System.Security.Cryptography;
using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Tests.Services;

public sealed class RefreshTokenStoreTests
{
    private static (AppDbContext Db, SqliteConnection Connection) CreateSqliteDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE "IdentitySessions" (
                "SessionId" TEXT NOT NULL CONSTRAINT "PK_IdentitySessions" PRIMARY KEY,
                "AccountType" TEXT NOT NULL,
                "AccountId" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "LastSeenAtUtc" TEXT NOT NULL,
                "ExpiresAtUtc" TEXT NOT NULL,
                "RevokedAtUtc" TEXT NULL,
                "RevocationReason" TEXT NULL,
                "DeviceName" TEXT NULL,
                "UserAgent" TEXT NULL,
                "LastSeenIp" TEXT NULL
            );
            CREATE TABLE "RefreshTokens" (
                "RefreshTokenId" TEXT NOT NULL CONSTRAINT "PK_RefreshTokens" PRIMARY KEY,
                "SessionId" TEXT NOT NULL,
                "TokenHash" BLOB NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "ExpiresAtUtc" TEXT NOT NULL,
                "ConsumedAtUtc" TEXT NULL,
                "RevokedAtUtc" TEXT NULL,
                "ReplacedByRefreshTokenId" TEXT NULL,
                CONSTRAINT "FK_RefreshTokens_IdentitySessions_SessionId"
                    FOREIGN KEY ("SessionId") REFERENCES "IdentitySessions" ("SessionId") ON DELETE CASCADE,
                CONSTRAINT "FK_RefreshTokens_RefreshTokens_ReplacedByRefreshTokenId"
                    FOREIGN KEY ("ReplacedByRefreshTokenId") REFERENCES "RefreshTokens" ("RefreshTokenId") ON DELETE NO ACTION
            );
            CREATE TABLE "SecurityAudit" (
                "AuditId" INTEGER NOT NULL CONSTRAINT "PK_SecurityAudit" PRIMARY KEY AUTOINCREMENT,
                "OccurredAtUtc" TEXT NOT NULL,
                "EventType" TEXT NOT NULL,
                "AccountType" TEXT NULL,
                "AccountId" TEXT NULL,
                "SessionId" TEXT NULL,
                "SourceIp" TEXT NULL,
                "CorrelationId" TEXT NULL,
                "Details" TEXT NULL
            );
            """;
        command.ExecuteNonQuery();

        var db = new AppDbContext(options);
        return (db, connection);
    }

    [Fact]
    public async Task RotateAsync_UsesExpectedAccountAndTreatsDuplicateTokenAsReuse()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var sessionId = Guid.NewGuid();
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var hash = SHA256.HashData(tokenBytes);
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

    [Fact]
    public async Task CreateSqliteDb_EnforcesRefreshTokenSessionForeignKey()
    {
        var (db, connection) = CreateSqliteDb();
        await using (connection)
        await using (db)
        {
            var now = DateTime.UtcNow;
            db.RefreshTokens.Add(new RefreshTokenRecord
            {
                RefreshTokenId = Guid.NewGuid(),
                SessionId = Guid.NewGuid(),
                TokenHash = SHA256.HashData(RandomNumberGenerator.GetBytes(32)),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(1)
            });

            var foreignKeyViolation = async () => await db.SaveChangesAsync();
            var exception = await foreignKeyViolation.Should().ThrowAsync<DbUpdateException>();
            exception.Which.InnerException.Should().BeOfType<SqliteException>()
                .Which.SqliteExtendedErrorCode.Should().Be(787);
        }
    }

    [Fact]
    public async Task RotateAsync_SqliteRelational_NormalRotation_SucceedsWithForeignKeysEnforced()
    {
        var (db, connection) = CreateSqliteDb();
        await using (connection)
        await using (db)
        {
            var sessionId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            db.IdentitySessions.Add(new IdentitySession
            {
                SessionId = sessionId,
                AccountType = "Admin",
                AccountId = "10",
                CreatedAtUtc = now,
                LastSeenAtUtc = now,
                ExpiresAtUtc = now.AddDays(2)
            });
            var oldBytes = RandomNumberGenerator.GetBytes(32);
            var oldHash = SHA256.HashData(oldBytes);
            var oldTokenId = Guid.NewGuid();
            db.RefreshTokens.Add(new RefreshTokenRecord
            {
                RefreshTokenId = oldTokenId,
                SessionId = sessionId,
                TokenHash = oldHash,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(1)
            });
            await db.SaveChangesAsync();

            var store = new EfRefreshTokenStore(db, new SecurityAuditWriter(db));
            var repId = Guid.NewGuid();
            var repHash = SHA256.HashData(RandomNumberGenerator.GetBytes(32));

            var result = await store.RotateAsync(oldHash, repHash, repId, "Admin", "10", now, now.AddDays(1));

            result.Outcome.Should().Be(RefreshTokenStoreOutcome.Rotated);
            result.SessionId.Should().Be(sessionId);

            db.ChangeTracker.Clear();
            var oldToken = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.RefreshTokenId == oldTokenId);
            oldToken.ConsumedAtUtc.Should().Be(now);
            oldToken.ReplacedByRefreshTokenId.Should().Be(repId);

            var repToken = await db.RefreshTokens.SingleAsync(t => t.RefreshTokenId == repId);
            repToken.SessionId.Should().Be(sessionId);
            repToken.ConsumedAtUtc.Should().BeNull();
            repToken.ReplacedByRefreshTokenId.Should().BeNull();
        }
    }

    [Fact]
    public async Task RotateAsync_SqliteRelational_ReuseDetected_RevokesSessionAndWritesAudit()
    {
        var (db, connection) = CreateSqliteDb();
        await using (connection)
        await using (db)
        {
            var sessionId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            db.IdentitySessions.Add(new IdentitySession
            {
                SessionId = sessionId,
                AccountType = "Admin",
                AccountId = "10",
                CreatedAtUtc = now,
                LastSeenAtUtc = now,
                ExpiresAtUtc = now.AddDays(2)
            });
            var oldBytes = RandomNumberGenerator.GetBytes(32);
            var oldHash = SHA256.HashData(oldBytes);
            var oldTokenId = Guid.NewGuid();
            db.RefreshTokens.Add(new RefreshTokenRecord
            {
                RefreshTokenId = oldTokenId,
                SessionId = sessionId,
                TokenHash = oldHash,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(1)
            });
            await db.SaveChangesAsync();

            var store = new EfRefreshTokenStore(db, new SecurityAuditWriter(db));
            var repId1 = Guid.NewGuid();
            var repHash1 = SHA256.HashData(RandomNumberGenerator.GetBytes(32));

            var firstRotate = await store.RotateAsync(oldHash, repHash1, repId1, "Admin", "10", now, now.AddDays(1));
            firstRotate.Outcome.Should().Be(RefreshTokenStoreOutcome.Rotated);

            // Present the consumed token again (reuse attack)
            var repId2 = Guid.NewGuid();
            var repHash2 = SHA256.HashData(RandomNumberGenerator.GetBytes(32));
            var reuseRotate = await store.RotateAsync(oldHash, repHash2, repId2, "Admin", "10", now.AddMinutes(1), now.AddDays(1));

            reuseRotate.Outcome.Should().Be(RefreshTokenStoreOutcome.Reused);
            reuseRotate.SessionId.Should().Be(sessionId);

            var session = await db.IdentitySessions.SingleAsync(s => s.SessionId == sessionId);
            session.RevokedAtUtc.Should().NotBeNull();
            session.RevocationReason.Should().Be("refresh_token_reuse");

            var audit = await db.SecurityAudit.SingleAsync(a => a.EventType == "refresh_token_reuse");
            audit.AccountType.Should().Be("Admin");
            audit.AccountId.Should().Be("10");
            audit.SessionId.Should().Be(sessionId);
            audit.EventType.Should().Be("refresh_token_reuse");
            audit.Details.Should().Contain("detected");
            audit.Details.Should().Contain("Previously used credential presented again; session revoked.");
            audit.Details.Should().NotContain(Convert.ToHexString(oldHash));
            audit.Details.Should().NotContain("token=");
        }
    }

    [Fact]
    public async Task RotateAsync_SqliteRelational_TwoConsecutiveRotations_MaintainsTokenChain()
    {
        var (db, connection) = CreateSqliteDb();
        await using (connection)
        await using (db)
        {
            var sessionId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            db.IdentitySessions.Add(new IdentitySession
            {
                SessionId = sessionId,
                AccountType = "Admin",
                AccountId = "10",
                CreatedAtUtc = now,
                LastSeenAtUtc = now,
                ExpiresAtUtc = now.AddDays(2)
            });
            var token1Bytes = RandomNumberGenerator.GetBytes(32);
            var token1Hash = SHA256.HashData(token1Bytes);
            var token1Id = Guid.NewGuid();
            db.RefreshTokens.Add(new RefreshTokenRecord
            {
                RefreshTokenId = token1Id,
                SessionId = sessionId,
                TokenHash = token1Hash,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(1)
            });
            await db.SaveChangesAsync();

            var store = new EfRefreshTokenStore(db, new SecurityAuditWriter(db));

            // Rotation 1: Token 1 -> Token 2
            var token2Id = Guid.NewGuid();
            var token2Bytes = RandomNumberGenerator.GetBytes(32);
            var token2Hash = SHA256.HashData(token2Bytes);
            var result1 = await store.RotateAsync(token1Hash, token2Hash, token2Id, "Admin", "10", now, now.AddDays(1));
            result1.Outcome.Should().Be(RefreshTokenStoreOutcome.Rotated);

            // Rotation 2: Token 2 -> Token 3
            var now2 = now.AddMinutes(5);
            var token3Id = Guid.NewGuid();
            var token3Bytes = RandomNumberGenerator.GetBytes(32);
            var token3Hash = SHA256.HashData(token3Bytes);
            var result2 = await store.RotateAsync(token2Hash, token3Hash, token3Id, "Admin", "10", now2, now.AddDays(1));
            result2.Outcome.Should().Be(RefreshTokenStoreOutcome.Rotated);

            // Verify chain
            db.ChangeTracker.Clear();
            var t1 = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.RefreshTokenId == token1Id);
            t1.ConsumedAtUtc.Should().Be(now);
            t1.ReplacedByRefreshTokenId.Should().Be(token2Id);

            var t2 = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.RefreshTokenId == token2Id);
            t2.ConsumedAtUtc.Should().Be(now2);
            t2.ReplacedByRefreshTokenId.Should().Be(token3Id);

            var t3 = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.RefreshTokenId == token3Id);
            t3.ConsumedAtUtc.Should().BeNull();
            t3.ReplacedByRefreshTokenId.Should().BeNull();

            var session = await db.IdentitySessions.SingleAsync(s => s.SessionId == sessionId);
            session.RevokedAtUtc.Should().BeNull();

            // Reusing token 1 now must trigger reuse revocation
            var reuse1 = await store.RotateAsync(token1Hash, SHA256.HashData(RandomNumberGenerator.GetBytes(32)), Guid.NewGuid(), "Admin", "10", now2.AddMinutes(1), now.AddDays(1));
            reuse1.Outcome.Should().Be(RefreshTokenStoreOutcome.Reused);
            (await db.IdentitySessions.SingleAsync(s => s.SessionId == sessionId)).RevokedAtUtc.Should().NotBeNull();
        }
    }

    [Theory]
    [InlineData("Server=localhost;Database=CinemaBD;Trusted_Connection=True;TrustServerCertificate=True")]
    [InlineData("Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True")]
    [InlineData("Server=localhost;Database=CinemaBD_prod;Trusted_Connection=True;TrustServerCertificate=True")]
    public void VerifyConnectionString_RejectsDatabaseOtherThanCinemaBDVerify(string connectionString)
    {
        var act = () => ValidateVerifyConnectionString(connectionString);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CinemaBD_verify*");
    }

    [Fact]
    public void VerifyConnectionString_AcceptsCinemaBDVerifyDatabase()
    {
        var valid = "Server=localhost;Database=CinemaBD_verify;Trusted_Connection=True;TrustServerCertificate=True";
        var act = () => ValidateVerifyConnectionString(valid);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task RotateAsync_SqlServer_WhenVerifyFlagProvided_RunsIntegrationTest()
    {
        var connStr = Environment.GetEnvironmentVariable("CINEMABD_VERIFY_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            // Default: skipped when environment variable is unset
            return;
        }

        ValidateVerifyConnectionString(connStr);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connStr)
            .Options;

        await using var db = new AppDbContext(options);
        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.IdentitySessions.Add(new IdentitySession
        {
            SessionId = sessionId,
            AccountType = "Admin",
            AccountId = "verify_flag_test",
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            ExpiresAtUtc = now.AddDays(2)
        });
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenHash = SHA256.HashData(tokenBytes);
        var tokenId = Guid.NewGuid();
        db.RefreshTokens.Add(new RefreshTokenRecord
        {
            RefreshTokenId = tokenId,
            SessionId = sessionId,
            TokenHash = tokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(1)
        });
        await db.SaveChangesAsync();

        try
        {
            var store = new EfRefreshTokenStore(db, new SecurityAuditWriter(db));
            var repId = Guid.NewGuid();
            var repHash = SHA256.HashData(RandomNumberGenerator.GetBytes(32));

            var rot = await store.RotateAsync(tokenHash, repHash, repId, "Admin", "verify_flag_test", now, now.AddDays(1));
            rot.Outcome.Should().Be(RefreshTokenStoreOutcome.Rotated);

            var reused = await store.RotateAsync(tokenHash, repHash, Guid.NewGuid(), "Admin", "verify_flag_test", now.AddMinutes(1), now.AddDays(1));
            reused.Outcome.Should().Be(RefreshTokenStoreOutcome.Reused);
        }
        finally
        {
            var tokens = await db.RefreshTokens.Where(t => t.SessionId == sessionId).ToListAsync();
            db.RefreshTokens.RemoveRange(tokens);
            var session = await db.IdentitySessions.SingleOrDefaultAsync(s => s.SessionId == sessionId);
            if (session is not null) db.IdentitySessions.Remove(session);
            await db.SaveChangesAsync();
        }
    }

    private static string ValidateVerifyConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.InitialCatalog, "CinemaBD_verify", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"CINEMABD_VERIFY_CONNECTION_STRING must target database 'CinemaBD_verify', but got '{builder.InitialCatalog}'.");
        }
        return connectionString;
    }
}
