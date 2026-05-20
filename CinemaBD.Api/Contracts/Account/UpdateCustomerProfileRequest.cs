namespace CinemaBD.Api.Contracts.Account;

public record UpdateCustomerProfileRequest(
    string FullName,
    string? Email,
    string? PhoneNumber,
    DateTime? BirthDate);
