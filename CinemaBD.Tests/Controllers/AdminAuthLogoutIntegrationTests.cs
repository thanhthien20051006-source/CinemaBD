using CinemaBD.Api;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    public AdminAuthLogoutIntegrationTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminToken_WhenNotRevoked_ShouldAccessAdminEndpointSuccessfully()
    {
        // Control test: verify that a valid unrevoked admin token accesses admin endpoint (200 OK)
        var client = _factory.CreateClient();
        var token = CreateTestAdminToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "unrevoked admin token must be accepted by the JWT pipeline");
    }

    [Fact]
    public async Task AdminToken_AfterLogout_ShouldReturn401WhenReused()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = CreateTestAdminToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act 1: Initial logout request -> should succeed (200 OK)
        var logoutResponse = await client.PostAsync("/api/admin/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK, "initial logout with valid token must succeed");

        // Act 2: Re-use the revoked token on the logout endpoint or any admin endpoint
        var reusedResponse = await client.PostAsync("/api/admin/auth/logout", null);

        // Assert: The real ASP.NET Core JwtBearer pipeline rejects revoked token with 401 Unauthorized
        reusedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "revoked token must be rejected with 401 Unauthorized");
    }

    private static string CreateTestAdminToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestApiFactory.TestJwtKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "admin:01"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(JwtRegisteredClaimNames.UniqueName, "admin_test"),
                new Claim("full_name", "Admin Test User"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("account_type", "admin")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = TestApiFactory.TestJwtIssuer,
            Audience = TestApiFactory.TestJwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public class TestApiFactory : WebApplicationFactory<CinemaBD.Api.Program>
    {
        public const string TestJwtKey = "SuperSecretKeyForIntegrationTesting1234567890!";
        public const string TestJwtIssuer = "CinemaBD.Test";
        public const string TestJwtAudience = "CinemaBD.Client.Test";
        public const string TestQrKey = "SuperSecretQrSigningKeyForTesting1234567890!";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.UseSetting("Jwt:Key", TestJwtKey);
            builder.UseSetting("Jwt:Issuer", TestJwtIssuer);
            builder.UseSetting("Jwt:Audience", TestJwtAudience);
            builder.UseSetting("Security:QrSigningKey", TestQrKey);
            builder.UseSetting("DatabaseSeed:AllowDemoData", "false");

            builder.ConfigureServices(services =>
            {
                // Remove existing AppDbContext and use InMemory
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor != null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("AdminAuthLogoutTestDb_" + Guid.NewGuid()));

                // Replace IRevokedTokenService with in-memory concurrent store
                var revokedDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRevokedTokenService));
                if (revokedDescriptor != null)
                    services.Remove(revokedDescriptor);

                services.AddSingleton<IRevokedTokenService, InMemoryRevokedTokenService>();
            });
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
        {
            var isRevoked = _revokedTokens.TryGetValue(jti, out var exp) && exp > DateTime.UtcNow;
            return Task.FromResult(isRevoked);
        }
    }
}
