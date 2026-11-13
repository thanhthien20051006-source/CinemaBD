using CinemaBD.Api.Contracts.Common;
using CinemaBD.Api.Controllers.Admin;
using CinemaBD.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaBD.Tests.Controllers;

public class AdminAuthLogoutTests
{
    private static CinemaBD.Api.Security.IdentityRequestResolver CreateRequestResolver()
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test"
        }).Build();
        return new CinemaBD.Api.Security.IdentityRequestResolver(
            new CinemaBD.Api.Security.AccessTokenProofValidator(config),
            Mock.Of<IIdentitySessionService>(),
            Mock.Of<IRevokedTokenService>(),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<CinemaBD.Api.Security.IdentityRequestResolver>>());
    }

    [Fact]
    public async Task AdminLogout_WithValidJwtClaims_ShouldRevokeTokenAndReturnSuccess()
    {
        // Arrange
        var adminAuthServiceMock = new Mock<IAdminAuthApiService>();
        var revokedTokenServiceMock = new Mock<IRevokedTokenService>();

        var accountPasswordServiceMock = new Mock<IAccountPasswordService>();
        var sessionServiceMock = new Mock<IIdentitySessionService>();
        var sid = Guid.NewGuid();
        sessionServiceMock.Setup(x => x.RevokeSessionAsync(sid, "Admin", "12", "logout", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var controller = new AdminAuthController(adminAuthServiceMock.Object, revokedTokenServiceMock.Object, accountPasswordServiceMock.Object, sessionServiceMock.Object, identityRequestResolver: CreateRequestResolver());

        var testJti = Guid.NewGuid().ToString("N");
        var expiryTime = DateTimeOffset.UtcNow.AddHours(1);
        var expiryUnixSeconds = expiryTime.ToUnixTimeSeconds().ToString();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, testJti),
            new Claim(JwtRegisteredClaimNames.Exp, expiryUnixSeconds),
            new Claim(JwtRegisteredClaimNames.Sub, "admin:12"),
            new Claim("sid", sid.ToString("D")),
            new Claim("account_type", "admin"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await controller.Logout(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Đã đăng xuất admin thành công.");

        revokedTokenServiceMock.Verify(
            x => x.RevokeAsync(testJti, It.Is<DateTime>(dt => Math.Abs((dt - expiryTime.UtcDateTime).TotalSeconds) < 2), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AdminChangePassword_WithAdminIdentity_ShouldDelegateAndSucceed()
    {
        var adminAuthServiceMock = new Mock<IAdminAuthApiService>();
        var revokedTokenServiceMock = new Mock<IRevokedTokenService>();
        var passwordServiceMock = new Mock<IAccountPasswordService>();
        var sessionServiceMock = new Mock<IIdentitySessionService>();
        var controller = new AdminAuthController(adminAuthServiceMock.Object, revokedTokenServiceMock.Object, passwordServiceMock.Object, sessionServiceMock.Object, identityRequestResolver: CreateRequestResolver());
        var expires = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("account_type", "admin"),
                    new Claim(JwtRegisteredClaimNames.Sub, "admin:12"),
                    new Claim(JwtRegisteredClaimNames.Jti, "jti-1"),
                    new Claim(JwtRegisteredClaimNames.Exp, expires.ToString())
                }, "Bearer"))
            }
        };

        var result = await controller.ChangePassword(new AdminAuthController.ChangeAdminPasswordRequest("CurrentPassword123!", "NewPassword123!"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        passwordServiceMock.Verify(x => x.ChangeAdminPasswordAsync(12, "CurrentPassword123!", "NewPassword123!", "jti-1", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdminLogout_WithoutJtiClaim_ShouldReturnUnauthorized()
    {
        // Arrange
        var adminAuthServiceMock = new Mock<IAdminAuthApiService>();
        var revokedTokenServiceMock = new Mock<IRevokedTokenService>();

        var accountPasswordServiceMock = new Mock<IAccountPasswordService>();
        var controller = new AdminAuthController(adminAuthServiceMock.Object, revokedTokenServiceMock.Object, accountPasswordServiceMock.Object, Mock.Of<IIdentitySessionService>(), identityRequestResolver: CreateRequestResolver());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        // Act
        var result = await controller.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        revokedTokenServiceMock.Verify(x => x.RevokeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
