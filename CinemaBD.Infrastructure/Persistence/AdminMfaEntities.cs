namespace CinemaBD.Infrastructure.Persistence;

/// <summary>
/// Stores MFA TOTP configuration and anti-replay state for an Admin account.
/// The TOTP secret is protected via ASP.NET Core Data Protection at rest.
/// </summary>
public sealed class AdminMfa
{
    public int AdminID { get; set; }
    public byte[] SecretProtected { get; set; } = default!;
    public bool Enabled { get; set; }
    public DateTime? EnabledAtUtc { get; set; }
    public long? LastUsedStep { get; set; }

    public LegacyAdmin Admin { get; set; } = default!;
}

/// <summary>
/// Stores single-use recovery code hashes for an Admin account.
/// Plain recovery codes are displayed once and never stored or logged.
/// </summary>
public sealed class AdminRecoveryCode
{
    public int Id { get; set; }
    public int AdminID { get; set; }
    public byte[] CodeHash { get; set; } = default!;
    public DateTime? UsedAtUtc { get; set; }

    public LegacyAdmin Admin { get; set; } = default!;
}
