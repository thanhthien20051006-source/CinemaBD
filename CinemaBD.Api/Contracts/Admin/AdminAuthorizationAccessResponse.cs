namespace CinemaBD.Api.Contracts.Admin;

public sealed record AdminAuthorizationAccessResponse(int RoleId, bool IsMaster, IReadOnlyCollection<string> Permissions);
