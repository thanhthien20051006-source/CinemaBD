namespace CinemaBD.Api.Contracts.Admin;

public record AdminAuthResponse(int AdminId, string Username, string FullName, string? Role, string Token, bool MustChangePassword = false, bool PasswordExpired = false, string? RefreshToken = null, Guid? SessionId = null);
