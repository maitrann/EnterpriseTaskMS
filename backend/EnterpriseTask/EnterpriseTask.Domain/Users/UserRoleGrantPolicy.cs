namespace EnterpriseTask.Domain.Users;

public static class UserRoleGrantPolicy
{
    public static UserRoleGrantDecision CanRemoveRole(UserRoleGrantContext context)
    {
        if (!context.RemovedRoleIsAdmin)
        {
            return UserRoleGrantDecision.Allowed;
        }

        if (context.TargetIsActiveAdmin && context.ActiveAdminCount <= 1)
        {
            return UserRoleGrantDecision.LastAdminDenied;
        }

        return UserRoleGrantDecision.Allowed;
    }
}

public sealed record UserRoleGrantContext(
    bool RemovedRoleIsAdmin,
    bool TargetIsActiveAdmin,
    int ActiveAdminCount);

public enum UserRoleGrantDecision
{
    Allowed,
    LastAdminDenied
}
