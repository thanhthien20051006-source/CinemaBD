using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace CinemaBD.Tests.Security;

public sealed class AdminMfaVerifiedTokenTests
{
    [Fact]
    public void CreateToken_AddsMfaVerifiedOnlyWhenMfaWasCompleted()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
            ["Jwt:Issuer"] = "CinemaBD",
            ["Jwt:Audience"] = "CinemaBD.Client"
        }).Build();
        var tokenService = new JwtTokenService(config);
        var admin = new UserAccount { Id = "admin:3", Username = "admin", FullName = "Admin", PasswordHash = "hash" };

        var ordinaryToken = new JwtSecurityTokenHandler().ReadJwtToken(tokenService.CreateToken(admin, Guid.NewGuid(), false, false, false));
        var verifiedToken = new JwtSecurityTokenHandler().ReadJwtToken(tokenService.CreateToken(admin, Guid.NewGuid(), false, false, true));

        ordinaryToken.Claims.Should().NotContain(x => x.Type == "mfa_verified");
        verifiedToken.Claims.Should().ContainSingle(x => x.Type == "mfa_verified" && x.Value == "true");
    }
}
