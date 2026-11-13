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
    public async Task AdminToken_WithMasterRoleFlag_ShouldAccessAdminEndpoint()
    {
        var client = _factory.CreateClient();
        var token = CreateTestAdminToken("admin:1");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminToken_AfterLogout_ShouldReturn401WhenReused()
    {
        var client = _factory.CreateClient();
        var token = CreateTestAdminToken("admin:1");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var logoutResponse = await client.PostAsync("/api/admin/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reusedResponse = await client.PostAsync("/api/admin/auth/logout", null);
        reusedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminToken_WhenAdminHasNoActiveRole_ShouldBeDenied()
    {
        var client = _factory.CreateClient();
        var token = CreateTestAdminToken("admin:2");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static string CreateTestAdminToken(string subject)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestApiFactory.TestJwtKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, subject),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(JwtRegisteredClaimNames.UniqueName, "admin_test"),
                new Claim("full_name", "Admin Test User"),
                new Claim("account_type", "admin")
            }),
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
        private readonly string _databaseName = "AdminAuthLogoutTestDb_" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Jwt:Key", TestJwtKey);
            builder.UseSetting("Jwt:Issuer", TestJwtIssuer);
            builder.UseSetting("Jwt:Audience", TestJwtAudience);
            builder.UseSetting("Security:QrSigningKey", TestQrKey);
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused.invalid;Database=AdminAuthTest;User Id=test;Password=test");
            builder.UseSetting("DatabaseSeed:AllowDemoData", "false");

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
            });
        }

        private static void RemoveService<TService>(IServiceCollection services)
        {
            var descriptors = services.Where(descriptor => descriptor.ServiceType == typeof(TService)).ToList();
            foreach (var descriptor in descriptors)
                services.Remove(descriptor);
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
    }
}
