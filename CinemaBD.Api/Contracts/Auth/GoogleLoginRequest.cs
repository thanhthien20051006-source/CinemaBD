namespace CinemaBD.Api.Contracts.Auth;

public record GoogleLoginRequest(string IdToken, string? DeviceName = null);
