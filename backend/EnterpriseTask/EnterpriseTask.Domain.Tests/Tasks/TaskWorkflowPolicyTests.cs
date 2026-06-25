using EnterpriseTask.Domain.Tasks;
using Xunit;

namespace EnterpriseTask.Domain.Tests.Tasks;

public sealed class TaskWorkflowPolicyTests
{
    [Theory]
    [InlineData(TaskStatusIds.New, 1)]
    [InlineData(TaskStatusIds.Assigned, 2)]
    [InlineData(TaskStatusIds.InProgress, 3)]
    [InlineData(TaskStatusIds.PendingReview, 4)]
    [InlineData(TaskStatusIds.Completed, 5)]
    [InlineData(TaskStatusIds.Closed, 6)]
    [InlineData(TaskStatusIds.OnHold, 7)]
    [InlineData(TaskStatusIds.Cancelled, 8)]
    [InlineData(TaskStatusIds.Overdue, 9)]
    public void TaskStatusIds_MatchPostgresSeedOrder(long actual, long expected)
    {
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(TaskStatusIds.New, TaskStatusIds.Assigned)]
    [InlineData(TaskStatusIds.New, TaskStatusIds.Cancelled)]
    [InlineData(TaskStatusIds.Assigned, TaskStatusIds.InProgress)]
    [InlineData(TaskStatusIds.Assigned, TaskStatusIds.OnHold)]
    [InlineData(TaskStatusIds.Assigned, TaskStatusIds.Cancelled)]
    [InlineData(TaskStatusIds.InProgress, TaskStatusIds.PendingReview)]
    [InlineData(TaskStatusIds.InProgress, TaskStatusIds.Completed)]
    [InlineData(TaskStatusIds.InProgress, TaskStatusIds.OnHold)]
    [InlineData(TaskStatusIds.InProgress, TaskStatusIds.Cancelled)]
    [InlineData(TaskStatusIds.PendingReview, TaskStatusIds.InProgress)]
    [InlineData(TaskStatusIds.PendingReview, TaskStatusIds.Completed)]
    [InlineData(TaskStatusIds.PendingReview, TaskStatusIds.Cancelled)]
    [InlineData(TaskStatusIds.Completed, TaskStatusIds.Closed)]
    [InlineData(TaskStatusIds.Completed, TaskStatusIds.Cancelled)]
    [InlineData(TaskStatusIds.OnHold, TaskStatusIds.Assigned)]
    [InlineData(TaskStatusIds.OnHold, TaskStatusIds.InProgress)]
    [InlineData(TaskStatusIds.OnHold, TaskStatusIds.Cancelled)]
    [InlineData(TaskStatusIds.Overdue, TaskStatusIds.InProgress)]
    [InlineData(TaskStatusIds.Overdue, TaskStatusIds.Completed)]
    [InlineData(TaskStatusIds.Overdue, TaskStatusIds.Cancelled)]
    public void CanTransition_ReturnsTrue_ForAllowedTransitions(long currentStatusId, long nextStatusId)
    {
        var canTransition = TaskWorkflowPolicy.CanTransition(currentStatusId, nextStatusId);

        Assert.True(canTransition);
    }

    [Theory]
    [InlineData(null, TaskStatusIds.Assigned)]
    [InlineData(TaskStatusIds.New, TaskStatusIds.New)]
    [InlineData(TaskStatusIds.New, TaskStatusIds.Completed)]
    [InlineData(TaskStatusIds.Assigned, TaskStatusIds.PendingReview)]
    [InlineData(TaskStatusIds.Closed, TaskStatusIds.InProgress)]
    [InlineData(TaskStatusIds.Cancelled, TaskStatusIds.InProgress)]
    [InlineData(TaskStatusIds.InProgress, TaskStatusIds.Overdue)]
    public void CanTransition_ReturnsFalse_ForDisallowedTransitions(long? currentStatusId, long nextStatusId)
    {
        var canTransition = TaskWorkflowPolicy.CanTransition(currentStatusId, nextStatusId);

        Assert.False(canTransition);
    }

    [Fact]
    public void CanTransition_AllowsClosedReopen_WhenExplicitlyEnabled()
    {
        var canTransition = TaskWorkflowPolicy.CanTransition(
            TaskStatusIds.Closed,
            TaskStatusIds.InProgress,
            allowClosedReopen: true);

        Assert.True(canTransition);
    }
}
