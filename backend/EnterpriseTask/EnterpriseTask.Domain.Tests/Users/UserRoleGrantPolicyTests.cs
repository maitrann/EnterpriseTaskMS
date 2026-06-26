using EnterpriseTask.Domain.Users;
using Xunit;

namespace EnterpriseTask.Domain.Tests.Users;

public sealed class UserRoleGrantPolicyTests
{
    [Fact]
    public void CanRemoveRole_AllowsRemovingNonAdminRole_FromLastAdmin()
    {
        var decision = UserRoleGrantPolicy.CanRemoveRole(new UserRoleGrantContext(
            RemovedRoleIsAdmin: false,
            TargetIsActiveAdmin: true,
            ActiveAdminCount: 1));

        Assert.Equal(UserRoleGrantDecision.Allowed, decision);
    }

    [Fact]
    public void CanRemoveRole_DeniesRemovingAdminRole_FromLastActiveAdmin()
    {
        var decision = UserRoleGrantPolicy.CanRemoveRole(new UserRoleGrantContext(
            RemovedRoleIsAdmin: true,
            TargetIsActiveAdmin: true,
            ActiveAdminCount: 1));

        Assert.Equal(UserRoleGrantDecision.LastAdminDenied, decision);
    }

    [Fact]
    public void CanRemoveRole_AllowsRemovingAdminRole_WhenAnotherActiveAdminExists()
    {
        var decision = UserRoleGrantPolicy.CanRemoveRole(new UserRoleGrantContext(
            RemovedRoleIsAdmin: true,
            TargetIsActiveAdmin: true,
            ActiveAdminCount: 2));

        Assert.Equal(UserRoleGrantDecision.Allowed, decision);
    }
}
