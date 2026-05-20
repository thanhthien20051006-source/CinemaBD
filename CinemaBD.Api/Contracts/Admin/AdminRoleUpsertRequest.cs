namespace CinemaBD.Api.Contracts.Admin;

public record AdminRoleUpsertRequest(string Name, bool IsMaster, bool IsActive);
