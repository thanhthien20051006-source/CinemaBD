using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Security;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace CinemaBD.Tests.Services;

public sealed class AdminAuthClaimsTests
{
    [Fact]
    public async Task AdminLogin_MapsRoleAndPermissionClaimsFromDatabase()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var hasher = new Pbkdf2PasswordHasher();
        db.Roles.Add(new LegacyRole { MaCV = 6, TenChucVu = "Movie Manager", IsMaster = false, IsActive = true });
        db.Admins.Add(new LegacyAdmin { AdminID = 32, Username = "editor", Password = hasher.Hash("AdminPassword123!"), FullName = "Editor", IsActive = true, MaCV = 6 });
        db.Permissions.Add(new LegacyPermission { MaCN = 21, TenChucNang = "Quản lý phim" });
        db.RolePermissions.Add(new LegacyRolePermission { MaPQ = 31, MaCV = 6, MaCN = 21 });
        await db.SaveChangesAsync();

        var tokenService = new JwtTokenService(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "CinemaBD.Tests", ["Jwt:Audience"] = "CinemaBD.Tests.Client", ["Jwt:Key"] = "0123456789abcdef0123456789abcdef"
        }).Build());
        var service = new AdminAuthApiService(db, hasher, tokenService, new IdentitySessionService(db), new AdminAuthorizationService(db));

        var result = await service.LoginAsync("editor", "AdminPassword123!");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        jwt.Claims.Single(x => x.Type == "ma_cv").Value.Should().Be("6");
        jwt.Claims.Single(x => x.Type == "is_master").Value.Should().Be("false");
        jwt.Claims.Single(x => x.Type == "permission").Value.Should().Be("Quản lý phim");
        jwt.Claims.Single(x => x.Type == "account_type").Value.Should().Be("admin");
        (await db.IdentitySessions.SingleAsync()).AccountType.Should().Be("Admin");
    }

    [Fact]
    public async Task AdminLogin_WithMasterFlag_EmitsMasterClaim()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var hasher = new Pbkdf2PasswordHasher();
        db.Roles.Add(new LegacyRole { MaCV = 5, TenChucVu = "Root", IsMaster = true, IsActive = true });
        db.Admins.Add(new LegacyAdmin { AdminID = 33, Username = "master", Password = hasher.Hash("AdminPassword123!"), IsActive = true, MaCV = 5 });
        await db.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "0123456789abcdef0123456789abcdef"
        }).Build();
        var service = new AdminAuthApiService(db, hasher, new JwtTokenService(config), new IdentitySessionService(db), new AdminAuthorizationService(db));

        var result = await service.LoginAsync("master", "AdminPassword123!");

        new JwtSecurityTokenHandler().ReadJwtToken(result.Token).Claims.Single(x => x.Type == "is_master").Value.Should().Be("true");
    }
}
