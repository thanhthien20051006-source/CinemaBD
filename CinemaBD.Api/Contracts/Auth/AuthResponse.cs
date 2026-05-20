namespace CinemaBD.Api.Contracts.Auth;

public record AuthResponse(string UserId, string Username, string FullName, string Token);
