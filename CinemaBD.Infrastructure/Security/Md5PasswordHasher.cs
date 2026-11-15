using CinemaBD.Domain.Entities;

namespace CinemaBD.Infrastructure.Security;

/// <summary>
/// Compatibility facade for legacy callers. New authentication code should depend on Pbkdf2PasswordHasher.
/// </summary>
[Obsolete("Use Pbkdf2PasswordHasher; this facade remains temporarily for legacy initialization code and tests.")]
public class Md5PasswordHasher
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    public string Hash(string password) => _hasher.Hash(password);

    public virtual bool Verify(string plainTextPassword, string storedHash)
        => _hasher.Verify(plainTextPassword, storedHash);

    public virtual bool DummyVerify(string password) => _hasher.DummyVerify(password);

    public bool NeedsRehash(string storedHash) => _hasher.NeedsRehash(storedHash);
}
