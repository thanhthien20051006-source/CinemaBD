using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CinemaBD.Application.Interfaces;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public sealed class AdminAccountService : IAdminAccountService
{
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%^&*()-_=+";
    private static readonly Regex UsernamePattern = new("^[A-Za-z0-9_]{3,32}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly AppDbContext _db;
    private readonly Pbkdf2PasswordHasher _passwordHasher;
    private readonly ISecurityAuditWriter _audit;

    public AdminAccountService(AppDbContext db, Pbkdf2PasswordHasher passwordHasher, ISecurityAuditWriter audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _audit = audit;
    }

    public static bool IsValidUsername(string? username)
        => username is not null && UsernamePattern.IsMatch(username);

    public static string GenerateTemporaryPassword()
    {
        var chars = new char[16];
        chars[0] = Pick(Uppercase);
        chars[1] = Pick(Lowercase);
        chars[2] = Pick(Digits);
        chars[3] = Pick(Special);
        const string all = Uppercase + Lowercase + Digits + Special;
        for (var i = 4; i < chars.Length; i++) chars[i] = Pick(all);
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    public async Task<AdminAccountCreated> CreateAsync(string username, string fullName, int roleId, CancellationToken cancellationToken = default)
    {
        username = username?.Trim() ?? string.Empty;
        fullName = fullName?.Trim() ?? string.Empty;
        if (!IsValidUsername(username) || fullName.Length is < 1 or > 100 || roleId <= 0)
            throw new InvalidOperationException("Không thể tạo tài khoản quản trị.");

        var usernameKey = username.ToUpperInvariant();
        if (await _db.Admins.AnyAsync(x => x.Username.ToUpper() == usernameKey, cancellationToken))
            throw new InvalidOperationException("Không thể tạo tài khoản quản trị.");

        var role = await _db.Roles.AsNoTracking()
            .Where(x => x.MaCV == roleId && x.IsActive)
            .Select(x => new { x.MaCV, x.IsMaster })
            .SingleOrDefaultAsync(cancellationToken);
        if (role is null || role.IsMaster)
            throw new InvalidOperationException("Không thể tạo tài khoản quản trị.");

        var temporaryPassword = GenerateTemporaryPassword();
        var admin = new LegacyAdmin
        {
            Username = username,
            Password = _passwordHasher.Hash(temporaryPassword),
            FullName = fullName,
            Role = null,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            MaCV = role.MaCV,
            MustChangePassword = true
        };
        _db.Admins.Add(admin);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(new SecurityAuditEvent(
            "admin_account_created",
            ActorType: "Admin",
            ActorId: admin.AdminID.ToString(System.Globalization.CultureInfo.InvariantCulture)), cancellationToken);

        return new AdminAccountCreated(admin.AdminID, temporaryPassword);
    }

    private static char Pick(string choices) => choices[RandomNumberGenerator.GetInt32(choices.Length)];
}
