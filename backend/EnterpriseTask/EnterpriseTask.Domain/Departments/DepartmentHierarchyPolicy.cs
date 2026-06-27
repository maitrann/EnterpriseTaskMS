namespace EnterpriseTask.Domain.Departments;

public static class DepartmentHierarchyPolicy
{
    public static DepartmentHierarchyDecision CanAssignParent(DepartmentHierarchyContext context)
    {
        if (context.DepartmentId is not null && context.DepartmentId == context.ParentDepartmentId)
        {
            return DepartmentHierarchyDecision.SelfParentDenied;
        }

        if (context.ParentDepartmentId is not null && context.DescendantDepartmentIds.Contains(context.ParentDepartmentId.Value))
        {
            return DepartmentHierarchyDecision.CycleDenied;
        }

        return DepartmentHierarchyDecision.Allowed;
    }

    public static DepartmentDeactivationDecision CanDeactivate(DepartmentDeactivationContext context)
    {
        if (context.ActiveTaskCount > 0)
        {
            return DepartmentDeactivationDecision.ActiveTasksDenied;
        }

        if (context.ActiveChildDepartmentCount > 0)
        {
            return DepartmentDeactivationDecision.ActiveChildrenDenied;
        }

        return DepartmentDeactivationDecision.Allowed;
    }
}

public sealed record DepartmentHierarchyContext(
    long? DepartmentId,
    long? ParentDepartmentId,
    IReadOnlySet<long> DescendantDepartmentIds);

public enum DepartmentHierarchyDecision
{
    Allowed,
    SelfParentDenied,
    CycleDenied
}

public sealed record DepartmentDeactivationContext(
    int ActiveTaskCount,
    int ActiveChildDepartmentCount);

public enum DepartmentDeactivationDecision
{
    Allowed,
    ActiveTasksDenied,
    ActiveChildrenDenied
}
