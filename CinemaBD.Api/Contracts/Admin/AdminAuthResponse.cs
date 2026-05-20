namespace CinemaBD.Api.Contracts.Admin;

public record AdminAuthResponse(int AdminId, string Username, string FullName, string? Role, string Token);
