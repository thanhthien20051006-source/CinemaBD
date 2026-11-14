using CinemaBD.Api;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace CinemaBD.Tests.Controllers;

public class AdminAuthLogoutIntegrationTests : IClassFixture<AdminAuthLogoutIntegrationTests.TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public AdminAuthLogoutIntegrationTests(TestApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AdminToken_WithMasterAndPermissionClaims_ShouldAccessAdminEndpoint()
    {
        var client = _factory.CreateClient();
        var sessionId = _factory.CreateActiveSession();
        var token = CreateTestAdminToken("admin:1", sessionId, isMaster: true, permissions: ["phanquyen"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminToken_AfterLogout_ShouldReturn401WhenReused()
    {
        var client = _factory.CreateClient();
        var sessionId = _factory.CreateActiveSession();
        var token = CreateTestAdminToken("admin:1", sessionId, isMaster: true, permissions: ["phanquyen"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var logoutResponse = await client.PostAsync("/api/admin/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reusedResponse = await client.PostAsync("/api/admin/auth/logout", null);
        reusedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminToken_WithoutSessionId_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        var token = CreateTestAdminToken("admin:1", sessionId: null, includeSessionId: false, isMaster: true, permissions: ["phanquyen"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminToken_WithStaleMasterClaimButNoPermissionClaim_ShouldReturn403()
    {
        var client = _factory.CreateClient();
        var sessionId = _factory.CreateActiveSession();
        // The token claims master access, but the current database says admin:2 is not
        // master and the token has no route permission. Current authorization must deny it.
        var token = CreateTestAdminToken("admin:2", sessionId, isMaster: true);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminToken_WhenAdminHasNoActiveRole_ShouldBeDenied()
    {
        var client = _factory.CreateClient();
        var sessionId = _factory.CreateActiveSession();
        var token = CreateTestAdminToken("admin:3", sessionId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static string CreateTestAdminToken(
        string subject,
        Guid? sessionId,
        bool includeSessionId = true,
        bool isMaster = false,
        params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.UniqueName, "admin_test"),
            new("full_name", "Admin Test User"),
            new("account_type", "admin"),
            new(ClaimTypes.Role, "Admin"),
            new("is_master", isMaster ? "true" : "false")
        };

        if (includeSessionId)
        {
            if (sessionId is null || sessionId == Guid.Empty)
                throw new ArgumentException("A valid session id is required when the sid claim is included.", nameof(sessionId));
            claims.Add(new Claim("sid", sessionId.Value.ToString("D")));
        }

        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestApiFactory.TestJwtKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = TestApiFactory.TestJwtIssuer,
            Audience = TestApiFactory.TestJwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    public class TestApiFactory : WebApplicationFactory<CinemaBD.Api.Program>
    {
        public const string TestJwtKey = "SuperSecretKeyForIntegrationTesting1234567890!";
        public const string TestJwtIssuer = "CinemaBD.Test";
        public const string TestJwtAudience = "CinemaBD.Client.Test";
        public const string TestQrKey = "SuperSecretQrSigningKeyForTesting1234567890!";
        public static readonly Guid SessionId = Guid.NewGuid();
        private readonly string _databaseName = "AdminAuthLogoutTestDb_" + Guid.NewGuid();
        private readonly TestIdentitySessionService _identitySessionService = new();

        public Guid CreateActiveSession() => _identitySessionService.CreateActiveSession();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Jwt:Key", TestJwtKey);
            builder.UseSetting("Jwt:Issuer", TestJwtIssuer);
            builder.UseSetting("Jwt:Audience", TestJwtAudience);
            builder.UseSetting("Security:QrSigningKey", TestQrKey);
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused.invalid;Database=AdminAuthTest;User Id=test;Password=test");
            builder.UseSetting("DatabaseSeed:AllowDemoData", "false");
            builder.UseSetting("Database:AllowAutoMigrate", "false");
            builder.UseSetting("Database:AllowedNames:0", string.Empty);

            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor != null) services.Remove(dbDescriptor);

                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
                RemoveService<IAdminAuthorizationService>(services);
                services.AddScoped<IAdminAuthorizationService, TestAdminAuthorizationService>();
                RemoveService<IAdminRoleService>(services);
                services.AddScoped<IAdminRoleService, TestAdminRoleService>();
                RemoveService<IRevokedTokenService>(services);
                services.AddSingleton<IRevokedTokenService, InMemoryRevokedTokenService>();
                RemoveService<IIdentitySessionService>(services);
                services.AddSingleton<IIdentitySessionService>(_identitySessionService);
                RemoveService<IRefreshTokenService>(services);
                services.AddScoped<IRefreshTokenService, TestRefreshTokenService>();
            });
        }

        private static void RemoveService<TService>(IServiceCollection services)
        {
            var descriptors = services.Where(descriptor => descriptor.ServiceType == typeof(TService)).ToList();
            foreach (var descriptor in descriptors)
                services.Remove(descriptor);
        }

        private sealed class TestRefreshTokenService : IRefreshTokenService
        {
            public Task<RefreshRotationResult> RotateAsync(string refreshToken, string expectedAccountType, string expectedAccountId, CancellationToken cancellationToken = default)
                => Task.FromResult(new RefreshRotationResult("access", "refresh", SessionId, "Admin", "1"));
        }

        private sealed class TestIdentitySessionService : IIdentitySessionService
        {
            private readonly ConcurrentDictionary<Guid, bool> _activeSessions = new();

            public Guid CreateActiveSession()
            {
                var sessionId = Guid.NewGuid();
                _activeSessions[sessionId] = true;
                return sessionId;
            }

            public Task<Guid> CreateSessionAsync(string accountType, string accountId, string? deviceName = null, string? userAgent = null, CancellationToken cancellationToken = default)
            {
                var sessionId = CreateActiveSession();
                return Task.FromResult(sessionId);
            }

            public Task<string> IssueRefreshTokenAsync(Guid sessionId, CancellationToken cancellationToken = default) => Task.FromResult("test-refresh-token");
            public Task<bool> IsActiveAsync(Guid sessionId, CancellationToken cancellationToken = default) => Task.FromResult(_activeSessions.TryGetValue(sessionId, out var active) && active);
            public Task<IReadOnlyCollection<IdentitySessionInfo>> ListSessionsAsync(string accountType, string accountId, Guid currentSessionId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<IdentitySessionInfo>>([]);

            public Task<bool> RevokeSessionAsync(Guid sessionId, string accountType, string accountId, string reason, CancellationToken cancellationToken = default)
            {
                if (!_activeSessions.TryGetValue(sessionId, out var active) || !active)
                    return Task.FromResult(false);
                _activeSessions[sessionId] = false;
                return Task.FromResult(true);
            }

            public Task<bool> RevokeSessionByRefreshTokenAsync(string refreshToken, string accountType, string accountId, string reason, CancellationToken cancellationToken = default) => Task.FromResult(false);
            public Task RevokeAllAsync(string accountType, string accountId, string reason, CancellationToken cancellationToken = default)
            {
                foreach (var sessionId in _activeSessions.Keys)
                    _activeSessions[sessionId] = false;
                return Task.CompletedTask;
            }
        }

        private sealed class TestAdminAuthorizationService : IAdminAuthorizationService
        {
            public Task<AdminAuthorizationAccess> GetAccessForAdminAsync(int adminId, CancellationToken cancellationToken = default)
                => Task.FromResult(adminId == 1
                    ? new AdminAuthorizationAccess(1, true, Array.Empty<string>())
                    : throw new InvalidOperationException("Role unavailable"));
        }

        private sealed class TestAdminRoleService : IAdminRoleService
        {
            public Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Role>>([]);
            public Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Role?>(null);
            public Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default) => Task.FromResult(role);
            public Task<Role?> UpdateAsync(int id, Role role, CancellationToken cancellationToken = default) => Task.FromResult<Role?>(null);
            public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(false);
            public Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Permission>>([]);
            public Task<IReadOnlyCollection<Permission>> GetAssignedPermissionsAsync(int roleId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Permission>>([]);
            public Task AssignPermissionAsync(int roleId, int permissionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RemovePermissionAsync(int roleId, int permissionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }

    public class InMemoryRevokedTokenService : IRevokedTokenService
    {
        private readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();

        public Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
        {
            _revokedTokens[jti] = expiresAtUtc;
            return Task.CompletedTask;
        }

        public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
            => Task.FromResult(_revokedTokens.TryGetValue(jti, out var exp) && exp > DateTime.UtcNow);

        public Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _revokedTokens.Where(kvp => kvp.Value <= now).Select(kvp => kvp.Key).ToList();
            int count = 0;
            foreach (var key in expiredKeys)
            {
                if (_revokedTokens.TryRemove(key, out _))
                    count++;
            }
            return Task.FromResult(count);
        }
    }
}
