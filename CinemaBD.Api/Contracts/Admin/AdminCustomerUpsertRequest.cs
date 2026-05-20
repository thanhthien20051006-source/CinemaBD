namespace CinemaBD.Api.Contracts.Admin;

public record AdminCustomerUpsertRequest(string FullName, string Username, string Password, string? Email, string? PhoneNumber, DateTime? BirthDate);
