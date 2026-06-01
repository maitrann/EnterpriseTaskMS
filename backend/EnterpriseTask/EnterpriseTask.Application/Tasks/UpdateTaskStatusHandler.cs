namespace EnterpriseTask.Application.Tasks;

public sealed class UpdateTaskStatusHandler(ITaskCommands taskCommands, ITaskPolicyQueries taskPolicyQueries)
{
    public async Task<TaskCommandResult> HandleAsync(
        Guid actorUserId,
        Guid taskId,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var access = await taskPolicyQueries.GetAccessAsync(actorUserId, taskId, "task.update", cancellationToken);
        if (!access.Exists)
        {
            return TaskCommandResult.NotFound;
        }

        if (!access.CanAccess || !access.HasPermission)
        {
            return TaskCommandResult.Forbidden;
        }

        return await taskCommands.UpdateStatusAsync(actorUserId, taskId, request, cancellationToken);
    }
}
