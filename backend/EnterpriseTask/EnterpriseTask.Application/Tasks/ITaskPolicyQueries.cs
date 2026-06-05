namespace EnterpriseTask.Application.Tasks;

public interface ITaskPolicyQueries
{
    Task<bool> HasPermissionAsync(Guid actorUserId, string permissionCode, CancellationToken cancellationToken);

    Task<bool> CanUseDepartmentAsync(Guid actorUserId, long? departmentId, CancellationToken cancellationToken);

    Task<TaskAccessResult> GetAccessAsync(
        Guid actorUserId,
        Guid taskId,
        string permissionCode,
        CancellationToken cancellationToken);
}

public sealed record TaskAccessResult(bool Exists, bool HasPermission, bool CanAccess);
