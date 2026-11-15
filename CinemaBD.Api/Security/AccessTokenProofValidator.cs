using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CinemaBD.Api.Security;

public sealed class AccessTokenProofValidator(IConfiguration configuration)
{
    public VerifiedAccessIdentity? Validate(string? token, bool allowExpired = false)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var key = configuration["Jwt:Key"];
            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience)) return null;
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateLifetime = !allowExpired,
                ClockSkew = TimeSpan.Zero
            };
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out var validated);
            if (validated is not JwtSecurityToken jwt || !string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal)) return null;
            var subjects = jwt.Claims.Where(x => x.Type == JwtRegisteredClaimNames.Sub).Select(x => x.Value).Distinct().ToArray();
            var sidClaims = jwt.Claims.Where(x => x.Type == "sid").Select(x => x.Value).ToArray();
            var accountTypes = jwt.Claims.Where(x => x.Type == "account_type").Select(x => x.Value).Distinct(StringComparer.Ordinal).ToArray();
            var jtis = jwt.Claims.Where(x => x.Type == JwtRegisteredClaimNames.Jti).Select(x => x.Value).Distinct().ToArray();
            if (subjects.Length != 1 || sidClaims.Length != 1 || accountTypes.Length != 1 || jtis.Length != 1
                || !Guid.TryParse(sidClaims[0], out var sessionId) || sessionId == Guid.Empty) return null;
            return accountTypes[0] switch
            {
                "customer" when !subjects[0].StartsWith("admin:", StringComparison.OrdinalIgnoreCase)
                    => new VerifiedAccessIdentity("KHACHHANG", subjects[0], sessionId, principal, jtis[0]),
                "admin" when subjects[0].StartsWith("admin:", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(subjects[0]["admin:".Length..], out var adminId) && adminId > 0
                    => new VerifiedAccessIdentity("Admin", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), sessionId, principal, jtis[0]),
                _ => null
            };
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }
}

public sealed record VerifiedAccessIdentity(string AccountType, string AccountId, Guid SessionId, ClaimsPrincipal Principal, string Jti);
