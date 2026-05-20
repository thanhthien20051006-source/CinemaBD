namespace CinemaBD.Api.Contracts.Account;

public record CustomerProfileResponse(
    string UserId,
    string Username,
    string FullName,
    string? Email,
    string? PhoneNumber,
    DateTime? BirthDate,
    decimal TotalSpent
);
