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
    private static readonly TimeSpan MfaChallengeTokenLifetime = TimeSpan.FromMinutes(5);
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public string CreateToken(UserAccount user, Guid sessionId, bool restrictedToPasswordChange = false)
        => CreateToken(user, sessionId, restrictedToPasswordChange, false);

    public string CreateToken(UserAccount user, Guid sessionId, bool restrictedToPasswordChange, bool restrictedToMfaEnrollment)
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

        if (restrictedToMfaEnrollment)
            claims.Add(new Claim("mfa_enrollment_only", "true"));

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

    public string CreateMfaChallengeToken(int adminId, string username, out string jti, out DateTime expiresAtUtc)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "CinemaBD";
        var audience = _configuration["Jwt:Audience"] ?? "CinemaBD.Client";
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");

        jti = Guid.NewGuid().ToString("N");
        expiresAtUtc = DateTime.UtcNow.Add(MfaChallengeTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, $"admin:{adminId}"),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new("token_type", "mfa_challenge"),
            new("admin_id", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture))
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateMfaChallengeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var issuer = _configuration["Jwt:Issuer"] ?? "CinemaBD";
        var audience = _configuration["Jwt:Audience"] ?? "CinemaBD.Client";
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwt || !string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                return null;

            if (principal.FindFirst("token_type")?.Value != "mfa_challenge")
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
