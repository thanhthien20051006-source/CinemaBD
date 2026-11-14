using CinemaBD.Api.Contracts.Admin;
using CinemaBD.Api.Contracts.Auth;
using CinemaBD.Api.Contracts.Common;
using CinemaBD.Api.Controllers;
using CinemaBD.Api.Controllers.Admin;
using CinemaBD.Api.Security;
using CinemaBD.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace CinemaBD.Tests.Controllers;

public sealed class AuthControllerRefreshTests
{
    private static IdentityRequestResolver CreateRequestResolver(string accountType, string accountId, Guid sessionId)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test"
        }).Build();

        var proofValidator = new AccessTokenProofValidator(config);
        var sessionService = new Mock<IIdentitySessionService>();
        sessionService.Setup(x => x.IsActiveAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var revokedService = new Mock<IRevokedTokenService>();
        revokedService.Setup(x => x.IsRevokedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Subclass or mock resolver
        var mockResolver = new Mock<IdentityRequestResolver>(
            proofValidator,
            sessionService.Object,
            revokedService.Object,
            Mock.Of<ILogger<IdentityRequestResolver>>());

        mockResolver.Setup(r => r.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), true))
            .ReturnsAsync(new VerifiedSessionIdentity(sessionId, accountType, accountId, "jti_123", DateTime.UtcNow.AddHours(1)));

        return mockResolver.Object;
    }

    [Fact]
    public async Task CustomerRefresh_WhenRotateAsyncThrowsUnauthorized_Returns401WithGenericMessage()
    {
        var sessionId = Guid.NewGuid();
        var resolver = CreateRequestResolver("KHACHHANG", "KH01", sessionId);

        var refreshServiceMock = new Mock<IRefreshTokenService>();
        refreshServiceMock
            .Setup(s => s.RotateAsync("bad_refresh_token", "KHACHHANG", "KH01", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Internal detail: token expired at 12:00"));

        var controller = new AuthController(
            Mock.Of<IAuthService>(),
            Mock.Of<IRevokedTokenService>(),
            Mock.Of<IAccountPasswordService>(),
            Mock.Of<IIdentitySessionService>(),
            refreshServiceMock.Object,
            resolver);

        var request = new RefreshRequest("valid.access.token", "bad_refresh_token");
        var result = await controller.Refresh(request, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.StatusCode.Should().Be(401);

        var apiResponse = unauthorizedResult.Value as ApiResponse<object>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("Refresh token không hợp lệ hoặc đã hết hạn.");
        apiResponse.Message.Should().NotContain("Internal detail");
    }

    [Fact]
    public async Task AdminRefresh_WhenRotateAsyncThrowsUnauthorized_Returns401WithGenericMessage()
    {
        var sessionId = Guid.NewGuid();
        var resolver = CreateRequestResolver("Admin", "1", sessionId);

        var refreshServiceMock = new Mock<IRefreshTokenService>();
        refreshServiceMock
            .Setup(s => s.RotateAsync("bad_admin_token", "Admin", "1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Internal detail: reuse detected for session"));

        var controller = new AdminAuthController(
            Mock.Of<IAdminAuthApiService>(),
            Mock.Of<IRevokedTokenService>(),
            Mock.Of<IAccountPasswordService>(),
            Mock.Of<IIdentitySessionService>(),
            refreshServiceMock.Object,
            resolver);

        var request = new AdminAuthController.AdminRefreshRequest("valid.admin.access.token", "bad_admin_token");
        var result = await controller.Refresh(request, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.StatusCode.Should().Be(401);

        var apiResponse = unauthorizedResult.Value as ApiResponse<object>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("Refresh token không hợp lệ hoặc đã hết hạn.");
        apiResponse.Message.Should().NotContain("Internal detail");
    }
}
