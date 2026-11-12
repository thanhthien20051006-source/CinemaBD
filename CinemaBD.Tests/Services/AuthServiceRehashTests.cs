using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Cryptography;
using System.Text;

namespace CinemaBD.Tests.Services;

public class AuthServiceRehashTests
{
    private static string ComputeMd5(string input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    [Fact]
    public async Task AdminLogin_WithLegacyMd5Password_ShouldRehashToIdentityAndRequirePasswordReset()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        var hasher = new Md5PasswordHasher();
        var tokenServiceMock = new Mock<ITokenService>();
        tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<UserAccount>())).Returns("dummy_token");

        var rawPassword = "adminSecretPassword123";
        var md5Hash = ComputeMd5(rawPassword);

        db.Admins.Add(new LegacyAdmin
        {
            AdminID = 1,
            Username = "admin_md5",
            Password = md5Hash,
            FullName = "Admin MD5",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var adminService = new AdminAuthApiService(db, hasher, tokenServiceMock.Object);

        // Act
        var result = await adminService.LoginAsync("admin_md5", rawPassword);

        // Assert
        result.Token.Should().Be("dummy_token");
        var updatedAdmin = await db.Admins.FirstAsync(x => x.Username == "admin_md5");
        updatedAdmin.Password.Should().StartWith("IDENTITY:");
        updatedAdmin.MustChangePassword.Should().BeTrue();
        updatedAdmin.PasswordExpiresAt.Should().NotBeNull();
        updatedAdmin.PasswordExpiresAt!.Value.Should().BeAfter(DateTime.UtcNow);
        hasher.Verify(rawPassword, updatedAdmin.Password).Should().BeTrue();
    }

    [Fact]
    public async Task CustomerLogin_WithLegacyMd5Password_ShouldRehashToIdentityAndRequirePasswordReset()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        var hasher = new Md5PasswordHasher();
        var tokenServiceMock = new Mock<ITokenService>();
        tokenServiceMock.Setup(x => x.CreateToken(It.IsAny<UserAccount>())).Returns("customer_token");

        var configMock = new Mock<IConfiguration>();

        var rawPassword = "customerSecret123";
        var md5Hash = ComputeMd5(rawPassword);

        db.Customers.Add(new LegacyCustomer
        {
            MaKH = "KH001",
            HoTen = "Khách Hàng Test",
            TKhoan = "cust_md5",
            MatKhau = md5Hash
        });
        await db.SaveChangesAsync();

        var authService = new AuthService(db, hasher, tokenServiceMock.Object, configMock.Object);

        // Act
        var token = await authService.LoginAsync("cust_md5", rawPassword);

        // Assert
        token.Should().Be("customer_token");
        var updatedCustomer = await db.Customers.FirstAsync(x => x.TKhoan == "cust_md5");
        updatedCustomer.MatKhau.Should().StartWith("IDENTITY:");
        updatedCustomer.MustChangePassword.Should().BeTrue();
        updatedCustomer.PasswordExpiresAt.Should().NotBeNull();
        updatedCustomer.PasswordExpiresAt!.Value.Should().BeAfter(DateTime.UtcNow);
        hasher.Verify(rawPassword, updatedCustomer.MatKhau).Should().BeTrue();
    }
}
