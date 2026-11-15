namespace CinemaBD.Application.Interfaces;

public sealed record AdminLoginResult(
    int AdminId,
    string Username,
    string FullName,
    string? Role,
    string Token,
    string? RefreshToken,
    Guid? SessionId,
    bool MustChangePassword,
    bool PasswordExpired,
    bool MfaRequired = false,
    string? MfaChallenge = null,
    bool MfaEnrollmentRestricted = false
);

public interface IAdminAuthApiService
{
    Task<AdminLoginResult> LoginAsync(string username, string password, string? deviceName, string? userAgent, CancellationToken cancellationToken = default);
    Task<AdminLoginResult> VerifyMfaAsync(string mfaChallenge, string code, bool isRecoveryCode, string? deviceName, string? userAgent, CancellationToken cancellationToken = default);
    Task<(string OtpauthUri, string ManualEntryKey)> StartMfaEnrollmentAsync(int adminId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ConfirmMfaEnrollmentAsync(int adminId, string code, CancellationToken cancellationToken = default);
    Task DisableMfaAsync(int adminId, string password, string code, bool isRecoveryCode, CancellationToken cancellationToken = default);
}
