using CinemaBD.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBD.Api.Security;

public sealed class IdentityRequestResolver(
    AccessTokenProofValidator proofValidator,
    IIdentitySessionService sessionService,
    IRevokedTokenService revokedTokenService,
    ILogger<IdentityRequestResolver> logger)
{
    public async Task<VerifiedAccessIdentity?> ResolveAsync(string authorization, CancellationToken cancellationToken = default)
    {
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        var identity = proofValidator.Validate(token, allowExpired: true);
        if (identity is null || !await sessionService.IsActiveAsync(identity.SessionId, cancellationToken)) return null;
        if (await revokedTokenService.IsRevokedAsync(identity.Jti, cancellationToken)) return null;
        logger.LogDebug("IAM session proof validated for {AccountType} account {AccountId}.", identity.AccountType, identity.AccountId);
        return identity;
    }
}
