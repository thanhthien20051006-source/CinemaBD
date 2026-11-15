namespace CinemaBD.Api.Contracts.Admin;

public sealed record AdminSecurityAccountRequest(string AccountType, string AccountId);
public sealed record AdminSecurityLockRequest(string AccountType, string AccountId, bool Locked);
