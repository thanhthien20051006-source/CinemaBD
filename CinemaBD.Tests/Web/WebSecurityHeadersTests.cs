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
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor != null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("WebSecurityHeadersTestDb_" + Guid.NewGuid()));
            });
        }
    }
}
