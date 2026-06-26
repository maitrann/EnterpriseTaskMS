using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
[Route("api/users")]
public sealed class UsersController(
    IUserQueries userQueries,
    IUserAdministrationCommands userCommands,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> Get(
        [FromQuery] UserListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await userQueries.GetUsersAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await userQueries.GetUserAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("{id:guid}/lock")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> Lock(Guid id, CancellationToken cancellationToken)
    {
        return ToActionResult(await userCommands.SetActiveAsync(
            currentUser.GetRequiredScope().UserId,
            id,
            isActive: false,
            cancellationToken));
    }

    [HttpPost("{id:guid}/unlock")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken cancellationToken)
    {
        return ToActionResult(await userCommands.SetActiveAsync(
            currentUser.GetRequiredScope().UserId,
            id,
            isActive: true,
            cancellationToken));
    }

    [HttpPost("{id:guid}/roles")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> AssignRole(
        Guid id,
        UserRoleAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await userCommands.AssignRoleAsync(id, request.RoleId, cancellationToken));
    }

    [HttpDelete("{id:guid}/roles/{roleId:long}")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> RemoveRole(Guid id, long roleId, CancellationToken cancellationToken)
    {
        return ToActionResult(await userCommands.RemoveRoleAsync(id, roleId, cancellationToken));
    }

    [HttpPost("{id:guid}/department-scopes")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> AssignDepartmentScope(
        Guid id,
        UserDepartmentScopeAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await userCommands.AssignDepartmentScopeAsync(
            id,
            request.DepartmentId,
            cancellationToken));
    }

    [HttpDelete("{id:guid}/department-scopes/{departmentId:long}")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> RemoveDepartmentScope(
        Guid id,
        long departmentId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await userCommands.RemoveDepartmentScopeAsync(id, departmentId, cancellationToken));
    }

    private IActionResult ToActionResult(UserAdministrationResult result)
    {
        return result switch
        {
            UserAdministrationResult.Success => NoContent(),
            UserAdministrationResult.NotFound => NotFound(),
            UserAdministrationResult.RoleNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Role not found",
                detail: "The requested role was not found."),
            UserAdministrationResult.DepartmentNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Department not found",
                detail: "The requested active department was not found."),
            UserAdministrationResult.SelfLockDenied => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "User lock conflict",
                detail: "Administrators cannot lock their own account."),
            UserAdministrationResult.LastAdminDenied => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "User administration conflict",
                detail: "The last active administrator cannot be locked or demoted."),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unexpected user administration result.")
        };
    }
}
