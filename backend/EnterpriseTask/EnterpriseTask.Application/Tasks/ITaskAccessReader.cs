namespace EnterpriseTask.Application.Tasks;

public interface ITaskAccessReader
{
    Task<bool> HasPermissionAsync(Guid actorUserId, string permissionCode, CancellationToken cancellationToken);

    Task<bool> CanUseDepartmentAsync(Guid actorUserId, long? departmentId, CancellationToken cancellationToken);

    Task<bool> CanAccessTaskAsync(Guid actorUserId, Guid taskId, CancellationToken cancellationToken);

    Task<TaskAccessResult> GetTaskAccessAsync(
        Guid actorUserId,
        Guid taskId,
        string permissionCode,
        CancellationToken cancellationToken);
}

public sealed record TaskAccessResult(bool Exists, bool HasPermission, bool CanAccess);
