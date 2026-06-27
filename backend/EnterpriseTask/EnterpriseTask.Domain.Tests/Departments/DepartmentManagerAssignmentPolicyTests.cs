using EnterpriseTask.Domain.Departments;
using Xunit;

namespace EnterpriseTask.Domain.Tests.Departments;

public sealed class DepartmentManagerAssignmentPolicyTests
{
    private static readonly Guid ManagerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void CanAssignManager_AllowsClearingManager()
    {
        var decision = DepartmentManagerAssignmentPolicy.CanAssignManager(new DepartmentManagerAssignmentContext(
            ManagerId: null,
            ManagerIsActive: false));

        Assert.Equal(DepartmentManagerAssignmentDecision.Allowed, decision);
    }

    [Fact]
    public void CanAssignManager_DeniesUnavailableManager()
    {
        var decision = DepartmentManagerAssignmentPolicy.CanAssignManager(new DepartmentManagerAssignmentContext(
            ManagerId,
            ManagerIsActive: false));

        Assert.Equal(DepartmentManagerAssignmentDecision.ManagerUnavailable, decision);
    }

    [Fact]
    public void CanAssignManager_AllowsActiveManager()
    {
        var decision = DepartmentManagerAssignmentPolicy.CanAssignManager(new DepartmentManagerAssignmentContext(
            ManagerId,
            ManagerIsActive: true));

        Assert.Equal(DepartmentManagerAssignmentDecision.Allowed, decision);
    }
}
