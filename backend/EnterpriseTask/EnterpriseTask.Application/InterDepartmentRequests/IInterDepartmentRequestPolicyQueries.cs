using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.InterDepartmentRequests;

public interface IInterDepartmentRequestPolicyQueries
{
    Task<InterDepartmentRequestAccessResult> GetAccessAsync(
        UserScope scope,
        Guid requestId,
        CancellationToken cancellationToken);

    Task<bool> OwnerBelongsToTargetDepartmentAsync(
        Guid ownerId,
        long? targetDepartmentId,
        CancellationToken cancellationToken);
}

public sealed record InterDepartmentRequestAccessResult(
    bool Exists,
    bool CanAccess,
    bool CanCoordinate,
    Guid? RequesterUserId,
    long? TargetDepartmentId,
    string? Status);
