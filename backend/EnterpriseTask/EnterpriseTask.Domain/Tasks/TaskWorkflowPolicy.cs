namespace EnterpriseTask.Domain.Tasks;

public static class TaskWorkflowPolicy
{
    private static readonly IReadOnlyDictionary<long, long[]> Transitions = new Dictionary<long, long[]>
    {
        [TaskStatusIds.New] = [TaskStatusIds.Assigned, TaskStatusIds.Cancelled],
        [TaskStatusIds.Assigned] = [TaskStatusIds.InProgress, TaskStatusIds.OnHold, TaskStatusIds.Cancelled],
        [TaskStatusIds.InProgress] = [TaskStatusIds.PendingReview, TaskStatusIds.Completed, TaskStatusIds.OnHold, TaskStatusIds.Cancelled],
        [TaskStatusIds.PendingReview] = [TaskStatusIds.InProgress, TaskStatusIds.Completed, TaskStatusIds.Cancelled],
        [TaskStatusIds.Completed] = [TaskStatusIds.Closed, TaskStatusIds.Cancelled],
        [TaskStatusIds.OnHold] = [TaskStatusIds.Assigned, TaskStatusIds.InProgress, TaskStatusIds.Cancelled],
        [TaskStatusIds.Closed] = [],
        [TaskStatusIds.Cancelled] = [],
        [TaskStatusIds.Overdue] = [TaskStatusIds.InProgress, TaskStatusIds.Completed, TaskStatusIds.Cancelled]
    };

    public static bool CanTransition(long? currentStatusId, long nextStatusId, bool allowClosedReopen = false)
    {
        if (currentStatusId is null || currentStatusId.Value == nextStatusId)
        {
            return false;
        }

        if (allowClosedReopen
            && currentStatusId.Value == TaskStatusIds.Closed
            && nextStatusId == TaskStatusIds.InProgress)
        {
            return true;
        }

        return Transitions.TryGetValue(currentStatusId.Value, out var allowedStatusIds)
            && allowedStatusIds.Contains(nextStatusId);
    }
}
