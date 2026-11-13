namespace CinemaBD.Infrastructure.Persistence;

/// <summary>
/// A server-side login session. SessionId is carried as the JWT sid claim and
/// must be checked against an active row on every authenticated request.
/// </summary>
public sealed class IdentitySession
{
    public Guid SessionId { get; set; }
    public string AccountType { get; set; } = default!;
    public string AccountId { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
}
