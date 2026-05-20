namespace CinemaBD.Api.Contracts.Admin;

public record AdminEmployeeUpsertRequest(string FullName, DateTime? BirthDate, string? PhoneNumber, string? Email, DateTime? StartDate, int? RoleId, bool IsActive);
