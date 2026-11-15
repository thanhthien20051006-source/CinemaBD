using CinemaBD.Api.Filters;
using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace CinemaBD.Tests.Security;

public sealed class MasterMfaEnforcementTests
{
    private static IConfiguration CreateConfig(bool requireForMaster = true, bool requireForAllAdmins = false)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "0123456789abcdef0123456789abcdef",
            ["Jwt:Issuer"] = "CinemaBD",
            ["Jwt:Audience"] = "CinemaBD.Client",
            ["Mfa:RequireForMaster"] = requireForMaster ? "true" : "false",
            ["Mfa:RequireForAllAdmins"] = requireForAllAdmins ? "true" : "false",
            ["ADMIN_RECOVERY_KEY"] = "RecoveryKeySecret123!"
        }).Build();

    private sealed class TestAdminAuthService : IAdminAuthorizationService
    {
        private readonly bool _isMaster;
        public TestAdminAuthService(bool isMaster) => _isMaster = isMaster;
        public Task<AdminAuthorizationAccess> GetAccessForAdminAsync(int adminId, CancellationToken cancellationToken = default)
            => Task.FromResult(new AdminAuthorizationAccess(1, _isMaster, Array.Empty<string>()));
    }

    [Fact]
    public async Task MasterAdmin_WithoutMfa_GetsTokenWithMfaEnrollmentOnlyClaim()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = CreateConfig(requireForMaster: true);
        var hasher = new Pbkdf2PasswordHasher();
        var dataProtection = new EphemeralDataProtectionProvider();
        var totpService = new TotpMfaService(dataProtection);
        var tokenService = new JwtTokenService(config);
        var sessionService = new IdentitySessionService(db);
        var revokedTokenService = new RevokedTokenService(db);

        db.Admins.Add(new LegacyAdmin { AdminID = 20, Username = "master_user", Password = hasher.Hash("MasterPass123!"), IsActive = true, MaCV = 1 });
        await db.SaveChangesAsync();

        var service = new AdminAuthApiService(
            db, hasher, tokenService, sessionService, new TestAdminAuthService(isMaster: true),
            totpService, revokedTokenService, config, null, NullLogger<AdminAuthApiService>.Instance, null);

        var result = await service.LoginAsync("master_user", "MasterPass123!");

        result.MfaEnrollmentRestricted.Should().BeTrue();
        result.Token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);
        jwt.Claims.Should().Contain(c => c.Type == "mfa_enrollment_only" && c.Value == "true");
    }

    [Fact]
    public async Task RegularAdmin_WithoutMfa_DoesNotGetMfaEnrollmentRestriction()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = CreateConfig(requireForMaster: true);
        var hasher = new Pbkdf2PasswordHasher();
        var dataProtection = new EphemeralDataProtectionProvider();
        var totpService = new TotpMfaService(dataProtection);
        var tokenService = new JwtTokenService(config);
        var sessionService = new IdentitySessionService(db);
        var revokedTokenService = new RevokedTokenService(db);

        db.Admins.Add(new LegacyAdmin { AdminID = 21, Username = "regular_admin", Password = hasher.Hash("AdminPass123!"), IsActive = true, MaCV = 2 });
        await db.SaveChangesAsync();

        var service = new AdminAuthApiService(
            db, hasher, tokenService, sessionService, new TestAdminAuthService(isMaster: false),
            totpService, revokedTokenService, config, null, NullLogger<AdminAuthApiService>.Instance, null);

        var result = await service.LoginAsync("regular_admin", "AdminPass123!");

        result.MfaEnrollmentRestricted.Should().BeFalse();
        result.Token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);
        jwt.Claims.Should().NotContain(c => c.Type == "mfa_enrollment_only");
    }

    [Theory]
    [InlineData("/api/admin/auth/mfa/enroll/start", "POST", true)]
    [InlineData("/api/admin/auth/mfa/enroll/confirm", "POST", true)]
    [InlineData("/api/admin/auth/logout", "POST", true)]
    [InlineData("/api/admin/auth/sessions", "GET", true)]
    [InlineData("/api/admin/movies", "POST", false)]
    [InlineData("/api/admin/movies", "GET", false)]
    [InlineData("/api/admin/dashboard", "GET", false)]
    [InlineData("/api/admin/invoices", "GET", false)]
    [InlineData("/api/admin/vouchers", "POST", false)]
    public async Task MfaEnrollmentRestrictionFilter_EnforcesEndpointIsolation(string path, string method, bool expectedAllowed)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("mfa_enrollment_only", "true"), new Claim("account_type", "admin")], "Bearer"));

        var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());
        var filterContext = new AuthorizationFilterContext(actionContext, Array.Empty<IFilterMetadata>());
        context.RequestServices = new ServiceCollection().AddSingleton<IConfiguration>(CreateConfig()).BuildServiceProvider();
        var filter = new MfaEnrollmentRestrictionFilter();

        await filter.OnAuthorizationAsync(filterContext);

        if (expectedAllowed)
            filterContext.Result.Should().BeNull();
        else
            filterContext.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task MasterAdmin_CannotDisableMfa_WhenRequireForMasterIsTrue()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = CreateConfig(requireForMaster: true);
        var hasher = new Pbkdf2PasswordHasher();
        var dataProtection = new EphemeralDataProtectionProvider();
        var totpService = new TotpMfaService(dataProtection);
        var tokenService = new JwtTokenService(config);
        var sessionService = new IdentitySessionService(db);
        var revokedTokenService = new RevokedTokenService(db);

        byte[] secret = totpService.GenerateSecret(20);
        db.Admins.Add(new LegacyAdmin { AdminID = 22, Username = "master_disable_test", Password = hasher.Hash("MasterPass123!"), IsActive = true, MaCV = 1 });
        db.AdminMfas.Add(new AdminMfa
        {
            AdminID = 22,
            SecretProtected = totpService.ProtectSecret(secret),
            Enabled = true,
            EnabledAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AdminAuthApiService(
            db, hasher, tokenService, sessionService, new TestAdminAuthService(isMaster: true),
            totpService, revokedTokenService, config, null, NullLogger<AdminAuthApiService>.Instance, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DisableMfaAsync(22, "MasterPass123!", "123456", false));

        ex.Message.Should().Contain("Tài khoản Master không được phép tắt xác thực hai bước");
    }

    [Fact]
    public async Task RegularAdmin_CanDisableMfa_WithValidPasswordAndCode_RevokingSessions()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = CreateConfig(requireForMaster: true);
        var hasher = new Pbkdf2PasswordHasher();
        var dataProtection = new EphemeralDataProtectionProvider();
        var totpService = new TotpMfaService(dataProtection);
        var tokenService = new JwtTokenService(config);
        var sessionService = new IdentitySessionService(db);
        var revokedTokenService = new RevokedTokenService(db);

        byte[] secret = totpService.GenerateSecret(20);
        db.Admins.Add(new LegacyAdmin { AdminID = 23, Username = "reg_disable_test", Password = hasher.Hash("AdminPass123!"), IsActive = true, MaCV = 2 });
        db.AdminMfas.Add(new AdminMfa
        {
            AdminID = 23,
            SecretProtected = totpService.ProtectSecret(secret),
            Enabled = true,
            EnabledAtUtc = DateTime.UtcNow
        });
        // Create an existing session
        var sid = await sessionService.CreateSessionAsync("Admin", "23", "Browser", "Agent");
        await db.SaveChangesAsync();

        var service = new AdminAuthApiService(
            db, hasher, tokenService, sessionService, new TestAdminAuthService(isMaster: false),
            totpService, revokedTokenService, config, null, NullLogger<AdminAuthApiService>.Instance, null);

        long currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30L;
        string validCode = totpService.ComputeTotp(secret, currentStep);

        // Disabling with wrong password fails
        var wrongPass = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DisableMfaAsync(23, "WrongPass!", validCode, false));
        wrongPass.Message.Should().Be("Mật khẩu không chính xác.");

        // Disabling with valid password and code succeeds
        await service.DisableMfaAsync(23, "AdminPass123!", validCode, false);

        var mfaInDb = await db.AdminMfas.FirstOrDefaultAsync(x => x.AdminID == 23);
        mfaInDb.Should().BeNull();

        // Check that session is revoked
        var isSessionActive = await sessionService.IsActiveAsync(sid);
        isSessionActive.Should().BeFalse("All sessions must be revoked upon MFA disable");
    }

    [Fact]
    public async Task EmergencyMasterRecovery_ResetsMfa_AndForcesPasswordChange()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = CreateConfig(requireForMaster: true);
        var hasher = new Pbkdf2PasswordHasher();
        var dataProtection = new EphemeralDataProtectionProvider();
        var totpService = new TotpMfaService(dataProtection);
        var sessionService = new IdentitySessionService(db);

        byte[] secret = totpService.GenerateSecret(20);
        db.Admins.Add(new LegacyAdmin { AdminID = 24, Username = "master_emergency", Password = hasher.Hash("MasterOldPass!"), IsActive = true, MaCV = 1 });
        db.AdminMfas.Add(new AdminMfa
        {
            AdminID = 24,
            SecretProtected = totpService.ProtectSecret(secret),
            Enabled = true,
            EnabledAtUtc = DateTime.UtcNow
        });
        db.AdminRecoveryCodes.Add(new AdminRecoveryCode
        {
            AdminID = 24,
            CodeHash = totpService.HashRecoveryCode("TEST-1234"),
            UsedAtUtc = null
        });
        await db.SaveChangesAsync();

        var emergencyService = new AdminEmergencyRecoveryService(
            db, config, sessionService, new TestAdminAuthService(isMaster: true), null, NullLogger<AdminEmergencyRecoveryService>.Instance);

        // Wrong recovery key fails
        bool failedResult = await emergencyService.EmergencyResetMasterMfaAsync(24, "WrongRecoveryKey!", "Lost phone");
        failedResult.Should().BeFalse();

        // Correct recovery key succeeds
        bool successResult = await emergencyService.EmergencyResetMasterMfaAsync(24, "RecoveryKeySecret123!", "Lost phone");
        successResult.Should().BeTrue();

        var mfaInDb = await db.AdminMfas.FirstOrDefaultAsync(x => x.AdminID == 24);
        mfaInDb.Should().BeNull("MFA record must be removed on emergency reset");

        var codesInDb = await db.AdminRecoveryCodes.Where(x => x.AdminID == 24).ToListAsync();
        codesInDb.Should().BeEmpty("All recovery codes must be purged on emergency reset");

        var admin = await db.Admins.FirstAsync(x => x.AdminID == 24);
        admin.MustChangePassword.Should().BeTrue("Master admin must be forced to change password on emergency reset");
        admin.PasswordExpiresAt.Should().NotBeNull();
    }
}
