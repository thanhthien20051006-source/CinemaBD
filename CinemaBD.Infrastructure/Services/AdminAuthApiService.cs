using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminAuthApiService : IAdminAuthApiService
{
    private readonly AppDbContext _db;
    private readonly Pbkdf2PasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IIdentitySessionService _identitySessionService;
    private readonly IAdminAuthorizationService _adminAuthorizationService;

    public AdminAuthApiService(AppDbContext db, Pbkdf2PasswordHasher passwordHasher, ITokenService tokenService, IIdentitySessionService identitySessionService, IAdminAuthorizationService adminAuthorizationService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _identitySessionService = identitySessionService;
        _adminAuthorizationService = adminAuthorizationService;
    }

    public async Task<(int AdminId, string Username, string FullName, string? Role, string Token, string RefreshToken, Guid SessionId, bool MustChangePassword, bool PasswordExpired)> LoginAsync(string username, string password, string? deviceName = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(x => x.Username == username && x.IsActive, cancellationToken);
        if (admin == null)
        {
            _passwordHasher.DummyVerify(password);
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");
        }

        var verification = _passwordHasher.VerifyDetailed(password, admin.Password);
        if (verification == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Tài khoản hoặc mật khẩu không chính xác.");

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
        var account = new UserAccount
        {
            Id = $"admin:{admin.AdminID}",
            Username = admin.Username,
            FullName = string.IsNullOrWhiteSpace(admin.FullName) ? admin.Username : admin.FullName,
            PasswordHash = admin.Password,
            MustChangePassword = mustChangePassword,
            PasswordExpired = passwordExpired
        };

        var access = await _adminAuthorizationService.GetAccessForAdminAsync(admin.AdminID, cancellationToken);
        account.RoleId = access.RoleId;
        account.IsMaster = access.IsMaster;
        account.Permissions = access.Permissions;

        var accountId = admin.AdminID.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var sessionId = await _identitySessionService.CreateSessionAsync("Admin", accountId, deviceName, userAgent, cancellationToken);
        try
        {
            var token = _tokenService.CreateToken(account, sessionId, mustChangePassword);
            var refreshToken = await _identitySessionService.IssueRefreshTokenAsync(sessionId, cancellationToken);
            return (admin.AdminID, admin.Username, account.FullName, admin.Role, token, refreshToken, sessionId, mustChangePassword, passwordExpired);
        }
        catch
        {
            await _identitySessionService.RevokeSessionAsync(sessionId, "Admin", accountId, "token_issuance_failed", cancellationToken);
            throw;
        }
    }
}
