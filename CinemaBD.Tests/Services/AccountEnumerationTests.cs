using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace CinemaBD.Tests.Services;

public class AccountEnumerationTests
{
    private const string ExpectedLoginErrorMessage = "Tài khoản hoặc mật khẩu không chính xác.";
    private const string ExpectedRegisterConflictMessage = "Không thể hoàn tất đăng ký với thông tin đã cung cấp. Vui lòng kiểm tra lại hoặc đăng nhập.";

    [Fact]
    public async Task CustomerLogin_InvalidUser_And_InvalidPassword_ShouldReturnIdenticalErrorMessage()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        var hasher = new Md5PasswordHasher();
        var tokenServiceMock = new Mock<ITokenService>();
        var configMock = new Mock<IConfiguration>();

        db.Customers.Add(new LegacyCustomer
        {
            MaKH = "KH001",
            HoTen = "Valid User",
            TKhoan = "validuser",
            MatKhau = hasher.Hash("CorrectPassword123")
        });
        await db.SaveChangesAsync();

        var authService = new AuthService(db, hasher, tokenServiceMock.Object, configMock.Object);

        // Case 1: Non-existent user
        var actNonExistent = () => authService.LoginAsync("nonexistentuser", "somepassword");
        var exNonExistent = await actNonExistent.Should().ThrowAsync<InvalidOperationException>();
        exNonExistent.WithMessage(ExpectedLoginErrorMessage);

        // Case 2: Existent user, wrong password
        var actWrongPass = () => authService.LoginAsync("validuser", "WrongPassword999");
        var exWrongPass = await actWrongPass.Should().ThrowAsync<InvalidOperationException>();
        exWrongPass.WithMessage(ExpectedLoginErrorMessage);

        // Message equality assertion
        exNonExistent.And.Message.Should().Be(exWrongPass.And.Message);
    }

    [Fact]
    public async Task AdminLogin_InvalidUser_And_InvalidPassword_ShouldReturnIdenticalErrorMessage()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        var hasher = new Md5PasswordHasher();
        var tokenServiceMock = new Mock<ITokenService>();

        db.Admins.Add(new LegacyAdmin
        {
            AdminID = 1,
            Username = "validadmin",
            Password = hasher.Hash("AdminPass123!"),
            FullName = "Valid Admin",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var adminService = new AdminAuthApiService(db, hasher, tokenServiceMock.Object);

        // Case 1: Non-existent admin
        var actNonExistent = () => adminService.LoginAsync("nonexistentadmin", "somepassword");
        var exNonExistent = await actNonExistent.Should().ThrowAsync<InvalidOperationException>();
        exNonExistent.WithMessage(ExpectedLoginErrorMessage);

        // Case 2: Existent admin, wrong password
        var actWrongPass = () => adminService.LoginAsync("validadmin", "WrongPassword999");
        var exWrongPass = await actWrongPass.Should().ThrowAsync<InvalidOperationException>();
        exWrongPass.WithMessage(ExpectedLoginErrorMessage);

        // Message equality assertion
        exNonExistent.And.Message.Should().Be(exWrongPass.And.Message);
    }

    [Fact]
    public async Task CustomerRegister_ExistingUsername_ShouldReturnGenericMessage()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new AppDbContext(options);
        var hasher = new Md5PasswordHasher();
        var tokenServiceMock = new Mock<ITokenService>();
        var configMock = new Mock<IConfiguration>();

        db.Customers.Add(new LegacyCustomer
        {
            MaKH = "KH001",
            HoTen = "Existing Customer",
            TKhoan = "duplicateuser",
            MatKhau = hasher.Hash("pass")
        });
        await db.SaveChangesAsync();

        var authService = new AuthService(db, hasher, tokenServiceMock.Object, configMock.Object);

        var act = () => authService.RegisterAsync("New Name", "duplicateuser", "newpass123", "new@example.com", "0900000000");
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage(ExpectedRegisterConflictMessage);
    }
}
