using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Infrastructure.Services;

public sealed class RevokedTokenService : IRevokedTokenService
{
    private readonly AppDbContext _db;
    private readonly ISecurityAuditWriter? _securityAuditWriter;
    private readonly ILogger<RevokedTokenService>? _logger;

    public RevokedTokenService(AppDbContext db, ISecurityAuditWriter? securityAuditWriter = null, ILogger<RevokedTokenService>? logger = null)
    {
        _db = db;
        _securityAuditWriter = securityAuditWriter;
        _logger = logger;
    }

    public async Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti) || expiresAtUtc <= DateTime.UtcNow)
            return;

        var existing = await _db.RevokedTokens.FindAsync([jti], cancellationToken);
        if (existing == null)
            _db.RevokedTokens.Add(new RevokedToken { Jti = jti, ExpiresAtUtc = expiresAtUtc });
        else if (expiresAtUtc > existing.ExpiresAtUtc)
            existing.ExpiresAtUtc = expiresAtUtc;

        await _db.SaveChangesAsync(cancellationToken);
        if (_securityAuditWriter is not null)
            await _securityAuditWriter.WriteAsync(new SecurityAuditEvent("token_revocation", Result: "success", Details: "Access token revoked; token identifier omitted."), cancellationToken);
        else
            _logger?.LogInformation("Security event token_revocation completed; token identifier omitted.");
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var isRevoked = await _db.RevokedTokens.AnyAsync(x => x.Jti == jti && x.ExpiresAtUtc > now, cancellationToken);

        if (!isRevoked)
            await _db.RevokedTokens.Where(x => x.Jti == jti && x.ExpiresAtUtc <= now).ExecuteDeleteAsync(cancellationToken);

        return isRevoked;
    }
}
