namespace CinemaBD.Api.Contracts.Admin;

public record AdminEmployeeResponse(int Id, string FullName, DateTime? BirthDate, string? PhoneNumber, string? Email, DateTime? StartDate, int? RoleId, string? RoleName, bool IsActive);
