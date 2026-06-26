namespace EnterpriseTask.Domain.Users;

public static class UserLockPolicy
{
    public static UserLockDecision CanChangeActiveState(UserLockContext context)
    {
        if (context.TargetUserId == context.ActorUserId && !context.NextIsActive)
        {
            return UserLockDecision.SelfLockDenied;
        }

        if (!context.NextIsActive && context.TargetIsAdmin && context.ActiveAdminCount <= 1)
        {
            return UserLockDecision.LastAdminDenied;
        }

        return UserLockDecision.Allowed;
    }
}

public sealed record UserLockContext(
    Guid ActorUserId,
    Guid TargetUserId,
    bool TargetIsAdmin,
    int ActiveAdminCount,
    bool NextIsActive);

public enum UserLockDecision
{
    Allowed,
    SelfLockDenied,
    LastAdminDenied
}
