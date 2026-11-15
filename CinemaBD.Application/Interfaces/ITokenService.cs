using CinemaBD.Domain.Entities;
using System.Security.Claims;

namespace CinemaBD.Application.Interfaces;

public interface ITokenService
{
    string CreateToken(UserAccount user, Guid sessionId, bool restrictedToPasswordChange = false);
    string CreateToken(UserAccount user, Guid sessionId, bool restrictedToPasswordChange, bool restrictedToMfaEnrollment);
    string CreateToken(UserAccount user, Guid sessionId, bool restrictedToPasswordChange, bool restrictedToMfaEnrollment, bool mfaVerified)
        => CreateToken(user, sessionId, restrictedToPasswordChange, restrictedToMfaEnrollment);
    string CreateMfaChallengeToken(int adminId, string username, out string jti, out DateTime expiresAtUtc);
    ClaimsPrincipal? ValidateMfaChallengeToken(string token);
}
