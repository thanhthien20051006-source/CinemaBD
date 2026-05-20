using CinemaBD.Application.Interfaces;
using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Infrastructure.Services;

public class AdminAuthApiService : IAdminAuthApiService
{
    private readonly AppDbContext _db;
    private readonly Md5PasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AdminAuthApiService(AppDbContext db, Md5PasswordHasher passwordHasher, ITokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<(int AdminId, string Username, string FullName, string? Role, string Token)> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(x => x.Username == username && x.IsActive, cancellationToken);
        if (admin == null)
            throw new InvalidOperationException("Tài khoản admin không tồn tại.");

        if (!_passwordHasher.Verify(password, admin.Password))
            throw new InvalidOperationException("Sai mật khẩu admin.");

        if (string.Equals(admin.Password, password, StringComparison.Ordinal))
        {
            admin.Password = _passwordHasher.Hash(password);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var token = _tokenService.CreateToken(new UserAccount
        {
            Id = $"admin:{admin.AdminID}",
            Username = admin.Username,
            FullName = string.IsNullOrWhiteSpace(admin.FullName) ? admin.Username : admin.FullName,
            PasswordHash = admin.Password
        });

        return (admin.AdminID, admin.Username, string.IsNullOrWhiteSpace(admin.FullName) ? admin.Username : admin.FullName!, admin.Role, token);
    }
}



