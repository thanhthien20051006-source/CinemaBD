using CinemaBD.Web.Core;
using FluentAssertions;

namespace CinemaBD.Tests.Web;

public sealed class AdminNavigationAccessTests
{
    [Fact]
    public void Can_WhenMasterFlagIsFalseAndPermissionsAreEmpty_Denies()
    {
        var access = new AdminNavigationAccess();

        access.Can("phim").Should().BeFalse();
        access.Can().Should().BeFalse();
    }

    [Fact]
    public void Can_WhenMasterFlagIsTrue_AllowsWithoutRoleNameHeuristics()
    {
        var access = new AdminNavigationAccess { IsMaster = true };

        access.Can("phim").Should().BeTrue();
    }

    [Fact]
    public void Can_WhenPermissionMatches_GrantsOnlyMatchingFunction()
    {
        var access = new AdminNavigationAccess { Permissions = new HashSet<string>(["phim"], StringComparer.OrdinalIgnoreCase) };

        access.Can("phim").Should().BeTrue();
        access.Can("nhanvien").Should().BeFalse();
    }
}
