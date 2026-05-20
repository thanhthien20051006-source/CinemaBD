namespace CinemaBD.Api.Contracts.Admin;

public record AdminRoleResponse(int Id, string Name, bool IsMaster, bool IsActive);
