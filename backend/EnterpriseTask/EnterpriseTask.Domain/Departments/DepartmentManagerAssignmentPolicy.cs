namespace EnterpriseTask.Domain.Departments;

public static class DepartmentManagerAssignmentPolicy
{
    public static DepartmentManagerAssignmentDecision CanAssignManager(DepartmentManagerAssignmentContext context)
    {
        if (context.ManagerId is null)
        {
            return DepartmentManagerAssignmentDecision.Allowed;
        }

        return context.ManagerIsActive
            ? DepartmentManagerAssignmentDecision.Allowed
            : DepartmentManagerAssignmentDecision.ManagerUnavailable;
    }
}

public sealed record DepartmentManagerAssignmentContext(
    Guid? ManagerId,
    bool ManagerIsActive);

public enum DepartmentManagerAssignmentDecision
{
    Allowed,
    ManagerUnavailable
}
