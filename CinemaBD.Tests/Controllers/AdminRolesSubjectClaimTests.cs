using System.Security.Claims;
using CinemaBD.Api.Controllers.Admin;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CinemaBD.Tests.Controllers;

public sealed class AdminRolesSubjectClaimTests
{
    [Fact]
    public async Task GetAuthorizationAccess_UsesSubWhenItIsTheOnlySubjectClaim()
    {
        await AssertAuthorizedSubjectAsync(
            [new Claim("sub", "admin:12")],
            expectedAdminId: 12);
    }

    [Fact]
    public async Task GetAuthorizationAccess_UsesNameIdentifierWhenSubIsMissing()
    {
        await AssertAuthorizedSubjectAsync(
            [new Claim(ClaimTypes.NameIdentifier, "admin:23")],
            expectedAdminId: 23);
    }

    [Fact]
    public async Task GetAuthorizationAccess_PrefersSubWhenBothSubjectClaimsExist()
    {
        await AssertAuthorizedSubjectAsync(
            [new Claim("sub", "admin:12"), new Claim(ClaimTypes.NameIdentifier, "admin:23")],
            expectedAdminId: 12);
    }

    [Fact]
    public async Task GetAuthorizationAccess_ForbidsWhenBothSubjectClaimsAreMissing()
    {
        var (controller, authorizationService) = CreateController([]);

        var result = await controller.GetAuthorizationAccess(CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        authorizationService.Verify(service => service.GetAccessForAdminAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("admin:0")]
    [InlineData("admin:-1")]
    [InlineData("admin:abc")]
    [InlineData("")]
    public async Task GetAuthorizationAccess_ForbidsMalformedSub(string subject)
    {
        var (controller, authorizationService) = CreateController([new Claim("sub", subject)]);

        var result = await controller.GetAuthorizationAccess(CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        authorizationService.Verify(service => service.GetAccessForAdminAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAuthorizationAccess_DoesNotFallbackWhenSubExistsButIsMalformed()
    {
        var (controller, authorizationService) = CreateController(
            [new Claim("sub", "admin:0"), new Claim(ClaimTypes.NameIdentifier, "admin:23")]);

        var result = await controller.GetAuthorizationAccess(CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        authorizationService.Verify(service => service.GetAccessForAdminAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static async Task AssertAuthorizedSubjectAsync(IEnumerable<Claim> claims, int expectedAdminId)
    {
        var (controller, authorizationService) = CreateController(claims);

        var result = await controller.GetAuthorizationAccess(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        authorizationService.Verify(
            service => service.GetAccessForAdminAsync(expectedAdminId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static (AdminRolesController Controller, Mock<IAdminAuthorizationService> AuthorizationService) CreateController(
        IEnumerable<Claim> claims)
    {
        var authorizationService = new Mock<IAdminAuthorizationService>();
        authorizationService
            .Setup(service => service.GetAccessForAdminAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminAuthorizationAccess(1, true, Array.Empty<string>()));

        var controller = new AdminRolesController(
            Mock.Of<IAdminRoleService>(),
            authorizationService.Object,
            Mock.Of<ILogger<AdminRolesController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"))
                }
            }
        };

        return (controller, authorizationService);
    }
}
