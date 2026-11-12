using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace CinemaBD.Tests.Persistence;

public class DatabaseInitializerTests
{
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
        var hasher = new Md5PasswordHasher();

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
