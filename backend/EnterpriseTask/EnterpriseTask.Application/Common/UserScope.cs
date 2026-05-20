namespace EnterpriseTask.Application.Common;

public sealed record UserScope(long UserId, long? DepartmentId, bool IsAdmin, bool IsManager)
{
    public bool CanSeeDepartmentData => IsAdmin || IsManager;
}
