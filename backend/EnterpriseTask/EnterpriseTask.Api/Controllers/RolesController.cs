using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
[Route("api/roles")]
public sealed class RolesController(IRoleQueries roleQueries) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await roleQueries.GetRolesAsync(cancellationToken));
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetPermissions(CancellationToken cancellationToken)
    {
        return Ok(await roleQueries.GetPermissionsAsync(cancellationToken));
    }
}
