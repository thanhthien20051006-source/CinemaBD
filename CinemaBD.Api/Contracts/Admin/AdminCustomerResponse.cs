namespace CinemaBD.Api.Contracts.Admin;

public record AdminCustomerResponse(string Id, string FullName, string Username, string? Email, string? PhoneNumber, DateTime? BirthDate);
