using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.InterDepartmentRequests;

public sealed class AssignInterRequestOwnerHandler(
    IInterDepartmentRequestCommands requestCommands,
    IInterDepartmentRequestPolicyQueries requestPolicyQueries)
{
    public async Task<InterDepartmentRequestCommandResult> HandleAsync(
        UserScope scope,
        Guid requestId,
        AssignOwnerRequest request,
        CancellationToken cancellationToken)
    {
        var access = await requestPolicyQueries.GetAccessAsync(scope, requestId, cancellationToken);
        if (!access.Exists)
        {
            return InterDepartmentRequestCommandResult.NotFound;
        }

        if (!access.CanCoordinate)
        {
            return InterDepartmentRequestCommandResult.Forbidden;
        }

        if (access.Status is not ("received" or "processing" or "waiting-target"))
        {
            return InterDepartmentRequestCommandResult.InvalidState;
        }

        if (!await requestPolicyQueries.OwnerBelongsToTargetDepartmentAsync(
                request.OwnerId,
                access.TargetDepartmentId,
                cancellationToken))
        {
            return InterDepartmentRequestCommandResult.Forbidden;
        }

        return await requestCommands.AssignOwnerAsync(scope, requestId, request, cancellationToken);
    }
}
