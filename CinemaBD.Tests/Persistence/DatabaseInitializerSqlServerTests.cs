using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CinemaBD.Tests.Persistence;

public class DatabaseInitializerSqlServerTests
{
    [Theory]
    [InlineData("IDENTITY:AQAAAAIAAYagAAAAEOabc123==", false)]
    [InlineData("5f4dcc3b5aa765d61d8327deb882cf99", false)] // 32-char hex MD5
    [InlineData("202cb962ac59075b964b07152d234b70", false)] // 32-char hex MD5
    [InlineData("5F4DCC3B5AA765D61D8327DEB882CF99", false)] // 32-char hex MD5 uppercase
    [InlineData("GoogleAuth_108234857234", false)]           // Google dummy password
    [InlineData("googleauth_abc123", false)]                // Google dummy lowercase
    [InlineData("GOOGLE_OAUTH_TOKEN", false)]               // Google external dummy
    [InlineData("EXTERNAL_AUTH_DUMMY", false)]              // External dummy
    [InlineData("NO_PASSWORD", false)]                      // No password placeholder
    [InlineData("no_password", false)]                      // No password lowercase
    [InlineData("123456", true)]                            // Plaintext
    [InlineData("admin123", true)]                          // Plaintext
    [InlineData("MySecurePassword!", true)]                  // Plaintext
    [InlineData("5f4dcc3b5aa765d61d8327deb882cf9g", true)] // 32 chars with invalid hex char 'g'
    [InlineData("5f4dcc3b5aa765d61d8327deb882cf9", true)]  // 31 chars
    [InlineData("5f4dcc3b5aa765d61d8327deb882cf99a", true)]// 33 chars
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsPlaintextPassword_ShouldIdentifyPlaintextCorrectly(string? password, bool expectedIsPlaintext)
    {
        var result = DatabaseInitializer.IsPlaintextPassword(password);
        result.Should().Be(expectedIsPlaintext);
    }

    [Fact]
    public async Task EnsureBootstrapAdmin_WhenConfigProvided_ShouldCreateAdminWithMasterRoleAndMustChangePassword()
    {
        using var db = CreateBootstrapDb();
        var config = CreateBootstrapConfiguration("sysadmin_bootstrap", "InitialSecurePassword123!");

        var initializer = new DatabaseInitializer(db, config, NullLogger<DatabaseInitializer>.Instance);
        await initializer.EnsureBootstrapAdminAsync();

        var created = await db.Admins.FirstOrDefaultAsync(x => x.Username == "sysadmin_bootstrap");
        created.Should().NotBeNull();
        created!.MustChangePassword.Should().BeTrue();
        created.PasswordExpiresAt.Should().NotBeNull();
        created.Password.Should().StartWith("IDENTITY:");
        created.MaCV.Should().Be(1);
        new Pbkdf2PasswordHasher().Verify("InitialSecurePassword123!", created.Password).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureBootstrapAdmin_WhenConfigurationMissing_ShouldSkipAndWarn()
    {
        using var db = CreateBootstrapDb();
        var logger = new ListLogger<DatabaseInitializer>();
        // Explicit empty values keep this test isolated from developer/CI bootstrap environment variables.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BOOTSTRAP_ADMIN_USERNAME"] = string.Empty,
                ["BOOTSTRAP_ADMIN_PASSWORD"] = string.Empty
            })
            .Build();
        var initializer = new DatabaseInitializer(db, config, logger);

        await initializer.EnsureBootstrapAdminAsync();

        (await db.Admins.CountAsync()).Should().Be(0);
        logger.Entries.Should().Contain(x => x.Level == LogLevel.Warning && x.Message.Contains("BOOTSTRAP_ADMIN_USERNAME"));
    }

    [Fact]
    public async Task EnsureBootstrapAdmin_WhenPasswordIsWeak_ShouldRejectWithoutLoggingIt()
    {
        using var db = CreateBootstrapDb();
        const string weakSecret = "weakpassword";
        var logger = new ListLogger<DatabaseInitializer>();
        var initializer = new DatabaseInitializer(db, CreateBootstrapConfiguration("sysadmin_bootstrap", weakSecret), logger);

        var action = () => initializer.EnsureBootstrapAdminAsync();

        await action.Should().ThrowAsync<InvalidOperationException>();
        (await db.Admins.CountAsync()).Should().Be(0);
        logger.Entries.Should().NotContain(x => x.Message.Contains(weakSecret));
    }

    [Fact]
    public async Task EnsureBootstrapAdmin_WhenAnyAdminExists_ShouldNotCreateOrOverwriteOnRepeatedRuns()
    {
        using var db = CreateBootstrapDb();
        const string originalPassword = "ExistingAdminPassword123!";
        var hasher = new Pbkdf2PasswordHasher();
        var originalHash = hasher.Hash(originalPassword);
        db.Admins.Add(new LegacyAdmin
        {
            Username = "existing_admin",
            Password = originalHash,
            Role = "Admin",
            IsActive = true,
            MaCV = 1
        });
        await db.SaveChangesAsync();

        var firstInitializer = new DatabaseInitializer(db,
            CreateBootstrapConfiguration("sysadmin_bootstrap", "BootstrapPassword123!"),
            NullLogger<DatabaseInitializer>.Instance);
        await firstInitializer.EnsureBootstrapAdminAsync();

        var secondInitializer = new DatabaseInitializer(db,
            CreateBootstrapConfiguration("existing_admin", "ChangedBootstrapPassword123!"),
            NullLogger<DatabaseInitializer>.Instance);
        await secondInitializer.EnsureBootstrapAdminAsync();

        (await db.Admins.CountAsync()).Should().Be(1);
        var existing = await db.Admins.SingleAsync();
        existing.Password.Should().Be(originalHash);
        hasher.Verify(originalPassword, existing.Password).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureBootstrapAdmin_WhenMasterRoleIsMissing_ShouldNotAssignAnArbitraryRole()
    {
        using var db = CreateBootstrapDb(includeMasterRole: false);
        db.Roles.Add(new LegacyRole
        {
            MaCV = 2,
            TenChucVu = "Nhân viên",
            IsMaster = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var initializer = new DatabaseInitializer(db,
            CreateBootstrapConfiguration("sysadmin_bootstrap", "InitialSecurePassword123!"),
            NullLogger<DatabaseInitializer>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.EnsureBootstrapAdminAsync());
        (await db.Admins.CountAsync()).Should().Be(0);
    }

    private static AppDbContext CreateBootstrapDb(bool includeMasterRole = true)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("BootstrapAdminTestDb_" + Guid.NewGuid())
            .Options;
        var db = new AppDbContext(options);
        if (includeMasterRole)
        {
            db.Roles.Add(new LegacyRole
            {
                MaCV = 1,
                TenChucVu = "Quản trị hệ thống",
                IsMaster = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }
        return db;
    }

    private static IConfiguration CreateBootstrapConfiguration(string username, string password) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BOOTSTRAP_ADMIN_USERNAME"] = username,
            ["BOOTSTRAP_ADMIN_PASSWORD"] = password
        })
        .Build();

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
    public async Task UpgradeLegacyPlaintextPasswords_ShouldUpgradePlaintextToIdentityHash_AndRequireChange()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UpgradePlaintextDb_" + Guid.NewGuid())
            .Options;

        using var db = new AppDbContext(options);

        // Seed customer with plaintext password
        db.Customers.Add(new LegacyCustomer
        {
            MaKH = "KH_PLAIN_01",
            HoTen = "Khách Hàng Plaintext",
            TKhoan = "kh_plain",
            MatKhau = "plaintext_secret_123",
            MustChangePassword = false,
            PasswordExpiresAt = null
        });

        // Seed admin with plaintext password
        db.Admins.Add(new LegacyAdmin
        {
            Username = "admin_plain",
            Password = "admin_secret_456",
            FullName = "Admin Plaintext",
            Role = "Admin",
            MustChangePassword = false,
            PasswordExpiresAt = null
        });

        // Seed customer with existing MD5 hash (should not be treated as plaintext)
        db.Customers.Add(new LegacyCustomer
        {
            MaKH = "KH_MD5_01",
            HoTen = "Khách Hàng MD5",
            TKhoan = "kh_md5",
            MatKhau = "5f4dcc3b5aa765d61d8327deb882cf99",
            MustChangePassword = false,
            PasswordExpiresAt = null
        });

        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder().Build();
        var initializer = new DatabaseInitializer(db, config, NullLogger<DatabaseInitializer>.Instance);
        var hasher = new Pbkdf2PasswordHasher();

        // Act: Run password upgrade
        await initializer.UpgradeLegacyPlaintextPasswordsAsync();

        // Assert
        var upgradedCustomer = await db.Customers.FirstAsync(x => x.MaKH == "KH_PLAIN_01");
        upgradedCustomer.MatKhau.Should().StartWith("IDENTITY:");
        upgradedCustomer.MustChangePassword.Should().BeTrue();
        upgradedCustomer.PasswordExpiresAt.Should().NotBeNull();
        upgradedCustomer.PasswordExpiresAt!.Value.Should().BeAfter(DateTime.UtcNow.AddDays(6));

        // Verify the original plaintext password verifies against the upgraded hash
        hasher.Verify("plaintext_secret_123", upgradedCustomer.MatKhau).Should().BeTrue();

        // Verify that raw plaintext comparison no longer succeeds
        hasher.Verify("plaintext_secret_123", "plaintext_secret_123").Should().BeFalse();

        var upgradedAdmin = await db.Admins.FirstAsync(x => x.Username == "admin_plain");
        upgradedAdmin.Password.Should().StartWith("IDENTITY:");
        upgradedAdmin.MustChangePassword.Should().BeTrue();
        hasher.Verify("admin_secret_456", upgradedAdmin.Password).Should().BeTrue();

        // The MD5 customer should NOT have been modified by the plaintext upgrade pass
        var md5Customer = await db.Customers.FirstAsync(x => x.MaKH == "KH_MD5_01");
        md5Customer.MatKhau.Should().Be("5f4dcc3b5aa765d61d8327deb882cf99");
    }

    [Fact(Skip = "Yêu cầu SQL Server container thật (docker-compose --profile container-db). Không dùng làm bằng chứng kiểm chứng khi môi trường chưa khởi chạy SQL Server.")]
    public async Task DatabaseInitializer_OnRealSqlServer_WhenContainerAvailable_ShouldInitializeSuccessfully()
    {
        // Check if SQL Server container is accessible
        var testConnString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION")
            ?? "Server=localhost,1433;Database=CinemaBD_IntegrationTest;User Id=sa;Password=Your_strong_password123!;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=3";

        var canConnect = false;
        try
        {
            using var connection = new SqlConnection(testConnString);
            await connection.OpenAsync();
            canConnect = true;
        }
        catch
        {
            // SQL Server container not running in this environment - test passes gracefully
            canConnect = false;
        }

        if (!canConnect)
        {
            // Container not running: verify graceful detection
            return;
        }

        // When container is available: verify on real SQL Server
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(testConnString)
            .Options;

        using var db = new AppDbContext(options);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = testConnString,
                ["Database:AllowAutoMigrate"] = "true",
                ["Database:AllowedNames:0"] = "CinemaBD_IntegrationTest",
                ["DatabaseSeed:AllowDemoData"] = "false"
            })
            .Build();

        var initializer = new DatabaseInitializer(db, config, NullLogger<DatabaseInitializer>.Instance);
        await initializer.InitializeAsync();

        // Verify database can be queried
        var hasTables = await db.Database.CanConnectAsync();
        hasTables.Should().BeTrue();
    }
}
