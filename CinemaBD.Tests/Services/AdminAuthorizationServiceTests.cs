using CinemaBD.Domain.Entities;
using CinemaBD.Infrastructure.Persistence;
using CinemaBD.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CinemaBD.Tests.Services;

public sealed class AdminAuthorizationServiceTests
{
    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    [Fact]
    public async Task GetAccessForAdminAsync_WhenAdminRoleDoesNotExist_ThrowsAndDenies()
    {
        await using var db = CreateDbContext();
        db.Admins.Add(new LegacyAdmin { AdminID = 101, Username = "orphan", Password = "unused", IsActive = true, MaCV = 404 });
        await db.SaveChangesAsync();

        var service = new AdminAuthorizationService(db);
        var act = () => service.GetAccessForAdminAsync(101);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAccessForAdminAsync_WhenRoleIsMaster_GrantsMasterAccessFromFlag()
    {
        await using var db = CreateDbContext();
        db.Roles.Add(new LegacyRole { MaCV = 5, TenChucVu = "custom label", IsMaster = true, IsActive = true });
        db.Admins.Add(new LegacyAdmin { AdminID = 102, Username = "master", Password = "unused", IsActive = true, MaCV = 5, Role = "Not Admin" });
        await db.SaveChangesAsync();

        var access = await new AdminAuthorizationService(db).GetAccessForAdminAsync(102);

        access.IsMaster.Should().BeTrue();
        access.RoleId.Should().Be(5);
        access.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccessForAdminAsync_WhenRoleIsOrdinary_ReturnsOnlyAssignedFunctions()
    {
        await using var db = CreateDbContext();
        db.Roles.Add(new LegacyRole { MaCV = 6, TenChucVu = "Admin", IsMaster = false, IsActive = true });
        db.Admins.Add(new LegacyAdmin { AdminID = 103, Username = "ordinary", Password = "unused", IsActive = true, MaCV = 6, Role = "Admin" });
        db.Permissions.AddRange(
            new LegacyPermission { MaCN = 21, TenChucNang = "Quản lý phim" },
            new LegacyPermission { MaCN = 22, TenChucNang = "Quản lý nhân viên" });
        db.RolePermissions.Add(new LegacyRolePermission { MaPQ = 31, MaCV = 6, MaCN = 21 });
        await db.SaveChangesAsync();

        var access = await new AdminAuthorizationService(db).GetAccessForAdminAsync(103);

        access.IsMaster.Should().BeFalse();
        access.Permissions.Should().ContainSingle().Which.Should().Be("Quản lý phim");
        AdminPermissionMatcher.Matches(access.Permissions.Single(), ["phim"]).Should().BeTrue();
        AdminPermissionMatcher.Matches(access.Permissions.Single(), ["nhanvien"]).Should().BeFalse();
    }

    [Fact]
    public async Task GetAccessForAdminAsync_WhenOrdinaryRoleHasNoAssignments_ReturnsEmptyPermissions()
    {
        await using var db = CreateDbContext();
        db.Roles.Add(new LegacyRole { MaCV = 7, TenChucVu = "Empty", IsMaster = false, IsActive = true });
        db.Admins.Add(new LegacyAdmin { AdminID = 104, Username = "empty", Password = "unused", IsActive = true, MaCV = 7 });
        await db.SaveChangesAsync();

        var access = await new AdminAuthorizationService(db).GetAccessForAdminAsync(104);

        access.IsMaster.Should().BeFalse();
        access.Permissions.Should().BeEmpty();
    }
}
