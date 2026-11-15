using CinemaBD.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaBD.Tests.Web;

public class WebSecurityHeadersTests : IClassFixture<WebSecurityHeadersTests.TestWebFactory>
{
    private readonly TestWebFactory _factory;

    public WebSecurityHeadersTests(TestWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RealPipeline_HttpResponse_ShouldIncludeSecurityHeaders()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act: send real HTTP request through the pipeline
        var response = await client.GetAsync("/");

        // Assert: verify headers on the real HTTP response
        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");

        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");

        response.Headers.Contains("Referrer-Policy").Should().BeTrue();
        response.Headers.GetValues("Referrer-Policy").Should().Contain("no-referrer");
    }

    public class TestWebFactory : WebApplicationFactory<CinemaBD.Web.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            var randomBytes = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
            var randomKey = Convert.ToBase64String(randomBytes);

            var randomQrBytes = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randomQrBytes);
            var randomQrKey = Convert.ToBase64String(randomQrBytes);

            builder.UseSetting("Jwt:Key", randomKey);
            builder.UseSetting("Jwt:Issuer", "CinemaBD.Test");
            builder.UseSetting("Jwt:Audience", "CinemaBD.Client.Test");
            builder.UseSetting("Security:QrSigningKey", randomQrKey);
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused.invalid;Database=WebSecurityHeadersTest;User Id=test;Password=test");
            builder.UseSetting("DatabaseSeed:AllowDemoData", "false");
            builder.UseSetting("Database:AllowAutoMigrate", "false");

            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor != null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("WebSecurityHeadersTestDb_" + Guid.NewGuid()));
            });
        }
    }
}
