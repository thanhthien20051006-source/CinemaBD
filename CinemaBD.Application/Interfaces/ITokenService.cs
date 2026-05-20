using CinemaBD.Domain.Entities;

namespace CinemaBD.Application.Interfaces;

public interface ITokenService
{
    string CreateToken(UserAccount user);
}
