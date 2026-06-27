using EnterpriseTask.Domain.Departments;
using Xunit;

namespace EnterpriseTask.Domain.Tests.Departments;

public sealed class DepartmentHierarchyPolicyTests
{
    [Fact]
    public void CanAssignParent_DeniesSelfParent()
    {
        var decision = DepartmentHierarchyPolicy.CanAssignParent(new DepartmentHierarchyContext(
            DepartmentId: 10,
            ParentDepartmentId: 10,
            DescendantDepartmentIds: new HashSet<long>()));

        Assert.Equal(DepartmentHierarchyDecision.SelfParentDenied, decision);
    }

    [Fact]
    public void CanAssignParent_DeniesDescendantAsParent()
    {
        var decision = DepartmentHierarchyPolicy.CanAssignParent(new DepartmentHierarchyContext(
            DepartmentId: 10,
            ParentDepartmentId: 30,
            DescendantDepartmentIds: new HashSet<long> { 20, 30 }));

        Assert.Equal(DepartmentHierarchyDecision.CycleDenied, decision);
    }

    [Fact]
    public void CanAssignParent_AllowsNullParent()
    {
        var decision = DepartmentHierarchyPolicy.CanAssignParent(new DepartmentHierarchyContext(
            DepartmentId: 10,
            ParentDepartmentId: null,
            DescendantDepartmentIds: new HashSet<long> { 20, 30 }));

        Assert.Equal(DepartmentHierarchyDecision.Allowed, decision);
    }

    [Fact]
    public void CanAssignParent_AllowsNonDescendantParent()
    {
        var decision = DepartmentHierarchyPolicy.CanAssignParent(new DepartmentHierarchyContext(
            DepartmentId: 10,
            ParentDepartmentId: 40,
            DescendantDepartmentIds: new HashSet<long> { 20, 30 }));

        Assert.Equal(DepartmentHierarchyDecision.Allowed, decision);
    }

    [Fact]
    public void CanAssignParent_AllowsCreateWithParent()
    {
        var decision = DepartmentHierarchyPolicy.CanAssignParent(new DepartmentHierarchyContext(
            DepartmentId: null,
            ParentDepartmentId: 10,
            DescendantDepartmentIds: new HashSet<long>()));

        Assert.Equal(DepartmentHierarchyDecision.Allowed, decision);
    }

    [Fact]
    public void CanDeactivate_DeniesDepartmentWithActiveTasks()
    {
        var decision = DepartmentHierarchyPolicy.CanDeactivate(new DepartmentDeactivationContext(
            ActiveTaskCount: 1,
            ActiveChildDepartmentCount: 0));

        Assert.Equal(DepartmentDeactivationDecision.ActiveTasksDenied, decision);
    }

    [Fact]
    public void CanDeactivate_PrioritizesActiveTasksOverActiveChildren()
    {
        var decision = DepartmentHierarchyPolicy.CanDeactivate(new DepartmentDeactivationContext(
            ActiveTaskCount: 1,
            ActiveChildDepartmentCount: 1));

        Assert.Equal(DepartmentDeactivationDecision.ActiveTasksDenied, decision);
    }

    [Fact]
    public void CanDeactivate_DeniesDepartmentWithActiveChildren()
    {
        var decision = DepartmentHierarchyPolicy.CanDeactivate(new DepartmentDeactivationContext(
            ActiveTaskCount: 0,
            ActiveChildDepartmentCount: 1));

        Assert.Equal(DepartmentDeactivationDecision.ActiveChildrenDenied, decision);
    }

    [Fact]
    public void CanDeactivate_AllowsSafeDepartment()
    {
        var decision = DepartmentHierarchyPolicy.CanDeactivate(new DepartmentDeactivationContext(
            ActiveTaskCount: 0,
            ActiveChildDepartmentCount: 0));

        Assert.Equal(DepartmentDeactivationDecision.Allowed, decision);
    }
}
