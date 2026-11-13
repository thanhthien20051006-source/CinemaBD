using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface ITokenService
{
    string CreateToken(UserAccount user, Guid sessionId, bool restrictedToPasswordChange = false);
}
