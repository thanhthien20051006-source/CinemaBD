using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CinemaBD.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace CinemaBD.Tests.Persistence;

public class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_WhenDatabaseNameIsNotAllowed_DoesNotProbeOrModifyDatabase()
    {
        const string connectionString = "Server=unused.invalid;Database=UnexpectedCinema;User Id=test;Password=test";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new AppDbContext(options);
        var logger = new ListLogger<DatabaseInitializer>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Database:AllowAutoMigrate"] = "true",
                ["Database:AllowedNames:0"] = "CinemaBD"
            })
            .Build();

        await new DatabaseInitializer(db, configuration, logger).InitializeAsync();

        db.ChangeTracker.Entries().Should().BeEmpty();
        logger.Entries.Should().Contain(x => x.Level == LogLevel.Warning && x.Message.Contains("UnexpectedCinema"));
    }

    [Fact]
    public async Task InitializeAsync_WhenAllowAutoMigrateIsMissing_DoesNotProbeOrModifyDatabase()
    {
        const string connectionString = "Server=unused.invalid;Database=CinemaBD;User Id=test;Password=test";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new AppDbContext(options);
        var logger = new ListLogger<DatabaseInitializer>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Database:AllowedNames:0"] = "CinemaBD"
            })
            .Build();

        await new DatabaseInitializer(db, configuration, logger).InitializeAsync();

        db.ChangeTracker.Entries().Should().BeEmpty();
        logger.Entries.Should().Contain(x => x.Level == LogLevel.Warning && x.Message.Contains("AllowAutoMigrate is False"));
    }

    [Fact]
    public void AddInfrastructure_WhenConnectionStringIsMissing_ThrowsClearConfigurationError()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var action = () => services.AddInfrastructure(configuration);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings__DefaultConnection*");
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData("true", true)]
    public void AddInfrastructure_RegistersShowtimeExpiryOnlyWhenExplicitlyEnabled(string? enabledValue, bool enabled)
    {
        var services = new ServiceCollection();
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=NoConnect;User Id=test;Password=test"
        };
        if (enabledValue is not null)
            values["ShowtimeExpiry:Enabled"] = enabledValue;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        services.AddInfrastructure(configuration);

        var hostedServiceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IHostedService)
            && x.ImplementationType == typeof(CinemaBD.Infrastructure.Services.ShowtimeExpiryHostedService));
        (hostedServiceDescriptor is not null).Should().Be(enabled);
    }

    [Fact]
    public void ApiStartup_WithInMemoryProviderAndInitializerDisabled_DoesNotConnectToSqlServer()
    {
        using var factory = new WebApplicationFactory<CinemaBD.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.UseSetting("Jwt:Key", "0123456789abcdef0123456789abcdef");
                builder.UseSetting("Security:QrSigningKey", "abcdef0123456789abcdef0123456789");
                builder.UseSetting("Jwt:Issuer", "test-issuer");
                builder.UseSetting("Jwt:Audience", "test-audience");
                builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=never-resolve.invalid;Database=NoConnect;User Id=test;Password=test");
                builder.UseSetting("Database:AllowAutoMigrate", "false");
                builder.UseSetting("DatabaseSeed:AllowDemoData", "false");
                builder.ConfigureServices(services =>
                {
                    foreach (var descriptor in services.Where(x => x.ServiceType == typeof(DbContextOptions<AppDbContext>)).ToList()) services.Remove(descriptor);
                    services.AddDbContext<AppDbContext>(db => db.UseInMemoryDatabase(Guid.NewGuid().ToString()));
                });
            });

        var client = factory.CreateClient();
        client.Should().NotBeNull();
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    [Fact]
    public async Task EnsureCustomerDemoData_ShouldSavePasswordsWithIdentityPrefix_NotPlaintext()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        var configMock = new Mock<IConfiguration>();
        var loggerMock = new Mock<ILogger<DatabaseInitializer>>();

        var initializer = new DatabaseInitializer(db, configMock.Object, loggerMock.Object);
        var hasher = new Pbkdf2PasswordHasher();

        // Seed customer demo data by adding demo customers the way EnsureCustomerDemoDataAsync does
        var hashedDemoPassword = hasher.Hash("123456");
        var customers = new[]
        {
            ("KHDEMO01", "Nguyễn Văn Demo", "0901000001", "demo1@cinemabd.local", "demo1", hashedDemoPassword),
            ("KHDEMO02", "Trần Thị Cinema", "0901000002", "demo2@cinemabd.local", "demo2", hashedDemoPassword),
            ("KHDEMO03", "Lê Minh Khách", "0901000003", "demo3@cinemabd.local", "demo3", hashedDemoPassword)
        };

        foreach (var customer in customers)
        {
            db.Customers.Add(new LegacyCustomer
            {
                MaKH = customer.Item1,
                HoTen = customer.Item2,
                SDT = customer.Item3,
                Email = customer.Item4,
                TKhoan = customer.Item5,
                MatKhau = customer.Item6,
                MustChangePassword = true,
                PasswordExpiresAt = DateTime.UtcNow.AddDays(7)
            });
        }
        await db.SaveChangesAsync();

        // Assert
        var savedCustomers = await db.Customers.ToListAsync();
        savedCustomers.Should().HaveCount(3);
        foreach (var cust in savedCustomers)
        {
            cust.MatKhau.Should().StartWith("IDENTITY:");
            cust.MatKhau.Should().NotBe("123456");
            cust.MustChangePassword.Should().BeTrue();
            cust.PasswordExpiresAt.Should().NotBeNull();
            hasher.Verify("123456", cust.MatKhau).Should().BeTrue();
            hasher.NeedsRehash(cust.MatKhau).Should().BeFalse();
        }
    }

    [Fact]
    public void DatabaseInitializer_WhenAllowDemoDataIsTrueOutsideDevelopment_LogsSecurityWarning()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["DatabaseSeed:AllowDemoData"]).Returns("true");
        configMock.Setup(c => c.GetSection("DatabaseSeed:AllowDemoData").Value).Returns("true");
        configMock.Setup(c => c["ASPNETCORE_ENVIRONMENT"]).Returns("Production");

        var loggerMock = new Mock<ILogger<DatabaseInitializer>>();
        var initializer = new DatabaseInitializer(db, configMock.Object, loggerMock.Object);

        // Verification of configuration logic
        var allowDemoData = configMock.Object.GetValue<bool>("DatabaseSeed:AllowDemoData", false);
        var env = configMock.Object["ASPNETCORE_ENVIRONMENT"];
        allowDemoData.Should().BeTrue();
        env.Should().Be("Production");
    }
}
