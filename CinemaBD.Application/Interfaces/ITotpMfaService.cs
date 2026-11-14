namespace CinemaBD.Application.Interfaces;

public interface ITotpMfaService
{
    byte[] GenerateSecret(int byteLength = 20);
    string ToBase32(byte[] data);
    byte[] FromBase32(string base32);
    string GenerateOtpAuthUri(string username, string base32Secret, string issuer = "CinemaBD");
    string ComputeTotp(byte[] secret, long timeStep);
    (bool IsValid, long MatchedStep, bool IsReplay) VerifyTotp(byte[] secret, string code, long? lastUsedStep, long? unixTimeSeconds = null, int allowedWindow = 1);
    IReadOnlyList<string> GenerateRecoveryCodes(int count = 10);
    byte[] HashRecoveryCode(string code);
    bool VerifyRecoveryCode(string inputCode, byte[] storedHash);
    byte[] ProtectSecret(byte[] secretBytes);
    byte[] UnprotectSecret(byte[] protectedSecret);
}
