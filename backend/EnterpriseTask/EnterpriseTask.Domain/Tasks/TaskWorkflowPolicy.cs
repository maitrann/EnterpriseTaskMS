namespace EnterpriseTask.Domain.Tasks;

public static class TaskWorkflowPolicy
{
    private static readonly IReadOnlyDictionary<long, long[]> Transitions = new Dictionary<long, long[]>
    {
        [TaskStatusIds.Created] = [TaskStatusIds.Assigned, TaskStatusIds.Cancelled],
        [TaskStatusIds.Assigned] = [TaskStatusIds.Accepted, TaskStatusIds.Rejected, TaskStatusIds.Cancelled],
        [TaskStatusIds.Accepted] = [TaskStatusIds.InProgress, TaskStatusIds.Cancelled],
        [TaskStatusIds.InProgress] = [TaskStatusIds.Waiting, TaskStatusIds.Completed, TaskStatusIds.Cancelled],
        [TaskStatusIds.Waiting] = [TaskStatusIds.InProgress, TaskStatusIds.Cancelled],
        [TaskStatusIds.Completed] = [TaskStatusIds.Closed, TaskStatusIds.Cancelled],
        [TaskStatusIds.Closed] = [],
        [TaskStatusIds.Cancelled] = [],
        [TaskStatusIds.Rejected] = []
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
