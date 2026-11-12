using CinemaBD.Api.Contracts.Common;
using CinemaBD.Api.Controllers.Admin;
using CinemaBD.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaBD.Tests.Controllers;

public class AdminAuthLogoutTests
{
    [Fact]
    public async Task AdminLogout_WithValidJwtClaims_ShouldRevokeTokenAndReturnSuccess()
    {
        // Arrange
        var adminAuthServiceMock = new Mock<IAdminAuthApiService>();
        var revokedTokenServiceMock = new Mock<IRevokedTokenService>();

        var controller = new AdminAuthController(adminAuthServiceMock.Object, revokedTokenServiceMock.Object);

        var testJti = Guid.NewGuid().ToString("N");
        var expiryTime = DateTimeOffset.UtcNow.AddHours(1);
        var expiryUnixSeconds = expiryTime.ToUnixTimeSeconds().ToString();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, testJti),
            new Claim(JwtRegisteredClaimNames.Exp, expiryUnixSeconds),
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
    public async Task AdminLogout_WithoutJtiClaim_ShouldReturnUnauthorized()
    {
        // Arrange
        var adminAuthServiceMock = new Mock<IAdminAuthApiService>();
        var revokedTokenServiceMock = new Mock<IRevokedTokenService>();

        var controller = new AdminAuthController(adminAuthServiceMock.Object, revokedTokenServiceMock.Object);
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
