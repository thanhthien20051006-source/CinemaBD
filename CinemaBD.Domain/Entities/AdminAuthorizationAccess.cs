namespace CinemaBD.Domain.Entities;

public sealed record AdminAuthorizationAccess(int RoleId, bool IsMaster, IReadOnlyCollection<string> Permissions);
