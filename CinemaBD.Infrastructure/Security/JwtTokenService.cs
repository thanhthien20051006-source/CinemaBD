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
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PasswordChangeTokenLifetime = TimeSpan.FromMinutes(10);
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public string CreateToken(UserAccount user, Guid sessionId, bool restrictedToPasswordChange = false)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("A valid identity session is required to issue an access token.", nameof(sessionId));

        restrictedToPasswordChange |= user.MustChangePassword || user.PasswordExpired;
        var issuer = _configuration["Jwt:Issuer"] ?? "CinemaBD";
        var audience = _configuration["Jwt:Audience"] ?? "CinemaBD.Client";
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
        var isAdmin = user.Id.StartsWith("admin:", StringComparison.OrdinalIgnoreCase);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("sid", sessionId.ToString("D")),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("full_name", user.FullName),
            new(ClaimTypes.Role, isAdmin ? "Admin" : "Customer"),
            new("account_type", isAdmin ? "admin" : "customer")
        };

        if (isAdmin && user.RoleId is > 0)
        {
            claims.Add(new Claim("ma_cv", user.RoleId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            claims.Add(new Claim("is_master", user.IsMaster ? "true" : "false"));
            claims.AddRange(user.Permissions.Select(permission => new Claim("permission", permission)));
        }

        if (restrictedToPasswordChange)
            claims.Add(new Claim("password_change_only", "true"));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(restrictedToPasswordChange ? PasswordChangeTokenLifetime : AccessTokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
