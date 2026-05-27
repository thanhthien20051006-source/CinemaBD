using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CinemaBD.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(UserAccount user)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "CinemaBD";
        var audience = _configuration["Jwt:Audience"] ?? "CinemaBD.Client";
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");

        var isAdmin = user.Id.StartsWith("admin:", StringComparison.OrdinalIgnoreCase);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("full_name", user.FullName),
            new(ClaimTypes.Role, isAdmin ? "Admin" : "Customer"),
            new("account_type", isAdmin ? "admin" : "customer")
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
