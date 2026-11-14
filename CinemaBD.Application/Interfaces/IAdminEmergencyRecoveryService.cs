namespace CinemaBD.Application.Interfaces;

/// <summary>
/// Controlled operational recovery service for resetting MFA on Master accounts
/// in an emergency (e.g. Master admin lost device and recovery codes).
/// Requires verified bootstrap credentials and writes high-severity audit events.
/// </summary>
public interface IAdminEmergencyRecoveryService
{
    Task<bool> EmergencyResetMasterMfaAsync(int adminId, string bootstrapPassword, string reason, CancellationToken cancellationToken = default);
}
