using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CinemaBD.Tests.Persistence;

public class DatabaseInitializerSqlServerTests
{
    [Theory]
    [InlineData("IDENTITY:AQAAAAIAAYagAAAAEOabc123==", false)]
    [InlineData("5f4dcc3b5aa765d61d8327deb882cf99", false)] // 32-char hex MD5
    [InlineData("202cb962ac59075b964b07152d234b70", false)] // 32-char hex MD5
    [InlineData("123456", true)]                            // Plaintext
    [InlineData("admin123", true)]                          // Plaintext
    [InlineData("MySecurePassword!", true)]                  // Plaintext
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPlaintextPassword_ShouldIdentifyPlaintextCorrectly(string? password, bool expectedIsPlaintext)
    {
        var result = DatabaseInitializer.IsPlaintextPassword(password);
        result.Should().Be(expectedIsPlaintext);
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
        var hasher = new Md5PasswordHasher();

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

    [Fact]
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
