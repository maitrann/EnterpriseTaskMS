namespace EnterpriseTask.Application.Tasks;

public sealed class CreateTaskHandler(ITaskCommands taskCommands, ITaskPolicyQueries taskPolicyQueries)
{
    public async Task<TaskCreateResult> HandleAsync(
        Guid actorUserId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!await taskPolicyQueries.HasPermissionAsync(actorUserId, "task.create", cancellationToken))
        {
            return new TaskCreateResult(TaskCommandResult.Forbidden);
        }

        if (!await taskPolicyQueries.CanUseDepartmentAsync(actorUserId, request.DepartmentId, cancellationToken))
        {
            return new TaskCreateResult(TaskCommandResult.Forbidden);
        }

        var requiresAssignPermission =
            request.AssigneeId is not null && request.AssigneeId.Value != actorUserId
            || NormalizeUserIds(request.CollaboratorIds).Length > 0
            || NormalizeUserIds(request.WatcherIds).Length > 0;

        if (requiresAssignPermission
            && !await taskPolicyQueries.HasPermissionAsync(actorUserId, "task.assign", cancellationToken))
        {
            return new TaskCreateResult(TaskCommandResult.Forbidden);
        }

        return await taskCommands.CreateAsync(actorUserId, request, cancellationToken);
    }

    private static Guid[] NormalizeUserIds(Guid[]? userIds)
    {
        return (userIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
    }
}
