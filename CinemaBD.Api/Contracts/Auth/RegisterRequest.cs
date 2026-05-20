namespace CinemaBD.Api.Contracts.Auth;

public record RegisterRequest(string FullName, string Username, string Password, string? Email, string? PhoneNumber);
