namespace CinemaBD.Application.Interfaces;

public interface IIdentitySessionService
{
    Task<Guid> CreateSessionAsync(string accountType, string accountId, CancellationToken cancellationToken = default);
    Task<bool> IsActiveAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
