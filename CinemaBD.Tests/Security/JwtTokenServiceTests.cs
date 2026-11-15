using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaBD.Tests.Security;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_EmitsSessionIdAndExpiresWithinFifteenMinutes()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "CinemaBD.Tests",
            ["Jwt:Audience"] = "CinemaBD.Tests.Client",
            ["Jwt:Key"] = "0123456789abcdef0123456789abcdef"
        }).Build();
        var service = new JwtTokenService(configuration);
        var sessionId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var token = service.CreateToken(new UserAccount
        {
            Id = "KH001",
            FullName = "Test Customer",
            Username = "customer",
            PasswordHash = "not-used"
        }, sessionId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Single(x => x.Type == "sid").Value.Should().Be(sessionId.ToString("D"));
        jwt.ValidTo.Should().BeOnOrAfter(before.AddMinutes(14));
        jwt.ValidTo.Should().BeOnOrBefore(DateTime.UtcNow.AddMinutes(15));
    }

    [Fact]
    public void CreateToken_RejectsEmptySessionId()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "0123456789abcdef0123456789abcdef"
        }).Build();
        var service = new JwtTokenService(configuration);

        var act = () => service.CreateToken(new UserAccount
        {
            Id = "KH001",
            FullName = "Test Customer",
            Username = "customer",
            PasswordHash = "not-used"
        }, Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
