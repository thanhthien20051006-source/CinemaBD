namespace CinemaBD.Application.Exceptions;

/// <summary>Indicates that an account is currently locked during an eligible login attempt.</summary>
public sealed class AccountLockedException : Exception
{
    public AccountLockedException(DateTime lockoutEndUtc)
        : base("Account is locked.")
    {
        LockoutEndUtc = DateTime.SpecifyKind(lockoutEndUtc, DateTimeKind.Utc);
    }

    public DateTime LockoutEndUtc { get; }
}
