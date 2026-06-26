using EnterpriseTask.Domain.Users;
using Xunit;

namespace EnterpriseTask.Domain.Tests.Users;

public sealed class UserLockPolicyTests
{
    private static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void CanChangeActiveState_DeniesSelfLock()
    {
        var decision = UserLockPolicy.CanChangeActiveState(new UserLockContext(
            ActorId,
            ActorId,
            TargetIsAdmin: true,
            ActiveAdminCount: 2,
            NextIsActive: false));

        Assert.Equal(UserLockDecision.SelfLockDenied, decision);
    }

    [Fact]
    public void CanChangeActiveState_DeniesLockingLastActiveAdmin()
    {
        var decision = UserLockPolicy.CanChangeActiveState(new UserLockContext(
            ActorId,
            OtherUserId,
            TargetIsAdmin: true,
            ActiveAdminCount: 1,
            NextIsActive: false));

        Assert.Equal(UserLockDecision.LastAdminDenied, decision);
    }

    [Fact]
    public void CanChangeActiveState_AllowsLockingNonLastAdmin()
    {
        var decision = UserLockPolicy.CanChangeActiveState(new UserLockContext(
            ActorId,
            OtherUserId,
            TargetIsAdmin: true,
            ActiveAdminCount: 2,
            NextIsActive: false));

        Assert.Equal(UserLockDecision.Allowed, decision);
    }

    [Fact]
    public void CanChangeActiveState_AllowsUnlockingSelf()
    {
        var decision = UserLockPolicy.CanChangeActiveState(new UserLockContext(
            ActorId,
            ActorId,
            TargetIsAdmin: true,
            ActiveAdminCount: 1,
            NextIsActive: true));

        Assert.Equal(UserLockDecision.Allowed, decision);
    }
}
