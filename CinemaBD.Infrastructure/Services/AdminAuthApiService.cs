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

    public AdminAuthApiService(AppDbContext db, Pbkdf2PasswordHasher passwordHasher, ITokenService tokenService, IIdentitySessionService identitySessionService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _identitySessionService = identitySessionService;
    }

    public async Task<(int AdminId, string Username, string FullName, string? Role, string Token, bool MustChangePassword, bool PasswordExpired)> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
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

        var sessionId = await _identitySessionService.CreateSessionAsync("Admin", admin.AdminID.ToString(System.Globalization.CultureInfo.InvariantCulture), cancellationToken);
        var token = _tokenService.CreateToken(account, sessionId, mustChangePassword);
        return (admin.AdminID, admin.Username, account.FullName, admin.Role, token, mustChangePassword, passwordExpired);
    }
}
