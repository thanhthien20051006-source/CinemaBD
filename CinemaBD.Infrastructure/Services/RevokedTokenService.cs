using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public sealed class RevokedTokenService : IRevokedTokenService
{
    private readonly AppDbContext _db;

    public RevokedTokenService(AppDbContext db) => _db = db;

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
