using EnterpriseTask.Application.Common;
using Xunit;

namespace EnterpriseTask.Domain.Tests.Common;

public sealed class UserScopeTests
{
    [Fact]
    public void FromRoleCodes_RecognizesCanonicalElevatedRoles()
    {
        var userId = Guid.NewGuid();

        var admin = UserScope.FromRoleCodes(userId, 10, [RoleCodes.Admin]);
        var director = UserScope.FromRoleCodes(userId, 10, [RoleCodes.Director]);
        var manager = UserScope.FromRoleCodes(userId, 10, [RoleCodes.Manager]);

        Assert.True(admin.IsAdmin);
        Assert.True(admin.CanSeeAllData);
        Assert.True(director.IsDirector);
        Assert.True(director.CanSeeAllData);
        Assert.True(manager.IsManager);
        Assert.True(manager.CanSeeDepartmentData);
        Assert.False(manager.CanSeeAllData);
    }

    [Fact]
    public void FromRoleCodes_DoesNotTreatRoleNameFragmentsAsRoleCodes()
    {
        var scope = UserScope.FromRoleCodes(
            Guid.NewGuid(),
            10,
            ["not-admin", "assistant_director", "managerial"]);

        Assert.False(scope.IsAdmin);
        Assert.False(scope.IsDirector);
        Assert.False(scope.IsManager);
        Assert.False(scope.CanSeeAllData);
        Assert.False(scope.CanSeeDepartmentData);
    }
}
