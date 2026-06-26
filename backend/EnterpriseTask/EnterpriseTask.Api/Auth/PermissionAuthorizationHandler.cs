using EnterpriseTask.Application.Common;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseTask.Api.Auth;

internal sealed class PermissionAuthorizationHandler(IPermissionChecker permissionChecker)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!ClaimsPrincipalScopeReader.TryGetUserId(context.User, out var actorUserId))
        {
            return;
        }

        if (await permissionChecker.HasPermissionAsync(actorUserId, requirement.PermissionCode, CancellationToken.None))
        {
            context.Succeed(requirement);
        }
    }
}
