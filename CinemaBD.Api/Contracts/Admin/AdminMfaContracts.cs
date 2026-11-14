namespace CinemaBD.Api.Contracts.Admin;

public record AdminMfaEnrollStartResponse(string OtpauthUri, string ManualEntryKey);

public record AdminMfaEnrollConfirmRequest(string Code);

public record AdminMfaEnrollConfirmResponse(IReadOnlyList<string> RecoveryCodes);

public record AdminMfaVerifyRequest(string MfaChallenge, string Code, bool IsRecoveryCode = false, string? DeviceName = null);

public record AdminMfaDisableRequest(string Password, string Code, bool IsRecoveryCode = false);
