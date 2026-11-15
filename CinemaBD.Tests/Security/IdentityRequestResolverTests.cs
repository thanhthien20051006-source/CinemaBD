using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CinemaBD.Api.Security;
using CinemaBD.Application.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace CinemaBD.Tests.Security;

public sealed class IdentityRequestResolverTests
{
    private const string JwtKey = "0123456789abcdef0123456789abcdef";

    private static string CreateToken(bool includeSid)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "KH-1"),
            new(JwtRegisteredClaimNames.Jti, "legacy-jti"),
            new("account_type", "customer")
        };
        if (includeSid) claims.Add(new Claim("sid", Guid.NewGuid().ToString("D")));
        var token = new JwtSecurityToken("CinemaBD.Tests", "CinemaBD.Tests.Client", claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static IdentityRequestResolver CreateResolver(Mock<IIdentitySessionService> sessions, Mock<IRevokedTokenService> revoked)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "CinemaBD.Tests",
            ["Jwt:Audience"] = "CinemaBD.Tests.Client",
            ["Jwt:Key"] = JwtKey
        }).Build();
        return new IdentityRequestResolver(new AccessTokenProofValidator(configuration), sessions.Object, revoked.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<IdentityRequestResolver>>());
    }

    [Fact]
    public async Task ResolveAsync_RejectsLegacyTwelveHourTokenWithoutSid()
    {
        var sessions = new Mock<IIdentitySessionService>(MockBehavior.Strict);
        var revoked = new Mock<IRevokedTokenService>(MockBehavior.Strict);
        var resolver = CreateResolver(sessions, revoked);

        var result = await resolver.ResolveAsync($"Bearer {CreateToken(includeSid: false)}");

        result.Should().BeNull();
        sessions.VerifyNoOtherCalls();
        revoked.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_AcceptsSignedLegacyDurationTokenWhenItHasLiveSessionClaim()
    {
        var sessions = new Mock<IIdentitySessionService>();
        sessions.Setup(x => x.IsActiveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var revoked = new Mock<IRevokedTokenService>();
        revoked.Setup(x => x.IsRevokedAsync("legacy-jti", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var resolver = CreateResolver(sessions, revoked);

        var result = await resolver.ResolveAsync($"Bearer {CreateToken(includeSid: true)}");

        result.Should().NotBeNull();
        result!.AccountType.Should().Be("KHACHHANG");
        result.AccountId.Should().Be("KH-1");
        sessions.Verify(x => x.IsActiveAsync(result.SessionId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
