using EnterpriseTask.Domain.Tasks;
using Xunit;

namespace EnterpriseTask.Domain.Tests.Tasks;

public sealed class TaskScopePolicyTests
{
    private static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void CanAccess_AllowsElevatedActor_ToReadConfidentialTaskOutsideDepartment()
    {
        var context = CreateContext(
            canSeeAllData: true,
            actorDepartmentId: 10,
            taskDepartmentId: 20,
            isConfidential: true);

        Assert.True(TaskScopePolicy.CanAccess(context));
    }

    [Theory]
    [InlineData("creator")]
    [InlineData("reporter")]
    [InlineData("assignee")]
    public void CanAccess_AllowsRelatedActor_ToReadConfidentialTask(string relationship)
    {
        var context = relationship switch
        {
            "creator" => CreateContext(createdBy: ActorId, isConfidential: true),
            "reporter" => CreateContext(reporterId: ActorId, isConfidential: true),
            "assignee" => CreateContext(assigneeId: ActorId, isConfidential: true),
            _ => throw new ArgumentOutOfRangeException(nameof(relationship), relationship, null)
        };

        Assert.True(TaskScopePolicy.CanAccess(context));
    }

    [Fact]
    public void CanAccess_AllowsManager_ToReadNonConfidentialTaskInOwnDepartment()
    {
        var context = CreateContext(
            canSeeDepartmentData: true,
            actorDepartmentId: 10,
            taskDepartmentId: 10);

        Assert.True(TaskScopePolicy.CanAccess(context));
    }

    [Fact]
    public void CanAccess_AllowsScopedManager_ToReadNonConfidentialTaskInGrantedDepartment()
    {
        var context = CreateContext(
            canSeeDepartmentData: true,
            actorDepartmentId: 10,
            scopedDepartmentIds: [20],
            taskDepartmentId: 20);

        Assert.True(TaskScopePolicy.CanAccess(context));
    }

    [Fact]
    public void CanAccess_DeniesManager_ToReadConfidentialTaskByDepartmentOnly()
    {
        var context = CreateContext(
            canSeeDepartmentData: true,
            actorDepartmentId: 10,
            taskDepartmentId: 10,
            isConfidential: true);

        Assert.False(TaskScopePolicy.CanAccess(context));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanAccess_DeniesUnrelatedEmployee_ToReadOutOfScopeTask(bool isConfidential)
    {
        var context = CreateContext(
            actorDepartmentId: 10,
            taskDepartmentId: 20,
            isConfidential: isConfidential);

        Assert.False(TaskScopePolicy.CanAccess(context));
    }

    private static TaskScopeContext CreateContext(
        bool canSeeAllData = false,
        bool canSeeDepartmentData = false,
        long? actorDepartmentId = 10,
        IReadOnlyCollection<long>? scopedDepartmentIds = null,
        Guid? createdBy = null,
        Guid? reporterId = null,
        Guid? assigneeId = null,
        long? taskDepartmentId = 10,
        bool isConfidential = false)
    {
        return new TaskScopeContext(
            ActorId,
            canSeeAllData,
            canSeeDepartmentData,
            actorDepartmentId,
            scopedDepartmentIds ?? [],
            createdBy ?? OtherUserId,
            reporterId ?? OtherUserId,
            assigneeId ?? OtherUserId,
            taskDepartmentId,
            isConfidential);
    }
}
