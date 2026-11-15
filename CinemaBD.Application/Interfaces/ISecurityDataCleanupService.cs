namespace CinemaBD.Application.Interfaces;

public sealed record SecurityCleanupResult(int AuditRecordsDeleted, int SessionsDeleted, int RefreshTokensDeleted, int RevokedTokensDeleted);

public interface ISecurityDataCleanupService
{
    Task<SecurityCleanupResult> CleanupAsync(DateTime nowUtc, TimeSpan auditRetention, CancellationToken cancellationToken = default);
}
