using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CinemaBD.Tests.Services;

public sealed class SecurityAuditQueryServiceTests
{
    [Fact]
    public async Task QueryAsync_FiltersAndPaginatesWithWhitelistedOutput_OnSqlite()
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
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            await using (db)
            {
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = """
                        CREATE TABLE "SecurityAudit" (
                            "AuditId" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
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
                    await command.ExecuteNonQueryAsync();
                }
                var now = DateTime.UtcNow;
                var session = Guid.NewGuid();
                db.SecurityAudit.AddRange(
                    new SecurityAuditRecord { OccurredAtUtc = now.AddMinutes(-3), EventType = "login_failed", AccountType = "KHACHHANG", AccountId = "KH1", SessionId = session, CorrelationId = "trace-1", Details = "safe" },
                    new SecurityAuditRecord { OccurredAtUtc = now.AddMinutes(-2), EventType = "login_failed", AccountType = "KHACHHANG", AccountId = "KH1", SessionId = session, CorrelationId = "trace-2", Details = "safe" },
                    new SecurityAuditRecord { OccurredAtUtc = now.AddMinutes(-1), EventType = "mfa_login_failed", AccountType = "Admin", AccountId = "5", SessionId = Guid.NewGuid(), Details = "other" });
                await db.SaveChangesAsync();
                var service = new SecurityAuditQueryService(db);

                var page = await service.QueryAsync(new SecurityAuditQuery("KHACHHANG", "KH1", session, now.AddMinutes(-5), now, "login_failed", 2, 1));

                page.TotalCount.Should().Be(2);
                page.Items.Should().ContainSingle();
                page.Page.Should().Be(2);
                page.Items.Single().EventType.Should().Be("login_failed");
                var serialized = System.Text.Json.JsonSerializer.Serialize(page.Items.Single());
                serialized.Should().NotContain("SourceIp");
                serialized.Should().NotContain("TokenHash");
                serialized.Should().NotContain("Password");
            }
        }
    }

    private sealed class RecordingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }

    [Fact]
    public async Task ThresholdAlerts_LogWarningWithCorrelationId_AtConfiguredThreshold()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:Alerts:FailedLoginCount"] = "2",
            ["Security:Alerts:WindowMinutes"] = "10"
        }).Build();
        var logger = new RecordingLogger<SecurityAlertEvaluator>();
        var evaluator = new SecurityAlertEvaluator(db, new CinemaBD.Infrastructure.Security.SecurityAlertThresholds(configuration), logger);
        var now = DateTime.UtcNow;
        db.SecurityAudit.AddRange(
            new SecurityAuditRecord { EventType = "login_failed", AccountType = "KHACHHANG", AccountId = "KH1", OccurredAtUtc = now.AddMinutes(-1) },
            new SecurityAuditRecord { EventType = "login_failed", AccountType = "KHACHHANG", AccountId = "KH1", OccurredAtUtc = now });
        await db.SaveChangesAsync();

        await evaluator.EvaluateAsync("login_failed", "KHACHHANG", "KH1", Guid.NewGuid(), "trace-safe");

        logger.Messages.Should().ContainSingle(message => message.Contains("CorrelationId trace-safe") && message.Contains("Security alert threshold"));
        logger.Messages.Should().NotContain(message => message.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QueryAsync_RejectsInvalidUtcRange()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var queryService = new SecurityAuditQueryService(db);

        var invalidQuery = () => queryService.QueryAsync(new SecurityAuditQuery(FromUtc: DateTime.Now));

        await invalidQuery.Should().ThrowAsync<ArgumentException>();
    }
}
