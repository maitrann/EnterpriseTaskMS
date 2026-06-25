namespace EnterpriseTask.Application.Common;

public sealed record UserScope(Guid UserId, long? DepartmentId, bool IsAdmin, bool IsDirector, bool IsManager)
{
    public bool CanSeeAllData => IsAdmin || IsDirector;

    public bool CanSeeDepartmentData => CanSeeAllData || IsManager;

    public static UserScope FromRoleCodes(Guid userId, long? departmentId, IEnumerable<string> roleCodes)
    {
        var roles = roleCodes.ToArray();
        return new UserScope(
            userId,
            departmentId,
            roles.Any(RoleCodes.IsAdmin),
            roles.Any(RoleCodes.IsDirector),
            roles.Any(RoleCodes.IsManager));
    }
}
