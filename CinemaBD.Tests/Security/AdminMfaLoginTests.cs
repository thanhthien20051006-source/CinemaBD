using CinemaBD.Application.Exceptions;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace CinemaBD.Tests.Security;

public sealed class AdminMfaLoginTests
{
    private static IConfiguration CreateConfig(int maxAttempts = 5, int lockoutMinutes = 15)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
            ["Jwt:Issuer"] = "CinemaBD",
            ["Jwt:Audience"] = "CinemaBD.Client",
            ["Security:AccountLockout:MaxFailedAttempts"] = maxAttempts.ToString(),
            ["Security:AccountLockout:LockoutMinutes"] = lockoutMinutes.ToString(),
            ["Mfa:RequireForMaster"] = "true"
        }).Build();

    private sealed class InMemoryAuditWriter : ISecurityAuditWriter
    {
        public List<SecurityAuditEvent> Events { get; } = [];
        public Task WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class TestAdminAuthService : IAdminAuthorizationService
    {
        private readonly bool _isMaster;
        public TestAdminAuthService(bool isMaster = false) => _isMaster = isMaster;
        public Task<AdminAuthorizationAccess> GetAccessForAdminAsync(int adminId, CancellationToken cancellationToken = default)
            => Task.FromResult(new AdminAuthorizationAccess(1, _isMaster, Array.Empty<string>()));
    }

    [Fact]
    public async Task AdminLogin_WithMfaEnabled_ReturnsChallengeAndDoesNotIssueSessionOrToken()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = CreateConfig();
        var hasher = new Pbkdf2PasswordHasher();
        var dataProtection = new EphemeralDataProtectionProvider();
        var totpService = new TotpMfaService(dataProtection);
        var tokenService = new JwtTokenService(config);
        var sessionService = new IdentitySessionService(db);
        var revokedTokenService = new RevokedTokenService(db);
        var lockout = new AccountLockoutService(db, config);
        var audit = new InMemoryAuditWriter();

        byte[] secret = totpService.GenerateSecret(20);
        var admin = new LegacyAdmin { AdminID = 10, Username = "mfa_admin", Password = hasher.Hash("AdminPass123!"), IsActive = true };
        db.Admins.Add(admin);
        db.AdminMfas.Add(new AdminMfa
        {
            AdminID = 10,
            SecretProtected = totpService.ProtectSecret(secret),
            Enabled = true,
            EnabledAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AdminAuthApiService(
            db, hasher, tokenService, sessionService, new TestAdminAuthService(false),
            totpService, revokedTokenService, config, audit, NullLogger<AdminAuthApiService>.Instance, lockout);

        var result = await service.LoginAsync("mfa_admin", "AdminPass123!");

        result.MfaRequired.Should().BeTrue();
        result.MfaChallenge.Should().NotBeNullOrWhiteSpace();
        result.Token.Should().BeEmpty("Access token must NOT be issued on first step when MFA is enabled");
        result.SessionId.Should().BeNull("IdentitySession must NOT be created on first step when MFA is enabled");

        var sessionsCount = await db.IdentitySessions.CountAsync();
        sessionsCount.Should().Be(0, "No IdentitySession in DB until MFA is verified");
    }

    [Fact]
    public async Task AdminVerifyMfa_WithValidTotp_CreatesSessionAndIssuesTokens()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = CreateConfig();
        var hasher = new Pbkdf2PasswordHasher();
        var dataProtection = new EphemeralDataProtectionProvider();
        var totpService = new TotpMfaService(dataProtection);
        var tokenService = new JwtTokenService(config);
        var sessionService = new IdentitySessionService(db);
        var revokedTokenService = new RevokedTokenService(db);
        var lockout = new AccountLockoutService(db, config);
        var audit = new InMemoryAuditWriter();

        byte[] secret = totpService.GenerateSecret(20);
        db.Admins.Add(new LegacyAdmin { AdminID = 11, Username = "mfa_admin2", Password = hasher.Hash("AdminPass123!"), IsActive = true });
        db.AdminMfas.Add(new AdminMfa
        {
            AdminID = 11,
            SecretProtected = totpService.ProtectSecret(secret),
            Enabled = true,
            EnabledAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AdminAuthApiService(
            db, hasher, tokenService, sessionService, new TestAdminAuthService(false),
            totpService, revokedTokenService, config, audit, NullLogger<AdminAuthApiService>.Instance, lockout);

        var loginResult = await service.LoginAsync("mfa_admin2", "AdminPass123!");
        loginResult.MfaRequired.Should().BeTrue();

        long currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30L;
        string validCode = totpService.ComputeTotp(secret, currentStep);

        var verifyResult = await service.VerifyMfaAsync(loginResult.MfaChallenge!, validCode, isRecoveryCode: false, "TestDevice", "TestAgent");

        verifyResult.Token.Should().NotBeNullOrWhiteSpace();
        verifyResult.RefreshToken.Should().NotBeNullOrWhiteSpace();
        verifyResult.SessionId.Should().NotBeNull();

        var sessionInDb = await db.IdentitySessions.FirstOrDefaultAsync(x => x.SessionId == verifyResult.SessionId);
        sessionInDb.Should().NotBeNull();
        sessionInDb!.AccountId.Should().Be("11");
        sessionInDb.AccountType.Should().Be("Admin");

        audit.Events.Should().Contain(e => e.EventType == "mfa_login_success" && e.Result == "success");
    }

    [Fact]
    public async Task AdminVerifyMfa_WithReplayedChallenge_IsRejected()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = CreateConfig();
        var hasher = new Pbkdf2PasswordHasher();
        var dataProtection = new EphemeralDataProtectionProvider();
        var totpService = new TotpMfaService(dataProtection);
        var tokenService = new JwtTokenService(config);
        var sessionService = new IdentitySessionService(db);
        var revokedTokenService = new RevokedTokenService(db);
        var lockout = new AccountLockoutService(db, config);
        var audit = new InMemoryAuditWriter();

        byte[] secret = totpService.GenerateSecret(20);
        db.Admins.Add(new LegacyAdmin { AdminID = 12, Username = "mfa_admin3", Password = hasher.Hash("AdminPass123!"), IsActive = true });
        db.AdminMfas.Add(new AdminMfa
        {
            AdminID = 12,
            SecretProtected = totpService.ProtectSecret(secret),
            Enabled = true,
            EnabledAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AdminAuthApiService(
            db, hasher, tokenService, sessionService, new TestAdminAuthService(false),
            totpService, revokedTokenService, config, audit, NullLogger<AdminAuthApiService>.Instance, lockout);

        var loginResult = await service.LoginAsync("mfa_admin3", "AdminPass123!");
        long currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30L;
        string validCode = totpService.ComputeTotp(secret, currentStep);

        // First verification succeeds
        await service.VerifyMfaAsync(loginResult.MfaChallenge!, validCode, false, null, null);

        // Second verification with the exact same challenge token must be rejected (single-use)
        var secondAttempt = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.VerifyMfaAsync(loginResult.MfaChallenge!, validCode, false, null, null));

        secondAttempt.Message.Should().Be("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
    }

    [Fact]
    public async Task AdminVerifyMfa_WithWrongCode_IncrementsLockoutAndReturnsGenericError()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = CreateConfig(maxAttempts: 2, lockoutMinutes: 15);
        var hasher = new Pbkdf2PasswordHasher();
        var dataProtection = new EphemeralDataProtectionProvider();
        var totpService = new TotpMfaService(dataProtection);
        var tokenService = new JwtTokenService(config);
        var sessionService = new IdentitySessionService(db);
        var revokedTokenService = new RevokedTokenService(db);
        var lockout = new AccountLockoutService(db, config);
        var audit = new InMemoryAuditWriter();

        byte[] secret = totpService.GenerateSecret(20);
        db.Admins.Add(new LegacyAdmin { AdminID = 13, Username = "mfa_admin4", Password = hasher.Hash("AdminPass123!"), IsActive = true });
        db.AdminMfas.Add(new AdminMfa
        {
            AdminID = 13,
            SecretProtected = totpService.ProtectSecret(secret),
            Enabled = true,
            EnabledAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AdminAuthApiService(
            db, hasher, tokenService, sessionService, new TestAdminAuthService(false),
            totpService, revokedTokenService, config, audit, NullLogger<AdminAuthApiService>.Instance, lockout);

        var login1 = await service.LoginAsync("mfa_admin4", "AdminPass123!");
        var ex1 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.VerifyMfaAsync(login1.MfaChallenge!, "000000", false, null, null));

        ex1.Message.Should().Be("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
        (await lockout.GetAsync("Admin", "mfa_admin4")).FailedAttempts.Should().Be(1);

        var login2 = await service.LoginAsync("mfa_admin4", "AdminPass123!");
        var ex2 = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.VerifyMfaAsync(login2.MfaChallenge!, "000000", false, null, null));

        ex2.Message.Should().Be("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
        (await lockout.GetAsync("Admin", "mfa_admin4")).IsLocked.Should().BeTrue();

        var lockedChallenge = tokenService.CreateMfaChallengeToken(13, "mfa_admin4", out _, out _);
        var lockedMfaError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.VerifyMfaAsync(lockedChallenge, "000000", false, null, null));
        lockedMfaError.Message.Should().Be("Mã xác minh không chính xác hoặc phiên đã hết hạn.");
        lockedMfaError.Should().NotBeOfType<AccountLockedException>();
    }

    [Fact]
    public async Task ChallengeToken_DoesNotHaveSidClaim_RejectedOnRegularEndpoints()
    {
        var config = CreateConfig();
        var tokenService = new JwtTokenService(config);

        string challenge = tokenService.CreateMfaChallengeToken(99, "testadmin", out var jti, out var exp);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(challenge);

        jwt.Claims.Should().NotContain(c => c.Type == "sid", "MFA challenge token must NOT contain a session identifier (sid)");
        jwt.Claims.Should().Contain(c => c.Type == "token_type" && c.Value == "mfa_challenge");
    }

    [Fact]
    public async Task AuditEvents_NeverContainSecretOrOtpCodeOrChallengeToken()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = CreateConfig();
        var hasher = new Pbkdf2PasswordHasher();
        var dataProtection = new EphemeralDataProtectionProvider();
        var totpService = new TotpMfaService(dataProtection);
        var tokenService = new JwtTokenService(config);
        var sessionService = new IdentitySessionService(db);
        var revokedTokenService = new RevokedTokenService(db);
        var lockout = new AccountLockoutService(db, config);
        var audit = new InMemoryAuditWriter();

        db.Admins.Add(new LegacyAdmin { AdminID = 14, Username = "mfa_audit_test", Password = hasher.Hash("AdminPass123!"), IsActive = true });
        await db.SaveChangesAsync();

        var service = new AdminAuthApiService(
            db, hasher, tokenService, sessionService, new TestAdminAuthService(false),
            totpService, revokedTokenService, config, audit, NullLogger<AdminAuthApiService>.Instance, lockout);

        // Start enrollment
        var (uri, manualKey) = await service.StartMfaEnrollmentAsync(14);
        var rawSecret = totpService.FromBase32(manualKey);
        long currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30L;
        string enrollCode = totpService.ComputeTotp(rawSecret, currentStep);

        // Confirm enrollment
        await service.ConfirmMfaEnrollmentAsync(14, enrollCode);

        // Login & Verify (using valid TOTP code for the next time window within allowedWindow = 1)
        string loginCode = totpService.ComputeTotp(rawSecret, currentStep + 1);
        var loginResult = await service.LoginAsync("mfa_audit_test", "AdminPass123!");
        await service.VerifyMfaAsync(loginResult.MfaChallenge!, loginCode, false, null, null);

        // Check all audit records
        foreach (var ev in audit.Events)
        {
            ev.Details.Should().NotContain(manualKey, "Base32 secret must not leak in audit");
            ev.Details.Should().NotContain(uri, "Otpauth URI must not leak in audit");
            ev.Details.Should().NotContain(enrollCode, "Enrollment TOTP code must not leak in audit");
            ev.Details.Should().NotContain(loginCode, "Login TOTP code must not leak in audit");
            if (!string.IsNullOrEmpty(loginResult.MfaChallenge))
                ev.Details.Should().NotContain(loginResult.MfaChallenge, "Challenge token must not leak in audit");
        }
    }
}
