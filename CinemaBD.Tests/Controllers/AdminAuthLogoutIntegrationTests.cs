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
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;

namespace CinemaBD.Tests.Controllers;

public class AdminAuthLogoutIntegrationTests : IClassFixture<AdminAuthLogoutIntegrationTests.TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public AdminAuthLogoutIntegrationTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminToken_AfterLogout_ShouldReturn401WhenReused()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var adminUser = new UserAccount
        {
            Id = "admin:01",
            Username = "admin_test",
            FullName = "Admin Test User"
        };
        var token = tokenService.CreateToken(adminUser);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act 1: Initial logout request -> should succeed (200 OK)
        var logoutResponse = await client.PostAsync("/api/admin/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2: Re-use the revoked token on the logout endpoint or any admin endpoint
        var reusedResponse = await client.PostAsync("/api/admin/auth/logout", null);

        // Assert: The real ASP.NET Core JwtBearer pipeline rejects revoked token with 401 Unauthorized
        reusedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public class TestApiFactory : WebApplicationFactory<CinemaBD.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "SuperSecretKeyForIntegrationTesting1234567890!",
                    ["Jwt:Issuer"] = "CinemaBD.Test",
                    ["Jwt:Audience"] = "CinemaBD.Client.Test",
                    ["Security:QrSigningKey"] = "SuperSecretQrSigningKeyForTesting1234567890!",
                    ["DatabaseSeed:AllowDemoData"] = "false"
                });
            });

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
