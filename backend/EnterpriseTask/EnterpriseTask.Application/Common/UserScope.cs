namespace EnterpriseTask.Application.Common;

public sealed record UserScope(Guid UserId, long? DepartmentId, bool IsAdmin, bool IsDirector, bool IsManager)
{
    public bool CanSeeAllData => IsAdmin || IsDirector;

    public bool CanSeeDepartmentData => CanSeeAllData || IsManager;
}
