using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaBD.Infrastructure.Services;

public class AdminAuthApiService : IAdminAuthApiService
{
    private readonly AppDbContext _db;
    private readonly Pbkdf2PasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IIdentitySessionService _identitySessionService;
    private readonly IAdminAuthorizationService _adminAuthorizationService;
    private readonly ITotpMfaService _totpService;
    private readonly IRevokedTokenService _revokedTokenService;
    private readonly IConfiguration _configuration;
    private readonly ISecurityAuditWriter? _securityAuditWriter;
    private readonly ILogger<AdminAuthApiService>? _logger;
    private readonly IAccountLockoutService? _lockoutService;

    public AdminAuthApiService(
        AppDbContext db,
        Pbkdf2PasswordHasher passwordHasher,
        ITokenService tokenService,
        IIdentitySessionService identitySessionService,
        IAdminAuthorizationService adminAuthorizationService,
        ITotpMfaService totpService,
        IRevokedTokenService revokedTokenService,
        IConfiguration configuration,
        ISecurityAuditWriter? securityAuditWriter = null,
        ILogger<AdminAuthApiService>? logger = null,
        IAccountLockoutService? lockoutService = null)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _identitySessionService = identitySessionService;
        _adminAuthorizationService = adminAuthorizationService;
        _totpService = totpService;
        _revokedTokenService = revokedTokenService;
        _configuration = configuration;
        _securityAuditWriter = securityAuditWriter;
        _logger = logger;
        _lockoutService = lockoutService;
    }

    public AdminAuthApiService(
        AppDbContext db,
        Pbkdf2PasswordHasher passwordHasher,
        ITokenService tokenService,
        IIdentitySessionService identitySessionService,
        IAdminAuthorizationService adminAuthorizationService,
        ISecurityAuditWriter? securityAuditWriter = null,
        ILogger<AdminAuthApiService>? logger = null,
        IAccountLockoutService? lockoutService = null)
        : this(
            db,
            passwordHasher,
            tokenService,
            identitySessionService,
            adminAuthorizationService,
            new TotpMfaService(new DefaultFallbackDataProtectionProvider()),
            new RevokedTokenService(db),
            new DefaultFallbackConfiguration(),
            securityAuditWriter,
            logger,
            lockoutService)
    {
    }

    private sealed class DefaultFallbackDataProtectionProvider : Microsoft.AspNetCore.DataProtection.IDataProtectionProvider
    {
        public Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(string purpose) => new FallbackProtector();
        private sealed class FallbackProtector : Microsoft.AspNetCore.DataProtection.IDataProtector
        {
            public Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(string purpose) => this;
            public byte[] Protect(byte[] plaintext) => plaintext;
            public byte[] Unprotect(byte[] protectedData) => protectedData;
        }
    }

    private sealed class DefaultFallbackConfiguration : IConfiguration
    {
        public string? this[string key] { get => null; set { } }
        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => NullToken.Instance;
        public IConfigurationSection GetSection(string key) => new FallbackSection(key);
        private sealed class NullToken : Microsoft.Extensions.Primitives.IChangeToken
        {
            public static readonly NullToken Instance = new();
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new NullDisp();
            private sealed class NullDisp : IDisposable { public void Dispose() { } }
        }
        private sealed class FallbackSection(string key) : IConfigurationSection
        {
            public string? this[string k] { get => null; set { } }
            public string Key => key;
            public string Path => key;
            public string? Value { get => null; set { } }
            public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
            public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => NullToken.Instance;
            public IConfigurationSection GetSection(string k) => this;
        }
    }

    public async Task<AdminLoginResult> LoginAsync(string username, string password, string? deviceName = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var lockout = _lockoutService is null ? null : await _lockoutService.GetAsync("Admin", username, cancellationToken);
        if (lockout is { IsLocked: false, LockoutEndUtc: not null } expiredLockout && _lockoutService is not null)
        {
            await _lockoutService.ResetAsync("Admin", username, cancellationToken);
            if (expiredLockout.AccountId is not null)
                await WriteAuditSafelyAsync("account_unlocked", "Admin", expiredLockout.AccountId, deviceName, userAgent, "success", "Temporary lockout expired automatically.", cancellationToken);
        }

        var admin = await _db.Admins.FirstOrDefaultAsync(x => x.Username == username && x.IsActive, cancellationToken);
        if (lockout?.IsLocked == true)
        {
            _passwordHasher.DummyVerify(password);
            await WriteAuditSafelyAsync("login_failed", "Admin", lockout.AccountId ?? admin?.AdminID.ToString(System.Globalization.CultureInfo.InvariantCulture), deviceName, userAgent, "failure", "Invalid credentials; temporary lockout active.", cancellationToken);
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
        }
        if (admin == null)
        {
            _passwordHasher.DummyVerify(password);
            await RegisterInvalidAttemptAsync(username, null, deviceName, userAgent, cancellationToken);
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
        }

        var verification = _passwordHasher.VerifyDetailed(password, admin.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            await RegisterInvalidAttemptAsync(username, admin.AdminID.ToString(System.Globalization.CultureInfo.InvariantCulture), deviceName, userAgent, cancellationToken);
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded || _passwordHasher.NeedsRehash(admin.Password))
        {
            admin.Password = _passwordHasher.Hash(password);
            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                admin.MustChangePassword = true;
                admin.PasswordExpiresAt = DateTime.UtcNow.AddDays(7);
            }
            await _db.SaveChangesAsync(cancellationToken);
        }

        var passwordExpired = admin.PasswordExpiresAt is not null && admin.PasswordExpiresAt <= DateTime.UtcNow;
        var mustChangePassword = admin.MustChangePassword || passwordExpired;
        var fullName = string.IsNullOrWhiteSpace(admin.FullName) ? admin.Username : admin.FullName;

        var mfa = await _db.AdminMfas.FirstOrDefaultAsync(x => x.AdminID == admin.AdminID, cancellationToken);
        var isMfaEnabled = mfa?.Enabled == true;

        var access = await _adminAuthorizationService.GetAccessForAdminAsync(admin.AdminID, cancellationToken);
        var isMaster = access.IsMaster;
        var requireForMaster = _configuration.GetValue<bool>("Mfa:RequireForMaster", true);
        var requireForAllAdmins = _configuration.GetValue<bool>("Mfa:RequireForAllAdmins", false);
        if (isMfaEnabled || requireForAllAdmins)
        {
            var challengeToken = _tokenService.CreateMfaChallengeToken(admin.AdminID, admin.Username, out _, out _);
            return new AdminLoginResult(
                admin.AdminID,
                admin.Username,
                fullName,
                admin.Role,
                Token: string.Empty,
                RefreshToken: null,
                SessionId: null,
                mustChangePassword,
                passwordExpired,
                MfaRequired: true,
                MfaChallenge: challengeToken,
                MfaEnrollmentRestricted: isMaster && requireForMaster || requireForAllAdmins);
        }

        var isMfaEnrollmentRestricted = isMaster && requireForMaster || requireForAllAdmins && !isMfaEnabled;

        var account = new UserAccount
        {
            Id = $"admin:{admin.AdminID}",
            Username = admin.Username,
            FullName = fullName,
            PasswordHash = admin.Password,
            MustChangePassword = mustChangePassword,
            PasswordExpired = passwordExpired,
            RoleId = access.RoleId,
            IsMaster = access.IsMaster,
            Permissions = access.Permissions
        };

        var accountId = admin.AdminID.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var sessionId = await _identitySessionService.CreateSessionAsync("Admin", accountId, deviceName, userAgent, cancellationToken);
        try
        {
            var token = _tokenService.CreateToken(account, sessionId, mustChangePassword, isMfaEnrollmentRestricted, mfaVerified: false);
            var refreshToken = await _identitySessionService.IssueRefreshTokenAsync(sessionId, cancellationToken);
            await WriteAuditSafelyAsync("login_success", "Admin", accountId, deviceName, userAgent, "success", "Login succeeded.", cancellationToken, sessionId);
            if (_lockoutService is not null)
            {
                await _lockoutService.ResetAsync("Admin", username, cancellationToken);
                if (lockout?.FailedAttempts > 0)
                    await WriteAuditSafelyAsync("account_unlocked", "Admin", accountId, deviceName, userAgent, "success", "Successful authentication cleared prior failed-attempt state.", cancellationToken, sessionId);
            }
            return new AdminLoginResult(
                admin.AdminID,
                admin.Username,
                fullName,
                admin.Role,
                token,
                refreshToken,
                sessionId,
                mustChangePassword,
                passwordExpired,
                MfaRequired: false,
                MfaChallenge: null,
                MfaEnrollmentRestricted: isMfaEnrollmentRestricted);
        }
        catch
        {
            await _identitySessionService.RevokeSessionAsync(sessionId, "Admin", accountId, "token_issuance_failed", cancellationToken);
            throw;
        }
    }

    public async Task<AdminLoginResult> VerifyMfaAsync(string mfaChallenge, string code, bool isRecoveryCode, string? deviceName, string? userAgent, CancellationToken cancellationToken = default)
    {
        var principal = _tokenService.ValidateMfaChallengeToken(mfaChallenge);
        var jti = principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var username = principal?.FindFirst(ClaimTypes.Name)?.Value
            ?? principal?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
            ?? string.Empty;
        var adminIdStr = principal?.FindFirst("admin_id")?.Value;

        if (principal == null || string.IsNullOrWhiteSpace(jti) || string.IsNullOrWhiteSpace(adminIdStr) || !int.TryParse(adminIdStr, out var adminId) || adminId <= 0)
        {
            if (!string.IsNullOrWhiteSpace(username))
                await RegisterInvalidAttemptAsync(username, null, deviceName, userAgent, cancellationToken);
            await WriteAuditSafelyAsync("mfa_login_failed", "Admin", null, deviceName, userAgent, "failure", "Invalid or expired MFA challenge token.", cancellationToken);
            throw new InvalidOperationException("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
        }

        if (await _revokedTokenService.IsRevokedAsync(jti, cancellationToken))
        {
            await RegisterInvalidAttemptAsync(username, adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), deviceName, userAgent, cancellationToken);
            await WriteAuditSafelyAsync("mfa_login_failed", "Admin", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), deviceName, userAgent, "failure", "Replayed MFA challenge token rejected.", cancellationToken);
            throw new InvalidOperationException("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
        }

        // Challenge tokens are single-use: revoke immediately upon consumption
        await _revokedTokenService.RevokeAsync(jti, DateTime.UtcNow.AddMinutes(5), cancellationToken);

        var lockout = _lockoutService is null ? null : await _lockoutService.GetAsync("Admin", username, cancellationToken);
        if (lockout?.IsLocked == true)
        {
            await WriteAuditSafelyAsync("mfa_login_failed", "Admin", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), deviceName, userAgent, "failure", "MFA verification attempt while locked.", cancellationToken);
            throw new InvalidOperationException("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
        }

        var admin = await _db.Admins.FirstOrDefaultAsync(x => x.AdminID == adminId && x.IsActive, cancellationToken);
        if (admin != null && string.IsNullOrWhiteSpace(username))
            username = admin.Username;

        var mfa = await _db.AdminMfas.FirstOrDefaultAsync(x => x.AdminID == adminId, cancellationToken);

        if (admin == null || mfa == null || !mfa.Enabled || mfa.SecretProtected == null)
        {
            await RegisterInvalidAttemptAsync(username, adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), deviceName, userAgent, cancellationToken);
            await WriteAuditSafelyAsync("mfa_login_failed", "Admin", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), deviceName, userAgent, "failure", "Admin MFA not configured or inactive.", cancellationToken);
            throw new InvalidOperationException("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
        }

        var accountId = admin.AdminID.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (isRecoveryCode)
        {
            byte[] inputHash = _totpService.HashRecoveryCode(code);
            if (_db.Database.IsRelational())
            {
                var affected = await _db.AdminRecoveryCodes
                    .Where(x => x.AdminID == adminId && x.CodeHash == inputHash && x.UsedAtUtc == null)
                    .ExecuteUpdateAsync(u => u.SetProperty(x => x.UsedAtUtc, DateTime.UtcNow), cancellationToken);

                if (affected == 0)
                {
                    await RegisterInvalidAttemptAsync(username, accountId, deviceName, userAgent, cancellationToken);
                    await WriteAuditSafelyAsync("mfa_login_failed", "Admin", accountId, deviceName, userAgent, "failure", "Invalid or already-consumed recovery code.", cancellationToken);
                    throw new InvalidOperationException("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
                }
            }
            else
            {
                var recoveryRecord = await _db.AdminRecoveryCodes.FirstOrDefaultAsync(
                    x => x.AdminID == adminId && x.CodeHash == inputHash && x.UsedAtUtc == null,
                    cancellationToken);

                if (recoveryRecord == null)
                {
                    await RegisterInvalidAttemptAsync(username, accountId, deviceName, userAgent, cancellationToken);
                    await WriteAuditSafelyAsync("mfa_login_failed", "Admin", accountId, deviceName, userAgent, "failure", "Invalid or already-consumed recovery code.", cancellationToken);
                    throw new InvalidOperationException("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
                }

                recoveryRecord.UsedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }

            await WriteAuditSafelyAsync("recovery_code_used", "Admin", accountId, deviceName, userAgent, "success", "Single-use recovery code consumed.", cancellationToken);
        }
        else
        {
            byte[] secret = _totpService.UnprotectSecret(mfa.SecretProtected);
            var (isValid, matchedStep, isReplay) = _totpService.VerifyTotp(secret, code, mfa.LastUsedStep);

            if (!isValid)
            {
                await RegisterInvalidAttemptAsync(username, accountId, deviceName, userAgent, cancellationToken);
                var detail = isReplay ? "Replayed TOTP code rejected." : "Invalid TOTP code.";
                await WriteAuditSafelyAsync("mfa_login_failed", "Admin", accountId, deviceName, userAgent, "failure", detail, cancellationToken);
                throw new InvalidOperationException("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
            }

            if (_db.Database.IsRelational())
            {
                var affected = await _db.AdminMfas
                    .Where(x => x.AdminID == adminId && (x.LastUsedStep == null || x.LastUsedStep < matchedStep))
                    .ExecuteUpdateAsync(u => u.SetProperty(x => x.LastUsedStep, matchedStep), cancellationToken);

                if (affected == 0)
                {
                    await RegisterInvalidAttemptAsync(username, accountId, deviceName, userAgent, cancellationToken);
                    await WriteAuditSafelyAsync("mfa_login_failed", "Admin", accountId, deviceName, userAgent, "failure", "Replayed TOTP code rejected.", cancellationToken);
                    throw new InvalidOperationException("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
                }
            }
            else
            {
                if (mfa.LastUsedStep.HasValue && mfa.LastUsedStep.Value >= matchedStep)
                {
                    await RegisterInvalidAttemptAsync(username, accountId, deviceName, userAgent, cancellationToken);
                    await WriteAuditSafelyAsync("mfa_login_failed", "Admin", accountId, deviceName, userAgent, "failure", "Replayed TOTP code rejected.", cancellationToken);
                    throw new InvalidOperationException("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
                }

                mfa.LastUsedStep = matchedStep;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        var passwordExpired = admin.PasswordExpiresAt is not null && admin.PasswordExpiresAt <= DateTime.UtcNow;
        var mustChangePassword = admin.MustChangePassword || passwordExpired;
        var fullName = string.IsNullOrWhiteSpace(admin.FullName) ? admin.Username : admin.FullName;

        var access = await _adminAuthorizationService.GetAccessForAdminAsync(admin.AdminID, cancellationToken);
        var account = new UserAccount
        {
            Id = $"admin:{admin.AdminID}",
            Username = admin.Username,
            FullName = fullName,
            PasswordHash = admin.Password,
            MustChangePassword = mustChangePassword,
            PasswordExpired = passwordExpired,
            RoleId = access.RoleId,
            IsMaster = access.IsMaster,
            Permissions = access.Permissions
        };

        var sessionId = await _identitySessionService.CreateSessionAsync("Admin", accountId, deviceName, userAgent, cancellationToken);
        try
        {
            var token = _tokenService.CreateToken(account, sessionId, mustChangePassword, restrictedToMfaEnrollment: false, mfaVerified: true);
            var refreshToken = await _identitySessionService.IssueRefreshTokenAsync(sessionId, cancellationToken);
            await WriteAuditSafelyAsync("mfa_login_success", "Admin", accountId, deviceName, userAgent, "success", isRecoveryCode ? "MFA login succeeded via recovery code." : "MFA login succeeded via TOTP.", cancellationToken, sessionId);

            if (_lockoutService is not null)
            {
                await _lockoutService.ResetAsync("Admin", username, cancellationToken);
                if (lockout?.FailedAttempts > 0)
                    await WriteAuditSafelyAsync("account_unlocked", "Admin", accountId, deviceName, userAgent, "success", "Successful MFA authentication cleared prior failed-attempt state.", cancellationToken, sessionId);
            }

            return new AdminLoginResult(
                admin.AdminID,
                admin.Username,
                fullName,
                admin.Role,
                token,
                refreshToken,
                sessionId,
                mustChangePassword,
                passwordExpired);
        }
        catch
        {
            await _identitySessionService.RevokeSessionAsync(sessionId, "Admin", accountId, "token_issuance_failed", cancellationToken);
            throw;
        }
    }

    public async Task<(string OtpauthUri, string ManualEntryKey)> StartMfaEnrollmentAsync(int adminId, CancellationToken cancellationToken = default)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(x => x.AdminID == adminId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản quản trị.");

        if (admin.MustChangePassword || (admin.PasswordExpiresAt.HasValue && admin.PasswordExpiresAt <= DateTime.UtcNow))
            throw new InvalidOperationException("Cần đổi mật khẩu trước khi thiết lập xác thực hai bước.");

        byte[] rawSecret = _totpService.GenerateSecret(20);
        byte[] protectedSecret = _totpService.ProtectSecret(rawSecret);

        var mfa = await _db.AdminMfas.FirstOrDefaultAsync(x => x.AdminID == adminId, cancellationToken);
        if (mfa == null)
        {
            mfa = new AdminMfa
            {
                AdminID = adminId,
                SecretProtected = protectedSecret,
                Enabled = false,
                EnabledAtUtc = null,
                LastUsedStep = null
            };
            _db.AdminMfas.Add(mfa);
        }
        else
        {
            mfa.SecretProtected = protectedSecret;
            mfa.Enabled = false;
            mfa.EnabledAtUtc = null;
            mfa.LastUsedStep = null;
        }

        await _db.SaveChangesAsync(cancellationToken);

        string manualKey = _totpService.ToBase32(rawSecret);
        string uri = _totpService.GenerateOtpAuthUri(admin.Username, manualKey, "CinemaBD");

        await WriteAuditSafelyAsync("mfa_enrollment_started", "Admin", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), null, null, "success", "Admin MFA enrollment initialized.", cancellationToken);

        return (uri, manualKey);
    }

    public async Task<IReadOnlyList<string>> ConfirmMfaEnrollmentAsync(int adminId, string code, CancellationToken cancellationToken = default)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(x => x.AdminID == adminId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản quản trị.");

        var mfa = await _db.AdminMfas.FirstOrDefaultAsync(x => x.AdminID == adminId, cancellationToken)
            ?? throw new InvalidOperationException("Chưa khởi tạo quá trình thiết lập xác thực hai bước.");

        byte[] secret = _totpService.UnprotectSecret(mfa.SecretProtected);
        var (isValid, matchedStep, _) = _totpService.VerifyTotp(secret, code, mfa.LastUsedStep);

        if (!isValid)
        {
            await WriteAuditSafelyAsync("mfa_login_failed", "Admin", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), null, null, "failure", "Invalid confirmation code during MFA enrollment.", cancellationToken);
            throw new InvalidOperationException("Mã xác thực không hợp lệ. Vui lòng kiểm tra lại đồng hồ thiết bị.");
        }

        mfa.Enabled = true;
        mfa.EnabledAtUtc = DateTime.UtcNow;
        mfa.LastUsedStep = matchedStep;

        var recoveryCodes = _totpService.GenerateRecoveryCodes(10);
        var existingCodes = await _db.AdminRecoveryCodes.Where(x => x.AdminID == adminId).ToListAsync(cancellationToken);
        if (existingCodes.Count > 0)
            _db.AdminRecoveryCodes.RemoveRange(existingCodes);

        foreach (var rc in recoveryCodes)
        {
            _db.AdminRecoveryCodes.Add(new AdminRecoveryCode
            {
                AdminID = adminId,
                CodeHash = _totpService.HashRecoveryCode(rc),
                UsedAtUtc = null
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        await WriteAuditSafelyAsync("mfa_enabled", "Admin", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), null, null, "success", "Admin MFA enabled successfully with 10 single-use recovery codes.", cancellationToken);

        return recoveryCodes;
    }

    public async Task DisableMfaAsync(int adminId, string password, string code, bool isRecoveryCode, CancellationToken cancellationToken = default)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(x => x.AdminID == adminId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản quản trị.");

        var access = await _adminAuthorizationService.GetAccessForAdminAsync(adminId, cancellationToken);
        var requireForMaster = _configuration.GetValue<bool>("Mfa:RequireForMaster", true);
        if (access.IsMaster && requireForMaster)
            throw new InvalidOperationException("Tài khoản Master không được phép tắt xác thực hai bước khi chính sách bắt buộc MFA đang bật.");

        if (_passwordHasher.VerifyDetailed(password, admin.Password) == PasswordVerificationResult.Failed)
        {
            await WriteAuditSafelyAsync("mfa_disabled", "Admin", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), null, null, "failure", "MFA disable rejected: invalid password.", cancellationToken);
            throw new InvalidOperationException("Mật khẩu không chính xác.");
        }

        var mfa = await _db.AdminMfas.FirstOrDefaultAsync(x => x.AdminID == adminId, cancellationToken);
        if (mfa == null || !mfa.Enabled)
            throw new InvalidOperationException("Tài khoản chưa bật xác thực hai bước.");

        if (isRecoveryCode)
        {
            byte[] inputHash = _totpService.HashRecoveryCode(code);
            var recoveryRecord = await _db.AdminRecoveryCodes.FirstOrDefaultAsync(
                x => x.AdminID == adminId && x.CodeHash == inputHash && x.UsedAtUtc == null,
                cancellationToken);

            if (recoveryRecord == null)
            {
                await WriteAuditSafelyAsync("mfa_disabled", "Admin", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), null, null, "failure", "MFA disable rejected: invalid recovery code.", cancellationToken);
                throw new InvalidOperationException("Mã xác minh không chính xác.");
            }

            recoveryRecord.UsedAtUtc = DateTime.UtcNow;
        }
        else
        {
            byte[] secret = _totpService.UnprotectSecret(mfa.SecretProtected);
            var (isValid, _, _) = _totpService.VerifyTotp(secret, code, mfa.LastUsedStep);
            if (!isValid)
            {
                await WriteAuditSafelyAsync("mfa_disabled", "Admin", adminId.ToString(System.Globalization.CultureInfo.InvariantCulture), null, null, "failure", "MFA disable rejected: invalid TOTP code.", cancellationToken);
                throw new InvalidOperationException("Mã xác minh không chính xác.");
            }
        }

        _db.AdminMfas.Remove(mfa);
        var recoveryCodes = await _db.AdminRecoveryCodes.Where(x => x.AdminID == adminId).ToListAsync(cancellationToken);
        if (recoveryCodes.Count > 0)
            _db.AdminRecoveryCodes.RemoveRange(recoveryCodes);

        await _db.SaveChangesAsync(cancellationToken);

        var adminAccountId = adminId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await _identitySessionService.RevokeAllAsync("Admin", adminAccountId, "mfa_disabled", cancellationToken);
        await WriteAuditSafelyAsync("mfa_disabled", "Admin", adminAccountId, null, null, "success", "Admin MFA disabled; all other sessions revoked.", cancellationToken);
    }

    private async Task RegisterInvalidAttemptAsync(string username, string? accountId, string? deviceName, string? userAgent, CancellationToken cancellationToken)
    {
        var state = _lockoutService is null ? null : await _lockoutService.RecordFailureAsync("Admin", username, accountId, cancellationToken);
        var description = state?.IsLocked == true ? "Invalid credentials; account temporarily locked." : "Invalid credentials.";
        await WriteAuditSafelyAsync("login_failed", "Admin", accountId, deviceName, userAgent, "failure", description, cancellationToken);
        if (state?.IsLocked == true)
        {
            _logger?.LogWarning("Security lockout activated for {AccountType} account {AccountId} after configured failed-login threshold.", "Admin", accountId ?? "unknown");
            await WriteAuditSafelyAsync("account_locked", "Admin", accountId, deviceName, userAgent, "locked", "Temporary lockout activated.", cancellationToken);
        }
        else if (state is not null && state.FailedAttempts >= 3)
        {
            _logger?.LogWarning("Multiple failed login attempts detected for {AccountType} account {AccountId}.", "Admin", accountId ?? "unknown");
            await WriteAuditSafelyAsync("multiple_login_failures", "Admin", accountId, deviceName, userAgent, "warning", "Multiple consecutive invalid credentials.", cancellationToken);
        }
    }

    private async Task WriteAuditAsync(string eventType, string actorType, string? actorId, string? deviceName, string? userAgent, string result, string? details, CancellationToken cancellationToken, Guid? sessionId = null)
    {
        if (_securityAuditWriter is not null)
            await _securityAuditWriter.WriteAsync(new SecurityAuditEvent(eventType, actorType, actorId, sessionId, DeviceName: deviceName, UserAgent: userAgent, Result: result, Details: details), cancellationToken);
        else
            _logger?.LogInformation("Security event {EventType} for {ActorType} {ActorId} result {Result}: {Details}", eventType, actorType, actorId ?? "unknown", result, details);
    }

    private async Task WriteAuditSafelyAsync(string eventType, string actorType, string? actorId, string? deviceName, string? userAgent, string result, string? details, CancellationToken cancellationToken, Guid? sessionId = null)
    {
        try
        {
            await WriteAuditAsync(eventType, actorType, actorId, deviceName, userAgent, result, details, cancellationToken, sessionId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger?.LogError(exception, "Failed to persist security audit event {EventType} for {ActorType}.", eventType, actorType);
        }
    }
}
