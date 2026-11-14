using System.Security.Cryptography;
using System.Text;
using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CinemaBD.Infrastructure.Security;

public sealed class AdminEmergencyRecoveryService : IAdminEmergencyRecoveryService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IIdentitySessionService _identitySessionService;
    private readonly IAdminAuthorizationService _adminAuthorizationService;
    private readonly ISecurityAuditWriter? _securityAuditWriter;
    private readonly ILogger<AdminEmergencyRecoveryService>? _logger;

    public AdminEmergencyRecoveryService(
        AppDbContext db,
        IConfiguration configuration,
        IIdentitySessionService identitySessionService,
        IAdminAuthorizationService adminAuthorizationService,
        ISecurityAuditWriter? securityAuditWriter = null,
        ILogger<AdminEmergencyRecoveryService>? logger = null)
    {
        _db = db;
        _configuration = configuration;
        _identitySessionService = identitySessionService;
        _adminAuthorizationService = adminAuthorizationService;
        _securityAuditWriter = securityAuditWriter;
        _logger = logger;
    }

    public async Task<bool> EmergencyResetMasterMfaAsync(int adminId, string bootstrapPassword, string reason, CancellationToken cancellationToken = default)
    {
        var configuredBootstrap = _configuration["BOOTSTRAP_ADMIN_PASSWORD"] ?? _configuration["DatabaseSeed:BootstrapAdminPassword"];
        if (string.IsNullOrWhiteSpace(configuredBootstrap))
            throw new InvalidOperationException("Bootstrap admin password is not configured for emergency recovery.");

        byte[] expectedBytes = Encoding.UTF8.GetBytes(configuredBootstrap);
        byte[] inputBytes = Encoding.UTF8.GetBytes(bootstrapPassword ?? string.Empty);
        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, inputBytes))
        {
            if (_securityAuditWriter is not null)
            {
                await _securityAuditWriter.WriteAsync(new SecurityAuditEvent(
                    "emergency_mfa_reset", "Admin", adminId.ToString(), null,
                    Result: "failure", Details: "Unauthorized attempt to trigger emergency Master MFA reset."), cancellationToken);
            }
            return false;
        }

        var admin = await _db.Admins.FirstOrDefaultAsync(x => x.AdminID == adminId && x.IsActive, cancellationToken);
        if (admin == null)
            return false;

        var access = await _adminAuthorizationService.GetAccessForAdminAsync(adminId, cancellationToken);
        if (!access.IsMaster)
            throw new InvalidOperationException("Emergency MFA reset can only be executed for Master admin accounts.");

        var mfa = await _db.AdminMfas.FirstOrDefaultAsync(x => x.AdminID == adminId, cancellationToken);
        if (mfa != null)
        {
            _db.AdminMfas.Remove(mfa);
        }

        var recoveryCodes = await _db.AdminRecoveryCodes.Where(x => x.AdminID == adminId).ToListAsync(cancellationToken);
        if (recoveryCodes.Count > 0)
        {
            _db.AdminRecoveryCodes.RemoveRange(recoveryCodes);
        }

        admin.MustChangePassword = true;
        admin.PasswordExpiresAt = DateTime.UtcNow.AddHours(24);

        await _db.SaveChangesAsync(cancellationToken);

        await _identitySessionService.RevokeAllAsync("Admin", adminId.ToString(), "emergency_master_mfa_reset", cancellationToken);

        if (_securityAuditWriter is not null)
        {
            var sanitizedReason = SecurityMetadataSanitizer.SanitizeDetails(reason) ?? "Master MFA reset via emergency bootstrap authority.";
            await _securityAuditWriter.WriteAsync(new SecurityAuditEvent(
                "emergency_mfa_reset", "Admin", adminId.ToString(), null,
                Result: "success", Details: sanitizedReason), cancellationToken);
        }

        return true;
    }
}
