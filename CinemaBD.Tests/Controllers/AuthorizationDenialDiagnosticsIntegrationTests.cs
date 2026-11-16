using CinemaBD.Api;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Collections.Concurrent;

namespace CinemaBD.Tests.Controllers;

[Collection("AuthorizationDenialDiagnostics")]
public sealed class AuthorizationDenialDiagnosticsIntegrationTests : IClassFixture<AuthorizationDenialDiagnosticsIntegrationTests.TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public AuthorizationDenialDiagnosticsIntegrationTests(TestApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AdminRoles_InvalidSubject_LogsReasonAndPresenceOnlyContext()
    {
        _factory.ClearLogs();
        using var client = _factory.CreateClient();
        var sessionId = Guid.NewGuid();
        await _factory.ActivateSessionAsync(sessionId);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/roles/authorization-access");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("admin-without-id", sessionId, [new Claim("account_type", "admin")]));
        request.Headers.Add("X-Test-Sensitive", "PII-USERNAME-SECRET");
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        AssertDiagnosticFields(_factory.Logs, "subject_format_invalid", true, true, true);
        AssertNoSensitiveValues("admin-without-id", _factory.Logs);
    }

    [Fact]
    public async Task PasswordChangeRestriction_LogsReasonAndPresenceOnlyContext()
    {
        _factory.ClearLogs();
        using var client = _factory.CreateClient();
        using var response = await SendBearerAsync(client, "/api/movies", "admin:1",
            new Claim("password_change_only", "true"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        AssertDiagnosticFields(_factory.Logs, "password_change_restricted", true, true, true);
        AssertNoSensitiveValues("admin:1", _factory.Logs);
    }

    [Fact]
    public async Task MfaEnrollmentRestriction_LogsReasonAndPresenceOnlyContext()
    {
        _factory.ClearLogs();
        using var client = _factory.CreateClient();
        using var response = await SendBearerAsync(client, "/api/account/profile", "admin:1",
            new Claim("mfa_enrollment_only", "true"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        AssertDiagnosticFields(_factory.Logs, "mfa_enrollment_restricted", true, true, true);
        AssertNoSensitiveValues("admin:1", _factory.Logs);
    }

    [Fact]
    public async Task AdminOnlyPolicyDenial_LogsReasonAndPresenceOnlyContext()
    {
        _factory.ClearLogs();
        using var client = _factory.CreateClient();
        using var response = await SendBearerAsync(client, "/api/admin/dashboard", "admin:1",
            new Claim("account_type", "not-admin"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        AssertDiagnosticFields(_factory.Logs, "admin_policy_failed", true, true, true);
        AssertNoSensitiveValues("admin:1", _factory.Logs);
    }

    private async Task<HttpResponseMessage> SendBearerAsync(HttpClient client, string path, string subject, params Claim[] extraClaims)
    {
        _factory.ClearLogs();
        var sessionId = Guid.NewGuid();
        await _factory.ActivateSessionAsync(sessionId);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(subject, sessionId, extraClaims));
        request.Headers.Add("X-Test-Sensitive", "PII-USERNAME-SECRET");
        return await client.SendAsync(request);
    }

    private static string CreateToken(string subject, Guid sessionId, IEnumerable<Claim> extraClaims)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("sid", sessionId.ToString("D")),
            new(JwtRegisteredClaimNames.UniqueName, "PII-USERNAME-SECRET")
        };
        if (!extraClaims.Any(claim => claim.Type == "account_type"))
            claims.Add(new Claim("account_type", "admin"));
        claims.AddRange(extraClaims);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(10),
            Issuer = TestApiFactory.TestJwtIssuer,
            Audience = TestApiFactory.TestJwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestApiFactory.TestJwtKey)),
                SecurityAlgorithms.HmacSha256)
        };
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(descriptor));
    }

    private static void AssertDiagnosticFields(IReadOnlyCollection<string> logs, string reason, bool authorizationHeader, bool hasSub, bool hasAccountType)
    {
        var entry = logs.Should().ContainSingle(log => log.Contains($"ReasonCode={reason}", StringComparison.Ordinal)).Which;
        entry.Should().Contain("TraceId=");
        entry.Should().Contain("Route=");
        entry.Should().Contain($"AuthorizationHeaderPresent={authorizationHeader}");
        entry.Should().Contain($"HasSubClaim={hasSub}");
        entry.Should().Contain($"HasAccountTypeClaim={hasAccountType}");
        entry.Should().Contain("AuthenticationScheme=Bearer");
    }

    private static void AssertNoSensitiveValues(string extraSensitiveValue, IEnumerable<string> logs)
    {
        string.Join("\n", logs).Should().NotContain(extraSensitiveValue)
            .And.NotContain("PII-USERNAME-SECRET")
            .And.NotContain("secret-admin-token-value")
            .And.NotContain("sensitive-username");
    }

    public sealed class TestApiFactory : WebApplicationFactory<CinemaBD.Api.Program>
    {
        public const string TestJwtKey = "DiagnosticsIntegrationTestKeyWith32Bytes!";
        public const string TestJwtIssuer = "CinemaBD.Diagnostics.Test";
        public const string TestJwtAudience = "CinemaBD.Diagnostics.Client";

        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private readonly RecordingLoggerProvider _logs = new();
        public IReadOnlyCollection<string> Logs => _logs.Messages;
        public void ClearLogs() => _logs.Clear();
        public async Task ActivateSessionAsync(Guid sessionId)
        {
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
            db.IdentitySessions.Add(new IdentitySession
            {
                SessionId = sessionId,
                AccountType = "Admin",
                AccountId = "1",
                CreatedAtUtc = DateTime.UtcNow,
                LastSeenAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
            });
            await db.SaveChangesAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();
            using (var pragma = _connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
                pragma.CommandText = "PRAGMA foreign_keys;";
                Convert.ToInt32(pragma.ExecuteScalar()).Should().Be(1);
            }
            using (var schema = _connection.CreateCommand())
            {
                schema.CommandText = """
                    CREATE TABLE IF NOT EXISTS "IdentitySessions" (
                        "SessionId" TEXT NOT NULL PRIMARY KEY,
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
                    CREATE TABLE IF NOT EXISTS "RevokedTokens" (
                        "Jti" TEXT NOT NULL PRIMARY KEY,
                        "ExpiresAtUtc" TEXT NOT NULL
                    );
                    """;
                schema.ExecuteNonQuery();
            }

            builder.UseEnvironment("Development");
            builder.UseSetting("Jwt:Key", TestJwtKey);
            builder.UseSetting("Jwt:Issuer", TestJwtIssuer);
            builder.UseSetting("Jwt:Audience", TestJwtAudience);
            builder.UseSetting("Security:QrSigningKey", "DiagnosticsIntegrationTestQrKeyWith32Bytes!");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused.invalid;Database=DiagnosticsTest;User Id=test;Password=test");
            builder.UseSetting("DatabaseSeed:AllowDemoData", "false");
            builder.UseSetting("Database:AllowAutoMigrate", "false");
            builder.UseSetting("Database:AllowedNames:0", string.Empty);
            builder.UseSetting("Security:Cleanup:Enabled", "false");

            builder.ConfigureLogging(logging => logging.AddProvider(_logs));
            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services.Where(item => item.ServiceType == typeof(DbContextOptions<AppDbContext>)).ToArray())
                    services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

                RemoveService<IRevokedTokenService>(services);
                services.AddSingleton<IRevokedTokenService, TestRevokedTokenService>();
                RemoveService<IIdentitySessionService>(services);
                services.AddSingleton<IIdentitySessionService>(_ => new TestIdentitySessionService(_connection));
                RemoveService<ISecurityAuditWriter>(services);
                services.AddSingleton<ISecurityAuditWriter, TestAuditWriter>();
                RemoveService<IAdminAuthorizationService>(services);
                services.AddSingleton<IAdminAuthorizationService, EmptyAdminAuthorizationService>();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _connection.Dispose();
        }

        private static void RemoveService<T>(IServiceCollection services)
        {
            foreach (var descriptor in services.Where(item => item.ServiceType == typeof(T)).ToArray())
                services.Remove(descriptor);
        }

        private sealed class TestRevokedTokenService : IRevokedTokenService
        {
            public Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default) => Task.FromResult(false);
            public Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        }

        private sealed class TestIdentitySessionService(SqliteConnection connection) : IIdentitySessionService
        {
            public Task<Guid> CreateSessionAsync(string accountType, string accountId, string? deviceName = null, string? userAgent = null, CancellationToken cancellationToken = default) => Task.FromResult(Guid.NewGuid());
            public Task<Guid> CreateSessionAsync(string accountType, string accountId, string? deviceName, string? userAgent, string? lastSeenIp, CancellationToken cancellationToken = default) => Task.FromResult(Guid.NewGuid());
            public Task<string> IssueRefreshTokenAsync(Guid sessionId, CancellationToken cancellationToken = default) => Task.FromResult("unused-refresh-token");
            public async Task<bool> IsActiveAsync(Guid sessionId, CancellationToken cancellationToken = default)
            {
                await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
                return await db.IdentitySessions.AsNoTracking().AnyAsync(session => session.SessionId == sessionId && session.RevokedAtUtc == null && session.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);
            }
            public Task<IReadOnlyCollection<IdentitySessionInfo>> ListSessionsAsync(string accountType, string accountId, Guid currentSessionId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<IdentitySessionInfo>>([]);
            public Task<bool> RevokeSessionAsync(Guid sessionId, string accountType, string accountId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
            public Task<bool> RevokeSessionByRefreshTokenAsync(string refreshToken, string accountType, string accountId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
            public Task RevokeAllAsync(string accountType, string accountId, string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class TestAuditWriter : ISecurityAuditWriter
        {
            public Task WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class EmptyAdminAuthorizationService : IAdminAuthorizationService
        {
            public Task<AdminAuthorizationAccess> GetAccessForAdminAsync(int adminId, CancellationToken cancellationToken = default)
                => Task.FromResult(new AdminAuthorizationAccess(1, false, Array.Empty<string>()));
        }

        private sealed class RecordingLoggerProvider : ILoggerProvider
        {
            private readonly ConcurrentQueue<string> _messages = new();
            public IReadOnlyCollection<string> Messages => _messages.ToArray();
            public void Clear() { while (_messages.TryDequeue(out _)) { } }
            public ILogger CreateLogger(string categoryName) => new RecordingLogger(_messages);
            public void Dispose() { }

            private sealed class RecordingLogger(ConcurrentQueue<string> messages) : ILogger
            {
                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
                public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                {
                    if (IsEnabled(logLevel))
                        messages.Enqueue(formatter(state, exception));
                }
            }
        }
    }
}
