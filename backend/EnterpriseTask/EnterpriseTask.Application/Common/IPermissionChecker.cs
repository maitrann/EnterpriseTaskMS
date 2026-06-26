namespace EnterpriseTask.Application.Common;

public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(Guid actorUserId, string permissionCode, CancellationToken cancellationToken);
}
