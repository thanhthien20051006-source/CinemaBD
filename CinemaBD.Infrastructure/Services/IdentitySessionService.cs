using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public sealed class IdentitySessionService : IIdentitySessionService
{
    private readonly AppDbContext _db;

    public IdentitySessionService(AppDbContext db) => _db = db;

    public async Task<Guid> CreateSessionAsync(string accountType, string accountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountType) || string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Session account identity is required.");

        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        _db.IdentitySessions.Add(new IdentitySession
        {
            SessionId = sessionId,
            AccountType = accountType,
            AccountId = accountId,
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            ExpiresAtUtc = now.AddDays(30)
        });
        await _db.SaveChangesAsync(cancellationToken);
        return sessionId;
    }

    public Task<bool> IsActiveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return _db.IdentitySessions.AsNoTracking().AnyAsync(
            session => session.SessionId == sessionId
                && session.RevokedAtUtc == null
                && session.ExpiresAtUtc > now,
            cancellationToken);
    }
}
