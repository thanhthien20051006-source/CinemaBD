namespace CinemaBD.Application.Interfaces;

/// <summary>
/// Controlled operational recovery service for resetting MFA on Master accounts
/// in an emergency (e.g. Master admin lost device and recovery codes).
/// Requires verified administrative recovery key (ADMIN_RECOVERY_KEY) and writes high-severity audit events.
/// </summary>
public interface IAdminEmergencyRecoveryService
{
    Task<bool> EmergencyResetMasterMfaAsync(int adminId, string recoveryKey, string reason, CancellationToken cancellationToken = default);
}
