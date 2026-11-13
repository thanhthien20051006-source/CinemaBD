using CinemaBD.Api.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CinemaBD.Tests.Security;

public sealed class AccessTokenProofValidatorTests
{
    private const string Key = "0123456789abcdef0123456789abcdef";

    private static AccessTokenProofValidator CreateValidator() => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:Issuer"] = "CinemaBD.Tests", ["Jwt:Audience"] = "CinemaBD.Tests.Client", ["Jwt:Key"] = Key
    }).Build());

    private static string CreateToken(bool includeSid, DateTime? expiry = null, string accountType = "customer", string subject = "KH001")
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("account_type", accountType)
        };
        if (includeSid) claims.Add(new Claim("sid", Guid.NewGuid().ToString("D")));
        var token = new JwtSecurityToken("CinemaBD.Tests", "CinemaBD.Tests.Client", claims,
            expires: expiry ?? DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public void Validate_RejectsLegacyJwtWithoutSid()
    {
        CreateValidator().Validate(CreateToken(includeSid: false)).Should().BeNull();
    }

    [Fact]
    public void Validate_RejectsDuplicateSidClaims()
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "KH001"), new Claim(JwtRegisteredClaimNames.Jti, "jti"),
            new Claim("account_type", "customer"), new Claim("sid", Guid.NewGuid().ToString()), new Claim("sid", Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken("CinemaBD.Tests", "CinemaBD.Tests.Client", claims,
            expires: DateTime.UtcNow.AddMinutes(15), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256));

        CreateValidator().Validate(new JwtSecurityTokenHandler().WriteToken(token)).Should().BeNull();
    }

    [Fact]
    public void Validate_RejectsExpiredTokenUnlessRefreshProofAllowsExpiry()
    {
        var expired = CreateToken(includeSid: true, DateTime.UtcNow.AddMinutes(-1));
        var validator = CreateValidator();

        validator.Validate(expired).Should().BeNull();
        validator.Validate(expired, allowExpired: true).Should().NotBeNull();
    }

    [Fact]
    public void Validate_RequiresAccountTypeAndSubjectToAgree()
    {
        CreateValidator().Validate(CreateToken(includeSid: true, accountType: "customer", subject: "admin:7")).Should().BeNull();
    }
}
