namespace CinemaBD.Infrastructure.Persistence;

/// <summary>
/// Stores only the SHA-256 digest of a refresh token; raw refresh tokens must
/// never be persisted or included in logs or audit records.
/// </summary>
public sealed class RefreshTokenRecord
{
    public Guid RefreshTokenId { get; set; }
    public Guid SessionId { get; set; }
    public byte[] TokenHash { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedByRefreshTokenId { get; set; }

    public IdentitySession Session { get; set; } = default!;
}
